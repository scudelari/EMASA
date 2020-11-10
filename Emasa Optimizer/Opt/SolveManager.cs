using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using NLoptNet;
using MPOC = MintPlayer.ObservableCollection;
using Prism.Mvvm;
using RhinoInterfaceLibrary;

namespace Emasa_Optimizer.Opt
{
    public class SolveManager : BindableBase
    {
        public SolveManager()
        {
            // Adds listeners to the problem configs
            _problemConfigs.CollectionChanged += ProblemConfigsOnCollectionChanged;

            Wpf_ProblemConfigs_View = CollectionViewSource.GetDefaultView(ProblemConfigs);

            //CollectionViewSource ProblemConfigs_Finalized_cvs = new CollectionViewSource() { Source = ProblemConfigs };
            //Wpf_OptimizedProblemConfigs_View = ProblemConfigs_Finalized_cvs.View;
            //Wpf_OptimizedProblemConfigs_View.Filter += inO => inO is ProblemConfig pc && pc.NlOptSolverWrapper.OptimizeTerminationException != null;
            //Wpf_OptimizedProblemConfigs_View.CollectionChanged += (inSender, inArgs) =>
            //{
            //    RaisePropertyChanged("Wpf_ConfigurationCount_NotOptimized");
            //    RaisePropertyChanged("Wpf_ConfigurationCount_Optimized");
            //};
            //// Setting live shaping
            //if (Wpf_OptimizedProblemConfigs_View is ICollectionViewLiveShaping ls)
            //{
            //    ls.LiveFilteringProperties.Add("NlOptSolverWrapper.OptimizeTerminationException");
            //    ls.IsLiveFiltering = true;
            //}
            //else throw new Exception($"List does not accept ICollectionViewLiveShaping.");

        }
        
        #region Problem Configuration Data & Management
        public bool Wpf_ConfigurationsHaveBeenGenerated => _problemConfigs.Count > 0;
        public int Wpf_ConfigurationCount_Generated => ProblemConfigs.Count;
        public int Wpf_ConfigurationCount_Successful => ProblemConfigs.Count(a => a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Success || a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Converged);

        public void WpfCommand_GenerateConfigurations_AndValidateForm()
        {
            try
            {
                #region Validating Form
                // Validating the form - Do we have one for Objective Function?
                if (!AppSS.I.ProbQuantMgn.WpfProblemQuantities_ObjectiveFunction.OfType<ProblemQuantity>().Any())
                {
                    MessageBox.Show("You must add at least one quantity to be used as Objective Function.", "Objective Function", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Validating the form - More than one Equality Constraints?
                if (AppSS.I.ProbQuantMgn.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>().Count(a => a.IsConstraint && a.ConstraintObjective == Quantity_ConstraintObjectiveEnum.EqualTo) > 1)
                {
                    MessageBox.Show($"The maximum number of equal constraints is 1.{Environment.NewLine}Add higher than or lower than instead.", "Equality Constraints", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Validating the form - Optimizer Type - Constraint Validity
                if (!AppSS.I.ProbQuantMgn.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>().Any())
                {
                    if (AppSS.I.NlOptOpt.UseLagrangian)
                    {
                        AppSS.I.NlOptOpt.UseLagrangian = false; // Forces false.
                    }
                }
                else // We have constraints
                {
                    if (!AppSS.I.NlOptOpt.UseLagrangian)
                    {
                        // Checks if we have equality of inequality constraints
                        bool hasEqualConstraint = false;
                        bool hasUnequalConstraint = false;

                        // Sets up the constraints
                        foreach (ProblemQuantity constraintQuantity in AppSS.I.ProbQuantMgn.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>())
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

                        switch (AppSS.I.NlOptOpt.NlOptSolverType)
                        {
                            case NLoptAlgorithm.LN_COBYLA: // Both
                            case NLoptAlgorithm.GN_ISRES: // Both
                            case NLoptAlgorithm.LD_SLSQP: // Both
                                // They are OK
                                break;

                            case NLoptAlgorithm.LD_CCSAQ:   // NE Only
                            case NLoptAlgorithm.GN_AGS:  // NE Only
                            case NLoptAlgorithm.LD_MMA: // NE Only
                                MessageBox.Show($"{ListDescSH.I.NlOptAlgorithmEnumDescriptions[AppSS.I.NlOptOpt.NlOptSolverType]} does not support equality constraints by itself (only inequality). You can use Augmented Lagrangian option to embed the constraint into the objective function.", "Constraints", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;

                            default:
                                MessageBox.Show($"{ListDescSH.I.NlOptAlgorithmEnumDescriptions[AppSS.I.NlOptOpt.NlOptSolverType]} does not support any type of constraints by itself. You can use Augmented Lagrangian option to embed the constraint into the objective function.", "Constraints", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                        }
                    }
                }
                #endregion

                #region Generating the Problem Config List
                int problemConfigIndex = 0;

                // Combines the lists of lines and integers
                List<IProblemConfig_CombinableVariable> combVars = new List<IProblemConfig_CombinableVariable>();
                combVars.AddRange(AppSS.I.Gh_Alg.GeometryDefs_LineList_View.OfType<LineList_GhGeom_ParamDef>());
                combVars.AddRange(AppSS.I.Gh_Alg.ConfigDefs_Integer_View.OfType<Integer_GhConfig_ParamDef>());

                // Checks if all have at least one option
                foreach (IProblemConfig_CombinableVariable combVar in combVars)
                {
                    if (!combVar.FlatCombinationList.Any())
                    {
                        throw new InvalidOperationException($"All configuration variable must have at least one option.{Environment.NewLine}{combVar.Name} is lacking options.");
                    }
                }

                // Gets a List of possible values for each variable 
                // Example: LineList 1 => FeSectionA, FeSectionB
                //          Integers => 1,2,3
                IEnumerable<IEnumerable<ProblemConfig_ElementCombinationValue>> possiblePerVariable = from a in combVars select a.FlatCombinationList;

                IEnumerable<IEnumerable<ProblemConfig_ElementCombinationValue>> allPossibleCombos = EmasaWPFLibraryStaticMethods.GetAllPossibleCombos(possiblePerVariable);

                // Gets the UNIQUE combinations - Heavily based on the Comparer found in ProblemConfig_ElementCombinationValue 
                HashSet<List<ProblemConfig_ElementCombinationValue>> uniqueCombs = new HashSet<List<ProblemConfig_ElementCombinationValue>>(new ProblemConfig_ElementCombinationValue.ProblemConfig_ElementCombinationValue_ListComparer());
                foreach (IEnumerable<ProblemConfig_ElementCombinationValue> possibleCombo in allPossibleCombos)
                {
                    List<ProblemConfig_ElementCombinationValue> possibleComboAsList = possibleCombo.ToList();
                    possibleComboAsList.Sort(new ProblemConfig_ElementCombinationValue.ProblemConfig_ElementCombinationValue_Comparer());
                    uniqueCombs.Add(possibleComboAsList);
                }

                // Adds the problem configs to the list
                ProblemConfigs.AddRange(uniqueCombs.Select(comb => new ProblemConfig(comb, problemConfigIndex++)));
                #endregion
            }
            catch (Exception e)
            {
                ExceptionViewer.Show(e);
            }
        }
        public void WpfCommand_ClearConfigurations()
        {
            ProblemConfigs.Clear();
        }

        private readonly MPOC.ObservableCollection<ProblemConfig> _problemConfigs = new MPOC.ObservableCollection<ProblemConfig>();
        private void ProblemConfigsOnCollectionChanged(object inSender, NotifyCollectionChangedEventArgs inE)
        {
            RaisePropertyChanged("Wpf_ConfigurationsHaveBeenGenerated");
            RaisePropertyChanged("Wpf_ConfigurationCount_Generated");
        }
        public MPOC.ObservableCollection<ProblemConfig> ProblemConfigs => _problemConfigs;
        public ICollectionView Wpf_ProblemConfigs_View { get; }

        private ProblemConfig _currentCalculatingProblemConfig;
        public ProblemConfig CurrentCalculatingProblemConfig
        {
            get => _currentCalculatingProblemConfig;
            set
            {
                SetProperty(ref _currentCalculatingProblemConfig, value);

                // Tries to make it visible in the list
                AppSS.I.BringListChildIntoView("OverlayProblemConfigurationList", _currentCalculatingProblemConfig, "BusyOptimizingOverlay");
            }
        }

        private ProblemConfig _wpf_CurrentlySelected_ProblemConfig;
        public ProblemConfig Wpf_CurrentlySelected_ProblemConfig
        {
            get => _wpf_CurrentlySelected_ProblemConfig;
            set => SetProperty(ref _wpf_CurrentlySelected_ProblemConfig, value);
        }

        private ProblemConfig _wpf_BestEval_ProblemConfig;
        public ProblemConfig Wpf_BestEval_ProblemConfig
        {
            get => _wpf_BestEval_ProblemConfig;
            set => SetProperty(ref _wpf_BestEval_ProblemConfig, value);
        }
        public void Update_Wpf_BestEval_ProblemConfig()
        {
            Wpf_BestEval_ProblemConfig = ProblemConfigs
                .Where(a => a.LastPoint.AllConstraintsRespected &&
                            (a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Converged ||
                             a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Success))
                .OrderBy(a => a.LastPoint.ObjectiveFunctionEval)
                .FirstOrDefault();
        }
        #endregion

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
        public CancellationTokenSource CancelSource
        {
            get => _cancelSource;
            set => _cancelSource = value ?? throw new InvalidOperationException($"{MethodBase.GetCurrentMethod()} does not accept null values.");
        }

        private TimeSpan _totalOptimizationElapsedTime;
        public TimeSpan TotalOptimizationElapsedTime
        {
            get => _totalOptimizationElapsedTime;
            set => SetProperty(ref _totalOptimizationElapsedTime, value);
        }

        public double AverageTotalSeconds_ConfigurationElapsedTime => _problemConfigs.Average(a => a.ProblemConfigOptimizeElapsedTime.TotalSeconds);
        
        #region Actions!
        public void OptimizeMissingProblemConfigurations()
        {
            // The missing problem configurations are:
            List<ProblemConfig> toOptimize = (from a in _problemConfigs
                where a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.NotStarted ||
                      a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Forced_Stop
                select a).ToList();

            if (toOptimize.Count == 0) return;

            Stopwatch sw = Stopwatch.StartNew();

            // Resets the cancellation source
            AppSS.I.SolveMgr.CancelSource = new CancellationTokenSource();

            // Instantiates the Fe Solver software
            AppSS.I.FeOpt.EvaluateRequiredFeOutputs();
            switch (AppSS.I.FeOpt.FeSolverType_Selected)
            {
                case FeSolverTypeEnum.Ansys:
                    AppSS.I.FeSolver = new FeSolverBase_Ansys();
                    break;

                case FeSolverTypeEnum.NotFeProblem:
                    AppSS.I.FeSolver = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            try
            {
                // Prepares Rhino for Screenshot output
                RhinoModel.RM.PrepareRhinoViewForImageAcquire();

                // Sets the progress bar
                AppSS.I.Overlay_ProgressBarMaximum = toOptimize.Count;
                AppSS.I.Overlay_ProgressBarIndeterminate = false;

                // Sets the default selections for the screenshots
                AppSS.I.ScreenShotOpt.SelectedDisplayImageResultClassification = AppSS.I.ScreenShotOpt.Wpf_ScreenshotList.OfType<IProblemQuantitySource>().FirstOrDefault();
                AppSS.I.ScreenShotOpt.SelectedDisplayDirection = AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.FirstOrDefault();

                // Runs optimizations for missing Problem Configurations
                for (int i = 0; i < toOptimize.Count; i++)
                {
                    AppSS.I.Overlay_ProgressBarCurrent = i;

                    ProblemConfig pc = toOptimize[i];

                    // Sets the current problem configuration
                    CurrentCalculatingProblemConfig = pc;
                    CurrentCalculatingProblemConfig.Optimize();

                    // User cancelled
                    if (CancelSource.IsCancellationRequested) break;
                }
            }
            finally
            {
                Update_Wpf_BestEval_ProblemConfig();

                CurrentCalculatingProblemConfig = null;

                // Terminates the Finite Element Solver
                AppSS.I.FeSolver?.Dispose();
                AppSS.I.FeSolver = null;

                // Returns Rhino to its default view state
                RhinoModel.RM.RestoreRhinoViewFromImageAcquire();

                // Saves the total elapsed time
                sw.Stop();
                TotalOptimizationElapsedTime = sw.Elapsed;
                
                RaisePropertyChanged("AverageTotalSeconds_ConfigurationElapsedTime");
            }
        }
        #endregion

        #region Report During Calculation 


        #endregion
    }
}
