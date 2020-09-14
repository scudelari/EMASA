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
using Emasa_Optimizer.Opt;
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
            functionPnts_cvs.Filter += (inSender, inArgs) =>
            {
                if (inArgs.Item is SolutionPoint sPnt && sPnt.EvalType == EvalTypeEnum.ObjectiveFunction) inArgs.Accepted = true;
            };
            WpfFunctionPoints = functionPnts_cvs.View;

            // Sets the views for the gradient points
            CollectionViewSource gradientPnts_cvs = new CollectionViewSource() { Source = _solPoints };
            gradientPnts_cvs.Filter += (inSender, inArgs) =>
            {
                if (inArgs.Item is SolutionPoint sPnt && sPnt.EvalType == EvalTypeEnum.Gradient) inArgs.Accepted = true;
            };
            WpfGradientPoints = gradientPnts_cvs.View;

            // Sets the views for the section definition points
            CollectionViewSource sectionDefPnts_cvs = new CollectionViewSource() { Source = _solPoints };
            sectionDefPnts_cvs.Filter += (inSender, inArgs) =>
            {
                if (inArgs.Item is SolutionPoint sPnt && sPnt.EvalType == EvalTypeEnum.SectionDefinition) inArgs.Accepted = true;
            };
            WpfSectionPoints = sectionDefPnts_cvs.View;

            #endregion

            #region Initializes the Wpf Helpers
            
            #endregion
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
        public ICollectionView WpfSectionPoints { get; }
        #endregion

        #region Actions!
        public void NlOpt_SolveSelectedProblem(bool inUpdateInterface = false)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Starts the Fe Solver software
            string nlOpt_Folder = Path.Combine(Gh_Alg.GhDataDirPath, "NlOpt", "FeWork");
            switch (FeOptions.FeSolverType_Selected)
            {
                case FeSolverTypeEnum.Ansys:
                    FeSolver = new FeSolver_Ansys(nlOpt_Folder, this);
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

            sw.Stop();
            NlOpt_TotalSolveTimeSpan = sw.Elapsed;
        }
        #endregion

        #region TimeSpans
        private TimeSpan _nlOpt_TotalSolveTimeSpan = TimeSpan.Zero;
        public TimeSpan NlOpt_TotalSolveTimeSpan
        {
            get => _nlOpt_TotalSolveTimeSpan;
            set => SetProperty(ref _nlOpt_TotalSolveTimeSpan, value);
        }
        #endregion
    }
}
