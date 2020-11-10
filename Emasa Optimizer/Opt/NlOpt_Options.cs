using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.WpfResources;
using NLoptNet;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt
{
    public class NlOpt_Options : BindableBase
    {
        public NlOpt_Options()
        {
            SetDefaultStoppingCriteria();
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
        public Dictionary<NLoptAlgorithm, string> NlOptAlgorithmEnumDescriptions => ListDescSH.I.NlOptAlgorithmEnumDescriptions;
        public string WpfNlOptSolverTypeString => UseLagrangian ? $"Lagrangian [{NlOptAlgorithmEnumDescriptions[NlOptSolverType]}]" : $"{NlOptAlgorithmEnumDescriptions[NlOptSolverType]}";

        // Use lagrangian will probably be deprecated as an input option - it will be used whenever the user asks for constraints
        private bool _useLagrangian = true;
        public bool UseLagrangian
        {
            get => _useLagrangian;
            set => SetProperty(ref _useLagrangian, value);
        }


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

        #region Stopping Criteria
        public void SetDefaultStoppingCriteria()
        {
            StopValueOnObjectiveFunction = 1e-6;
            IsOn_StopValueOnObjectiveFunction = false;

            RelativeToleranceOnFunctionValue = 0.001d;
            IsOn_RelativeToleranceOnFunctionValue = false;

            AbsoluteToleranceOnFunctionValue = 1e-6;
            IsOn_AbsoluteToleranceOnFunctionValue = false;

            RelativeToleranceOnParameterValue = 0.001d;
            IsOn_RelativeToleranceOnParameterValue = false;

            AbsoluteToleranceOnParameterValue.ReplaceItemsIfNew(DefaultParameterAbsoluteTolerance);
            IsOn_AbsoluteToleranceOnParameterValue = false;

            IsOn_MaximumIterations = true;
            MaximumIterations = 10000;

            // 1 hours
            MaximumRunTime = 60 * 60;
            IsOn_MaximumRunTime = false;
        }

        private double _stopValueOnObjectiveFunction = 1e-6;
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

        private double _relativeToleranceOnFunctionValue = 1e-3;
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
                //if (!_isOnRelativeToleranceOnFunctionValue) RelativeToleranceOnFunctionValue = -1d;
            }
        }

        private double _absoluteToleranceOnFunctionValue = 1e-6;
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

        private double _relativeToleranceOnParameterValue = 1e-3;
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

        private int _maximumIterations = 1000;
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

        private double _maximumRunTime = 60d*60d;
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
                foreach (Input_ParamDefBase inputParam in AppSS.I.Gh_Alg.InputDefs.OrderBy(a => a.IndexInDoubleArray))
                {
                    switch (inputParam)
                    {
                        case Double_Input_ParamDef double_Input_ParamDef:
                            toRet.Add(new AbsoluteToleranceOnParameterValue_NameValuePair(double_Input_ParamDef.Name, -1d));
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

        #region Objective Function Options
        public Dictionary<ObjectiveFunctionSumTypeEnum, Tuple<string, string>> ObjectiveFunctionSumTypeEnumDescriptions => ListDescSH.I.ObjectiveFunctionSumTypeEnumNameAndDescription;
        private ObjectiveFunctionSumTypeEnum _objectiveFunctionSumType = ObjectiveFunctionSumTypeEnum.Squares;
        public ObjectiveFunctionSumTypeEnum ObjectiveFunctionSumType
        {
            get => _objectiveFunctionSumType;
            set => SetProperty(ref _objectiveFunctionSumType, value);
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
}
