using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.ProblemDefs;
using NLoptNet;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt
{
    public class SolveManager : BindableBase
    {
        private readonly GhAlgorithm _gh_alg;
        public GhAlgorithm Gh_Alg { get => _gh_alg; }

        private readonly NloptManager _nloptManager;
        public NloptManager NlOptManager { get => _nloptManager; }

        private FeSolver _feSolver;
        public FeSolver FeSolver
        {
            get => _feSolver;
            set => SetProperty(ref _feSolver, value);
        }
        
        public SolveManager()
        {
            _gh_alg = new GhAlgorithm(this);
            _nloptManager = new NloptManager(this);
            _feOptions = new FeOptions(this);

            #region Decides on the available problems
            List<GeneralProblem> problems = new List<GeneralProblem>
                {
                new GeneralProblem(this),
                new TriangleProblem(this)
                };

            // Filters to those problems that actually target the current Gh
            problems = problems.Where(a => a.TargetsOpenGhAlgorithm).ToList();

            // If any of those overrides the general, removes the general from the list
            if (problems.Any(a => a.OverridesGeneralProblem))
            {
                problems = problems.Where(a => a.GetType().Name != nameof(GeneralProblem)).ToList();
            }

            // Saves it into the list
            _availableProblems = problems;

            // Builds the interface view for the problem list
            CollectionViewSource problem_cvs = new CollectionViewSource() { Source = _availableProblems };
            problem_cvs.SortDescriptions.Add(new SortDescription("WpfFriendlyName", ListSortDirection.Ascending));
            WpfAvailableProblems = problem_cvs.View;

            // Gets the default selection
            GeneralProblem genProblemInList = problems.FirstOrDefault(a => a.GetType().Name == nameof(GeneralProblem));
            WpfSelectedProblem = genProblemInList ?? problems.First();
            #endregion

            #region Initializes the Solution Points
            _solPoints = new FastObservableCollection<SolutionPoint>();

            // Sets the views for the function points
            CollectionViewSource functionPnts_cvs = new CollectionViewSource() {Source = _solPoints };
            WpfFunctionPoints = functionPnts_cvs.View;
            WpfFunctionPoints.Filter += inO => inO is SolutionPoint sPnt && sPnt.SolutionPointCalcType == SolutionPointCalcTypeEnum.ObjectiveFunction;


            // Sets the views for the gradient points
            CollectionViewSource gradientPnts_cvs = new CollectionViewSource() { Source = _solPoints };
            WpfGradientPoints = gradientPnts_cvs.View;
            WpfGradientPoints.Filter += inO => inO is SolutionPoint sPnt && sPnt.SolutionPointCalcType == SolutionPointCalcTypeEnum.Gradient;

            #endregion

            #region Initializes the Problem Quantities Views
            CollectionViewSource problemQuantities_All_cvs = new CollectionViewSource() { Source = _problemQuantities};
            WpfProblemQuantities_All = problemQuantities_All_cvs.View;

            CollectionViewSource problemQuantities_OutputOnly_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_OutputOnly = problemQuantities_OutputOnly_cvs.View;
            WpfProblemQuantities_OutputOnly.Filter += inO => (inO is ProblemQuantity pq && pq.IsOutputOnly);

            CollectionViewSource problemQuantities_ObjectiveFunction_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_ObjectiveFunction = problemQuantities_ObjectiveFunction_cvs.View;
            WpfProblemQuantities_ObjectiveFunction.Filter += inO => (inO is ProblemQuantity pq && pq.IsObjectiveFunctionMinimize);

            CollectionViewSource problemQuantities_Constraints_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_Constraint = problemQuantities_Constraints_cvs.View;
            WpfProblemQuantities_Constraint.Filter += inO => (inO is ProblemQuantity pq && pq.IsConstraint);
            #endregion

            CollectionViewSource problemQuantities_AvailableTypes_cvs = new CollectionViewSource() { Source = ProblemQuantityAvailableTypes };
            WpfProblemQuantityAvailableTypes = problemQuantities_AvailableTypes_cvs.View;
            WpfProblemQuantityAvailableTypes.SortDescriptions.Add(new SortDescription("IsFiniteElementData", ListSortDirection.Ascending));
        }
        
        #region Finite Element Problem Options
        private FeOptions _feOptions;
        public FeOptions FeOptions
        {
            get => _feOptions;
            set => SetProperty(ref _feOptions, value);
        }
        #endregion

        #region Available Problem Management
        private readonly List<GeneralProblem> _availableProblems;
        public ICollectionView WpfAvailableProblems { get; }
        public bool IsOnlyOneAvailableProblem => _availableProblems.Count == 1;
        private GeneralProblem _wpfSelectedProblem;
        public GeneralProblem WpfSelectedProblem
        {
            get => _wpfSelectedProblem;
            set => SetProperty(ref _wpfSelectedProblem, value);
        } 
        #endregion
        
        #region NlOpt Optimization Point Management
        private readonly FastObservableCollection<SolutionPoint> _solPoints;
        public void AddSolutionPoint(SolutionPoint inPoint)
        {
            inPoint.PointIndex = _solPoints.Count;
            _solPoints.Add(inPoint);
        }
        public SolutionPoint GetSolutionPointIfExists(double[] inPointVars)
        {
            // Will return the first it finds; or null if failed.
            return _solPoints.FirstOrDefault(a => a.InputValuesAsDoubleArray.SequenceEqual(inPointVars));
        }
        public ICollectionView WpfFunctionPoints { get; }
        public ICollectionView WpfGradientPoints { get; }
        #endregion

        #region Actions!
        public void NlOpt_SolveSelectedProblem(bool inUpdateInterface = false)
        {
            // Clears the current solution list
            _solPoints.Clear();

            // Starts the Fe Solver software
            FeOptions.EvaluateRequiredFeOutputs();
            string nlOpt_Folder = Path.Combine(Gh_Alg.GhDataDirPath, "NlOpt", "FeWork");
            switch (FeOptions.FeSolverType_Selected)
            {
                case FeSolverTypeEnum.Ansys:
                    FeSolver = new FeSolver_Ansys(nlOpt_Folder, this);
                    break;

                case FeSolverTypeEnum.NotFeProblem:
                    FeSolver = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            try
            {
                NlOptManager.SolveSelectedProblem(inUpdateInterface);
            }
            finally
            {
                FeSolver?.Dispose();
                FeSolver = null;
            }


        }
        #endregion





        #region Problem Quantity Available Types
        public FastObservableCollection<IProblemQuantitySource> ProblemQuantityAvailableTypes { get; } = new FastObservableCollection<IProblemQuantitySource>();
        public ICollectionView WpfProblemQuantityAvailableTypes { get; }
        #endregion

        #region Problem Quantities Selection

        public int ProblemQuantityMaxIndex { get; set; } = 1;

        private readonly FastObservableCollection<ProblemQuantity> _problemQuantities = new FastObservableCollection<ProblemQuantity>();
        public ICollectionView WpfProblemQuantities_OutputOnly { get; }
        public ICollectionView WpfProblemQuantities_ObjectiveFunction { get; }
        public ICollectionView WpfProblemQuantities_Constraint { get; }
        public ICollectionView WpfProblemQuantities_All { get; }
        public void AddProblemQuantity(ProblemQuantity inProblemQuantity)
        {
            _problemQuantities.Add(inProblemQuantity);
            //WpfProblemQuantities_ObjectiveFunction.Refresh();
        }
        public void DeleteProblemQuantity(ProblemQuantity inProblemQuantity)
        {
            _problemQuantities.Remove(inProblemQuantity);
            //WpfProblemQuantities_ObjectiveFunction.Refresh();
        }

        public void AddAllProblemQuantity_OutputOnly()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                ghDoubleList.AddProblemQuantity_OutputOnly(this);
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in FeOptions.WpfAllSelectedOutputResults.OfType<FeResultClassification>())
            {
                feResultClassification.AddProblemQuantity_OutputOnly(this);
            }
        }
        public void AddAllProblemQuantity_ConstraintObjective()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                ghDoubleList.AddProblemQuantity_ConstraintObjective(this);
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in FeOptions.WpfAllSelectedOutputResults.OfType<FeResultClassification>())
            {
                feResultClassification.AddProblemQuantity_ConstraintObjective(this);
            }
        }
        public void AddAllProblemQuantity_FunctionObjective()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                ghDoubleList.AddProblemQuantity_FunctionObjective(this);
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in FeOptions.WpfAllSelectedOutputResults.OfType<FeResultClassification>())
            {
                feResultClassification.AddProblemQuantity_FunctionObjective(this);
            }
        }
        #endregion
    }
}
