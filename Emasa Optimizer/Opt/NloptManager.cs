using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.WpfResources;
using NLoptNet;
using Prism.Mvvm;

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
            }
        }
        public Dictionary<NLoptAlgorithm, string> NlOptAlgorithmEnumDescriptions => ListDescriptionStaticHolder.ListDescSingleton.NlOptAlgorithmEnumDescriptions;

        private NloptResult? _result = null;
        public NloptResult? Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
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

        #region Actions!
        public void SolveSelectedProblem(bool inUpdateInterface = false)
        {
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

            using (NLOptMethod = new NLoptSolver(
                NlOptSolverType,
                (uint)_owner.WpfSelectedProblem.NumberOfVariables,
                IsOn_RelativeToleranceOnFunctionValue ? RelativeToleranceOnFunctionValue : -1d,
                IsOn_MaximumIterations ? MaximumIterations : -1
            ))
            {

                #region Setting the rest of the stop limits
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

                // The objective function is given by th wrapper
                NLOptMethod.SetMinObjective(_owner.WpfSelectedProblem.Function_NLOptFunctionWrapper);
                #endregion

                // Gets the start value array
                double[] startValues = _owner.Gh_Alg.GetInputStartPosition();

                // Runs the optimization!
                Result = NLOptMethod.Optimize(startValues, out double? bestEval);
            }
            // Resets the NlOptMethod to null
            NLOptMethod = null;
        }
        #endregion

        #region Optimization Variables
        public FastObservableCollection<FeResultOptimizeOptionsClass> OptimizationVariableList = new FastObservableCollection<FeResultOptimizeOptionsClass>();
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

    public class FeResultOptimizeOptionsClass : BindableBase
    {
        public FeResultOptimizeOptionsClass([NotNull] FeResultClassification inResult)
        {
            _result = inResult ?? throw new ArgumentNullException(nameof(inResult));

            // Sets the defaults in accordance with the ResultClass Type
            switch (_result.ResultFamily)
            {
                case FeResultFamilyEnum.Nodal_Reaction:

                    break;

                case FeResultFamilyEnum.Nodal_Displacement:
                    break;

                case FeResultFamilyEnum.SectionNode_Stress:
                    break;

                case FeResultFamilyEnum.SectionNode_Strain:
                    break;

                case FeResultFamilyEnum.ElementNodal_BendingStrain:
                    break;

                case FeResultFamilyEnum.ElementNodal_Force:
                    break;

                case FeResultFamilyEnum.ElementNodal_Strain:
                    break;

                case FeResultFamilyEnum.ElementNodal_Stress:
                    break;

                case FeResultFamilyEnum.Others:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private FeResultClassification _result;
        public FeResultClassification Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        private bool _optimize_AggregateType_Max = true;
        public bool Optimize_AggregateType_Max
        {
            get => _optimize_AggregateType_Max;
            set => SetProperty(ref _optimize_AggregateType_Max, value);
        }
        private bool _optimize_AggregateType_Min = false;
        public bool Optimize_AggregateType_Min
        {
            get => _optimize_AggregateType_Min;
            set => SetProperty(ref _optimize_AggregateType_Min, value);
        }
        private bool _optimize_AggregateType_Mean = false;
        public bool Optimize_AggregateType_Mean
        {
            get => _optimize_AggregateType_Mean;
            set => SetProperty(ref _optimize_AggregateType_Mean, value);
        }
        private bool _optimize_AggregateType_StandardDeviation = false;
        public bool Optimize_AggregateType_StandardDeviation
        {
            get => _optimize_AggregateType_StandardDeviation;
            set => SetProperty(ref _optimize_AggregateType_StandardDeviation, value);
        }
        
        private bool _optimize_Objective_Maximize = false;
        public bool Optimize_Objective_Maximize
        {
            get => _optimize_Objective_Maximize;
            set => SetProperty(ref _optimize_Objective_Maximize, value);
        }
        private bool _optimize_Objective_Minimize = true;
        public bool Optimize_Objective_Minimize
        {
            get => _optimize_Objective_Minimize;
            set => SetProperty(ref _optimize_Objective_Minimize, value);
        }
        private bool _optimize_Objective_ToValue = false;
        public bool Optimize_Objective_ToValue
        {
            get => _optimize_Objective_ToValue;
            set => SetProperty(ref _optimize_Objective_ToValue, value);
        }
        private double _optimize_Objective_Value = 0d;
        public double Optimize_Objective_Value
        {
            get => _optimize_Objective_Value;
            set => SetProperty(ref _optimize_Objective_Value, value);
        }


        private bool _optimize_ExpectedScaleRange_IsSet;
        public bool Optimize_ExpectedScaleRange_IsSet
        {
            get => _optimize_ExpectedScaleRange_IsSet;
            set => SetProperty(ref _optimize_ExpectedScaleRange_IsSet, value);
        }
        private double _optimize_ExpectedScale_Min;
        public double Optimize_ExpectedScale_Min
        {
            get => _optimize_ExpectedScale_Min;
            set => SetProperty(ref _optimize_ExpectedScale_Min, value);
        }
        private double _optimize_ExpectedScale_Max;
        public double Optimize_ExpectedScale_Max
        {
            get => _optimize_ExpectedScale_Max;
            set => SetProperty(ref _optimize_ExpectedScale_Max, value);
        }
        private DoubleValueRange _optimize_ExpectedScale_Range;
        public DoubleValueRange Optimize_ExpectedScale_Range => _optimize_ExpectedScale_Range ?? (_optimize_ExpectedScale_Range = new DoubleValueRange(_optimize_ExpectedScale_Min, _optimize_ExpectedScale_Max));

        private bool _optimize_LimitPenalty_IsSet;
        public bool Optimize_LimitPenalty_IsSet
        {
            get => _optimize_LimitPenalty_IsSet;
            set => SetProperty(ref _optimize_LimitPenalty_IsSet, value);
        }
        private double _optimize_LimitPenalty_Value;
        public double Optimize_LimitPenalty_Value
        {
            get => _optimize_LimitPenalty_Value;
            set => SetProperty(ref _optimize_LimitPenalty_Value, value);
        }
        private double _optimize_LimitPenalty_Min;
        public double Optimize_LimitPenalty_Min
        {
            get => _optimize_LimitPenalty_Min;
            set => SetProperty(ref _optimize_LimitPenalty_Min, value);
        }
        private double _optimize_LimitPenalty_Max;
        public double Optimize_LimitPenalty_Max
        {
            get => _optimize_LimitPenalty_Max;
            set => SetProperty(ref _optimize_LimitPenalty_Max, value);
        }
        private DoubleValueRange _optimize_LimitPenalty_Range;
        public DoubleValueRange Optimize_LimitPenalty_Range => _optimize_LimitPenalty_Range ?? (_optimize_LimitPenalty_Range = new DoubleValueRange(_optimize_LimitPenalty_Min, _optimize_LimitPenalty_Max));

        public string WpfFriendlyName
        {
            get
            {
                string objective = string.Empty;
                if (Optimize_Objective_Maximize) objective = "Maximize";
                else if (Optimize_Objective_Minimize) objective = "Minimize";
                else if (Optimize_Objective_ToValue) objective = $"To: {Optimize_Objective_Value:F3}";

                string aggregate = string.Empty;
                if (Optimize_AggregateType_Max) aggregate = "Max";
                else if(Optimize_AggregateType_Min) aggregate = "Min";
                else if(Optimize_AggregateType_Mean) aggregate = "Mean";
                else if(Optimize_AggregateType_StandardDeviation) aggregate = "StDev";

                return $"{Result.WpfFriendlyName} - {objective} - {aggregate}";
            }
        }
    }

}
