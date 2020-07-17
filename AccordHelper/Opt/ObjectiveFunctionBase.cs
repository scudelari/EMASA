extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Accord.Genetic;
using Accord.Math.Differentiation;
using Accord.Math.Optimization;
using Accord.Math.Random;
using AccordHelper.Annotations;
using AccordHelper.FEA;
using AccordHelper.Opt.ParamDefinitions;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace AccordHelper.Opt
{
    public abstract class ObjectiveFunctionBase : NonlinearObjectiveFunction, IFitnessFunction, INotifyPropertyChanged
    {
        public ProblemBase Problem { get; set; }
        public FeModelBase FeModel
        {
            get => Problem.FeModel;
        }
        protected ObjectiveFunctionBase()
        {
            // Initializes the lists
            InputDefs = new FastObservableCollection<Input_ParamDefBase>();
            IntermediateDefs = new FastObservableCollection<Output_ParamDefBase>();
            FinalDefs = new FastObservableCollection<Output_ParamDefBase>();

            InitializeVariables();

            // Sets the optimization data - cannot override so must be done in constructor
            NumberOfVariables = GetNumberOfVariables();
            Function = Function_Wrapper;
            Gradient = Gradient_Override;
        }

        protected abstract void InitializeVariables();

        /// <summary>
        /// Do not add directly to this list!
        /// </summary>
        public FastObservableCollection<Input_ParamDefBase> InputDefs { get; }
        public void AddParameterToInputs(Input_ParamDefBase inParam)
        {
            int currentTotal = InputDefs.Sum(a => a.VarCount);
            InputDefs.Add(inParam);
            inParam.IndexInDoubleArray = currentTotal;
        }

        public FastObservableCollection<Output_ParamDefBase> IntermediateDefs { get; }
        public FastObservableCollection<Output_ParamDefBase> FinalDefs { get; }

        /// <summary>
        /// Will get the start vector.
        /// If a parameter has a start position set, the start position will be used.
        /// Otherwise, the starting position will be calculated from the boundaries using the given logic.
        /// If a parameter does not have both boundaries, the logic will be that:
        /// 1- If only the Min boundary exists, the initial will be 10% higher than the minimum boundary.
        /// 2- If only the Max boundary exists, the initial will be 10% lower than the minimum boundary.
        /// 2- If neither exist, the initial will be 0.
        /// </summary>
        /// <param name="inStartPositionType">The type of the range for the variables with both ranges set.</param>
        /// <returns>Initial position vector.</returns>
        public double[] GetStartPosition(StartPositionType inStartPositionType)
        {
            double?[] starts = GetStartPositionAssignedList();
            double[] lowers = Input_ParamDefBase.GetLowerBounds(InputDefs);
            double[] uppers = Input_ParamDefBase.GetUpperBounds(InputDefs);

            if (lowers.Length != uppers.Length && lowers.Length != starts.Length) throw new Exception("Somehow, the count of lower and upper bounds, and of the existing start positions are different.");

            double[] output = new double[starts.Length];

            Random rnd = new Random();

            for (int i = 0; i < starts.Length; i++)
            {
                if (starts[i].HasValue)
                {
                    output[i] = starts[i].Value;
                    continue; ;
                }
                switch (inStartPositionType)
                {
                    case StartPositionType.CenterOfRange:
                        output[i] = (lowers[i] + uppers[i]) / 2d;
                        break;

                    case StartPositionType.Random:
                        output[i] = rnd.NextDouble() * (uppers[i] - lowers[i]) + lowers[i];
                        break;

                    case StartPositionType.TenPercentRandomFromCenter:
                        double center = starts[i] ?? (lowers[i] + uppers[i]) / 2d;
                        double rndUpperBound = Math.Min(uppers[i], center * 1.1d);
                        double rndLowerBound = Math.Max(lowers[i], center * 0.9d);

                        output[i] = rnd.NextDouble() * (rndUpperBound - rndLowerBound) + rndLowerBound;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(inStartPositionType), inStartPositionType, null);
                }
            }

            return output;
        }
        private double?[] GetStartPositionAssignedList()
        {
            List<double?> toRet = new List<double?>();

            foreach (Input_ParamDefBase inputDef in InputDefs)
            {
                switch (inputDef)
                {
                    case Double_Input_ParamDef doubleInputParamDef:
                        if (inputDef.Start == null) toRet.Add(null);
                        else toRet.Add((double)inputDef.Start);
                        break;

                    case Integer_Input_ParamDef integerInputParamDef:
                        if (inputDef.Start == null) toRet.Add(null);
                        else toRet.Add((double)(int)inputDef.Start);
                        break;

                    case Point_Input_ParamDef pointInputParamDef:
                        if (inputDef.Start == null)
                        {
                            toRet.Add(null);
                            toRet.Add(null);
                            toRet.Add(null);
                        }
                        else
                        {
                            Point3d pnt = (Point3d)inputDef.Start;
                            toRet.Add(pnt.X);
                            toRet.Add(pnt.Y);
                            toRet.Add(pnt.Z);
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputDef));
                }
            }

            return toRet.ToArray();
        }
        public InputParamChromosome GetStartGrasshopperChromosome(StartPositionType inStartPositionType)
        {
            // Try both - Normal (Gaussian) or Uniform distribution for the Random class
            return new InputParamChromosome(this,
                new ZigguratUniformOneGenerator(),
                new ZigguratUniformGenerator(0.75d, 1.25d), // mutationMultiplierGenerator that will be applied to the current value
                new ZigguratUniformGenerator(0.05d, 0.1d), // mutationAdditionGenerator - Sum of the RANGE size
                GetStartPosition(inStartPositionType));
        }

        private List<NonlinearConstraint> _otherConstraints = null;
        public List<NonlinearConstraint> OtherConstraints
        {
            get => _otherConstraints;
            set => _otherConstraints = value;
        }
        public List<NonlinearConstraint> GetAllConstraints()
        {
            // Sets the constraints based on the InputValues
            List<NonlinearConstraint> tmpConstraints = new List<NonlinearConstraint>();

            if (OtherConstraints != null && OtherConstraints.Count > 0) tmpConstraints.AddRange(OtherConstraints);

            foreach (Input_ParamDefBase ghInput in InputDefs)
            {
                tmpConstraints.AddRange(ghInput.ConstraintDefinitions(this));
            }
            if (tmpConstraints.Count != 0) return tmpConstraints;
            
            return null;
        }
        public double[] UpperBounds
        {
            get
            {
                List<double> upper = new List<double>();
                foreach (Input_ParamDefBase input in InputDefs)
                {
                    switch (input)
                    {
                        case Integer_Input_ParamDef ip:
                            upper.Add((double)ip.SearchRangeTyped.Range.Max);
                            break;
                        case Double_Input_ParamDef dp:
                            upper.Add(dp.SearchRangeTyped.Range.Max);
                            break;

                        case Point_Input_ParamDef pp:
                            upper.Add(pp.SearchRangeTyped.RangeX.Max);
                            upper.Add(pp.SearchRangeTyped.RangeY.Max);
                            upper.Add(pp.SearchRangeTyped.RangeZ.Max);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (upper.Count > 0) return upper.ToArray();

                return null;
            }
        }
        public double[] LowerBounds
        {
            get
            {
                List<double> lower = new List<double>();
                foreach (Input_ParamDefBase input in InputDefs)
                {
                    switch (input)
                    {
                        case Integer_Input_ParamDef ip:
                            lower.Add((double)ip.SearchRangeTyped.Range.Min);
                            break;
                        case Double_Input_ParamDef dp:
                            lower.Add(dp.SearchRangeTyped.Range.Min);
                            break;

                        case Point_Input_ParamDef pp:
                            lower.Add(pp.SearchRangeTyped.RangeX.Min);
                            lower.Add(pp.SearchRangeTyped.RangeY.Min);
                            lower.Add(pp.SearchRangeTyped.RangeZ.Min);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (lower.Count > 0) return lower.ToArray();
                
                return null;
            }
        }


        private int _functionHitCount;
        public int FunctionHitCount
        {
            get => _functionHitCount;
            set
            {
                _functionHitCount = value;
                OnPropertyChanged();
            }
        }

        private int _gradientHitCount;
        public int GradientHitCount
        {
            get => _gradientHitCount;
            set
            {
                _gradientHitCount = value;
                OnPropertyChanged();
            }
        }

        private int? _autoNumber = null;
        public virtual int GetNumberOfVariables()
        {
            if (_autoNumber.HasValue) return _autoNumber.Value;
            _autoNumber = InputDefs.Sum(a => a.VarCount);
            return _autoNumber.Value;
        }

        private PossibleSolution _currentSolution;
        public PossibleSolution CurrentSolution
        {
            get => _currentSolution;
            set
            {
                _currentSolution = value;
                OnPropertyChanged();
            }
        }

        private double _currentEval = double.MaxValue;
        public double CurrentEval
        {
            get => _currentEval;
            set
            {
                _currentEval = value;
                OnPropertyChanged();
            }
        }

        private double Function_Wrapper(double[] inValues)
        {
            FunctionHitCount++;

            // Creates a new possible solution
            CurrentSolution = new PossibleSolution(inValues, this, FunctionOrGradientEval.Function);

            // Adds it to the problems' possible solutions
            Problem.PossibleSolutions.Add(CurrentSolution);

            double fEval = Function_Override(inValues);
            
            if (Problem.SolverType != SolverType.Genetic)
                if (fEval <= Problem.TargetResidual) throw new SolverSuccessException(fEval);

            // Stores the value and signals the interface
            CurrentEval = fEval;

            return fEval;
        }
        private double Function_WrapperForGradient(double[] inValues)
        {
            GradientHitCount++;

            // Creates a new possible solution
            CurrentSolution = new PossibleSolution(inValues, this, FunctionOrGradientEval.Gradient);

            // Adds it to the problems' possible solutions
            Problem.PossibleSolutions.Add(CurrentSolution);

            try
            {
                return Function_Override(inValues);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Problem in the Function_WrapperForGradient.", e);
            }
        }
        public abstract double Function_Override(double[] inValues);

        /// <summary>
        /// The default implementation is to make an approximation using Finite Differences
        /// </summary>
        /// <param name="inValues">The points at which the function's Gradient is required.</param>
        /// <returns>The Gradient at the given point.</returns>
        public virtual double[] Gradient_Override(double[] inValues)
        {
            FiniteDifferences finDiff = new FiniteDifferences(NumberOfVariables, Function_WrapperForGradient);
            return finDiff.Gradient(inValues);
        }

        public double Evaluate(IChromosome inChromosome)
        {
            // The type of the chromosome shall be DoubleArray
            if (!(inChromosome is InputParamChromosome bldArray)) throw new ArgumentException("The Evaluate function received something that is not a DoubleArrayChromosome.");

            double[] inValues = bldArray.Value;

            // Should we check for the boundaries?

            /* Note: The for the Chromossomes, the fitness function is different than the Nonlinear Objective Function.
             The return value must be higher than 0.
             The higher the value, the better the chromossome.
             */

            return 1d / Function_Wrapper(bldArray.Value);
        }

        #region Helpers
        public static double SquareRootOfSquares(params double[] inValues)
        {
            double sumOfSquares = SumOfSquares(inValues);
            return Math.Sqrt(sumOfSquares);
        }
        public static double SumOfSquares(params double[] inValues)
        {
            double sumOfSquares = 0d;

            for (int i = 0; i < inValues.Length; i++)
            {
                sumOfSquares += inValues[i] * inValues[i];
            }

            return sumOfSquares;
        }
        #endregion

        #region Deprecated
        /// <summary>
        /// Gets the index in the *linearized* list of doubles that come from InputDefs of the given parameter name.
        /// </summary>
        /// <param name="inParamName">Parameter name to find the index.</param>
        /// <param name="inPositionOffset">An offset, useful for Points (X=0, Y=1, Z=2)</param>
        /// <returns></returns>
        public int GetInputIndexByName(string inParamName, int inPositionOffset = 0)
        {
            int varCount = 0;
            foreach (Input_ParamDefBase requiredGhInput in InputDefs)
            {
                switch (requiredGhInput)
                {
                    case Integer_Input_ParamDef _:
                        if (requiredGhInput.Name == inParamName) return varCount;
                        varCount += requiredGhInput.VarCount;
                        break;
                    case Double_Input_ParamDef _:
                        if (requiredGhInput.Name == inParamName) return varCount;
                        varCount += requiredGhInput.VarCount;
                        break;
                    case Point_Input_ParamDef _:
                        if (requiredGhInput.Name == inParamName) return varCount + inPositionOffset;
                        varCount += 3;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            throw new Exception($"Could not find parameter called {inParamName} in the list of InputDefs.");
        }
        /// <summary>
        /// Gets the index in the *linearized* list of doubles that come from InputDefs of the given parameter.
        /// </summary>
        /// <param name="inParamName">Parameter name to find the index.</param>
        /// <param name="inPositionOffset">An offset, useful for Points (X=0, Y=1, Z=2)</param>
        /// <returns></returns>
        public int GetInputIndexByParam(Input_ParamDefBase inParam, int inPositionOffset = 0)
        {
            int varCount = 0;
            foreach (Input_ParamDefBase requiredGhInput in InputDefs)
            {
                switch (requiredGhInput)
                {
                    case Integer_Input_ParamDef _:
                        if (requiredGhInput == inParam) return varCount;
                        varCount += requiredGhInput.VarCount;
                        break;
                    case Double_Input_ParamDef _:
                        if (requiredGhInput == inParam) return varCount;
                        varCount += requiredGhInput.VarCount;
                        break;
                    case Point_Input_ParamDef _:
                        if (requiredGhInput == inParam) return varCount + inPositionOffset;
                        varCount += 3;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            throw new Exception($"Could not find parameter {inParam} in the list of InputDefs.");
        } 
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string inPropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(inPropertyName));
        }
    }

    public enum StartPositionType
    {
        CenterOfRange,
        Random,
        TenPercentRandomFromCenter
    }
}
