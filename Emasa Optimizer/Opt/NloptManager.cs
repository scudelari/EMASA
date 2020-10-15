extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using NLoptNet;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.Opt
{
    public class NloptManager : BindableBase
    {
        [NotNull] private readonly SolveManager _owner;
        public NloptManager([NotNull] SolveManager inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            // Sets the defaults related to the stopping criteria
            SetDefaultStoppingCriteria();

            CollectionViewSource WpfNlOptEndCriteriaStatus_cvs = new CollectionViewSource() {Source = NlOptEndCriteriaStatus};
            WpfNlOptEndCriteriaStatus = WpfNlOptEndCriteriaStatus_cvs.View;
        }

        private NLoptSolver _nLOptMethod;
        public NLoptSolver NLOptMethod
        {
            get => _nLOptMethod;
            private set => SetProperty(ref _nLOptMethod, value);
        }

        private NLoptAlgorithm _nlOptSolverType = NLoptAlgorithm.LN_COBYLA;
        public NLoptAlgorithm NlOptSolverType
        {
            get => _nlOptSolverType;
            set
            {
                SetProperty(ref _nlOptSolverType, value);

                IsOn_PopulationSize = false;

                // Flags the other properties as updated
                RaisePropertyChanged("SolverNeedsPopulationSize");
                RaisePropertyChanged("WpfNlOptSolverTypeString");
            }
        }
        public Dictionary<NLoptAlgorithm, string> NlOptAlgorithmEnumDescriptions => ListDescriptionStaticHolder.ListDescSingleton.NlOptAlgorithmEnumDescriptions;
        public string WpfNlOptSolverTypeString => UseLagrangian ? $"Lagrangian [{NlOptAlgorithmEnumDescriptions[NlOptSolverType]}]" : $"{NlOptAlgorithmEnumDescriptions[NlOptSolverType]}";

        private bool _useLagrangian = true;
        public bool UseLagrangian
        {
            get => _useLagrangian;
            set => SetProperty(ref _useLagrangian, value);
        }

        #region TimeSpans
        private TimeSpan _nlOpt_TotalSolveTimeSpan = TimeSpan.Zero;
        public TimeSpan NlOpt_TotalSolveTimeSpan
        {
            get => _nlOpt_TotalSolveTimeSpan;
            set => SetProperty(ref _nlOpt_TotalSolveTimeSpan, value);
        }
        #endregion

        private NloptResult? _result = null;
        public NloptResult? Result
        {
            get => _result;
            set
            {
                SetProperty(ref _result, value);
                RaisePropertyChanged("WpfResultText");
                RaisePropertyChanged("WpfResultStyleTag");
            }
        }
        public string WpfResultText
        {
            get
            {
                if (!_result.HasValue) return "Not Started";

                switch (_result.Value)
                {
                    case NloptResult.FAILURE:
                        return "Generic failure code.";
                        break;

                    case NloptResult.INVALID_ARGS:
                        return "Invalid arguments (e.g. lower bounds are bigger than upper bounds, an unknown algorithm was specified, etc).";
                        break;

                    case NloptResult.OUT_OF_MEMORY:
                        return "Ran out of memory.";
                        break;

                    case NloptResult.ROUNDOFF_LIMITED:
                        return "Halted because roundoff errors limited progress. (In this case, the optimization still typically returns a useful result.).";
                        break;

                    case NloptResult.FORCED_STOP:
                        return "Halted by the user.";
                        break;

                    case NloptResult.SUCCESS:
                        return "Generic success return value.";
                        break;

                    case NloptResult.STOPVAL_REACHED:
                        return "Optimization stopped because the \"stop value on objective function\" was reached.";
                        break;

                    case NloptResult.FTOL_REACHED:
                        return "Optimization stopped because either the \"relative tolerance on function value\" or \"absolute tolerance on function value\" was reached.";
                        break;

                    case NloptResult.XTOL_REACHED:
                        return "Optimization stopped because either the \"relative tolerance on parameter value\" or \"one of the absolute tolerance on parameter value\" was reached.";
                        break;

                    case NloptResult.MAXEVAL_REACHED:
                        return "Optimization stopped because the maximum number of iterations was reached.";
                        break;

                    case NloptResult.MAXTIME_REACHED:
                        return "Optimization stopped because the maximum elapsed time was reached.";
                        break;

                    default:
                        return $"{_result.Value}";
                        break;
                }
            }
        }
        public string WpfResultStyleTag
        {
            get
            {
                if (!_result.HasValue) return "Gray";

                switch (_result.Value)
                {
                    case NloptResult.FAILURE:
                    case NloptResult.INVALID_ARGS:
                    case NloptResult.OUT_OF_MEMORY:
                        return "Red";


                    case NloptResult.ROUNDOFF_LIMITED:
                    case NloptResult.FORCED_STOP:
                        return "Yellow";

                    case NloptResult.SUCCESS:
                    case NloptResult.FTOL_REACHED:
                    case NloptResult.XTOL_REACHED:
                    case NloptResult.MAXEVAL_REACHED:
                    case NloptResult.MAXTIME_REACHED:
                    case NloptResult.STOPVAL_REACHED:
                        return "Green";

                    default:
                        return $"Gray";
                }
            }
        }
        
        private Exception _resultException;
        public Exception ResultException
        {
            get => _resultException;
            set
            {
                SetProperty(ref _resultException, value);
                RaisePropertyChanged("WpfResultExceptionMessage");
                RaisePropertyChanged("WpfResultExceptionVisibility");
            }
        }

        public Visibility WpfResultExceptionVisibility => ResultException == null ? Visibility.Hidden : Visibility.Visible;
        public string WpfResultExceptionMessage => _resultException?.Message ?? string.Empty;

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
        public CancellationTokenSource CancelSource
        {
            get => _cancelSource;
            set => _cancelSource = value ?? throw new InvalidOperationException($"{MethodBase.GetCurrentMethod()} does not accept null values.");
        }

        #region Stopping Criteria
        public void SetDefaultStoppingCriteria()
        {
            StopValueOnObjectiveFunction = 1e-6;
            IsOn_StopValueOnObjectiveFunction = true;

            RelativeToleranceOnFunctionValue = 0.001d;
            IsOn_RelativeToleranceOnFunctionValue = true;

            AbsoluteToleranceOnFunctionValue = 1e-6;
            IsOn_AbsoluteToleranceOnFunctionValue = false;

            RelativeToleranceOnParameterValue = 0.001d;
            IsOn_RelativeToleranceOnParameterValue = true;

            AbsoluteToleranceOnParameterValue.ReplaceItemsIfNew(DefaultParameterAbsoluteTolerance);
            IsOn_AbsoluteToleranceOnParameterValue = false;

            MaximumIterations = 10000;

            // 6 hours
            MaximumRunTime = 60 * 60 * 6;
            IsOn_MaximumRunTime = false;
        }

        private double _stopValueOnObjectiveFunction;
        /// <summary>
        /// Stop when an objective value of at least stopval is found: stop minimizing when an objective value ≤ stopval is found, or stop maximizing a value ≥ stopval is found. (Setting stopval to -HUGE_VAL for minimizing or +HUGE_VAL for maximizing disables this stopping criterion.)
        /// </summary>
        public double StopValueOnObjectiveFunction
        {
            get => _stopValueOnObjectiveFunction;
            set => SetProperty(ref _stopValueOnObjectiveFunction, value);
        }
        private bool _isOn_StopValueOnObjectiveFunction;
        public bool IsOn_StopValueOnObjectiveFunction
        {
            get => _isOn_StopValueOnObjectiveFunction;
            set => SetProperty(ref _isOn_StopValueOnObjectiveFunction, value);
        }

        private double _relativeToleranceOnFunctionValue;
        /// <summary>
        /// Set relative tolerance on function value: stop when an optimization step (or an estimate of the optimum) changes the objective function value by less than tol multiplied by the absolute value of the function value. (If there is any chance that your optimum function value is close to zero, you might want to set an absolute tolerance with nlopt_set_ftol_abs as well.) Criterion is disabled if tol is non-positive.
        /// </summary>
        public double RelativeToleranceOnFunctionValue
        {
            get => _relativeToleranceOnFunctionValue;
            set => SetProperty(ref _relativeToleranceOnFunctionValue, value);
        }
        private bool _isOnRelativeToleranceOnFunctionValue = true;
        public bool IsOn_RelativeToleranceOnFunctionValue
        {
            get => _isOnRelativeToleranceOnFunctionValue;
            set
            {
                SetProperty(ref _isOnRelativeToleranceOnFunctionValue, value);
                if (!_isOnRelativeToleranceOnFunctionValue) RelativeToleranceOnFunctionValue = -1d;
            }
        }

        private double _absoluteToleranceOnFunctionValue;
        /// <summary>
        /// Set absolute tolerance on function value: stop when an optimization step (or an estimate of the optimum) changes the function value by less than tol. Criterion is disabled if tol is non-positive.
        /// </summary>
        public double AbsoluteToleranceOnFunctionValue
        {
            get => _absoluteToleranceOnFunctionValue;
            set => SetProperty(ref _absoluteToleranceOnFunctionValue, value);
        }
        private bool _isOn_AbsoluteToleranceOnFunctionValue;
        public bool IsOn_AbsoluteToleranceOnFunctionValue
        {
            get => _isOn_AbsoluteToleranceOnFunctionValue;
            set => SetProperty(ref _isOn_AbsoluteToleranceOnFunctionValue, value);
        }

        private double _relativeToleranceOnParameterValue;
        /// <summary>
        /// Set relative tolerance on optimization parameters: stop when an optimization step (or an estimate of the optimum) causes a relative change the parameters x by less than tol
        /// </summary>
        public double RelativeToleranceOnParameterValue
        {
            get => _relativeToleranceOnParameterValue;
            set => SetProperty(ref _relativeToleranceOnParameterValue, value);
        }
        private bool _isOn_RelativeToleranceOnParameterValue;
        public bool IsOn_RelativeToleranceOnParameterValue
        {
            get => _isOn_RelativeToleranceOnParameterValue;
            set => SetProperty(ref _isOn_RelativeToleranceOnParameterValue, value);
        }

        private FastObservableCollection<AbsoluteToleranceOnParameterValue_NameValuePair> _absoluteToleranceOnParameterValue = new FastObservableCollection<AbsoluteToleranceOnParameterValue_NameValuePair>();
        /// <summary>
        /// Set absolute tolerances on optimization parameters. tol is a pointer to an array of length n (the dimension from nlopt_create) giving the tolerances: stop when an optimization step (or an estimate of the optimum) changes every parameter x[i] by less than tol[i]. (Note that nlopt_set_xtol_abs makes a copy of the tol array, so subsequent changes to the caller's tol have no effect on opt.) In nlopt_get_xtol_abs, tol must be an array of length n, which upon successful return contains a copy of the current tolerances. For convenience, the nlopt_set_xtol_abs1 may be used to set the absolute tolerances in all n optimization parameters to the same value. Criterion is disabled if tol is non-positive.
        /// </summary>
        public FastObservableCollection<AbsoluteToleranceOnParameterValue_NameValuePair> AbsoluteToleranceOnParameterValue
        {
            get => _absoluteToleranceOnParameterValue;
        }
        private bool _isOnAbsoluteToleranceOnParameterValue;
        public bool IsOn_AbsoluteToleranceOnParameterValue
        {
            get => _isOnAbsoluteToleranceOnParameterValue;
            set => SetProperty(ref _isOnAbsoluteToleranceOnParameterValue, value);
        }

        private int _maximumIterations;
        public int MaximumIterations
        {
            get => _maximumIterations;
            set => SetProperty(ref _maximumIterations, value);
        }
        private bool _isOn_MaximumIterations;
        public bool IsOn_MaximumIterations
        {
            get => _isOn_MaximumIterations;
            set => SetProperty(ref _isOn_MaximumIterations, value);
        }

        private double _maximumRunTime;
        /// <summary>
        /// Stop when the optimization time (in seconds) exceeds maxtime. (This is not a strict maximum: the time may exceed maxtime slightly, depending upon the algorithm and on how slow your function evaluation is.) Criterion is disabled if maxtime is non-positive.
        /// </summary>
        public double MaximumRunTime
        {
            get => _maximumRunTime;
            set => SetProperty(ref _maximumRunTime, value);
        }
        private bool _isOn_MaximumRunTime;
        public bool IsOn_MaximumRunTime
        {
            get => _isOn_MaximumRunTime;
            set => SetProperty(ref _isOn_MaximumRunTime, value);
        }

        public List<AbsoluteToleranceOnParameterValue_NameValuePair> DefaultParameterAbsoluteTolerance
        {
            get
            {
                List<AbsoluteToleranceOnParameterValue_NameValuePair> toRet = new List<AbsoluteToleranceOnParameterValue_NameValuePair>();
                foreach (Input_ParamDefBase inputParam in _owner.Gh_Alg.InputDefs.OrderBy(a => a.IndexInDoubleArray))
                {
                    switch (inputParam)
                    {
                        case Double_Input_ParamDef double_Input_ParamDef:
                            toRet.Add(new AbsoluteToleranceOnParameterValue_NameValuePair(double_Input_ParamDef.Name, -1d));
                            break;

                        case Integer_Input_ParamDef integer_Input_ParamDef:
                            toRet.Add(new AbsoluteToleranceOnParameterValue_NameValuePair(integer_Input_ParamDef.Name, -1d));
                            break;

                        case Point_Input_ParamDef point_Input_ParamDef:
                            toRet.Add(new AbsoluteToleranceOnParameterValue_NameValuePair(point_Input_ParamDef.Name + " - X", -1d));
                            toRet.Add(new AbsoluteToleranceOnParameterValue_NameValuePair(point_Input_ParamDef.Name + " - Y", -1d));
                            toRet.Add(new AbsoluteToleranceOnParameterValue_NameValuePair(point_Input_ParamDef.Name + " - Z", -1d));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(inputParam));
                    }
                }

                return toRet;
            }
        }

        // List with the end status of the Stop Criteria
        public FastObservableCollection<NlOptStopCriteriaStatus> NlOptEndCriteriaStatus = new FastObservableCollection<NlOptStopCriteriaStatus>();
        public ICollectionView WpfNlOptEndCriteriaStatus { get; }
        #endregion

        #region Population Size Options
        public Visibility SolverNeedsPopulationSize
        {
            get
            {
                switch (NlOptSolverType)
                {
                    case NLoptAlgorithm.G_MLSL:
                    case NLoptAlgorithm.G_MLSL_LDS:
                    case NLoptAlgorithm.GN_CRS2_LM:
                    case NLoptAlgorithm.GN_ISRES:
                        return Visibility.Visible;

                    default:
                        return Visibility.Collapsed;
                }
            }
        }
        private int _populationSize;
        public int PopulationSize
        {
            get => _populationSize;
            set => SetProperty(ref _populationSize, value);
        }
        private bool _isOn_PopulationSize;
        public bool IsOn_PopulationSize
        {
            get => _isOn_PopulationSize;
            set => SetProperty(ref _isOn_PopulationSize, value);
        }
        #endregion

        #region Finite Differences Gradient Options
        private int _finiteDiffPointsPerPartialDerivative = 2;
        public int FiniteDiff_PointsPerPartialDerivative
        {
            get => _finiteDiffPointsPerPartialDerivative;
            set => SetProperty(ref _finiteDiffPointsPerPartialDerivative, value);
        }
        private int _finiteDiff_PointsPerPartialDerivativeCenter = 0;
        public int FiniteDiff_PointsPerPartialDerivativeCenter
        {
            get => _finiteDiff_PointsPerPartialDerivativeCenter;
            set => SetProperty(ref _finiteDiff_PointsPerPartialDerivativeCenter, value);
        }
        #endregion

        #region Objective Function Options
        private ObjectiveFunctionSumTypeEnum _objectiveFunctionSumType = ObjectiveFunctionSumTypeEnum.Squares;
        public ObjectiveFunctionSumTypeEnum ObjectiveFunctionSumType
        {
            get => _objectiveFunctionSumType;
            set => SetProperty(ref _objectiveFunctionSumType, value);
        }
        #endregion

        #region Actions!
        public void SolveSelectedProblem(bool inUpdateInterface = false)
        {
            // Clears the stop status report
            NlOptEndCriteriaStatus.Clear();

            // Resets the cancellation token
            CancelSource = new CancellationTokenSource();

            // Resets
            Result = null;
            ResultException = null;

            #region Validates the solver limits
            if (IsOn_MaximumIterations && MaximumIterations <= 0) throw new InvalidOperationException("When active, the number of maximum iterations must be larger than 0.");
            if (IsOn_MaximumRunTime && MaximumRunTime <= 0) throw new InvalidOperationException("When active, the maximum runtime must be larger than 0.");
            if (IsOn_StopValueOnObjectiveFunction && StopValueOnObjectiveFunction <= 0) throw new InvalidOperationException("When active, the stop value of the objective function must be larger than 0.");
            if (IsOn_RelativeToleranceOnFunctionValue && RelativeToleranceOnFunctionValue <= 0) throw new InvalidOperationException("When active, the relative tolerance on the function value must be larger than 0.");
            if (IsOn_AbsoluteToleranceOnFunctionValue && AbsoluteToleranceOnFunctionValue <= 0) throw new InvalidOperationException("When active, the absolute tolerance on the function value must be larger than 0.");
            if (IsOn_RelativeToleranceOnParameterValue && RelativeToleranceOnParameterValue <= 0) throw new InvalidOperationException("When active, the relative tolerance on the input value must be larger than 0.");
            if (IsOn_AbsoluteToleranceOnParameterValue && AbsoluteToleranceOnParameterValue.All(a => a.ParameterTolerance <= 0)) throw new InvalidOperationException("When active, the at least one absolute tolerance on the input value must be larger than 0.");

            if (SolverNeedsPopulationSize == Visibility.Visible && IsOn_PopulationSize && PopulationSize <= 0) throw new InvalidOperationException("When active, the population size must be larger than 0.");

            if (!IsOn_MaximumIterations &&
                !IsOn_MaximumRunTime &&
                !IsOn_StopValueOnObjectiveFunction &&
                !IsOn_RelativeToleranceOnFunctionValue &&
                !IsOn_AbsoluteToleranceOnFunctionValue &&
                !IsOn_RelativeToleranceOnParameterValue &&
                !IsOn_AbsoluteToleranceOnParameterValue) throw new InvalidOperationException("At least one stop control must be set.");
            #endregion

            // Checks if we have equality of inequality constraints
            bool hasEqualConstraint = false;
            bool hasUnequalConstraint = false;

            foreach (ProblemQuantity constraintQuantity in _owner.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>())
            {
                // Regardless of the constraint type, it will always point to the same function
                switch (constraintQuantity.ConstraintObjective)
                {
                    case Quantity_ConstraintObjectiveEnum.EqualTo:
                        hasEqualConstraint = true;
                        break;

                    case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                    case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                        hasUnequalConstraint = true;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            using (NLOptMethod = new NLoptSolver(NlOptSolverType, 
                (uint)_owner.WpfSelectedProblem.NumberOfVariables, 
                UseLagrangian, hasEqualConstraint, hasUnequalConstraint ))
            {
                #region Setting the stop limits
                if (IsOn_MaximumIterations) NLOptMethod.SetStopOnMaximumIteration(MaximumIterations);
                if (IsOn_RelativeToleranceOnFunctionValue) NLOptMethod.SetRelativeToleranceOnFunctionValue(RelativeToleranceOnFunctionValue);
                if (IsOn_AbsoluteToleranceOnFunctionValue) NLOptMethod.SetAbsoluteToleranceOnFunctionValue(AbsoluteToleranceOnFunctionValue);
                if (IsOn_RelativeToleranceOnParameterValue) NLOptMethod.SetRelativeToleranceOnOptimizationParameter(RelativeToleranceOnParameterValue);
                if (IsOn_AbsoluteToleranceOnParameterValue)
                {
                    double[] tols = (from a in AbsoluteToleranceOnParameterValue select a.ParameterTolerance).ToArray();
                    NLOptMethod.SetAbsoluteToleranceOnOptimizationParameter(tols);
                }
                if (IsOn_MaximumRunTime) NLOptMethod.SetStopOnMaximumTime(MaximumRunTime);
                if (IsOn_StopValueOnObjectiveFunction) NLOptMethod.SetStopOnFunctionValue(StopValueOnObjectiveFunction);
                if (SolverNeedsPopulationSize == Visibility.Visible && IsOn_PopulationSize) NLOptMethod.SetPopulationSize((uint)PopulationSize);
                #endregion

                #region Setting up the problem itself
                // Boundaries of the input variables
                NLOptMethod.SetLowerBounds(_owner.Gh_Alg.InputDefs_LowerBounds);
                NLOptMethod.SetUpperBounds(_owner.Gh_Alg.InputDefs_UpperBounds);

                // The objective function is given by the wrapper
                NLOptMethod.SetMinObjective(_owner.WpfSelectedProblem.NlOptEntryPoint_ObjectiveFunction);
                #endregion

                // Sets the constraints as given by the quantity selections
                foreach (ProblemQuantity constraintQuantity in _owner.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>())
                {
                    // Regardless of the constraint type, it will always point to the same function
                    switch (constraintQuantity.ConstraintObjective)
                    {
                        case Quantity_ConstraintObjectiveEnum.EqualTo:
                            NLOptMethod.AddEqualZeroConstraint(constraintQuantity.NlOptEntryPoint_ConstraintFunction, constraintQuantity.ConstraintTolerance);
                            break;

                        case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                        case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                            NLOptMethod.AddLessOrEqualZeroConstraint(constraintQuantity.NlOptEntryPoint_ConstraintFunction, constraintQuantity.ConstraintTolerance);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                
                // Gets the start value array
                double[] startValues = _owner.Gh_Alg.GetInputStartPosition();

                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    // Runs the optimization!
                    Result = NLOptMethod.Optimize(startValues, out double? bestEval);
                }
                catch (Exception e)
                {
                    Result = NloptResult.FAILURE;
                    ResultException = e;
                    throw new Exception($"NlOpt failed. Return code: {Result}", e);
                }
                finally
                {
                    sw.Stop();
                    NlOpt_TotalSolveTimeSpan = sw.Elapsed;

                    // These depend on the solution points
                    int countSolPoints = _owner.WpfFunctionPoints.OfType<SolutionPoint>().Count();
                    SolutionPoint lastPoint = countSolPoints > 0 ? _owner.WpfFunctionPoints.OfType<SolutionPoint>().ElementAt(countSolPoints - 1) : null;
                    SolutionPoint beforeLastPoint = countSolPoints > 1 ? _owner.WpfFunctionPoints.OfType<SolutionPoint>().ElementAt(countSolPoints - 2) : null;

                    if (IsOn_StopValueOnObjectiveFunction)
                    {
                        if (countSolPoints == 0)
                        {
                            NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Stop Value", StopValueOnObjectiveFunction, double.NaN));
                        }
                        else
                        {
                            NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Stop Value", 
                                StopValueOnObjectiveFunction, 
                                Math.Abs(lastPoint.ObjectiveFunctionEval)));
                        }
                    }

                    if (IsOn_RelativeToleranceOnFunctionValue)
                    {
                        if (countSolPoints < 2)
                        {
                            if (countSolPoints == 1) NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Relative Tolerance", Math.Abs(RelativeToleranceOnFunctionValue * lastPoint.ObjectiveFunctionEval), double.NaN));
                            else NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Relative Tolerance", double.NaN, double.NaN));
                        }
                        else
                        {
                            NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Relative Tolerance", 
                                Math.Abs(RelativeToleranceOnFunctionValue * lastPoint.ObjectiveFunctionEval),
                                Math.Abs(lastPoint.ObjectiveFunctionEval - beforeLastPoint.ObjectiveFunctionEval)));
                        }
                    }

                    if (IsOn_AbsoluteToleranceOnFunctionValue)
                    {
                        if (countSolPoints < 2)
                        {
                            NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Absolute Tolerance", AbsoluteToleranceOnFunctionValue, double.NaN));
                        }
                        else
                        {
                            NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("F Absolute Tolerance",
                                AbsoluteToleranceOnFunctionValue,
                                Math.Abs(lastPoint.ObjectiveFunctionEval - beforeLastPoint.ObjectiveFunctionEval)));
                        }
                    }

                    if (IsOn_RelativeToleranceOnParameterValue)
                    {
                        if (countSolPoints < 2)
                        {
                            if (countSolPoints == 1) NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("X Relative Tolerance", Math.Abs(RelativeToleranceOnParameterValue * lastPoint.ObjectiveFunctionEval), double.NaN));
                            else NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("X Relative Tolerance", double.NaN, double.NaN));
                        }
                        else
                        {
                            foreach (KeyValuePair<Input_ParamDefBase, object> lastPointGhInput_Value in lastPoint.GhInput_Values)
                            {
                                switch (lastPointGhInput_Value.Key)
                                {
                                    case Double_Input_ParamDef doubleInputParamDef:
                                        NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus($"X Rel: {lastPointGhInput_Value.Key.Name}",
                                            Math.Abs(RelativeToleranceOnParameterValue * (double)lastPointGhInput_Value.Value),
                                            Math.Abs((double)lastPointGhInput_Value.Value - (double)beforeLastPoint.GhInput_Values[lastPointGhInput_Value.Key])));
                                        break;

                                    case Point_Input_ParamDef pointInputParamDef:
                                        NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus($"X Rel: {lastPointGhInput_Value.Key.Name} - X",
                                            Math.Abs(RelativeToleranceOnParameterValue * ((Point3d)lastPointGhInput_Value.Value).X),
                                            Math.Abs(((Point3d)lastPointGhInput_Value.Value).X - ((Point3d)beforeLastPoint.GhInput_Values[lastPointGhInput_Value.Key]).X)));

                                        NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus($"X Rel: {lastPointGhInput_Value.Key.Name} - Y",
                                            Math.Abs(RelativeToleranceOnParameterValue * ((Point3d)lastPointGhInput_Value.Value).Y),
                                            Math.Abs(((Point3d)lastPointGhInput_Value.Value).Y - ((Point3d)beforeLastPoint.GhInput_Values[lastPointGhInput_Value.Key]).Y)));

                                        NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus($"X Rel: {lastPointGhInput_Value.Key.Name} - Z",
                                            Math.Abs(RelativeToleranceOnParameterValue * ((Point3d)lastPointGhInput_Value.Value).Z),
                                            Math.Abs(((Point3d)lastPointGhInput_Value.Value).Z - ((Point3d)beforeLastPoint.GhInput_Values[lastPointGhInput_Value.Key]).Z)));
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(lastPointGhInput_Value.Key));
                                }
                            }
                        }
                    }

                    if (IsOn_AbsoluteToleranceOnParameterValue)
                    {
                        if (countSolPoints < 2)
                        {
                            if (countSolPoints == 1)
                            {
                                foreach (AbsoluteToleranceOnParameterValue_NameValuePair item in AbsoluteToleranceOnParameterValue)
                                {
                                    NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus($"X Abs: {item.ParameterName}", item.ParameterTolerance, double.NaN)); 
                                }
                            }
                            else NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("X Absolute Tolerance", double.NaN, double.NaN));
                        }
                        else
                        {
                            for (int i = 0; i < lastPoint.InputValuesAsDoubleArray.Length; i++)
                            {
                                AbsoluteToleranceOnParameterValue_NameValuePair current = AbsoluteToleranceOnParameterValue[i];

                                NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus($"X Abs: {current.ParameterName}",
                                    current.ParameterTolerance,
                                    Math.Abs(lastPoint.InputValuesAsDoubleArray[i] - beforeLastPoint.InputValuesAsDoubleArray[i])));
                            }
                        }
                    }

                    if (IsOn_MaximumIterations)
                    {
                        NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("Max. Iterations", (double)MaximumIterations, (double)_owner.WpfFunctionPoints.OfType<SolutionPoint>().Count()));
                    }
                    
                    if (IsOn_MaximumRunTime)
                    {
                        NlOptEndCriteriaStatus.Add(new NlOptStopCriteriaStatus("Max. Time", MaximumRunTime, NlOpt_TotalSolveTimeSpan.TotalSeconds));
                    }
                }
            }

            //Resets the NlOptMethod to null
            NLOptMethod = null;
        }
        #endregion
    }

    public enum StartPositionTypeEnum
    {
        Given,
        CenterOfRange,
        Random,
        PercentRandomFromCenter,
        PercentRandomFromGiven
    }

    public class AbsoluteToleranceOnParameterValue_NameValuePair : BindableBase
    {
        public AbsoluteToleranceOnParameterValue_NameValuePair(string inParameterName, double inParameterTolerance)
        {
            _parameterName = inParameterName;
            _parameterTolerance = inParameterTolerance;
        }

        private string _parameterName;
        public string ParameterName
        {
            get => _parameterName;
            set => SetProperty(ref _parameterName, value);
        }
        private double _parameterTolerance;
        public double ParameterTolerance
        {
            get => _parameterTolerance;
            set => SetProperty(ref _parameterTolerance, value);
        }
    }

    public class NlOptStopCriteriaStatus
    {
        public NlOptStopCriteriaStatus([NotNull] string inName, double inLimit, double inCurrent)
        {
            Name = inName ?? throw new ArgumentNullException(nameof(inName));
            Limit = inLimit;
            Current = inCurrent;
        }

        public string Name { get; private set; }
        public double Limit { get; private set; }
        public double Current { get; private set; }
        public bool IsStop
        {
            get
            {
                try
                {
                    if (Name == "Max. Time" || Name == "Max. Iterations") return Current >= Limit;
                    else return Current < Limit;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public enum ObjectiveFunctionSumTypeEnum
    {
        Simple,
        Squares,
    }
}
