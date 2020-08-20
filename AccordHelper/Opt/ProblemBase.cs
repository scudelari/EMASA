using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Accord;
using Accord.Collections;
using Accord.Genetic;
using Accord.IO;
using Accord.Math.Optimization;
using AccordHelper.Annotations;
using AccordHelper.FEA;
using AccordHelper.FEA.Items;
using AccordHelper.FEA.Loads;
using AccordHelper.Opt.ParamDefinitions;
using BaseWPFLibrary.Others;
using NLoptNet;
using Prism.Commands;
using Prism.Mvvm;
using RhinoInterfaceLibrary;

namespace AccordHelper.Opt
{
    [Serializable]
    public abstract class ProblemBase : BindableBase
    {
        private FeaSoftwareEnum? _feaType = null;
        public FeaSoftwareEnum? FeaType
        {
            get => _feaType;
            set
            {
                if (!value.HasValue) throw new InvalidOperationException($"{MethodBase.GetCurrentMethod()} can't be set to null.");

                string modelDir = Path.Combine(RhinoStaticMethods.GH_Auto_DataFolder(RhinoModel.RM.GrasshopperFullFileName), "FeModel");
                switch (value)
                {
                    case FeaSoftwareEnum.Ansys:
                        _feModel = new AnsysModel(modelDir, this);
                        _feModel.ModelName = ProblemFriendlyName;
                        break;

                    case FeaSoftwareEnum.Sap2000:
                        _feModel = new S2KModel(modelDir, this);
                        break;

                    case FeaSoftwareEnum.NoFea:
                        _feModel = null;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                SetProperty(ref _feaType, value);
            }
        }
        private FeModelBase _feModel = null;
        public FeModelBase FeModel
        {
            get => _feModel;
        }

        private ObjectiveFunctionBase _objectiveFunction;
        public ObjectiveFunctionBase ObjectiveFunction
        {
            get => _objectiveFunction;
        }

        // NLOpt Solvers
        private NLoptSolver _nLOptMethod;
        public NLoptSolver NLOptMethod
        {
            get => _nLOptMethod;
            set => SetProperty(ref _nLOptMethod, value);
        }

        private SolverStatus _status;
        public SolverStatus Status
        {
            get => _status;
            private set
            {
                SetProperty(ref _status, value);

                if (_status == SolverStatus.NotStarted) CanChangeInputs = true;
                else CanChangeInputs = false;

                // Gives a default message
                switch (_status)
                {
                    case SolverStatus.NotStarted:
                        StatusTextMessage = "Solver not Started.";
                        break;

                    case SolverStatus.Running:
                        StatusTextMessage = "Solver is Running.";
                        break;

                    case SolverStatus.NoProgress:
                        StatusTextMessage = "Solver can't proceed further.";
                        break;

                    case SolverStatus.MaxEvaluations:
                        StatusTextMessage = "Solver reached maximum evaluations.";
                        break;

                    case SolverStatus.Finished:
                        StatusTextMessage = "Solver finished successfully (target residual reached).";
                        break;

                    case SolverStatus.Cancelled:
                        StatusTextMessage = "Solver has been cancelled.";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RaisePropertyChanged("StatusText");
            }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case SolverStatus.NotStarted:
                        return "Not Started";

                    case SolverStatus.Running:
                        return "Running";

                    case SolverStatus.NoProgress:
                        return "No Progress";

                    case SolverStatus.MaxEvaluations:
                        return "Maximum Evaluations";

                    case SolverStatus.Finished:
                        return "Finished";

                    case SolverStatus.Cancelled:
                        return "Cancelled";

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        private string _statusTextMessage = "Solver Not Started";

        public string StatusTextMessage
        {
            get => _statusTextMessage;
            private set => SetProperty(ref _statusTextMessage, value);
        }
        private bool _canChangeInputs = true;

        public bool CanChangeInputs
        {
            get => _canChangeInputs;
            set => SetProperty(ref _canChangeInputs, value);
        }

        private NLoptAlgorithm _solverType = NLoptAlgorithm.LN_COBYLA;
        public NLoptAlgorithm SolverType
        {
            get => _solverType;
            set
            {
                SetProperty(ref _solverType, value);

                IsOn_PopulationSize = false;

                // Flags the other properties as updated
                RaisePropertyChanged("SolverNeedsPopulationSize");
            }
        }

        private StartPositionType _startPositionType = StartPositionType.Random;
        public StartPositionType StartPositionType
        {
            get => _startPositionType;
            set => SetProperty(ref _startPositionType, value);
        }

        protected ProblemBase(ObjectiveFunctionBase inObjectiveFunction)
        {
            Status = SolverStatus.NotStarted;

            // Gives a reference of this problem to the objective function
            inObjectiveFunction.Problem = this;
            // Stores the function given
            _objectiveFunction = inObjectiveFunction;


            // Generates the default views of list parameters
            InputDefs_ViewItems = CollectionViewSource.GetDefaultView(_objectiveFunction.InputDefs);
            IntermediateDefs_ViewItems = CollectionViewSource.GetDefaultView(_objectiveFunction.IntermediateDefs);
            FinalDefs_ViewItems = CollectionViewSource.GetDefaultView(_objectiveFunction.FinalDefs);

            CollectionViewSource ghLineDefs_ViewSource = new CollectionViewSource() {Source = _objectiveFunction.IntermediateDefs };
            GrasshopperLineListDefs_ViewItems = ghLineDefs_ViewSource.View;
            GrasshopperLineListDefs_ViewItems.Filter = GrasshopperLineListDefs_ViewItems_Filter;

            CollectionViewSource ghPointDefs_ViewSource = new CollectionViewSource() {Source = _objectiveFunction.IntermediateDefs};
            GrasshopperPointListDefs_ViewItems = ghPointDefs_ViewSource.View;
            GrasshopperPointListDefs_ViewItems.Filter = GrasshopperPointListDefs_ViewItems_Filter;

            CollectionViewSource functionSolutions_ViewSource = new CollectionViewSource {Source = _possibleSolutions};
            FunctionSolutions_ViewItems = functionSolutions_ViewSource.View;
            FunctionSolutions_ViewItems.Filter = FunctionSolutions_ViewItems_Filter;

            CollectionViewSource gradientSolutions_ViewSource = new CollectionViewSource {Source = _possibleSolutions};
            GradientSolutions_ViewItems = gradientSolutions_ViewSource.View;
            GradientSolutions_ViewItems.Filter = GradientSolutions_ViewItems_Filter;

            AllPossibleSolutions_ViewItems = CollectionViewSource.GetDefaultView(_possibleSolutions);

            CollectionViewSource allPossibleSections_ViewSource = new CollectionViewSource {Source = FeSectionPipe.GetAllSections()};
            allPossibleSections_ViewSource.SortDescriptions.Add(new SortDescription("OuterDiameter", ListSortDirection.Ascending));
            allPossibleSections_ViewSource.SortDescriptions.Add(new SortDescription("Thickness", ListSortDirection.Ascending));
            AllPossibleSections_ViewItems = allPossibleSections_ViewSource.View;

            // Fills the parameters
            AbsoluteToleranceOnParameterValue.ReplaceItemsIfNew(DefaultParameterAbsoluteTolerance);

            SetDefaultStoppingCriteria();

            SetDefaultScreenShots();
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
        #endregion

        #region Additional Parameters
        public Visibility SolverNeedsPopulationSize
        {
            get
            {
                switch (SolverType)
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

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
        public CancellationTokenSource CancelSource
        {
            get => _cancelSource;
            set
            {
                if (value == null) throw new InvalidOperationException($"{MethodBase.GetCurrentMethod()} does not accept null values.");
                else
                {
                    _cancelSource = value;
                }
            }
        }

        private double _totalSolveSeconds = 0d;
        public double TotalSolveSeconds
        {
            get => _totalSolveSeconds;
            set { SetProperty(ref _totalSolveSeconds, value); }
        }

        public void CleanUp_Solver()
        {
            // Cleans all the important state variables
            PossibleSolutions.Clear();
            ObjectiveFunction.Reset();

            // Resets the Solver Stop Options
            SetDefaultStoppingCriteria();

            if (NLOptMethod != null)
            {
                NLOptMethod.Dispose();
                NLOptMethod = null;
            }

            CancelSource = new CancellationTokenSource();

            Status = SolverStatus.NotStarted;
        }
        public void SetNLOptSolverManager()
        {
            // Validates the input
            if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("The selected solver requires boundaries.");

            // Validates the solver limits
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

            // Initializes the solver and also sets SOME of the stop limits
            NLOptMethod = new NLoptSolver(SolverType, (uint)ObjectiveFunction.NumberOfVariables,
                IsOn_RelativeToleranceOnFunctionValue ? RelativeToleranceOnFunctionValue : -1d,
                IsOn_MaximumIterations ? MaximumIterations : -1
                );

            // Setting the rest of the stop limits
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

            NLOptMethod.SetLowerBounds(ObjectiveFunction.LowerBounds);
            NLOptMethod.SetUpperBounds(ObjectiveFunction.UpperBounds);
            NLOptMethod.SetMinObjective(ObjectiveFunction.Function_NLOptFunctionWrapper);

            // Sets the FEA type
            FeaType = SolverOptions_SelectedFeaSoftware;
        }
        public void Solve()
        {
            Stopwatch sw = Stopwatch.StartNew();

            if (Status == SolverStatus.Finished) throw new Exception("The solver has already finished.");
            if (Status == SolverStatus.NoProgress) throw new Exception("The solver can't progress the solution further.");
            if (Status == SolverStatus.MaxEvaluations) throw new Exception("The solver reached the maximum number of evaluations.");

            // Starts the solver process
            if (FeModel != null)
            {
                FeModel.InitializeSoftware();
            }

            #region NLOpt Optimizations

                if (_nLOptMethod == null) throw new Exception("You did not start the solver. Please use the SetNLOptSolverManager function for this.");
                
                // This is the first time we are running this solver
                double[] vector = Status == SolverStatus.NotStarted ? ObjectiveFunction.GetStartPosition(StartPositionType) : BestSolutionSoFar.InputValuesAsDouble;

                NloptResult result = NloptResult.FAILURE;
                try
                {
                    double? finalScore;
                    result = NLOptMethod.Optimize(vector, out finalScore);

                    switch (result)
                    {
                        case NloptResult.FAILURE:
                            Status = SolverStatus.Failed;
                            StatusTextMessage = "Generic failure code.";
                            break;

                        case NloptResult.INVALID_ARGS:
                            Status = SolverStatus.Failed;
                            StatusTextMessage = "Invalid arguments (e.g. lower bounds are bigger than upper bounds, an unknown algorithm was specified, etc).";
                            break;

                        case NloptResult.OUT_OF_MEMORY:
                            Status = SolverStatus.Failed;
                            StatusTextMessage = "Ran out of memory.";
                            break;

                        case NloptResult.ROUNDOFF_LIMITED:
                            Status = SolverStatus.Failed;
                            StatusTextMessage = "Halted because roundoff errors limited progress. (In this case, the optimization still typically returns a useful result.).";
                            break;

                        case NloptResult.FORCED_STOP:
                            Status = SolverStatus.Cancelled;
                            StatusTextMessage = "Halted by the user.";
                            break;

                        case NloptResult.SUCCESS:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Generic success return value.";
                            break;

                        case NloptResult.STOPVAL_REACHED:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Optimization stopped because the \"stop value on objective function\" was reached.";
                            break;

                        case NloptResult.FTOL_REACHED:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Optimization stopped because either the \"relative tolerance on function value\" or \"absolute tolerance on function value\" was reached.";
                            break;

                        case NloptResult.XTOL_REACHED:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Optimization stopped because either the \"relative tolerance on parameter value\" or \"one of the absolute tolerance on parameter value\" was reached.";
                            break;

                        case NloptResult.MAXEVAL_REACHED:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Optimization stopped because the maximum number of iterations was reached.";
                            break;

                        case NloptResult.MAXTIME_REACHED:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Optimization stopped because the maximum elapsed time was reached.";
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (SolverEndException e)
                {
                    // We sent the message! Interprets the message that was received
                    switch (e.FinishStatus)
                    {
                        case SolverStatus.NoProgress:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "No Progress - The solver cannot iterate faster than the required minimum change limit.";
                            break;

                        case SolverStatus.MaxEvaluations:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                            break;

                        case SolverStatus.Finished:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Converged - The solver reached the desired residual value.";
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"The SolverEndException got an unexpected result!");
                    }
                }
            #endregion

            // Terminates the solver process
            if (FeModel != null)
            {
                FeModel.CloseApplication();
            }

            sw.Stop();
            TotalSolveSeconds = sw.Elapsed.TotalSeconds;
        }

        public void CleanUp_SectionEvaluation()
        {
            PossibleSolutions.Clear();
            ObjectiveFunction.Reset();

            CancelSource = new CancellationTokenSource();
        }
        public void EvaluateSections()
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Starts the solver process
            if (FeModel != null)
            {
                FeModel.InitializeSoftware(inIsSectionSelection: true);
            }

            // Makes a combination of the sections we need to evaluate
            HashSet<SectionCombination> combs = new HashSet<SectionCombination>();
            foreach (LineList_Output_ParamDef currentParam in GrasshopperLineListDefs_ViewItems.Cast<LineList_Output_ParamDef>()) 
            {
                foreach (FeSection currentParamSection in currentParam.SelectedSections)
                {
                    // Initializes the combination
                    SectionCombination comb = new SectionCombination();

                    // Adds the section of the first parameter
                    comb.Combination.Add(currentParam, currentParamSection);

                    foreach (LineList_Output_ParamDef otherParam in GrasshopperLineListDefs_ViewItems.Cast<LineList_Output_ParamDef>().Where(a => a!= currentParam))
                    {
                        foreach (FeSection otherParamSection in otherParam.SelectedSections)
                        {
                            // Adds the section of the other parameters. 
                            // the underlying Combination is a SortedList by the LineParam thus it will be consistent
                            comb.Combination.Add(otherParam, otherParamSection);
                        }
                    }

                    // Adds it to the HashSet if it is unique (hopefully)
                    combs.Add(comb);
                }
            }

            foreach (SectionCombination sectionCombination in combs)
            {
                // Configures the selected section for each line parameter
                foreach (KeyValuePair<LineList_Output_ParamDef, FeSection> keyValuePair in sectionCombination.Combination)
                {
                    keyValuePair.Key.SolveSection = keyValuePair.Value;
                }

                // Sets the start vector for the Rhino Input params
                double[] vector = ObjectiveFunction.GetStartPosition(StartPositionType);

                // Runs the evaluation. The underlying ObjectiveFunction will consume the defined sections accordingly
                double evalValue = ObjectiveFunction.Function_NLOptFunctionWrapper(vector);
            }

            // Terminates the solver process
            if (FeModel != null)
            {
                FeModel.CloseApplication();
            }

            sw.Stop();
            TotalSolveSeconds = sw.Elapsed.TotalSeconds;
        }

        #region Problem Variable Definition
        public ICollectionView InputDefs_ViewItems { get; set; }
        public ICollectionView IntermediateDefs_ViewItems { get; set; }
        public ICollectionView FinalDefs_ViewItems { get; set; }
        #endregion

        public ICollectionView FunctionSolutions_ViewItems { get; set; }
        private bool FunctionSolutions_ViewItems_Filter(object inObj)
        {
            if (!(inObj is PossibleSolution inSol)) throw new InvalidCastException($"The FunctionSolutions_ViewItems contains something other than a PossibleSolution object.");
            return inSol.EvalType == FunctionOrGradientEval.Function;
        }

        public ICollectionView GradientSolutions_ViewItems { get; set; }
        private bool GradientSolutions_ViewItems_Filter(object inObj)
        {
            if (!(inObj is PossibleSolution inSol)) throw new InvalidCastException($"The FunctionSolutions_ViewItems contains something other than a PossibleSolution object.");
            return inSol.EvalType == FunctionOrGradientEval.Gradient;
        }

        public ICollectionView AllPossibleSolutions_ViewItems { get; set; }
        protected FastObservableCollection<PossibleSolution> _possibleSolutions = new FastObservableCollection<PossibleSolution>();
        public FastObservableCollection<PossibleSolution> PossibleSolutions
        {
            get => _possibleSolutions;
        }
        private PossibleSolution _selectedPossibleSolution;
        public PossibleSolution SelectedPossibleSolution
        {
            get => _selectedPossibleSolution;
            set => SetProperty(ref _selectedPossibleSolution, value);
        }
        public PossibleSolution CurrentSolverSolution
        {
            get => ObjectiveFunction.CurrentSolution;
        }
        public PossibleSolution BestSolutionSoFar
        {
            get => PossibleSolutions.Where(a => a.EvalType == FunctionOrGradientEval.Function).OrderBy(a => a.Eval).First();
        }

        #region Client UI Helpers

        public List<AbsoluteToleranceOnParameterValue_NameValuePair> DefaultParameterAbsoluteTolerance
        {
            get
            {
                List<AbsoluteToleranceOnParameterValue_NameValuePair> toRet = new List<AbsoluteToleranceOnParameterValue_NameValuePair>();
                foreach (Input_ParamDefBase inputParam in ObjectiveFunction.InputDefs.OrderBy(a => a.IndexInDoubleArray))
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

        public bool SolvesCurrentGHFile { get; set; }
        public virtual string ProblemFriendlyName
        {
            get => this.GetType().FullName;
        }
        public virtual string ClassName
        {
            get => this.GetType().Name;
        }


        public Dictionary<NLoptAlgorithm, string> SolverTypeListWithCaptions { get; } = new Dictionary<NLoptAlgorithm, string>()
            {
                {NLoptAlgorithm.LN_COBYLA, "Cobyla [LN]"},
                {NLoptAlgorithm.LN_BOBYQA, "Bobyqa [LN]"},
                {NLoptAlgorithm.GN_DIRECT, "Dividing Rectangles [GN]"},
                {NLoptAlgorithm.GN_DIRECT_L, "Dividing Rectangles - Locally Biased [GN]"},
                {NLoptAlgorithm.GN_DIRECT_L_RAND, "Dividing Rectangles - Locally Biased With Some Randomization [GN]"},
                {NLoptAlgorithm.GN_CRS2_LM, "Controlled Random Search With Local Mutation [GN]"},
                {NLoptAlgorithm.GD_STOGO, "StoGo [GD]"},
                {NLoptAlgorithm.GD_STOGO_RAND, "StoGo - Randomized [GD]"},
                {NLoptAlgorithm.GN_ISRES, "Improved Stochastic Ranking Evolution Strategy [GN]"},
                {NLoptAlgorithm.GN_ESCH, "ESCH (evolutionary algorithm) [GN]"},
            };
        public Dictionary<StartPositionType, string> StartPositionTypeListWithCaptions { get; } = new Dictionary<StartPositionType, string>()
            {
                {StartPositionType.TenPercentRandomFromCenter, "10% from Center"},
                {StartPositionType.CenterOfRange, "Center of Input"},
                {StartPositionType.Random, "Random"},
            };
        private StartPositionType _solverOptions_SelectedStartPositionType = StartPositionType.CenterOfRange;
        public StartPositionType SolverOptions_SelectedStartPositionType
        {
            get => _solverOptions_SelectedStartPositionType;
            set => SetProperty(ref _solverOptions_SelectedStartPositionType, value);
        }

        private Dictionary<FeaSoftwareEnum, string> _feaSoftwareListWithCaptions = new Dictionary<FeaSoftwareEnum, string>();
        public Dictionary<FeaSoftwareEnum, string> FeaSoftwareListWithCaptions
        {
            get => _feaSoftwareListWithCaptions;
        }
        public void AddSupportedFeaSoftware(FeaSoftwareEnum inFeaSoftware)
        {
            if (inFeaSoftware == FeaSoftwareEnum.NoFea)
            {
                if (FeaSoftwareListWithCaptions.Count != 0) throw new Exception($"The NoFea Option must be exclusive.");
            }
            else
            {
                if (FeaSoftwareListWithCaptions.ContainsKey(FeaSoftwareEnum.NoFea)) throw new Exception($"The NoFea Option must be exclusive.");
            }

            switch (inFeaSoftware)
            {
                case FeaSoftwareEnum.Ansys:
                    FeaSoftwareListWithCaptions.Add(FeaSoftwareEnum.Ansys, "Ansys");
                    break;

                case FeaSoftwareEnum.Sap2000:
                    FeaSoftwareListWithCaptions.Add(FeaSoftwareEnum.Sap2000, "Sap2000");
                    break;

                case FeaSoftwareEnum.NoFea:
                    FeaSoftwareListWithCaptions.Add(FeaSoftwareEnum.NoFea, "No Fea");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Automatically select and disables the interface, if needed
            if (FeaSoftwareListWithCaptions.Count > 0)
            {
                SolverOptions_SelectedFeaSoftware = _feaSoftwareListWithCaptions.First().Key;
                if (_feaSoftwareListWithCaptions.Count == 1) SolverOptions_FeaSoftwareIsEnabled = false;
                else SolverOptions_FeaSoftwareIsEnabled = true;
            }
            else SolverOptions_FeaSoftwareIsEnabled = false;

            RaisePropertyChanged("FeaSoftwareListWithCaptions");

            IsFeaProblem = !FeaSoftwareListWithCaptions.ContainsKey(FeaSoftwareEnum.NoFea);
        }
        private FeaSoftwareEnum _solverOptions_SelectedFeaSoftware = FeaSoftwareEnum.NoFea;
        public FeaSoftwareEnum SolverOptions_SelectedFeaSoftware
        {
            get => _solverOptions_SelectedFeaSoftware;
            set => SetProperty(ref _solverOptions_SelectedFeaSoftware, value);
        }
        private bool _solverOptions_FeaSoftwareIsEnabled = false;
        public bool SolverOptions_FeaSoftwareIsEnabled
        {
            get => _solverOptions_FeaSoftwareIsEnabled;
            set => SetProperty(ref _solverOptions_FeaSoftwareIsEnabled, value);
        }
        private bool _isFeaProblem;
        public bool IsFeaProblem
        {
            get => _isFeaProblem;
            set
            {
                SetProperty(ref _isFeaProblem, value);

                if (_isFeaProblem)
                {
                    PossibleScreenShotList = new Dictionary<ScreenShotType, string>()
                        {
                            {ScreenShotType.RhinoShot, "Rhino"},

                            {ScreenShotType.EquivalentVonMisesStressOutput, "Von-Mises Stress"},
                            {ScreenShotType.EquivalentStrainOutput, "Equivalent Strain"},
                            {ScreenShotType.StrainEnergyOutput, "Strain Energy"},

                            {ScreenShotType.AxialDiagramOutput, "Axial Force"},
                            {ScreenShotType.VYDiagramOutput, "Shear Force Y"},
                            {ScreenShotType.VZDiagramOutput, "Shear Force Z"},
                            {ScreenShotType.TorsionDiagramOutput, "Torsion"},
                            {ScreenShotType.MYDiagramOutput, "Moment Y"},
                            {ScreenShotType.MZDiagramOutput, "Moment Z"},

                            {ScreenShotType.TotalDisplacementPlot, "Displacement Total"},
                            {ScreenShotType.XDisplacementPlot, "Displacement X"},
                            {ScreenShotType.YDisplacementPlot, "Displacement Y"},
                            {ScreenShotType.ZDisplacementPlot, "Displacement Z"},

                            {ScreenShotType.EigenvalueBuckling1, "Eigenvalue Buckling 1"},
                            {ScreenShotType.EigenvalueBuckling2, "Eigenvalue Buckling 2"},
                            {ScreenShotType.EigenvalueBuckling3, "Eigenvalue Buckling 3"},

                            {ScreenShotType.CodeCheck, "Basic Code Check"},
                        };
                }
                else
                {
                    PossibleScreenShotList = new Dictionary<ScreenShotType, string>()
                        {
                            {ScreenShotType.RhinoShot, "Rhino"},
                        };
                }
            }
        }

        #endregion
        
        #region DesiredScreenShotDefinition
        public Dictionary<ScreenShotType, string> PossibleScreenShotList { get; private set; }
        private ScreenShotType _addComboBox_SelectedScreenShotType;
        public ScreenShotType AddComboBox_SelectedScreenShotType
        {
            get => _addComboBox_SelectedScreenShotType;
            set => SetProperty(ref _addComboBox_SelectedScreenShotType, value);
        }

        public FastObservableCollection<DesiredScreenShotDefinition> DesiredScreenShots { get; } = new FastObservableCollection<DesiredScreenShotDefinition>();
        private DesiredScreenShotDefinition _options_SelectedDesiredScreenShotDefinition;
        public DesiredScreenShotDefinition Options_SelectedDesiredScreenShotDefinition
        {
            get => _options_SelectedDesiredScreenShotDefinition;
            set => SetProperty(ref _options_SelectedDesiredScreenShotDefinition, value);
        }

        private DelegateCommand _addDesiredScreenShotCommand;
        public DelegateCommand AddDesiredScreenShotCommand =>
            _addDesiredScreenShotCommand ?? (_addDesiredScreenShotCommand = new DelegateCommand(ExecuteAddDesiredScreenShotCommand));
        async void ExecuteAddDesiredScreenShotCommand()
        {
            DesiredScreenShotDefinition shotDefinition = new DesiredScreenShotDefinition(AddComboBox_SelectedScreenShotType);
            DesiredScreenShots.Add(shotDefinition);
            Options_SelectedDesiredScreenShotDefinition = shotDefinition;
        }

        private DelegateCommand _removeDesiredScreenShotCommand;
        public DelegateCommand RemoveDesiredScreenShotCommand =>
            _removeDesiredScreenShotCommand ?? (_removeDesiredScreenShotCommand = new DelegateCommand(ExecuteRemoveDesiredScreenShotCommand));
        async void ExecuteRemoveDesiredScreenShotCommand()
        {
            DesiredScreenShots.Remove(Options_SelectedDesiredScreenShotDefinition);
        }

        public Dictionary<ImageCaptureViewDirection, string> PossibleImageDirectionList { get; } = new Dictionary<ImageCaptureViewDirection, string>()
            {

                {ImageCaptureViewDirection.Top_Towards_ZNeg, ImageCaptureViewDirection.Top_Towards_ZNeg.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Front_Towards_YPos, ImageCaptureViewDirection.Front_Towards_YPos.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Back_Towards_YNeg, ImageCaptureViewDirection.Back_Towards_YNeg.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Right_Towards_XNeg, ImageCaptureViewDirection.Right_Towards_XNeg.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection. Left_Towards_XPos, ImageCaptureViewDirection.Left_Towards_XPos.ToString().Replace('_',' ')},

                {ImageCaptureViewDirection.Perspective_Top_Front_Edge, ImageCaptureViewDirection.Perspective_Top_Front_Edge.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Perspective_Top_Back_Edge, ImageCaptureViewDirection.Perspective_Top_Back_Edge.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Perspective_Top_Right_Edge, ImageCaptureViewDirection.Perspective_Top_Right_Edge.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Perspective_Top_Left_Edge, ImageCaptureViewDirection.Perspective_Top_Left_Edge.ToString().Replace('_',' ')},

                {ImageCaptureViewDirection.Perspective_TFR_Corner, ImageCaptureViewDirection.Perspective_TFR_Corner.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Perspective_TFL_Corner, ImageCaptureViewDirection.Perspective_TFL_Corner.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Perspective_TBR_Corner, ImageCaptureViewDirection.Perspective_TBR_Corner.ToString().Replace('_',' ')},
                {ImageCaptureViewDirection.Perspective_TBL_Corner, ImageCaptureViewDirection.Perspective_TBL_Corner.ToString().Replace('_',' ')},

                {ImageCaptureViewDirection.Perspective_Custom, ImageCaptureViewDirection.Perspective_Custom.ToString().Replace('_',' ')},
            };

        public void SaveSolutionImages()
        {
            // Creates the folders for the images
            foreach (DesiredScreenShotDefinition desiredScreenShot in DesiredScreenShots)
            {
                string screenShotDir = Path.Combine(RhinoStaticMethods.GH_Auto_ScreenShotFolder(RhinoModel.RM.GrasshopperFullFileName), desiredScreenShot.FriendlyName);
                if (Directory.Exists(screenShotDir)) Directory.Delete(screenShotDir, true);
                Directory.CreateDirectory(screenShotDir);
            }

            foreach (PossibleSolution possibleSolution in PossibleSolutions.Where(a => a.EvalType == FunctionOrGradientEval.Function))
            {
                foreach (KeyValuePair<string, Image> possibleSolutionScreenShot in possibleSolution.ScreenShots)
                {
                    string imageFilePath = Path.Combine(RhinoStaticMethods.GH_Auto_ScreenShotFolder(RhinoModel.RM.GrasshopperFullFileName), possibleSolutionScreenShot.Key, $"Iteration {possibleSolution.FunctionHitCount:000000}.png");
                    using (FileStream fs = new FileStream(imageFilePath, FileMode.Create))
                    {
                        possibleSolutionScreenShot.Value.Save(fs, ImageFormat.Png);
                    }
                }
            }
        }

        public virtual void SetDefaultScreenShots()
        {
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.RhinoShot, "Rhino - 0"));
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.RhinoShot, "Rhino - 1"));
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.RhinoShot, "Rhino - 2"));
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.RhinoShot, "Rhino - 3"));
        }
        #endregion

        #region Boundary Conditions Control
        public ICollectionView GrasshopperPointListDefs_ViewItems { get; set; } // Type is PointList_Output_ParamDef
        private bool GrasshopperPointListDefs_ViewItems_Filter(object inObj)
        {
            if (!(inObj is Output_ParamDefBase interParam)) throw new InvalidCastException("The GrasshopperLineListDefs_ViewItems contains something other than a Output_ParamDefBase object.");
            if (interParam is PointList_Output_ParamDef) return true;
            return false;
        }
        #endregion

        #region Frame Section Management
        public ICollectionView GrasshopperLineListDefs_ViewItems { get; set; } // Type is LineList_Output_ParamDef
        private bool GrasshopperLineListDefs_ViewItems_Filter(object inObj)
        {
            if (!(inObj is Output_ParamDefBase interParam)) throw new InvalidCastException("The GrasshopperLineListDefs_ViewItems contains something other than a Output_ParamDefBase object.");
            if (interParam is LineList_Output_ParamDef) return true;
            return false;
        }

        private LineList_Output_ParamDef _grasshopperLineListDefs_SelectedItem;
        public LineList_Output_ParamDef GrasshopperLineListDefs_SelectedItem
        {
            get => _grasshopperLineListDefs_SelectedItem;
            set
            {
                _grasshopperLineListDefs_SelectedItem = value;

                foreach (object item in GrasshopperLineListDefs_ViewItems)
                {
                    if (item is LineList_Output_ParamDef lparam)
                    {
                        lparam.SectionAssignment_Visibility = Visibility.Collapsed;

                        if (lparam == value)
                        {
                            lparam.SectionAssignment_Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        public ICollectionView AllPossibleSections_ViewItems { get; set; }
        //public FeSection GrasshopperLineList_Selected { get; set; }
        #endregion

        #region Load Controls
        public FeLoad_Inertial Load_Gravity { get; } = FeLoad_Inertial.StandardGravity;
        #endregion
    }

    public enum SolverStatus
    {
        NotStarted,
        Running,
        NoProgress,
        MaxEvaluations,
        Finished,
        Cancelled,
        Failed
    }

    public enum ImageCaptureViewDirection
    {
        Top_Towards_ZNeg,
        Front_Towards_YPos,
        Back_Towards_YNeg,
        Right_Towards_XNeg,
        Left_Towards_XPos,

        Perspective_Top_Front_Edge,
        Perspective_Top_Back_Edge,
        Perspective_Top_Right_Edge,
        Perspective_Top_Left_Edge,

        Perspective_TFR_Corner,
        Perspective_TFL_Corner,
        Perspective_TBR_Corner,
        Perspective_TBL_Corner,

        Perspective_Custom,
    }
    public enum ScreenShotType
    {
        // FEA
        EquivalentVonMisesStressOutput,

        EquivalentStrainOutput,

        StrainEnergyOutput,

        // Forces
        AxialDiagramOutput,
        MYDiagramOutput,
        MZDiagramOutput,
        TorsionDiagramOutput,
        VYDiagramOutput,
        VZDiagramOutput,


        // Displacement
        TotalDisplacementPlot,
        XDisplacementPlot,
        YDisplacementPlot,
        ZDisplacementPlot,

        // Eigenvalue Buckling Shapes
        EigenvalueBuckling1,
        EigenvalueBuckling2,
        EigenvalueBuckling3,

        // Code Check
        CodeCheck,

        // Rhino
        RhinoShot,
    }

    public class DesiredScreenShotDefinition : BindableBase
    {
        public DesiredScreenShotDefinition(ScreenShotType inShotType, string inFriendlyName = null, ImageCaptureViewDirection inDirection = ImageCaptureViewDirection.Perspective_TFL_Corner)
        {
            _friendlyName = string.IsNullOrWhiteSpace(inFriendlyName) ? $"{inShotType.ToString()}_{Sap2000Library.S2KStaticMethods.UniqueName(5)}" : inFriendlyName;

            _shotType = inShotType;
            Direction = inDirection;
        }

        public string Name => $"{ShotType}_{Direction}";
        public string DirectionFriendlyString
        {
            get
            {
                if (ShotType == ScreenShotType.RhinoShot) return "ViewPort";
                if (Direction != ImageCaptureViewDirection.Perspective_Custom) return Direction.ToString().Replace('_', ' ');
                return $"C {CustomDirX:F2} , {CustomDirY:F2} , {CustomDirZ:F2}";
            }
        }

        private string _friendlyName;
        public string FriendlyName
        {
            get => _friendlyName;
            set => SetProperty(ref _friendlyName, value);
        }

        private Image _image;
        public Image Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        private ImageCaptureViewDirection _direction;
        public ImageCaptureViewDirection Direction
        {
            get => _direction;
            set
            {
                SetProperty(ref _direction, value);

                // Sets the Directions
                RotateToAlign = Double.NaN;
                switch (Direction)
                {
                    case ImageCaptureViewDirection.Top_Towards_ZNeg:
                        CustomDirX = 0d;
                        CustomDirY = 0d;
                        CustomDirZ = 1d;
                        RotateToAlign = -90d;
                        break;

                    case ImageCaptureViewDirection.Front_Towards_YPos:
                        CustomDirX = 0d;
                        CustomDirY = -1d;
                        CustomDirZ = 0d;
                        break;

                    case ImageCaptureViewDirection.Back_Towards_YNeg:
                        CustomDirX = 0d;
                        CustomDirY = 1d;
                        CustomDirZ = 0d;
                        break;

                    case ImageCaptureViewDirection.Right_Towards_XNeg:
                        CustomDirX = 1d;
                        CustomDirY = 0d;
                        CustomDirZ = 0d;
                        break;

                    case ImageCaptureViewDirection.Left_Towards_XPos:
                        CustomDirX = -1d;
                        CustomDirY = 0d;
                        CustomDirZ = 0d;
                        break;

                    case ImageCaptureViewDirection.Perspective_Top_Front_Edge:
                        CustomDirX = 0.2d;
                        CustomDirY = -1d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_Top_Back_Edge:
                        CustomDirX = 0.2d;
                        CustomDirY = 1d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_Top_Right_Edge:
                        CustomDirX = 1d;
                        CustomDirY = 0.2d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_Top_Left_Edge:
                        CustomDirX = -1d;
                        CustomDirY = 0.2d;
                        CustomDirZ = 1d;
                        break;


                    case ImageCaptureViewDirection.Perspective_TFR_Corner:
                        CustomDirX = 1d;
                        CustomDirY = -1d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_TFL_Corner:
                        CustomDirX = -1d;
                        CustomDirY = -1d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_TBR_Corner:
                        CustomDirX = 1d;
                        CustomDirY = 1d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_TBL_Corner:
                        CustomDirX = -1d;
                        CustomDirY = 1d;
                        CustomDirZ = 1d;
                        break;

                    case ImageCaptureViewDirection.Perspective_Custom:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(Direction), Direction, null);
                }

                RaisePropertyChanged("IsCustomDirection");
            }
        }
        public bool IsCustomDirection => Direction == ImageCaptureViewDirection.Perspective_Custom;

        private double _customDirX;
        public double CustomDirX
        {
            get => _customDirX;
            set => SetProperty(ref _customDirX, value);
        }
        private double _customDirY;
        public double CustomDirY
        {
            get => _customDirY;
            set => SetProperty(ref _customDirY, value);
        }
        private double _customDirZ;
        public double CustomDirZ
        {
            get => _customDirZ;
            set => SetProperty(ref _customDirZ, value);
        }

        public double RotateToAlign = double.NaN;

        private ScreenShotType _shotType;
        public ScreenShotType ShotType
        {
            get => _shotType;
            set => SetProperty(ref _shotType, value);
        }

        private bool _legendAutoScale = true;
        public bool LegendAutoScale
        {
            get => _legendAutoScale;
            set
            {
                SetProperty(ref _legendAutoScale, value);

                if (_legendAutoScale)
                {
                    LegendScale_Min = double.NaN;
                    LegendScale_Max = double.NaN;
                }
                else
                {
                    LegendScale_Min = 0d;
                    LegendScale_Max = 100d;
                }

                RaisePropertyChanged("LegendRangeEnabled");
            }
        }
        public bool LegendRangeEnabled => !LegendAutoScale;

        private double _legendScale_Min = double.NaN;
        public double LegendScale_Min
        {
            get => _legendScale_Min;
            set => SetProperty(ref _legendScale_Min, value);
        }

        private double _legendScale_Max = double.NaN;
        public double LegendScale_Max
        {
            get => _legendScale_Max;
            set => SetProperty(ref _legendScale_Max, value);
        }

        public bool IsNotRhino => ShotType != ScreenShotType.RhinoShot;
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