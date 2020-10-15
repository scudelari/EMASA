using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ProbQuantity;
using Prism.Mvvm;
using MathNet.Numerics;
using MathNet.Numerics.Differentiation;
using MathNet.Numerics.LinearAlgebra;

namespace Emasa_Optimizer.ProblemDefs
{
    public class GeneralProblem : BindableBase
    {
        [NotNull] protected readonly SolveManager _owner;
        public GeneralProblem([NotNull] SolveManager inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));
        }

        public virtual string WpfFriendlyName => "FEA";
        public virtual string WpfProblemDescription => "Optimization based on the results of Finite Element Analysis";
        public virtual bool IsFea => true;
        public virtual bool TargetsOpenGhAlgorithm => true;
        public virtual bool OverridesGeneralProblem => false;

        public virtual int NumberOfVariables => _owner.Gh_Alg.InputDefs_VarCount;

        #region Current SolutionPoint Calculation Buffer
        private SolutionPointCalcTypeEnum _solPointCalcType;
        public SolutionPointCalcTypeEnum SolPointCalcType
        {
            get => _solPointCalcType;
            set => SetProperty(ref _solPointCalcType, value);
        }
        
        private SolutionPoint _currentCalc_SolutionPoint;
        public SolutionPoint CurrentCalc_SolutionPoint
        {
            get => _currentCalc_SolutionPoint;
            set
            {
                _currentCalc_SolutionPoint = value;
                RaisePropertyChanged();
                // Clears the Gradient definition
                CurrentCalc_GradientPoint = null;
            }
        }

        private SolutionPoint _currentCalc_GradientPoint;
        public SolutionPoint CurrentCalc_GradientPoint
        {
            get => _currentCalc_GradientPoint;
            set
            {
                _currentCalc_GradientPoint = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        private double CalculateSolutionPoint(double[] inPointVars)
        {
            // Starts the stopwatch
            Stopwatch sw = Stopwatch.StartNew();

            // Creates a new solution point
            SolutionPoint local_solPoint = new SolutionPoint(_owner, inPointVars, SolPointCalcType);

            switch (SolPointCalcType)
            {
                // What is the point we are currently calculating
                case SolutionPointCalcTypeEnum.ObjectiveFunction:
                    CurrentCalc_SolutionPoint = local_solPoint;
                    break;

                case SolutionPointCalcTypeEnum.Gradient:
                default:
                    CurrentCalc_GradientPoint = local_solPoint;
                    break;
            }

            // Changes the status of the SolutionPoint
            local_solPoint.Status = SolutionPointStatusEnum.Grasshopper_Updating;

            // Updates the grasshopper geometry of the SolutionPoint
            _owner.Gh_Alg.UpdateGrasshopperGeometry(local_solPoint);

            // If it is a Fe Problem, runs the model
            if (_owner.FeOptions.FeSolverType_Selected != FeSolverTypeEnum.NotFeProblem)
            {
                Stopwatch sw_Fe = Stopwatch.StartNew();

                // Changes the status of the solution point
                local_solPoint.Status = SolutionPointStatusEnum.FiniteElement_Running;

                // Generates the Abstraction of the Finite Element Model
                local_solPoint.FeModel = new FeModel(local_solPoint);

                // Runs the FeModel and acquires the results, putting them inside the FeModel parameter of the SolutionPoint class
                _owner.FeSolver.RunAnalysisAndCollectResults(local_solPoint.FeModel);

                sw_Fe.Stop();
                local_solPoint.FeInputCalcOutputTimeSpan = sw_Fe.Elapsed;
            }

            // We have all the raw outputs, so we can initialize the Quantity Value's Outputs
            local_solPoint.InitializeProblemQuantityOutputs();

            // Calculates the final value of the objective function based on the selected quantity values
            local_solPoint.CalculateObjectiveFunctionResult();
            
            // Outputs and Constraints have already been initialized, so they should be readily available.

            // Stops the watch and saves the timespan used to handle the timespan
            sw.Stop();
            local_solPoint.CalculateTimeSpan = sw.Elapsed;

            return local_solPoint.ObjectiveFunctionEval;
        }

        public double NlOptEntryPoint_ObjectiveFunction(double[] inPointVars, double[] inGradient = null)
        {
            // IMPORTANT: Errors are reported by the Unhandled Exceptions that "escape" this function

            // There was an error in the last evaluation - breaks
            if (inPointVars.Contains(double.NaN))
            {
                _owner.NlOptManager.NLOptMethod.ForceStop();
                throw new Exception("NlOpt Solver gave Not A Number (NaN) as input to the objective function.");
            }
            // Should we cancel the execution - Forces the NlOpt to Stop
            if (_owner.NlOptManager.CancelSource.IsCancellationRequested)
            {
                _owner.NlOptManager.NLOptMethod.ForceStop();
                throw new Exception("User stopped the solver.");
            }

            Stopwatch sw_total = Stopwatch.StartNew();

            // Creates and calculates a new solution point -> putting it in the given reference location
            SolPointCalcType = SolutionPointCalcTypeEnum.ObjectiveFunction;
            CalculateSolutionPoint(inPointVars);

            // Stores it in the list of calculated solutions
            _owner.AddSolutionPoint(CurrentCalc_SolutionPoint);

            // Gradient Necessary ?
            if (inGradient != null)
            {
                Stopwatch gradSw = Stopwatch.StartNew();

                CurrentCalc_SolutionPoint.Status = SolutionPointStatusEnum.Gradients_Running;

                // Initializes the gradient variables
                CurrentCalc_SolutionPoint.HasGradient = true;

                // Creates a new FiniteDifferences Manager
                NumericalDerivative nd = new NumericalDerivative(_owner.NlOptManager.FiniteDiff_PointsPerPartialDerivative, _owner.NlOptManager.FiniteDiff_PointsPerPartialDerivativeCenter);

                // Fills the gradient information 
                SolPointCalcType = SolutionPointCalcTypeEnum.Gradient;
                for (int i = 0; i < CurrentCalc_SolutionPoint.ObjectiveFunctionGradient.Length; i++)
                {
                    // Note: it will call the  CalculateSolutionPoint, which will assign the solution point calculated to the CurrentCalc_GradientPoint parameter
                    CurrentCalc_SolutionPoint.ObjectiveFunctionGradient[i] = nd.EvaluatePartialDerivative(CalculateSolutionPoint, CurrentCalc_SolutionPoint.InputValuesAsDoubleArray.Clone() as double[], i, 1, CurrentCalc_SolutionPoint.ObjectiveFunctionEval);

                    // Saves the solution point that was calculated to get the partial derivative
                    // Used when the constraint function is called
                    CurrentCalc_SolutionPoint.GradientSolutionPoints[i] = CurrentCalc_GradientPoint;
                }

                // Saves the copy to the return parameter
                double[] gradArray = CurrentCalc_SolutionPoint.ObjectiveFunctionGradient.Clone() as double[];
                gradArray.CopyTo(inGradient, 0);

                gradSw.Stop();
                CurrentCalc_SolutionPoint.TotalGradientTimeSpan = gradSw.Elapsed;
            }

            sw_total.Stop();
            CurrentCalc_SolutionPoint.TotalIterationTimeSpan = sw_total.Elapsed;

            CurrentCalc_SolutionPoint.Status = SolutionPointStatusEnum.Ended_Success;

            return CurrentCalc_SolutionPoint.ObjectiveFunctionEval;
        }
    }
}
