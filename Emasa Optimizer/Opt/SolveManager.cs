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
using BaseWPFLibrary.Bindings;
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
using LiveCharts;
using NLoptNet;
using MPOC = MintPlayer.ObservableCollection;
using Prism.Mvvm;
using RhinoInterfaceLibrary;
using Sap2000Library;

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
        public bool Wpf_HaveNotStartedProblems => _problemConfigs.Any(p => p.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.NotStarted);
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
        public void WpfCommand_DeleteAllConfigurations()
        {
            foreach (ProblemConfig pc in ProblemConfigs) pc.Reset();
                ProblemConfigs.Clear();
        }

        private readonly MPOC.ObservableCollection<ProblemConfig> _problemConfigs = new MPOC.ObservableCollection<ProblemConfig>();
        private void ProblemConfigsOnCollectionChanged(object inSender, NotifyCollectionChangedEventArgs inE)
        {
            RaisePropertyChanged("Wpf_HaveNotStartedProblems");
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
                AppSS.I.BringListChildIntoView("OverlayProblemConfigurationList", _currentCalculatingProblemConfig, AppSS.GetReferencedFrameworkElement("CustomOverlay_ContentGrid"));

                // Clears the chart in the overlay
                AppSS.I.ChartDisplayMgr.ClearSeriesValues(AppSS.I.ChartDisplayMgr.CalculatingEvalPlot_CartesianChart);
            }
        }

        private ProblemConfig _wpf_CurrentlySelected_ProblemConfig;
        public ProblemConfig Wpf_CurrentlySelected_ProblemConfig
        {
            get => _wpf_CurrentlySelected_ProblemConfig;
            set
            {
                // We are setting it to a problem config that exists
                if (value != null)
                {
                    // It is the first time - sets the default visibility of the charts
                    if (_wpf_CurrentlySelected_ProblemConfig == null)
                    {
                        foreach (ChartDisplayData chartDisplay in value.ChartData)
                        {
                            if (chartDisplay.RelatedQuantity is Input_ParamDefBase inputParam)
                            {
                                // Shows on left
                                foreach (ChartDisplayData_Series t in chartDisplay.SeriesData)
                                {
                                    t.IsPairSelected_AxisOnLeftSide = true;
                                    t.IsPairSelected_AxisOnRightSide = false;
                                }
                            }
                            else if (chartDisplay.RelatedQuantity is ProblemQuantity probConf)
                            {
                                if (probConf.IsObjectiveFunctionMinimize)
                                {
                                    // Shows on right side
                                    foreach (ChartDisplayData_Series t in chartDisplay.SeriesData)
                                    {
                                        t.IsPairSelected_AxisOnLeftSide = false;
                                        t.IsPairSelected_AxisOnRightSide = true;
                                    }
                                }
                                else
                                {
                                    // Both not visible
                                    foreach (ChartDisplayData_Series t in chartDisplay.SeriesData)
                                    {
                                        t.IsPairSelected_AxisOnLeftSide = false;
                                        t.IsPairSelected_AxisOnRightSide = false;
                                    }
                                }
                            }
                        }
                    }
                    else
                    { // Copies the existing visibility
                        foreach (ChartDisplayData currentDisplay in _wpf_CurrentlySelected_ProblemConfig.ChartData)
                        {
                            // Finds the right chart data in the new set
                            ChartDisplayData newDisplay = value.ChartData.FirstOrDefault(a => a.RelatedQuantity == currentDisplay.RelatedQuantity);
                            if (newDisplay != null)
                            {
                                for (int i = 0; i < newDisplay.SeriesData.Length; i++)
                                {
                                    newDisplay.SeriesData[i].IsPairSelected_AxisOnLeftSide = currentDisplay.SeriesData[i].IsPairSelected_AxisOnLeftSide;
                                    newDisplay.SeriesData[i].IsPairSelected_AxisOnRightSide = currentDisplay.SeriesData[i].IsPairSelected_AxisOnRightSide;
                                }
                            }
                        }
                    }
                }

                // Effectively selects the problem config
                SetProperty(ref _wpf_CurrentlySelected_ProblemConfig, value);

                // Clears the Eval chart
                AppSS.I.ChartDisplayMgr.ClearSeriesValues(AppSS.I.ChartDisplayMgr.PointPlotSummaryEvalPlot_CartesianChart);
                // Fills the Objective function eval chart
                AppSS.I.ChartDisplayMgr.AddSeriesValue(AppSS.I.ChartDisplayMgr.PointPlotSummaryEvalPlot_CartesianChart, Wpf_CurrentlySelected_ProblemConfig?.ObjectiveFunctionEvals);

                // Updates the Problem Config Quantity Plots
                AppSS.I.ChartDisplayMgr.UpdateChart(AppSS.I.ChartDisplayMgr.ProblemConfigDetailPlot_CartesianChart, Wpf_CurrentlySelected_ProblemConfig.ChartData);

                // Makes sure there is a point that is selected
                if (Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint == null) Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint = Wpf_CurrentlySelected_ProblemConfig.LastPoint;
                // Ensures visibility of the point in the list
                if (Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint != null)
                    AppSS.I.BringListChildIntoView( AppSS.GetReferencedFrameworkElement("NlOptPointDetails_PointSelectionList"), Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint);
            }
        }

        public void Wpf_SelectBestEvalProblemConfig()
        {
            // Attempts to select the best problem config that converged
            ProblemConfig toSelect = ProblemConfigs
                .Where(a => a.LastPoint != null && a.LastPoint.AllConstraintsRespected &&
                            (a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Converged ||
                             a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Success))
                .OrderBy(a => a.LastPoint.ObjectiveFunctionEval)
                .FirstOrDefault();

            // Selects the best if found, otherwise selects the first
            Wpf_CurrentlySelected_ProblemConfig = toSelect ?? ProblemConfigs.First();
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

        public double AverageTotalSeconds_ConfigurationElapsedTime => _problemConfigs.Count > 0 ? _problemConfigs.Average(a => a.ProblemConfigOptimizeElapsedTime.TotalSeconds) : 0d;
        
        #region Actions!
        private DateTime LastOptimizationFinishTime;
        public async void WpfCommand_OptimizeMissingProblemConfigurations()
        {
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                AppSS.I.Overlay_Reset();
                CustomOverlayBindings.I.ShowOverlay();

                void lf_Work()
                {
                    // Starts the Optimize Manager, initializing the connection with GrassHopper and other variables
                    CustomOverlayBindings.I.Title = "Working";


                    // The missing problem configurations are:
                    List<ProblemConfig> toOptimize = (from a in _problemConfigs
                                                      where a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.NotStarted ||
                                                            a.NlOptSolverWrapper.OptimizeTerminationException.OptimizeTerminationCode == NlOpt_OptimizeTerminationCodeEnum.Forced_Stop
                                                      select a).ToList();

                    if (toOptimize.Count == 0) return;

                    // Resets the cancellation source
                    AppSS.I.SolveMgr.CancelSource = new CancellationTokenSource();

                    // Instantiates the Fe Solver software
                    AppSS.I.FeOpt.EvaluateRequiredFeOutputs();
                    switch (AppSS.I.FeOpt.FeSolverType_Selected)
                    {
                        case FeSolverTypeEnum.Ansys:
                            AppSS.I.FeSolver = new FeSolverBase_Ansys();
                            break;

                        case FeSolverTypeEnum.Sap2000:
                            AppSS.I.FeSolver = new FeSolverBase_Sap2000();
                            AppSS.I.LockedInterfaceMessageVisibility = Visibility.Visible;
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
                        // Terminates the Finite Element Solver
                        AppSS.I.FeSolver?.Dispose();
                        AppSS.I.FeSolver = null;

                        // Returns Rhino to its default view state
                        RhinoModel.RM.RestoreRhinoViewFromImageAcquire();
                    }
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                CurrentCalculatingProblemConfig = null;

                // Saves the total elapsed time
                sw.Stop();
                TotalOptimizationElapsedTime = sw.Elapsed;
                RaisePropertyChanged("AverageTotalSeconds_ConfigurationElapsedTime");


                // Makes some default configurations in the views
                
                // Selects the Problem config with best eval
                Wpf_SelectBestEvalProblemConfig();

                // Forces a selection of the output quantity in the point details report to be the first used at the objective function
                AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay = AppSS.I.ProbQuantMgn.WpfProblemQuantities_ObjectiveFunction.OfType<ProblemQuantity>().First().QuantitySource;
                // Ensures visibility of the item in the list
                AppSS.I.BringListChildIntoView( AppSS.GetReferencedFrameworkElement("NlOptPointDetails_AvailableQuantitiesSelectionList"), AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay);
                

                // Selects the results tab item
                if (AppSS.FirstReferencedWindow is MainWindow mw)
                {
                    mw.ResultsTabItem.IsSelected = true;
                    mw.ResultsTab_SummarySubTabItem.IsSelected = true;
                }

                // Saves the finish time - this is a marker for outputs
                LastOptimizationFinishTime = DateTime.Now;

                // Closes the overlay
                CustomOverlayBindings.I.HideOverlayAndReset();
            }
        }
        #endregion

        #region Pointwise Model Creation
        public async void WpfCommand_PointwiseModelShape_ToRhino()
        {
            try
            {
                // Is it really selected?
                if (Wpf_CurrentlySelected_ProblemConfig == null) throw new Exception($"There is no selected Problem Config.");
                if (Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint == null) throw new Exception($"There is no selected Point.");

                BusyOverlayBindings.I.Title = $"Updating Grasshopper Geometry for Problem Config #{Wpf_CurrentlySelected_ProblemConfig.Index} - Point #{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex}";
                BusyOverlayBindings.I.SetIndeterminate("Updating Grasshopper.");
                BusyOverlayBindings.I.ShowOverlay();

                void lf_Work()
                {
                    AppSS.I.Gh_Alg.UpdateGrasshopperGeometry(Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint);
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                // Closes the overlay
                BusyOverlayBindings.I.HideOverlayAndReset();
            }
        }
        
        private string DataOutputFolderRoot => Path.Combine(Path.GetDirectoryName(AppSS.I.Gh_Alg.GrasshopperFullFileName), Path.GetFileNameWithoutExtension(AppSS.I.Gh_Alg.GrasshopperFullFileName), "Output", $"{LastOptimizationFinishTime:yyyy_MM_dd_HH_mm}");
        public async void WpfCommand_PointwiseModelShape_ToSap2000()
        {
            try
            {
                // Is SAP2000 Alive?
                //if (S2KModel.SM.IsAlive) throw new Exception($"There must be no instance of SAP2000 linked to this application. Please close SAP2000.");

                // Is it really selected?
                if (Wpf_CurrentlySelected_ProblemConfig == null) throw new Exception($"There is no selected Problem Config.");
                if (Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint == null) throw new Exception($"There is no selected Point.");

                string pointModelPath = Path.Combine(DataOutputFolderRoot, $"Prob#{Wpf_CurrentlySelected_ProblemConfig.Index:D2} P#{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex:D5}");
                if (!Directory.Exists(pointModelPath)) Directory.CreateDirectory(pointModelPath);

                string pointFileName = $"SAP2000 Prob#{Wpf_CurrentlySelected_ProblemConfig.Index:D2} P#{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex:D5}";

                BusyOverlayBindings.I.Title = $"Generating SAP2000 model for #{Wpf_CurrentlySelected_ProblemConfig.Index} - Point #{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex}";
                BusyOverlayBindings.I.SetIndeterminate("Generating SAP2000.");
                BusyOverlayBindings.I.ShowOverlay();

                void lf_Work()
                {
                    FeSolverBase_Sap2000 sapSolverBase = new FeSolverBase_Sap2000();
                    sapSolverBase.GeneratePointModel(Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.FeModel, pointModelPath, pointFileName);
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                // Closes the overlay
                BusyOverlayBindings.I.HideOverlayAndReset();
            }
        }
        public async void WpfCommand_PointwiseModelShape_ToAnsys()
        {
            try
            {
                FeSolverBase_Ansys solverAnsys = new FeSolverBase_Ansys();
                // Is Ansys Alive?
                if (solverAnsys.GetAllRunningProcesses().Count > 0) throw new Exception($"There must be no instance of Ansys linked to this application. Please close Ansys.");

                // Is it really selected?
                if (Wpf_CurrentlySelected_ProblemConfig == null) throw new Exception($"There is no selected Problem Config.");
                if (Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint == null) throw new Exception($"There is no selected Point.");

                string pointModelPath = Path.Combine(DataOutputFolderRoot, $"Prob#{Wpf_CurrentlySelected_ProblemConfig.Index:D2} P#{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex:D5}");
                if (!Directory.Exists(pointModelPath)) Directory.CreateDirectory(pointModelPath);

                string pointFileName = $"Ansys Prob#{Wpf_CurrentlySelected_ProblemConfig.Index:D2} P#{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex:D5}";

                BusyOverlayBindings.I.Title = $"Generating Ansys model for #{Wpf_CurrentlySelected_ProblemConfig.Index} - Point #{Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.PointIndex}";
                BusyOverlayBindings.I.SetIndeterminate("Generating Ansys.");
                BusyOverlayBindings.I.ShowOverlay();

                void lf_Work()
                {
                    solverAnsys.GeneratePointModel(Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.FeModel, pointModelPath, pointFileName);
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                // Closes the overlay
                BusyOverlayBindings.I.HideOverlayAndReset();
            }
        }
        #endregion

        #region Report During Calculation 


        #endregion
    }
}
