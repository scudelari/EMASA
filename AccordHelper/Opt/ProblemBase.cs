using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Accord;
using Accord.Collections;
using Accord.Genetic;
using Accord.IO;
using Accord.Math.Optimization;
using AccordHelper.FEA;
using BaseWPFLibrary.Others;
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

        private BaseOptimizationMethod _optMethod = null;
        public BaseOptimizationMethod OptMethod
        {
            get => _optMethod;
            set
            {
                _optMethod = value;
                _optMethod.Token = CancelSource.Token;
            }
        }

        private Population _genAlgPopulation = null;
        public Population GenAlgPopulation
        {
            get => _genAlgPopulation;
            set => _genAlgPopulation = value;
        }
        
        private int _maxIterations = 10000;
        public int MaxIterations
        {
            get => _maxIterations;
            set => SetProperty(ref _maxIterations, value);
        }

        private int _iterationCount = 0;
        public int IterationCount
        {
            get => _iterationCount;
            set => SetProperty(ref _iterationCount, value);
        }

        private SolverStatus _status = SolverStatus.NotInitialized;
        public SolverStatus Status
        {
            get => _status;
            private set
            {
                SetProperty(ref _status, value);
                RaisePropertyChanged("StatusText");
            }
        }
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case SolverStatus.NotInitialized:
                        return "Not Initialized";

                    case SolverStatus.Initialized:
                        return "Initialized";

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
        private string _statusTextMessage;
        public string StatusTextMessage
        {
            get => _statusTextMessage;
            private set => SetProperty(ref _statusTextMessage, value);
        }

        private SolverType _solverType = SolverType.BoundedBroydenFletcherGoldfarbShanno;
        public SolverType SolverType
        {
            get => _solverType;
            set => SetProperty(ref _solverType, value);
        }

        private StartPositionType _startPositionType = StartPositionType.Random;
        public StartPositionType StartPositionType
        {
            get => _startPositionType;
            set => SetProperty(ref _startPositionType, value);
        }

        protected ProblemBase(ObjectiveFunctionBase inObjectiveFunction)
        {
            // Sets the default value of the FEA solver
            FeaType = SupportedFeaSoftwares.First();

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

        private double _changeLimit = 1e-2;
        /// <summary>
        /// This is the criterion that will stop the iterations. It is a percentage limit of how much the Fitness function can change between iterations.
        /// </summary>
        public double ChangeLimit
        {
            get => _changeLimit;
            set => SetProperty(ref _changeLimit, value);
        }
        private double? LastBestEvalValue = null;

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
                    if (OptMethod != null) OptMethod.Token = _cancelSource.Token;
                    // The Genetic Algorithm already uses the token directly from the Source.
                }
            }
        }

        public void CleanUpSolver()
        {
            // Cleans all the important state variables
            FeaType = SupportedFeaSoftwares.First();

            LastBestEvalValue = null;
            PossibleSolutions.Clear();
            IterationCount = 0;
            ObjectiveFunction.FunctionHitCount = 0;
            ObjectiveFunction.GradientHitCount = 0;
            ObjectiveFunction.CurrentSolution = null;

            CancelSource = new CancellationTokenSource();

            Status = SolverStatus.NotInitialized;
            StatusTextMessage = "Not Initialized";
        }
        public void ResetSolver()
        {
            Status = SolverStatus.Initialized;
            StatusTextMessage = "Initialized";

            // Initializes each solver, depending on what they need
            switch (SolverType)
            {
                case SolverType.AugmentedLagrangian:
                    {
                        List<NonlinearConstraint> allConstraints = ObjectiveFunction.GetAllConstraints();
                        if (allConstraints == null) throw new Exception("Augmented Lagrangian requires constraints.");

                        AugmentedLagrangian alSolver = new AugmentedLagrangian(ObjectiveFunction, allConstraints);
                        alSolver.MaxEvaluations = MaxIterations;
                        OptMethod = alSolver;
                    }
                    break;

                case SolverType.Cobyla:
                    {
                        List<NonlinearConstraint> allConstraints = ObjectiveFunction.GetAllConstraints();
                        Cobyla cobSolver = allConstraints == null ? new Cobyla(ObjectiveFunction) : new Cobyla(ObjectiveFunction, allConstraints);

                        cobSolver.MaxIterations = MaxIterations;
                        OptMethod = cobSolver;
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

                        OptMethod = bbSolver;
                    }
                    break;

                case SolverType.ConjugateGradient_FletcherReeves:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Conjugate Gradient requires boundaries.");
                    ConjugateGradient cgSolver = new ConjugateGradient(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    cgSolver.MaxIterations = MaxIterations;
                    cgSolver.Method = ConjugateGradientMethod.FletcherReeves;

                    OptMethod = cgSolver;
                }
                    break;
                case SolverType.ConjugateGradient_PolakRibiere:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Conjugate Gradient requires boundaries.");
                    ConjugateGradient cgSolver = new ConjugateGradient(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    cgSolver.MaxIterations = MaxIterations;
                    cgSolver.Method = ConjugateGradientMethod.PolakRibiere;

                    OptMethod = cgSolver;
                }
                    break;
                case SolverType.ConjugateGradient_PositivePolakRibiere:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Conjugate Gradient requires boundaries.");
                    ConjugateGradient cgSolver = new ConjugateGradient(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    cgSolver.MaxIterations = MaxIterations;
                    cgSolver.Method = ConjugateGradientMethod.PositivePolakRibiere;

                    OptMethod = cgSolver;
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

                    OptMethod = ndSolver;
                }
                    break;

                case SolverType.ResilientBackpropagation:
                {
                    if (ObjectiveFunction.LowerBounds == null || ObjectiveFunction.UpperBounds == null) throw new Exception("Resilient Backpropagation requires boundaries.");
                    ResilientBackpropagation rbSolver = new ResilientBackpropagation(
                        ObjectiveFunction.NumberOfVariables, ObjectiveFunction.Function, ObjectiveFunction.Gradient);
                    rbSolver.Iterations = MaxIterations;

                    OptMethod = rbSolver;
                }
                    break;

                case SolverType.Genetic:
                    {

                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_solverType), _solverType, null);
            }
        }

        /// <summary>
        /// Returns true if finished.
        /// </summary>
        /// <returns></returns>
        public void Solve()
        {
            if (Status == SolverStatus.NotInitialized) throw new Exception("Please initialize the solver before trying to solve.");
            if (Status == SolverStatus.Finished) throw new Exception("The solver has already finished.");
            if (Status == SolverStatus.NoProgress) throw new Exception("The solver can't progress the solution further.");
            if (Status == SolverStatus.MaxEvaluations) throw new Exception("The solver reached the maximum number of evaluations.");

            // Terminates the solver process
            if (FeModel != null)
            {
                FeModel.InitializeModelAndSoftware();
            }

            #region Genetic Algorithm
            // For the Genetic Algorithm
            if (SolverType == SolverType.Genetic)
            {
                //if (_genAlgPopulation == null) throw new Exception("You did not start the solver. Please use the ResetSolver function for this.");

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
                    IterationCount++;

                    // The solution has been found *
                    if (GenAlgPopulation.BestChromosome.Fitness <= TargetResidual)
                    {
                        Status = SolverStatus.Finished;
                        StatusTextMessage = "Finish - Reached Target Residual";
                        return;
                    }

                    // This was the first iteration - we don't have anything to compare
                    if (Status == SolverStatus.Initialized)
                    {
                        LastBestEvalValue = GenAlgPopulation.BestChromosome.Fitness;
                        Status = SolverStatus.Running;
                    }
                    else // Wasn't the first
                    {
                        double effectiveChangeLimit = Math.Abs(GenAlgPopulation.BestChromosome.Fitness * ChangeLimit);
                        double delta = Math.Abs(GenAlgPopulation.BestChromosome.Fitness - LastBestEvalValue.Value);

                        if (delta < effectiveChangeLimit) // Reached the change limit
                        {
                            //Status = SolverStatus.NoProgress;
                            //StatusTextMessage = "Maximum Progress - Reached relative change limit";
                            //return;
                        }
                        else // Did not reach the change limit
                        {
                            // Just saves the value for the next iteration
                            LastBestEvalValue = GenAlgPopulation.BestChromosome.Fitness;
                        }
                    }

                    // Did we reach the maximum number of iterations?
                    if (IterationCount >= MaxIterations)
                    {
                        Status = SolverStatus.MaxEvaluations;
                        StatusTextMessage = "Maximum Evaluations - Reached maximum number of genetic epochs";
                        return;
                    }

                    // Was the cancellation toke requested?
                    if (CancelSource.Token.IsCancellationRequested)
                    {
                        Status = SolverStatus.Cancelled;
                        StatusTextMessage = "Cancelled by User - Resume possible";
                        return;
                    }
                }
            }
            #endregion


            #region Optimizations
            else {
                if (_optMethod == null) throw new Exception("You did not start the solver. Please use the ResetSolver function for this.");

                // This is the first time we are running this solver
                double[] vector = Status == SolverStatus.Initialized ? ObjectiveFunction.GetStartPosition(StartPositionType) : null;

                BoundedBroydenFletcherGoldfarbShannoStatus? bbSolverStatus = null;
                AugmentedLagrangianStatus? agSolverStatus = null;
                CobylaStatus? cobSolverStatus = null;
                ConjugateGradientCode? conjgradSolverStatus = null;
                NelderMeadStatus? nedSolverStatus = null;
                ResilientBackpropagationStatus? relSolverStatus = null;

                try
                {
                    bool minResult;
                    minResult = vector != null ? OptMethod.Minimize(vector) : OptMethod.Minimize();

                    // Saves the value of the iteration
                    LastBestEvalValue = OptMethod.Value;

                    // Saves the status
                    if (OptMethod is BoundedBroydenFletcherGoldfarbShanno bbSolver) bbSolverStatus = bbSolver.Status;
                    if (OptMethod is AugmentedLagrangian agSolver) agSolverStatus = agSolver.Status;
                    if (OptMethod is Cobyla cobSolver) cobSolverStatus = cobSolver.Status;
                    if (OptMethod is ConjugateGradient conjGradSolver) conjgradSolverStatus = conjGradSolver.Status;
                    if (OptMethod is NelderMead nedSolver) nedSolverStatus = nedSolver.Status;
                    if (OptMethod is ResilientBackpropagation relSolver) relSolverStatus = relSolver.Status;
                }
                catch (SolverSuccessException e)
                {
                    LastBestEvalValue = e.FinalValue;

                    // Forces the status to be Success
                    if (OptMethod is BoundedBroydenFletcherGoldfarbShanno) bbSolverStatus = BoundedBroydenFletcherGoldfarbShannoStatus.FunctionConvergence;
                    if (OptMethod is AugmentedLagrangian) agSolverStatus = AugmentedLagrangianStatus.Converged;
                    if (OptMethod is Cobyla) cobSolverStatus = CobylaStatus.Success;
                    if (OptMethod is ConjugateGradient) conjgradSolverStatus = ConjugateGradientCode.Success;
                }

                // Now it depends on the type of the solver
                if (bbSolverStatus.HasValue)
                {
                    switch (bbSolverStatus.Value)
                    {
                        case BoundedBroydenFletcherGoldfarbShannoStatus.Stop:
                            if (CancelSource.Token.IsCancellationRequested)
                            {
                                Status = SolverStatus.Cancelled;
                                StatusTextMessage = "Cancelled by User - Resume possible";
                                return;
                            }
                            else
                            {
                                Status = SolverStatus.MaxEvaluations;
                                StatusTextMessage = "The B-BFGS solver is in stop state";
                                return;
                            }

                        case BoundedBroydenFletcherGoldfarbShannoStatus.MaximumIterations:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                            return;

                        case BoundedBroydenFletcherGoldfarbShannoStatus.FunctionConvergence:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Reached B-BFGS function convergence";
                            return;

                        case BoundedBroydenFletcherGoldfarbShannoStatus.GradientConvergence:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Reached B-BFGS gradient convergence";
                            return;

                        case BoundedBroydenFletcherGoldfarbShannoStatus.LineSearchFailed:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "The B-BFGS had a Line Search Failure (Gradient Function Issue)";
                            return;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (agSolverStatus.HasValue)
                {
                    switch (agSolverStatus.Value)
                    {
                        case AugmentedLagrangianStatus.Converged:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Converged";
                            return;

                        case AugmentedLagrangianStatus.NoProgress:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Maximum Progress Reached";
                            return;

                        case AugmentedLagrangianStatus.MaxEvaluations:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of evaluations";
                            return;

                        case AugmentedLagrangianStatus.Cancelled:
                            Status = SolverStatus.Cancelled;
                            StatusTextMessage = "Cancelled by User - Resume possible";
                            return;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (cobSolverStatus.HasValue)
                {
                    switch (cobSolverStatus.Value)
                    {
                        case CobylaStatus.Success:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Success";
                            return;

                        case CobylaStatus.MaxIterationsReached:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                            return;

                        case CobylaStatus.DivergingRoundingErrors:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Maximum Progress (Diverging rounding errors)";
                            return;

                        case CobylaStatus.NoPossibleSolution:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Maximum Progress (No possible solution)";
                            return;

                        case CobylaStatus.Cancelled:
                            Status = SolverStatus.Cancelled;
                            StatusTextMessage = "Cancelled by User - Resume possible";
                            return;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (conjgradSolverStatus.HasValue)
                {
                    switch (conjgradSolverStatus.Value)
                    {
                        case ConjugateGradientCode.Success:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Success";
                            return;

                        case ConjugateGradientCode.StepSize:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Invalid Step Size";
                            return;

                        case ConjugateGradientCode.DescentNotObtained:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Descent Direction Not Obtained";
                            return;

                        case ConjugateGradientCode.RoundingErrors:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Rounding errors prevent further progress";
                            return;

                        case ConjugateGradientCode.StepHigh:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - The step size has reached the upper bound";
                            return;

                        case ConjugateGradientCode.StepLow:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - The step size has reached the lower bound";
                            return;

                        case ConjugateGradientCode.MaximumEvaluations:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                            return;

                        case ConjugateGradientCode.Precision:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - Relative width of the interval of uncertainty is at machine precision";
                            return;

                        case ConjugateGradientCode.Cancelled:
                            Status = SolverStatus.Cancelled;
                            StatusTextMessage = "Cancelled by User - Resume possible";
                            return;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (nedSolverStatus.HasValue)
                {
                    switch (nedSolverStatus.Value)
                    {
                        case NelderMeadStatus.ForcedStop:
                            Status = SolverStatus.Cancelled;
                            StatusTextMessage = "Cancelled by User - Resume possible";
                            return;

                        case NelderMeadStatus.Success:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Success";
                            return;

                        case NelderMeadStatus.MaximumTimeReached:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - The execution time exceeded the established limit";
                            return;

                        case NelderMeadStatus.MinimumAllowedValueReached:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - The minimum desired value has been reached";
                            return;

                        case NelderMeadStatus.MaximumEvaluationsReached:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                            return;

                        case NelderMeadStatus.Failure:
                            Status = SolverStatus.NoProgress;
                            StatusTextMessage = "Finish - The algorithm failed internally";
                            return;

                        case NelderMeadStatus.FunctionToleranceReached:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - The desired output tolerance (minimum change in the function output between two consecutive iterations) has been reached";
                            return;

                        case NelderMeadStatus.SolutionToleranceReached:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - The desired parameter tolerance (minimum change in the solution vector between two iterations) has been reached";
                            return;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (relSolverStatus.HasValue)
                {
                    switch (relSolverStatus.Value)
                    {
                        case ResilientBackpropagationStatus.Finished:
                            Status = SolverStatus.Finished;
                            StatusTextMessage = "Finish - Success";
                            return;

                        case ResilientBackpropagationStatus.MaxIterations:
                            Status = SolverStatus.MaxEvaluations;
                            StatusTextMessage = "Maximum Evaluations - Reached maximum number of iterations";
                            return;

                        case ResilientBackpropagationStatus.Cancelled:
                            Status = SolverStatus.Cancelled;
                            StatusTextMessage = "Cancelled by User - Resume possible";
                            return;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            #endregion

            // Terminates the solver process
            if (FeModel != null)
            {
                FeModel.CloseApplication();
            }
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

        public virtual List<FeaSoftwareEnum> SupportedFeaSoftwares { get; } = new List<FeaSoftwareEnum>() { FeaSoftwareEnum.NoFea };
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
        ResilientBackpropagation
    }

    public enum SolverStatus
    {
        NotInitialized,
        Initialized,
        Running,
        NoProgress,
        MaxEvaluations,
        Finished,
        Cancelled
    }
}
