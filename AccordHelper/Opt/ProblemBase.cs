using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using AccordHelper.FEA;
using BaseWPFLibrary.Others;
using NLoptNet;
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
                        _feModel = new AnsysModel(modelDir);
                        break;

                    case FeaSoftwareEnum.Sap2000:
                        _feModel = new S2KModel(modelDir);
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

        // Accord Optimization Solvers
        private BaseOptimizationMethod _accordOptMethod = null;
        public BaseOptimizationMethod AccordOptMethod
        {
            get => _accordOptMethod;
            set
            {
                _accordOptMethod = value;
                _accordOptMethod.Token = CancelSource.Token;
            }
        }

        // NLOpt Solvers
        private NLoptSolver _nLOptMethod;
        public NLoptSolver NLOptMethod
        {
            get => _nLOptMethod;
            set => SetProperty(ref _nLOptMethod, value);
        }


        // Accord Genetic Algorithm Solvers
        private Population _genAlgPopulation = null;
        public Population GenAlgPopulation
        {
            get => _genAlgPopulation;
            set => _genAlgPopulation = value;
        }

        private int _geneticEpochCount = 0;
        public int GeneticEpochCount
        {
            get => _geneticEpochCount;
            set => SetProperty(ref _geneticEpochCount, value);
        }

        private int _maxIterations = 10000;
        public int MaxIterations
        {
            get => _maxIterations;
            set => SetProperty(ref _maxIterations, value);
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


        private SolverType _solverType = SolverType.BoundedBroydenFletcherGoldfarbShanno;
        public SolverType SolverType
        {
            get => _solverType;
            set
            {
                SetProperty(ref _solverType, value);
                RaisePropertyChanged("TargetResidualText");
                RaisePropertyChanged("TargetResidualToolTip");

                RaisePropertyChanged("MinimumResidualChangeText");
                RaisePropertyChanged("MinimumResidualChangeToolTip");

                RaisePropertyChanged("MaximumIterationsText");
                RaisePropertyChanged("MaximumIterationsToolTip");

                RaisePropertyChanged("IsGenetic");
                RaisePropertyChanged("IsOther");
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

            // Stores the function given
            _objectiveFunction = inObjectiveFunction;
            // Gives a reference of this problem to the objective function
            _objectiveFunction.Problem = this;

            // Generates the default views of such parameters
            InputDefs_ViewItems = CollectionViewSource.GetDefaultView(_objectiveFunction.InputDefs);
            IntermediateDefs_ViewItems = CollectionViewSource.GetDefaultView(_objectiveFunction.IntermediateDefs);
            FinalDefs_ViewItems = CollectionViewSource.GetDefaultView(_objectiveFunction.FinalDefs);

            CollectionViewSource functionSolutions_ViewSource = new CollectionViewSource {Source = _possibleSolutions};
            FunctionSolutions_ViewItems = functionSolutions_ViewSource.View;
            FunctionSolutions_ViewItems.Filter = FunctionSolutions_ViewItems_Filter;

            CollectionViewSource gradientSolutions_ViewSource = new CollectionViewSource {Source = _possibleSolutions};
            GradientSolutions_ViewItems = gradientSolutions_ViewSource.View;
            GradientSolutions_ViewItems.Filter = GradientSolutions_ViewItems_Filter;

            AllPossibleSolutions_ViewItems = CollectionViewSource.GetDefaultView(_possibleSolutions);
        }
        private double _minResidualPercentChange = 0.01d;
        public double MinResidualPercentChange
        {
            get => _minResidualPercentChange;
            set => SetProperty(ref _minResidualPercentChange, value);
        }
        private bool _shouldLimitChange = true;
        public bool ShouldLimitChange
        {
            get => _shouldLimitChange;
            set => SetProperty(ref _shouldLimitChange, value);
        }


        private double _targetResidual = 1e-5;
        public double TargetResidual
        {
            get => _targetResidual;
            set
            {
                if (value <= 0d) throw new InvalidOperationException($"The target residual must be larger than 0.");
                SetProperty(ref _targetResidual, value);
            }
        }
        private bool _shouldTargetResidual = true;
        public bool ShouldTargetResidual
        {
            get => _shouldTargetResidual;
            set => SetProperty(ref _shouldTargetResidual, value);
        }


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

                    // Also updates the listeners of the cancellation token source.
                    if (AccordOptMethod != null) AccordOptMethod.Token = _cancelSource.Token;

                    // The Genetic Algorithm already uses the token directly from the Source.
                }
            }
        }


        private double _totalSolveSeconds = 0d;

        public double TotalSolveSeconds
        {
            get => _totalSolveSeconds;
            set { SetProperty(ref _totalSolveSeconds, value); }
        }

        public void CleanUpSolver_NewSolve()
        {
            // Cleans all the important state variables
            PossibleSolutions.Clear();
            GeneticEpochCount = 0;
            ObjectiveFunction.Reset();
            AccordOptMethod = null;
            if (NLOptMethod != null)
            {
                NLOptMethod.Dispose();
                NLOptMethod = null;
            }

            CancelSource = new CancellationTokenSource();

            Status = SolverStatus.NotStarted;
        }
        public void SetSolverManager()
        {
            Status = SolverStatus.NotStarted;

            // Initializes each solver, depending on what they need
            switch (SolverType)
            {
                case SolverType.AugmentedLagrangian:
                {
                    List<NonlinearConstraint> allConstraints = ObjectiveFunction.GetAllConstraints();
                    if (allConstraints == null) throw new Exception("Augmented Lagrangian requires constraints.");

                    AugmentedLagrangian alSolver = new AugmentedLagrangian(ObjectiveFunction, allConstraints);
                    alSolver.MaxEvaluations = MaxIterations;
                    AccordOptMethod = alSolver;
                }
                    break;

                case SolverType.Cobyla:
                {
                    List<NonlinearConstraint> allConstraints = ObjectiveFunction.GetAllConstraints();
                    Cobyla cobSolver = allConstraints == null ? new Cobyla(ObjectiveFunction) : new Cobyla(ObjectiveFunction, allConstraints);
                    cobSolver.MaxIterations = MaxIterations;
                    AccordOptMethod = cobSolver;
                }
                    break;

                case SolverType.BoundedBroydenFletcherGoldfarbShanno:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Bounded Broyden Fletcher Goldfarb Shanno requires boundaries.");
                    BoundedBroydenFletcherGoldfarbShanno bbSolver = new BoundedBroydenFletcherGoldfarbShanno(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    bbSolver.MaxIterations = MaxIterations;
                    bbSolver.LowerBounds = ObjectiveFunction.LowerBounds;
                    bbSolver.UpperBounds = ObjectiveFunction.UpperBounds;

                    bbSolver.FunctionTolerance = 1e9;

                    AccordOptMethod = bbSolver;
                }
                    break;

                case SolverType.ConjugateGradient_FletcherReeves:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Conjugate Gradient requires boundaries.");
                    ConjugateGradient cgSolver = new ConjugateGradient(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    cgSolver.MaxIterations = MaxIterations;
                    cgSolver.Method = ConjugateGradientMethod.FletcherReeves;

                    AccordOptMethod = cgSolver;
                }
                    break;

                case SolverType.ConjugateGradient_PolakRibiere:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Conjugate Gradient requires boundaries.");
                    ConjugateGradient cgSolver = new ConjugateGradient(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    cgSolver.MaxIterations = MaxIterations;
                    cgSolver.Method = ConjugateGradientMethod.PolakRibiere;

                    AccordOptMethod = cgSolver;
                }
                    break;

                case SolverType.ConjugateGradient_PositivePolakRibiere:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Conjugate Gradient requires boundaries.");
                    ConjugateGradient cgSolver = new ConjugateGradient(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    cgSolver.MaxIterations = MaxIterations;
                    cgSolver.Method = ConjugateGradientMethod.PositivePolakRibiere;

                    AccordOptMethod = cgSolver;
                }
                    break;

                case SolverType.NelderMead:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Nelder Mead requires boundaries.");
                    NelderMead ndSolver = new NelderMead(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function);
                    //ndSolver.MaxIterations = MaxIterations;
                    //ndSolver.LowerBounds = ObjectiveFunction.LowerBounds;
                    //ndSolver.UpperBounds = ObjectiveFunction.UpperBounds;

                    AccordOptMethod = ndSolver;
                }
                    break;

                case SolverType.ResilientBackpropagation:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Resilient Backpropagation requires boundaries.");
                    ResilientBackpropagation rbSolver = new ResilientBackpropagation(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    rbSolver.Iterations = MaxIterations;

                    AccordOptMethod = rbSolver;
                }
                    break;

                case SolverType.Genetic:
                {
                }
                    break;

                case SolverType.NLOpt_Cobyla:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("NLOpt Cobyla solver requires boundaries.");

                    NLOptMethod = new NLoptSolver(NLoptAlgorithm.LN_COBYLA, (uint)ObjectiveFunction.NumberOfVariables);
                    NLOptMethod.SetLowerBounds(ObjectiveFunction.LowerBounds);
                    NLOptMethod.SetUpperBounds(ObjectiveFunction.UpperBounds);
                    NLOptMethod.SetMinObjective(ObjectiveFunction.Function_NLOptFunctionWrapper);


                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_solverType), _solverType, null);
            }

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

            #region Genetic Algorithm

            // For the Genetic Algorithm
            if (SolverType == SolverType.Genetic)
            {
                //if (_genAlgPopulation == null) throw new Exception("You did not start the solver. Please use the SetSolverManager function for this.");

                Population pop = new Population(20,
                    ObjectiveFunction.GetStartGrasshopperChromosome(StartPositionType),
                    ObjectiveFunction,
                    new EliteSelection());
                GenAlgPopulation = pop;

                // Does the requested iterations
                while (true)
                {
                    // Runs one epoch
                    GenAlgPopulation.RunEpoch();

                    // Increases the counters
                    GeneticEpochCount++;

                    // The solution has been found *
                    if (GenAlgPopulation.BestChromosome.Fitness <= TargetResidual)
                    {
                        Status = SolverStatus.Finished;
                        StatusTextMessage = "Finish - Reached Target Residual";
                        return;
                    }

                    //// This was the first iteration - we don't have anything to compare
                    //if (Status == SolverStatus.Initialized)
                    //{
                    //    LastBestEvalValue = GenAlgPopulation.BestChromosome.Fitness;
                    //    Status = SolverStatus.Running;
                    //}
                    //else // Wasn't the first
                    //{
                    //    double effectiveChangeLimit = Math.Abs(GenAlgPopulation.BestChromosome.Fitness * ChangeLimit);
                    //    double delta = Math.Abs(GenAlgPopulation.BestChromosome.Fitness - LastBestEvalValue.Value);

                    //    if (delta < effectiveChangeLimit) // Reached the change limit
                    //    {
                    //        //Status = SolverStatus.NoProgress;
                    //        //StatusTextMessage = "Maximum Progress - Reached relative change limit";
                    //        //return;
                    //    }
                    //    else // Did not reach the change limit
                    //    {
                    //        // Just saves the value for the next iteration
                    //        LastBestEvalValue = GenAlgPopulation.BestChromosome.Fitness;
                    //    }
                    //}

                    // Did we reach the maximum number of iterations?
                    if (GeneticEpochCount >= MaxIterations)
                    {
                        Status = SolverStatus.MaxEvaluations;
                        StatusTextMessage = "Maximum Evaluations - Reached maximum number of genetic epochs";
                        return;
                    }

                    // Was the cancellation token requested?
                    if (CancelSource.Token.IsCancellationRequested)
                    {
                        Status = SolverStatus.Cancelled;
                        StatusTextMessage = "Cancelled by User - Resume possible";
                        return;
                    }
                }
            }

            #endregion

            #region Accord Optimizations
            else if (SolverType == SolverType.Cobyla ||
                     SolverType == SolverType.AugmentedLagrangian ||
                     SolverType == SolverType.BoundedBroydenFletcherGoldfarbShanno ||
                     SolverType == SolverType.ConjugateGradient_FletcherReeves ||
                     SolverType == SolverType.ConjugateGradient_PositivePolakRibiere ||
                     SolverType == SolverType.NelderMead ||
                     SolverType == SolverType.ResilientBackpropagation ||
                     SolverType == SolverType.ConjugateGradient_PolakRibiere)
            {
                if (_accordOptMethod == null) throw new Exception("You did not start the solver. Please use the SetSolverManager function for this.");

                // This is the first time we are running this solver
                double[] vector = Status == SolverStatus.NotStarted ? ObjectiveFunction.GetStartPosition(StartPositionType) : null;

                try
                {
                    bool minResult;

                    // Effectively runs the solve algorithm
                    minResult = vector != null ? AccordOptMethod.Minimize(vector) : AccordOptMethod.Minimize();

                    // The solver terminated internally - not from our own control
                    if (AccordOptMethod is BoundedBroydenFletcherGoldfarbShanno bbSolver)
                    {
                        switch (bbSolver.Status)
                        {
                            case BoundedBroydenFletcherGoldfarbShannoStatus.Stop:
                                if (CancelSource.Token.IsCancellationRequested)
                                {
                                    Status = SolverStatus.Cancelled;
                                    StatusTextMessage = "Cancelled by User - Resume possible";
                                    break;
                                }
                                else
                                {
                                    Status = SolverStatus.MaxEvaluations;
                                    StatusTextMessage = "The B-BFGS solver is in stop state";
                                    break;
                                }

                            case BoundedBroydenFletcherGoldfarbShannoStatus.MaximumIterations:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                                break;

                            case BoundedBroydenFletcherGoldfarbShannoStatus.FunctionConvergence:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Reached B-BFGS function convergence";
                                break;

                            case BoundedBroydenFletcherGoldfarbShannoStatus.GradientConvergence:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Reached B-BFGS gradient convergence";
                                break;

                            case BoundedBroydenFletcherGoldfarbShannoStatus.LineSearchFailed:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "The B-BFGS had a Line Search Failure (Gradient Function Issue)";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (AccordOptMethod is AugmentedLagrangian agSolver)
                    {
                        switch (agSolver.Status)
                        {
                            case AugmentedLagrangianStatus.Converged:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Converged";
                                break;

                            case AugmentedLagrangianStatus.NoProgress:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Maximum Progress Reached";
                                break;

                            case AugmentedLagrangianStatus.MaxEvaluations:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "Maximum Evaluations - Reached maximum number of evaluations";
                                break;

                            case AugmentedLagrangianStatus.Cancelled:
                                Status = SolverStatus.Cancelled;
                                StatusTextMessage = "Cancelled by User - Resume possible";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (AccordOptMethod is Cobyla cobSolver)
                    {
                        switch (cobSolver.Status)
                        {
                            case CobylaStatus.Success:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Success";
                                break;

                            case CobylaStatus.MaxIterationsReached:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                                break;

                            case CobylaStatus.DivergingRoundingErrors:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Maximum Progress (Diverging rounding errors)";
                                break;

                            case CobylaStatus.NoPossibleSolution:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Maximum Progress (No possible solution)";
                                break;

                            case CobylaStatus.Cancelled:
                                Status = SolverStatus.Cancelled;
                                StatusTextMessage = "Cancelled by User - Resume possible";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (AccordOptMethod is ConjugateGradient conjGradSolver)
                    {
                        switch (conjGradSolver.Status)
                        {
                            case ConjugateGradientCode.Success:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Success";
                                break;

                            case ConjugateGradientCode.StepSize:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Invalid Step Size";
                                break;

                            case ConjugateGradientCode.DescentNotObtained:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Descent Direction Not Obtained";
                                break;

                            case ConjugateGradientCode.RoundingErrors:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Rounding errors prevent further progress";
                                break;

                            case ConjugateGradientCode.StepHigh:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - The step size has reached the upper bound";
                                break;

                            case ConjugateGradientCode.StepLow:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - The step size has reached the lower bound";
                                break;

                            case ConjugateGradientCode.MaximumEvaluations:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                                break;

                            case ConjugateGradientCode.Precision:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - Relative width of the interval of uncertainty is at machine precision";
                                break;

                            case ConjugateGradientCode.Cancelled:
                                Status = SolverStatus.Cancelled;
                                StatusTextMessage = "Cancelled by User - Resume possible";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (AccordOptMethod is NelderMead nedSolver)
                    {
                        switch (nedSolver.Status)
                        {
                            case NelderMeadStatus.ForcedStop:
                                Status = SolverStatus.Cancelled;
                                StatusTextMessage = "Cancelled by User - Resume possible";
                                break;

                            case NelderMeadStatus.Success:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Success";
                                break;

                            case NelderMeadStatus.MaximumTimeReached:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - The execution time exceeded the established limit";
                                break;

                            case NelderMeadStatus.MinimumAllowedValueReached:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - The minimum desired value has been reached";
                                break;

                            case NelderMeadStatus.MaximumEvaluationsReached:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                                break;

                            case NelderMeadStatus.Failure:
                                Status = SolverStatus.NoProgress;
                                StatusTextMessage = "Finish - The algorithm failed internally";
                                break;

                            case NelderMeadStatus.FunctionToleranceReached:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - The desired output tolerance (minimum change in the function output between two consecutive iterations) has been reached";
                                break;

                            case NelderMeadStatus.SolutionToleranceReached:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - The desired parameter tolerance (minimum change in the solution vector between two iterations) has been reached";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (AccordOptMethod is ResilientBackpropagation relSolver)
                    {
                        switch (relSolver.Status)
                        {
                            case ResilientBackpropagationStatus.Finished:
                                Status = SolverStatus.Finished;
                                StatusTextMessage = "Finish - Success";
                                break;

                            case ResilientBackpropagationStatus.MaxIterations:
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                                break;

                            case ResilientBackpropagationStatus.Cancelled:
                                Status = SolverStatus.Cancelled;
                                StatusTextMessage = "Cancelled by User - Resume possible";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
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
            }
            #endregion

            #region NLOpt Optimizations
            else if (SolverType == SolverType.NLOpt_Cobyla)
            {
                if (_nLOptMethod == null) throw new Exception("You did not start the solver. Please use the SetSolverManager function for this.");
                
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
                            StatusTextMessage = "Invalid arguments (e.g. lower bounds are bigger than upper bounds, an unknown algorithm was specified, etcetera).";
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
                            StatusTextMessage = "Cancelled by User - Resume possible";
                            break;

                        case NloptResult.SUCCESS:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Generic success return value.";
                            break;

                        case NloptResult.STOPVAL_REACHED:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Optimization stopped because stopval (above) was reached.";
                            break;

                        case NloptResult.FTOL_REACHED:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Optimization stopped because ftol_rel or ftol_abs (above) was reached.";
                            break;

                        case NloptResult.XTOL_REACHED:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Optimization stopped because xtol_rel or xtol_abs (above) was reached.";
                            break;

                        case NloptResult.MAXEVAL_REACHED:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Optimization stopped because maxeval (above) was reached.";
                            break;

                        case NloptResult.MAXTIME_REACHED:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Optimization stopped because maxtime (above) was reached.";
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

        public bool SolvesCurrentGHFile { get; set; }
        public virtual string ProblemFriendlyName
        {
            get => this.GetType().FullName;
        }
        public virtual string ClassName
        {
            get => this.GetType().Name;
        }


        public Dictionary<SolverType, string> SolverTypeListWithCaptions { get; } = new Dictionary<SolverType, string>()
            {
                {SolverType.AugmentedLagrangian, "Aug. Lagrangian"},
                {SolverType.BoundedBroydenFletcherGoldfarbShanno, "B-BFGS"},
                {SolverType.Cobyla, "Cobyla"},
                {SolverType.ConjugateGradient_FletcherReeves, "Conj. Gradient - Fletcher Reeves"},
                {SolverType.ConjugateGradient_PolakRibiere, "Conj. Gradient - Polak Ribiere"},
                {SolverType.ConjugateGradient_PositivePolakRibiere, "Conj. Gradient - [+] Polak Ribiere"},
                {SolverType.Genetic, "Genetic"},
                {SolverType.NelderMead, "Nelder Mead"},
                {SolverType.ResilientBackpropagation, "Resilient Backpropagation"},
                {SolverType.NLOpt_Cobyla, "NLOpt Cobyla"}
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

        #endregion
    }

    public enum SolverType
    {
        AugmentedLagrangian,
        Cobyla,
        BoundedBroydenFletcherGoldfarbShanno,
        Genetic,
        ConjugateGradient_FletcherReeves,
        ConjugateGradient_PolakRibiere,
        ConjugateGradient_PositivePolakRibiere,
        NelderMead,
        ResilientBackpropagation,
        NLOpt_Cobyla,
        NLOptBobyqa
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
}