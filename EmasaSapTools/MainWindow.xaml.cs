using EmasaSapTools.Bindings;
using EmasaSapTools.DataGridTypes;
using EmasaSapTools.Monitors;
using EmasaSapTools.Optimization;
using EmasaSapTools.Resources;
using BaseWPFLibrary;
using RhinoInterfaceLibrary;
using ExcelDataReader;
using LibOptimization.Util;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Microsoft.Win32;
using MoreLinq;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.DataClasses.Results;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using Prism.Events;
using Excel = Microsoft.Office.Interop.Excel;

namespace EmasaSapTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Window Bindings and Helpers
        [Obsolete]
        private void CreateAndBind()
        {
            //FormBasicRefreshingBindings.Start(MainTabControl, false);

            //// Fixed bindigns and Actions - Good to go!
            //LoadCasesSolverOptionsBindings.Start(NonLinearCases_GroupBox);
            
            
            //ExtendOrShrinkLineBindings.Start(ExtendOrShrinkLineGroupBox);
            //AlignAreaBindings.Start(AlignAreaGroupBox);
            //AlignFrameBindings.Start(AlignFrameGroupBox);
            //AlignJointBindings.Start(AlignJointsGroupBox);
            
            //RenameItemsBindings.Start(Rename_TabItem_Panel);
            //CreateSpliceBindings.Start(CreateSpliceGroupBox);
            //SelectionInfoBindings.Start(Selection_TabItem_Panel);
            ////BreakFrameBindings.Start(BreakFrames_GroupBox);
            //SQLiteBindings.Start(SQLiteOperations_TabItem_Panel);


            //ManipulateItemsBindings.Start(ManipulateItems_TabItem);
            //TrussSolverBindings.Start(TrussSolverTabItem);
            
            // These are the bindings of the controls that must be refreshed, such as group lists
            //ResultsDisplacements_OutputCases_ListBox.DataContext = FormBasicRefreshingBindings.I;
            //ResultsDisplacements_Groups_ListBox.DataContext = FormBasicRefreshingBindings.I;
            //ParallelGhostStifferGroupComboBox.DataContext = FormBasicRefreshingBindings.I;
            ManipulateItems_Substitute_LinksToFrames_FrameSection.DataContext = FormBasicRefreshingBindings.I;
            //TrusSolver_LoadCase_ComboBox.DataContext = FormBasicRefreshingBindings.I;
        }



        /// <summary>
        /// This is a wildcard function that will make the initialization of a bindable object.
        /// It supports the inclusion the the Bound to reference, automatic linking of the binding object to the WPF FrameworkElement
        /// and the automatic inclusion of event responses
        /// </summary>
        /// <typeparam name="T">The type of the bindable object.</typeparam>
        /// <param name="inFe">The WPF FrameworkElement object that will be linked to this BindableSingleton object.</param>
        /// <param name="inSetToContext">Whether to set this BindableSingleton object to the DataContext of the FrameworkElement.</param>
        /// <param name="inBindable_CommandStartedHandler">The handler for the events of when the BindableSingleton object starts a command.</param>
        /// <param name="inBindable_CommandFinishedHandler">The handler for the events of when the BindableSingleton object finishes a command.</param> 
        private void CreateBindableType<T>(FrameworkElement inFe, bool inSetToContext = true, EventHandler inBindable_CommandStartedHandler= null, EventHandler inBindable_CommandFinishedHandler = null)
        {
            Type t = typeof(T);
            Type tBind = typeof(BindableSingleton<>);

            if (t.BaseType != tBind)
                throw new InvalidOperationException($"Type {t.Name} does not directly inherits the {tBind.Name} Type.");

            MethodInfo startMethod = t.GetMethod("Start");
            try
            {
                startMethod.Invoke(null, new object[] { inFe, inSetToContext });
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Could not call Start static function of type {t.Name}.", e);
            }

            PropertyInfo IProp = t.GetProperty("I");
            object BindableInstance;
            try
            {
                BindableInstance = IProp.GetMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Could not get the singleton instance of type {t.Name}.", e);
            }

            if (inBindable_CommandStartedHandler != null)
            {
                EventInfo startEvent = t.GetEvent("CommandStarted");
                try
                {
                    startEvent.AddEventHandler(BindableInstance, inBindable_CommandStartedHandler);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Could not attach the {inBindable_CommandStartedHandler.ToString()} delegate as an event handler to the CommandStarted event of the type {t.Name}.", e);
                }
            }

            if (inBindable_CommandFinishedHandler != null)
            {
                EventInfo startEvent = t.GetEvent("CommandFinished");
                try
                {
                    startEvent.AddEventHandler(BindableInstance, inBindable_CommandFinishedHandler);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Could not attach the {inBindable_CommandFinishedHandler.ToString()} delegate as an event handler to the CommandFinished event of the type {t.Name}.", e);
                }
            }
        }

        #endregion



        #region INIT
        public MainWindow()
        {
            Thread.CurrentThread.Name = "MainFormThread";
            
            InitializeComponent();

            // Saves references to the bounded FrameworkElements so that we can rescue them at a later stage
            FormSharedDataBindings.SaveReferenceToElement(this, "MainWindow");

            // Will get the x:Name automatically
            LoadCasesSolverOptionsBindings.SaveReferenceToElement(NonLinearCases_GroupBox);
            LoadCasesGeneralActionBindings.SaveReferenceToElement(LoadCasesGeneralAction_GroupBox);
            ConstraintCleanupBindings.SaveReferenceToElement(ConstraintCleanup_GroupBox);


            StagedConstructionBindings.SaveReferenceToElement(StagedConstruction_TabItem_Panel);
            StagedConstructionBindings.I.StifferComboBox_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000GroupList }).View;

            SelectionInfoBindings.SaveReferenceToElement(Selection_TabItem);

            DisneyCanopySurveyBindings.SaveReferenceToElement(CanopySurveyTabItem);
            DisneyCanopySurveyBindings.I.ResultsDisplacements_Cases_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000CaseList }).View;
            DisneyCanopySurveyBindings.I.ResultsDisplacements_Groups_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000GroupList }).View;
            DisneyCanopySurveyBindings.I.StepwiseReport_OutputGroups_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000GroupList }).View;
            
            RhinoOperationsBindings.SaveReferenceToElement(RhinoOperations_TabItem);

            RenameItemsBindings.SaveReferenceToElement(Rename_TabItem_Panel);

            ManipulateItemsBindings.SaveReferenceToElement(ManipulateItems_TabItem);

            TestBindings.SaveReferenceToElement(Test_StackPanel);

            MonitorConstraintsManipulationBindings.SaveReferenceToElement(ConstraintsAddGroupBox);

            StatusBarBindings.SaveReferenceToElement(WindowStatusBar);
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            // Shared items
            //FormSharedDataBindings.Start();

            // Initializes the binding objects
            //LoadCasesSolverOptionsBindings.Start(NonLinearCases_GroupBox);
            //LoadCasesGeneralActionBindings.Start(LoadCasesGeneralAction_GroupBox);
            //ConstraintCleanupBindings.Start(ConstraintCleanup_GroupBox);

            //StagedConstructionBindings.Start(StagedConstruction_TabItem_Panel);
            //StagedConstructionBindings.I.StifferComboBox_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000GroupList }).View;

            //DisneyCanopySurveyBindings.Start(CanopySurveyTabItem);
            //DisneyCanopySurveyBindings.I.ResultsDisplacements_Cases_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000CaseList }).View;
            //DisneyCanopySurveyBindings.I.ResultsDisplacements_Groups_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000GroupList }).View;
            //DisneyCanopySurveyBindings.I.StepwiseReport_OutputGroups_ViewItems = (new CollectionViewSource() { Source = FormSharedDataBindings.I.Sap2000GroupList }).View;

            //RhinoOperationsBindings.Start(RhinoOperations_TabItem);

            //RenameItemsBindings.Start(Rename_TabItem_Panel);

            //ManipulateItemsBindings.Start(ManipulateItems_TabItem);

            //TestBindings.Start(Test_StackPanel);

            //MonitorConstraintsManipulationBindings.Start(ConstraintsAddGroupBox);

            //StatusBarBindings.Start(WindowStatusBar);
            //StatusBarBindings.I.MonitorList.Add(MonitorConstraintsManipulationBindings.I);

            StatusBarBindings.I.MonitorList.Add(MonitorConstraintsManipulationBindings.I);

            // Sets the views of the items



            //CreateAndBind();

            // Creates the list of monitor status
            //monitorStatuses.Add(new ConstraintMonitor(this));
            //monitorStatuses.Add(new SelectionMonitor(this));

            //UpdateMonitorStatusBarText();


            // Subscribes to the GLOBAL events from the binders
            EventAggregatorSingleton.I.GetEvent<BindBeginCommandEvent>().Subscribe(BindBeginCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindEndCommandEvent>().Subscribe(BindEndCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindMessageEvent>().Subscribe(BindMessageEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Subscribe(BindGenericCommandEventHandler);

            UpdateInterface();
        }


        #endregion

        #region Binder Event Handlers
        private void BindBeginCommandEventHandler(BindCommandEventArgs inObj)
        {
            // Reads the event argument
            bool andHideSap = inObj.EventData is bool inParam && inParam;

            BusyOverlayBindings.I.ShowOverlay();

            if (andHideSap)
            {
                BusyOverlayBindings.I.SetBasic(inTitle: "Hiding SAP2000's Window to Speed-up Works.");
                S2KModel.SM.HideSapAsync();
            }

            // Fires the event to tell the monitors to pause.
            EventAggregatorSingleton.I.GetEvent<PauseMonitorEvent>().Publish(new PauseMonitorEventArgs(this));
        }
        private void DisableWindow(bool andHideSap = false)
        {
        }
        private void BindEndCommandEventHandler(BindCommandEventArgs inObj)
        {
            BusyOverlayBindings.I.HideOverlayAndReset();

            try
            {
                S2KModel.SM.ShowSapAsync();
            }
            catch
            {
            }

            // Fires the event to tell the monitors to resume.
            EventAggregatorSingleton.I.GetEvent<ResumeMonitorEvent>().Publish(new ResumeMonitorEventArgs(this));
        }
        private void EnableWindow()
        {
        }
        private void BindMessageEventHandler(BindMessageEventArgs inObj)
        {
            // If a message came along, we show the message overlay
            if (inObj.Title != null && inObj.Message != null)
            {
                MessageOverlay.ShowOverlay(inObj.Title, inObj.Message);
            }
        }
        private void BindGenericCommandEventHandler(BindCommandEventArgs inObj)
        {
            if (inObj.EventData is string order)
            {
                if (order == "ActivateWindow")
                {
                    Dispatcher.Invoke(() => Activate());
                    return;
                }
            }

            // This means that the given message will not be handled
            throw new NotImplementedException();
        }

        #endregion

        private void FocusFromOtherThread()
        {
            Dispatcher.InvokeAsync(() => { Focus(); });
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            UpdateInterface();
        }
        private async void UpdateInterface()
        {
            // Is SAP2000 Open?
            try
            {
                _ = S2KModel.SM;

                StatusBarBindings.I.Sap2000IsOpen = true;
                StatusBarBindings.I.SapFileName = S2KModel.SM.FullFileName;
            }
            catch
            {
                StatusBarBindings.I.Sap2000IsOpen = false;
                StatusBarBindings.I.SapFileName = "COULD NOT CONNECT TO AN INSTANCE OF SAP2000. ARE YOU SURE SAP2000 IS OPEN?";
                return;
            }

            // Are we already busy?
            if (BusyOverlayBindings.I.OverlayElement.Visibility != Visibility.Collapsed) return;

            DisableWindow();
            BusyOverlayBindings.I.Title = "Updating Interface Items";

            try
            {
                void lf_work()
                {
                    List<string> groupList = S2KModel.SM.GroupMan.GetGroupList(true);
                    FormSharedDataBindings.I.Sap2000GroupList.ReplaceItemsIfNew(groupList);

                    List<string> loadCaseList = S2KModel.SM.LCMan.GetAllNames(inUpdateInterface: true);
                    List<string> lnLoadCaseList = S2KModel.SM.LCMan.GetAllNames(filterType:LoadCaseTypeEnum.CASE_NONLINEAR_STATIC, inUpdateInterface: true);
                    FormSharedDataBindings.I.Sap2000CaseList.ReplaceItemsIfNew(loadCaseList);

                    List<string> frameSectionList = S2KModel.SM.FrameSecMan.GetSectionNameList(inUpdateInterface:true);

                    // TODO: Send these lists to their bindings

                }

                // Runs the job async
                Task task = new Task(() => lf_work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, "Could not refresh the interface items from SAP2000.");
            }
            finally
            {
                EnableWindow();
            }
        }

        private void MainAppWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (S2KModel.SM != null) S2KModel.SM.ShowSapAsync();
            }
            catch
            {
            }
        }
        #region OverlayHelpers

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BusyOverlayBindings.I.CancelOperation();
        }

        private void MessageOverlayGrid_CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageOverlay.HideOverlayAndReset();
        }

        private void MessageOverlayGrid_CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(MessageOverlay.MessageText);
        }

        #endregion



        #region MONITORS

        // Monitor Controllers
        public List<MonitorStatus> monitorStatuses = new List<MonitorStatus>();

        private SelectionMonitor selectionMonitor
        {
            get
            {
                try
                {
                    return (SelectionMonitor) monitorStatuses.First<MonitorStatus>(a => a is SelectionMonitor);
                }
                catch (Exception ex)
                {
                    ExceptionViewer.Show(new Exception(
                        $"Could not find the selection monitor. This is serious bug. Contact EMASA with the error message.",
                        ex));
                    Application.Current.Shutdown(1);
                    return null;
                }
            }
        }

        private void DisableAllMonitors()
        {
            foreach (MonitorStatus item in monitorStatuses)
                if (item.IsRunning)
                {
                    item.AutomaticCancel = true;
                    item.StopMonitor();
                }
        }

        private void ReenableDisabledMonitors()
        {
            foreach (MonitorStatus item in monitorStatuses)
                if (item.AutomaticCancel)
                    item.StartMonitor();
        }

        //public void UpdateMonitorStatusBarText()
        //{
        //    // Clears
        //    StatusBarBindings.I.Set_ActiveMonitorsTextBlock();

        //    // Makes the Monitor Display
        //    StatusBarBindings.I.Set_ActiveMonitorsTextBlock("Monitor: ", new SolidColorBrush(Colors.Black));

        //    if (constraintMonitor.IsRunning)
        //        StatusBarBindings.I.Set_ActiveMonitorsTextBlock(constraintMonitor.DisplayName,
        //            new SolidColorBrush(Colors.Green), TextDecorations.Underline);
        //    else
        //        StatusBarBindings.I.Set_ActiveMonitorsTextBlock(constraintMonitor.DisplayName,
        //            new SolidColorBrush(Colors.Gray));

        //    if (selectionMonitor.IsRunning)
        //        StatusBarBindings.I.Set_ActiveMonitorsTextBlock(selectionMonitor.DisplayName,
        //            new SolidColorBrush(Colors.Green), TextDecorations.Underline);
        //    else
        //        StatusBarBindings.I.Set_ActiveMonitorsTextBlock(selectionMonitor.DisplayName,
        //            new SolidColorBrush(Colors.Gray));
        //}


        private void SelectionMonitorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            selectionMonitor.StartMonitor();
        }

        private void SelectionMonitorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectionInfoBindings.I.GetGroups = false;
            selectionMonitor.AutomaticCancel = false;
            selectionMonitor.StopMonitor();
        }

        #endregion

        #region TAB - Manipulate Items

        private async void ExtendFrameAlterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting the list of selected frames."));
                    var selFrames = S2KModel.SM.FrameMan.GetSelected();
                    //progReporter.Report(ProgressData.SetMessage("Getting the list of selected cables."));
                    var selCables = S2KModel.SM.CableMan.GetSelected();
                    //progReporter.Report(ProgressData.SetMessage("Getting the list of selected links."));
                    var selLinks = S2KModel.SM.LinkMan.GetSelected();
                    //progReporter.Report(ProgressData.SetMessage("Getting the list of selected points."));
                    var selPoints = S2KModel.SM.PointMan.GetSelected();

                    if (selCables.Count != 0) throw new S2KHelperException($"Cables are not supported.");
                    if (selLinks.Count != 0) throw new S2KHelperException($"Links are not supported.");

                    // Groups the linear elements
                    var selLines = new List<SapLine>();
                    selLines.AddRange(selFrames);
                    //selLines.AddRange(selLinks);

                    // Validates: The each line must have one selected point
                    if (selPoints.Count == 0 || selLines.Count == 0)
                        throw new S2KHelperException($"You must select at least one point and one linear element.");
                    foreach (SapLine item in selLines)
                        if (selPoints.Count(a => item.IsPointIJ(a)) != 1)
                            throw new S2KHelperException(
                                $"Please select one, and only one, point on each linear element.");

                    // Sets the basic message before the iterations
                    //progReporter.Report(ProgressData.SetMessage("Working on line ***."));

                    for (int i = 0; i < selLines.Count; i++)
                    {
                        SapLine line = selLines[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, selLines.Count, line.Name));

                        // Finds the point linked to this line
                        SapPoint point = (from a in selPoints where line.IsPointIJ(a) select a).First();

                        if (line.iEndPoint != point && line.jEndPoint != point)
                            throw new S2KHelperException(
                                "The selected joint must be either the I or the J joint of the linear element.");

                        double distance = ExtendOrShrinkLineBindings.I.ExtendDistance.Value;

                        if (distance == 0) throw new S2KHelperException("The distance cannot be 0.");
                        if (line.Length <= distance && distance < 0)
                            throw new S2KHelperException(
                                "You cannot shrink the element so that it is shorter than it already is.");

                        UnitVector3D dispVector = line.Line.Direction;
                        if (line.iEndPoint == point) dispVector = dispVector.Negate();

                        Vector3D delta = dispVector.ScaleBy(distance);

                        // Replicates the point
                        SapPoint newPoint = point.CopyTo(delta);

                        // Changes the connectivity of the elements connected to the point
                        if (ExtendOrShrinkLineBindings.I.AllElements_IsChecked)
                            foreach (SapObject item in point.ConnectedElements)
                                item.ChangeConnectivity(point, newPoint);
                        else if (ExtendOrShrinkLineBindings.I.OnlyTheLine_IsChecked)
                            line.ChangeConnectivity(point, newPoint);

                        // Checks if old point shall be deleted
                        if (!point.HasConnections) S2KModel.SM.PointMan.DeletePoint(point);
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;

                // There was an error in the async method
                if (task.Exception != null) throw task.Exception;
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox($"Could not move the point.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                S2KModel.SM.RefreshView();
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        //private async void ExtendFrameProceedToOthersButton_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        DisableWindow();

        //        // Created in the UI thread
        //        CancelOpForm cancelOpForm = new CancelOpForm(this);

        //        // The async body
        //        void work()
        //        {
        //            //progReporter.Report(ProgressData.SetMessage("Getting the list of selected frames."));
        //            var selFrames = S2KModel.SM.FrameMan.GetSelected();
        //            //progReporter.Report(ProgressData.SetMessage("Getting the list of selected cables."));
        //            var selCables = S2KModel.SM.CableMan.GetSelected();
        //            //progReporter.Report(ProgressData.SetMessage("Getting the list of selected links."));
        //            var selLinks = S2KModel.SM.LinkMan.GetSelected();
        //            //progReporter.Report(ProgressData.SetMessage("Getting the list of selected points."));
        //            var selPoints = S2KModel.SM.PointMan.GetSelected();

        //            if (selCables.Count != 0) throw new S2KHelperException($"Cables are not supported.");
        //            if (selLinks.Count != 0) throw new S2KHelperException($"Links are not supported.");

        //            // Groups the linear elements
        //            var selLines = new List<SapLine>();
        //            selLines.AddRange(selFrames);
        //            //selLines.AddRange(selLinks);

        //            // Validates: The each line must have one selected point
        //            if (selPoints.Count == 0 || selLines.Count == 0)
        //                throw new S2KHelperException($"You must select at least one point and one linear element.");
        //            foreach (SapLine item in selLines)
        //                if (selPoints.Count(a => item.IsPointIJ(a)) != 1)
        //                    throw new S2KHelperException(
        //                        $"Please select one, and only one, point on each linear element.");

        //            // Now, the target line must be selected
        //            S2KModel.SM.ClearSelection();

        //            //progReporter.Report(ProgressData.SetMessage("Searching for Alignment Direction."));

        //            // Opens the cancel operation form
        //            cancelOpForm.MessageText =
        //                $"Please select *only* one option to use as alignment direction: {Environment.NewLine}*) Two, and only two, points.{Environment.NewLine}*) One, and only one, frame.";
        //            cancelOpForm.Show_FromOtherThread();

        //            // Pools for the new selection to get the line alignment
        //            Line3D alignLine = new Line3D();
        //            do
        //            {
        //                if (cancelOpForm.Token.IsCancellationRequested) return;

        //                Thread.Sleep(Properties.Settings.Default.SapPoolSleep);

        //                // Gets the selected points
        //                var selPointsAlign = S2KModel.SM.PointMan.GetSelected();
        //                // Gets the selected frames
        //                var selFramesAlign = S2KModel.SM.FrameMan.GetSelected();

        //                if (selPointsAlign.Count > 2 ||
        //                    selFramesAlign.Count > 1 ||
        //                    selFramesAlign.Count == 1 && selPointsAlign.Count != 0)
        //                {
        //                    S2KStaticMethods.ShowWarningMessageBox(
        //                        $"Please select *only* one option to use as alignment direction: {Environment.NewLine}- Two, and only two, points.{Environment.NewLine}- One, and only one, frame.");
        //                    S2KModel.SM.ClearSelection();
        //                    continue;
        //                }

        //                if (selPointsAlign.Count == 2)
        //                {
        //                    alignLine = new Line3D(selPointsAlign[0].Point, selPointsAlign[1].Point);
        //                    break;
        //                }

        //                if (selFramesAlign.Count == 1)
        //                {
        //                    alignLine = selFramesAlign[0].Line;
        //                    break;
        //                }
        //            } while (true);

        //            // Sets the basic message before the iterations
        //            //progReporter.Report(ProgressData.SetMessage("Working on line ***."));

        //            for (int i = 0; i < selLines.Count; i++)
        //            {
        //                SapLine line = selLines[i];
        //                //progReporter.Report(ProgressData.UpdateProgress(i, selLines.Count, line.Name));

        //                // The new target point
        //                SapPoint newPoint;

        //                if (ExtendOrShrinkLineBindings.I.Closest_IsChecked)
        //                {
        //                    newPoint = S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(alignLine
        //                        .ClosestPointsBetween(line.Line).Item1);
        //                }
        //                else
        //                {
        //                    // Vertical plane that contains the current line
        //                    Plane lineVertPlane = Plane.FromPoints(line.Line.StartPoint, line.Line.EndPoint,
        //                        new Point3D(line.Line.StartPoint.X, line.Line.StartPoint.Y,
        //                            line.Line.StartPoint.Z - 100));

        //                    var intersect = lineVertPlane.IntersectionWith(alignLine);
        //                    if (!intersect.HasValue)
        //                        throw new S2KHelperException(
        //                            $"Could not find the Z projection of element {line.Name} onto selected alignment line.");

        //                    newPoint = S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(intersect.Value);
        //                }

        //                // Finds the point linked to this line
        //                SapPoint point = (from a in selPoints where line.IsPointIJ(a) select a).First();

        //                if (line.iEndPoint != point && line.jEndPoint != point)
        //                    throw new S2KHelperException(
        //                        "The selected joint must be either the I or the J joint of the linear elements.");

        //                // Changes the connectivity of the elements connected to the point
        //                if (ExtendOrShrinkLineBindings.I.AllElements_IsChecked)
        //                    foreach (SapObject item in point.ConnectedElements)
        //                        item.ChangeConnectivity(point, newPoint);
        //                else if (ExtendOrShrinkLineBindings.I.OnlyTheLine_IsChecked)
        //                    line.ChangeConnectivity(point, newPoint);

        //                // Checks if old point shall be deleted
        //                if (!point.HasConnections) S2KModel.SM.PointMan.DeletePoint(point);
        //            }

        //            cancelOpForm.Close_FromOtherThread();
        //            return;
        //        }

        //        // Runs the job async
        //        Task task = new Task(() => work());
        //        task.Start();
        //        await task;

        //        // There was an error in the async method
        //        if (task.Exception != null) throw task.Exception;
        //    }
        //    catch (Exception ex)
        //    {
        //        S2KStaticMethods.ShowWarningMessageBox(
        //            $"Could not move the point(s).{Environment.NewLine}{ex.Message}");
        //    }
        //    finally
        //    {
        //        S2KModel.SM.RefreshView();
        //        ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
        //        EnableWindow();
        //    }
        //}

        private async void CreateSplicesButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow(true);
            BusyOverlayBindings.I.Title = "Adding splice elements";

            try
            {
                void work()
                {
                    // Gets selections
                    var selPoints = S2KModel.SM.PointMan.GetSelected(true);
                    var selFrames = S2KModel.SM.FrameMan.GetSelected(true);

                    // Checks if there are selected points
                    if (selPoints.Count == 0) throw new S2KHelperException("Please select joints.");
                    if (selFrames.Count == 0) throw new S2KHelperException("Please select frames.");

                    // Adds a group name
                    string groupName = "AutoSplice";
                    BusyOverlayBindings.I.SetIndeterminate($"Making sure the group called {groupName} exists.");
                    S2KModel.SM.GroupMan.AddGroup(groupName);

                    // Checks if each point has two frames and if they are colinear
                    foreach (SapPoint point in selPoints)
                    {
                        var pointFrames = (from a in selFrames
                            where a.IsPointIJ(point)
                            select a).ToList();

                        if (pointFrames.Count != 2 || !pointFrames[0].IsColinearTo(pointFrames[1]))
                            throw new S2KHelperException("Each joint must have two colinear frames linked to it.");
                    }

                    // Shall break each evenly
                    double breakDistance = CreateSpliceBindings.I.TotalSpliceLength / 2;

                    // Each side shall be at least 1 unit larger in length
                    var tooShortFrames = (from a in selFrames where a.Length < breakDistance + 1 select a).ToList();
                    if (tooShortFrames.Count > 0)
                    {
                        foreach (SapFrame tooShort in tooShortFrames) tooShort.Select();
                        throw new S2KHelperException(
                            "The selected frames are too short. The selection was updated for only the offending frame.");
                    }

                    // Creates a list of pairs - usefull as we are changing the frame defs
                    // This will allow the user to select both ends of the same frame in a line
                    var pairs = new List<(SapPoint point, SapFrame[] frames)>();
                    foreach (SapPoint point in selPoints)
                    {
                        // List of linked frames
                        var linkedFrames = (from a in selFrames
                            where a.IsPointIJ(point)
                            select a.DuplicateReference()).ToArray();

                        pairs.Add((point, linkedFrames));
                    }

                    S2KModel.SM.ClearSelection();

                    // Now, we make the changes
                    BusyOverlayBindings.I.SetDeterminate("Working with splice at joints.", "Joint");
                    var spliceFrames = new List<SapFrame>();
                    for (int i = 0; i < pairs.Count; i++)
                    {
                        SapPoint point = pairs[i].point;
                        var linkedFrames = pairs[i].frames;

                        BusyOverlayBindings.I.UpdateProgress(i, pairs.Count, point.Name);

                        // The selected point shall become special
                        point.Special = true;

                        var toJoin = new List<SapFrame>();

                        foreach (SapFrame oldFrame in linkedFrames)
                        {
                            // Must break each frame 
                            var newFrames = oldFrame.DivideAtDistanceFromPoint(point, breakDistance);

                            // Gets the frame part that is linked to the central point
                            foreach (SapFrame newFrame in newFrames)
                                if (newFrame.IsPointIJ(point)) toJoin.Add(newFrame);
                                else
                                    // It is the other frame. We must substitute in the pair list to allow for aligned changes
                                    foreach (var pair in pairs)
                                        if (pair.point != point) // Skips the current point as it was already treated!
                                            for (int k = 0; k < pair.frames.Length; k++)
                                                if (pair.frames[k].Name == oldFrame.Name)
                                                    pair.frames[k] = newFrame.DuplicateReference();
                        }

                        // Joins the two frames
                        SapFrame joinedSpliceFrame = null;
                        try
                        {
                            joinedSpliceFrame = S2KModel.SM.FrameMan.JoinFrames(toJoin);
                        }
                        catch
                        {
                            Thread.Sleep(200);
                            try
                            {
                                joinedSpliceFrame = S2KModel.SM.FrameMan.JoinFrames(toJoin);
                            }
                            catch
                            {
                            }
                        }

                        if (joinedSpliceFrame != null) spliceFrames.Add(joinedSpliceFrame);
                    }

                    BusyOverlayBindings.I.SetIndeterminate($"Selecting and adding the group {groupName} to the splices.");
                    S2KModel.SM.ClearSelection();
                    foreach (SapFrame item in spliceFrames)
                    {
                        item.Select();
                        item.AddGroup(groupName);
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                //S2KModel.SM.RefreshView();
                //S2KModel.SM.InterAuto.FlaUI_FocusMainWindow();
                EnableWindow();
            }
        }

        //private async void BreakFrameClosestPointButton_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        DisableWindow();

        //        // The async body
        //        void work()
        //        {
        //            //progReporter.Report(ProgressData.SetMessage("Getting the selected points.", true));
        //            var sPoints = S2KModel.SM.PointMan.GetSelected();

        //            if (sPoints.Count != 1)
        //                throw new S2KHelperException(
        //                    "You must select one - and only one point. It is the point closest to which the frame will be broken.");

        //            //progReporter.Report(ProgressData.SetMessage("Getting the selected frames.", true));
        //            var sFrames = S2KModel.SM.FrameMan.GetSelected();

        //            if (sFrames.Count != 1)
        //                throw new S2KHelperException(
        //                    "You must select one - and only one frame. It is the frame that will be broken.");

        //            //progReporter.Report(ProgressData.SetMessage("Breaking the frame.", true));

        //            // Puts a point in the frame that is closest to the selected point
        //            SapPoint onFrame = sFrames[0]
        //                .AddPointInFrameClosestToGiven(sPoints[0], $"KH_Link_{S2KStaticMethods.UniqueName(6)}");

        //            // Checks if is one of the extreme frames
        //            if (sFrames[0].IsPointIJ(onFrame))
        //                throw new S2KHelperException(
        //                    "The frame will not be broken as the closest point is one of the end joints of the frame.");

        //            // Breaks the frame at the either the given or added point - depends on the results of the AddPointInFrameClosestToGiven function.
        //            List<SapFrame> sapFrames = null;
        //            try
        //            {
        //                sapFrames = sFrames[0].DivideAtIntersectPoint(onFrame, "P");
        //            }
        //            catch (Exception)
        //            {
        //                throw new S2KHelperException(
        //                    "Cannot break the frame at the given point. Probably the closest point is too close (considering Sap2000 Merge Tolerance) to the frame as to prevent a point to be added all the while being too far to be captured as being on the line to break the frame. Try reducing Sap2000's Merge Tolerance.");
        //            }

        //            // Should we add a constraint?
        //            if (BreakFrameBindings.I.ClosestPointAddConstraint_IsChecked)
        //            {
        //                //progReporter.Report(ProgressData.SetMessage("Adding a constraint.", true));

        //                bool[] constVals =
        //                {
        //                    BreakFrameBindings.I.U1CheckBox_IsChecked, BreakFrameBindings.I.U2CheckBox_IsChecked,
        //                    BreakFrameBindings.I.U3CheckBox_IsChecked,
        //                    BreakFrameBindings.I.R1CheckBox_IsChecked, BreakFrameBindings.I.R2CheckBox_IsChecked,
        //                    BreakFrameBindings.I.R3CheckBox_IsChecked
        //                };

        //                string constName = BreakFrameBindings.I.OutConstraintName + S2KStaticMethods.UniqueName(10);

        //                if (BreakFrameBindings.I.BodyConstraintTypeRadioButton_IsChecked)
        //                {
        //                    // Creates a joint constraint
        //                    if (!S2KModel.SM.JointConstraintMan.SetBodyConstraint(constName, constVals))
        //                        throw new S2KHelperException($"Could not create constraint called {constName}.");
        //                }
        //                else if (BreakFrameBindings.I.LocalConstraintTypeRadioButton_IsChecked)
        //                {
        //                    // Creates a joint constraint
        //                    if (!S2KModel.SM.JointConstraintMan.SetLocalConstraint(constName, constVals))
        //                        throw new S2KHelperException($"Could not create constraint called {constName}.");
        //                }
        //                else if (BreakFrameBindings.I.EqualConstraintTypeRadioButton_IsChecked)
        //                {
        //                    // Creates a joint constraint
        //                    if (!S2KModel.SM.JointConstraintMan.SetEqualConstraint(constName, constVals))
        //                        throw new S2KHelperException($"Could not create constraint called {constName}.");
        //                }

        //                sPoints[0].AddJointConstraint(constName, false);
        //                onFrame.AddJointConstraint(constName, false);
        //            }
        //        }

        //        // Runs the job async
        //        Task task = new Task(() => work());
        //        task.Start();
        //        await task;


        //        // There was an error in getting the data or the data table was not acquired
        //        if (task.IsFaulted)
        //        {
        //            S2KStaticMethods.ShowErrorMessageBox("Could not break the frame.", task.Exception);
        //        }
        //        else
        //        {
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
        //    }
        //    finally
        //    {
        //        ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
        //        EnableWindow();
        //    }
        //}

        #endregion

        private void TESTER_Click(object sender, RoutedEventArgs e)
        {
            var allPoints = S2KModel.SM.PointMan.GetAll();

            foreach (SapPoint pnt in allPoints)
            foreach (string group in pnt.Groups)
                pnt.RemoveGroup(@group);
        }

        #region Rename Tab

        private async void RenameGetMaxBNumberButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage($"Getting list of Points."));
                    var allPoints = S2KModel.SM.PointMan.GetAll();

                    //progReporter.Report(ProgressData.SetMessage($"Finding largest Point."));
                    int max = int.MinValue;
                    foreach (SapPoint item in allPoints)
                        if (int.TryParse(item.Name, out int result))
                            if (result > max)
                                max = result;

                    RenameItemsBindings.I.LargestPoint = max;

                    //progReporter.Report(ProgressData.SetMessage($"Getting list of Frames."));
                    var allFrames = S2KModel.SM.FrameMan.GetAll();

                    foreach (SapFrame item in allFrames)
                        if (int.TryParse(item.Name, out int result))
                            if (result > max)
                                max = result;

                    RenameItemsBindings.I.LargestFrame = max;
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could get the Largest Numbered Point.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not get the Largest Numbered Frame.", ex);
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void RenameFromValueButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            try
            {
                void work()
                {
                    List<SapPoint> points = null;
                    List<SapFrame> frames = null;
                    List<SapArea> areas = null;
                    if (RenameItemsBindings.I.RenameAllIsChecked)
                    {
                        //progReporter.Report(ProgressData.SetMessage("Getting Points"));
                        points = S2KModel.SM.PointMan.GetAll();

                        //progReporter.Report(ProgressData.SetMessage("Getting Frames"));
                        frames = S2KModel.SM.FrameMan.GetAll();

                        //progReporter.Report(ProgressData.SetMessage("Getting Areas"));
                        areas = S2KModel.SM.AreaMan.GetAll();
                    }
                    else if (RenameItemsBindings.I.RenameSelectedIsChecked)
                    {
                        //progReporter.Report(ProgressData.SetMessage("Getting Points"));
                        points = S2KModel.SM.PointMan.GetSelected();

                        //progReporter.Report(ProgressData.SetMessage("Getting Frames"));
                        frames = S2KModel.SM.FrameMan.GetSelected();

                        //progReporter.Report(ProgressData.SetMessage("Getting Areas"));
                        areas = S2KModel.SM.AreaMan.GetSelected();
                    }
                    else
                    {
                        throw new S2KHelperException("The selected option on the radio buttons is invalid.");
                    }

                    int jumper = int.MinValue - 2 * points.Count;
                    int startBase = RenameItemsBindings.I.StartCountValue;

                    #region Points

                    for (int i = 0; i < points.Count; i++)
                    {
                        SapPoint point = points[i];
                        string newName = $"{jumper + i}";

                        //progReporter.Report(ProgressData.UpdateProgress(i, points.Count, null,$"Changing name of point: {point.Name} to {newName}."));
                        point.ChangeName(newName);
                    }

                    for (int i = 0; i < points.Count; i++)
                    {
                        SapPoint point = points[i];
                        string newName = $"{startBase + i}";

                        //progReporter.Report(ProgressData.UpdateProgress(i, points.Count, null,$"Changing name of point: {point.Name} to {newName}."));
                        point.ChangeName(newName);
                    }

                    #endregion

                    #region Frames

                    jumper = int.MinValue - 2 * frames.Count;
                    for (int i = 0; i < frames.Count; i++)
                    {
                        SapFrame frame = frames[i];
                        string newName = $"{jumper + i}";

                        //progReporter.Report(ProgressData.UpdateProgress(i, points.Count, null,$"Changing name of frame: {frame.Name} to {newName}."));
                        frame.ChangeName(newName);
                    }

                    for (int i = 0; i < frames.Count; i++)
                    {
                        SapFrame frame = frames[i];
                        string newName = $"{startBase + i}";

                        //progReporter.Report(ProgressData.UpdateProgress(i, points.Count, null,$"Changing name of frame: {frame.Name} to {newName}."));
                        frame.ChangeName(newName);
                    }

                    #endregion

                    #region Areas

                    jumper = int.MinValue - 2 * areas.Count;
                    for (int i = 0; i < areas.Count; i++)
                    {
                        SapArea area = areas[i];
                        string newName = $"{jumper + i}";

                        //progReporter.Report(ProgressData.UpdateProgress(i, points.Count, null,$"Changing name of area: {area.Name} to {newName}."));
                        area.ChangeName(newName);
                    }

                    for (int i = 0; i < areas.Count; i++)
                    {
                        SapArea area = areas[i];
                        string newName = $"{startBase + i}";

                        //progReporter.Report(ProgressData.UpdateProgress(i, points.Count, null,$"Changing name of area: {area.Name} to {newName}."));
                        area.ChangeName(newName);
                    }

                    #endregion
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox($"Could not rename items.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void RenameRemoveRegexButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            // Reads the Regex
            Regex renameFinder = new Regex(RenameItemsBindings.I.RegexToFind);

            try
            {
                string work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting All Points", true));
                    var points = S2KModel.SM.PointMan.GetAll();

                    (int Points, int Frames) renameCount = (0, 0);

                    foreach (SapPoint point in points)
                        if (renameFinder.IsMatch(point.Name))
                        {
                            if (RenameItemsBindings.I.RegexRenameToRand)
                            {
                                string uniqueName = S2KStaticMethods.UniqueName(10);

                                point.ChangeName(uniqueName);

                                renameCount.Points++;
                            }
                            else if (RenameItemsBindings.I.RegexJustRemoveRegex)
                            {
                                string newName = renameFinder.Replace(point.Name, "");

                                point.ChangeName(newName);

                                renameCount.Points++;
                            }
                            else
                            {
                                throw new S2KHelperException($"Invalid Radio Button Option.");
                            }
                        }

                    //progReporter.Report(ProgressData.SetMessage("Getting All Frames", true));
                    var frames = S2KModel.SM.FrameMan.GetAll();

                    foreach (SapFrame frame in frames)
                        if (renameFinder.IsMatch(frame.Name))
                        {
                            if (RenameItemsBindings.I.RegexRenameToRand)
                            {
                                string uniqueName = S2KStaticMethods.UniqueName(10);

                                frame.ChangeName(uniqueName);

                                renameCount.Frames++;
                            }
                            else if (RenameItemsBindings.I.RegexJustRemoveRegex)
                            {
                                string newName = renameFinder.Replace(frame.Name, "");

                                frame.ChangeName(newName);

                                renameCount.Frames++;
                            }
                            else
                            {
                                throw new S2KHelperException($"Invalid Radio Button Option.");
                            }
                        }

                    if (renameCount == (0, 0)) return "No points nor frames have been renamed.";
                    return
                        $"A total of {renameCount.Points} points have been renamed.{Environment.NewLine}A total of {renameCount.Frames} frames have been renamed.";
                }

                // Runs the job async
                var task = new Task<string>(() => work());
                task.Start();
                await task;

                // There was an error 
                if (task.IsFaulted) S2KStaticMethods.ShowErrorMessageBox("Could not rename items.", task.Exception);
                else
                    MessageBox.Show(task.Result, "Renamed Frames", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox($"Could not rename items.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        #endregion

        //private Vector3D PoolForAlignmentDirection_GetVector(CancelOpForm inCancelForm)
        //{
        //    // Opens the cancel operation form
        //    inCancelForm.MessageText =
        //        $"Please select *only* one option to use as alignment direction: {Environment.NewLine}*) Two, and only two, points.{Environment.NewLine}*) One, and only one, frame.";
        //    inCancelForm.Show_FromOtherThread();

        //    do
        //    {
        //        if (inCancelForm.Token.IsCancellationRequested) return Vector3D.NaN;

        //        Thread.Sleep(Properties.Settings.Default.MonitorSleep);

        //        // Gets the selected points
        //        var selPoints = S2KModel.SM.PointMan.GetSelected();
        //        // Gets the selected frames
        //        var selFrames = S2KModel.SM.FrameMan.GetSelected();

        //        if (selPoints.Count > 2 ||
        //            selFrames.Count > 1 ||
        //            selFrames.Count == 1 && selPoints.Count != 0)
        //        {
        //            S2KModel.SM.ClearSelection();
        //            S2KStaticMethods.ShowWarningMessageBox($"Please select *only* one option to use as alignment direction: {Environment.NewLine}- Two, and only two, points.{Environment.NewLine}- One, and only one, frame.");
        //            continue;
        //        }

        //        if (selPoints.Count == 2)
        //        {
        //            S2KModel.SM.ClearSelection();
        //            return selPoints[0].Point.VectorTo(selPoints[1].Point);
        //        }

        //        if (selFrames.Count == 1)
        //        {
        //            S2KModel.SM.ClearSelection();
        //            return selFrames[0].Vector;
        //        }
        //    } while (true);
        //}

        //private CoordinateSystem PoolForAlignmentDirection_GetCoordinateSystem(CancelOpForm inCancelForm)
        //{
        //    // Opens the cancel operation form
        //    inCancelForm.MessageText =
        //        $"Please select *only* one option to use as alignment direction: {Environment.NewLine}*) Two, and only two, points.{Environment.NewLine}*) One, and only one, frame.";
        //    inCancelForm.Show_FromOtherThread();

        //    do
        //    {
        //        if (inCancelForm.Token.IsCancellationRequested) return null;

        //        Thread.Sleep(Properties.Settings.Default.MonitorSleep);

        //        // Gets the selected points
        //        var selPoints = S2KModel.SM.PointMan.GetSelected();
        //        // Gets the selected frames
        //        var selFrames = S2KModel.SM.FrameMan.GetSelected();

        //        if (selPoints.Count > 2 ||
        //            selFrames.Count > 1 ||
        //            selFrames.Count == 1 && selPoints.Count != 0)
        //        {
        //            S2KModel.SM.ClearSelection();
        //            S2KStaticMethods.ShowWarningMessageBox(
        //                $"Please select *only* one option to use as alignment direction: {Environment.NewLine}- Two, and only two, points.{Environment.NewLine}- One, and only one, frame.");
        //            continue;
        //        }

        //        if (selPoints.Count == 2)
        //        {
        //            S2KModel.SM.ClearSelection();

        //            UnitVector3D vec1 = selPoints[0].Point.VectorTo(selPoints[1].Point).Normalize();

        //            // Local 1-2 is vertical, towards +Z. If the frame is vertical, then the local axis is towards +X
        //            UnitVector3D vecP = vec1.IsVectorVertical()
        //                ? UnitVector3D.Create(1, 0, 0)
        //                : UnitVector3D.Create(0, 0, 1);

        //            // V3 is perpendicular to both the axial vector and the reference vector
        //            UnitVector3D vec3 = vec1.CrossProduct(vecP);

        //            // V2 is then perpendicular to both
        //            UnitVector3D vec2 = vec3.CrossProduct(vec1);

        //            return new CoordinateSystem(Point3D.Origin, vec1, vec2, vec3);
        //        }

        //        if (selFrames.Count == 1)
        //        {
        //            S2KModel.SM.ClearSelection();
        //            return selFrames[0].GetCSysFromAxesDefinitions();
        //        }
        //    } while (true);
        //}

        #region Align: Frame

        private List<SapFrame> FrameAlign_SelectedFrames = null;
        private Vector3D FrameAlign_CurrentVector = Vector3D.NaN;
        private FrameAdvancedAxes_Plane2? FrameAlign_CurrentPlane = null;
        private int FrameAlign_CurrentFlipStatus = 0;

        //private async void BeginAlignFrameButton_Click(object sender, RoutedEventArgs e)
        //{
        //    DisableWindow();

        //    // Created in the UI thread
        //    CancelOpForm cancelOpForm = new CancelOpForm(this);

        //    bool work()
        //    {
        //        FrameAlign_SelectedFrames = null;
        //        FrameAlign_CurrentVector = Vector3D.NaN;
        //        FrameAlign_CurrentPlane = AlignFrameBindings.I.FrameAlignPlaneOption;
        //        FrameAlign_CurrentFlipStatus = 0;

        //        //progReporter.Report(ProgressData.SetMessage("Getting Selected Frames."));
        //        FrameAlign_SelectedFrames = S2KModel.SM.FrameMan.GetSelected();

        //        if (FrameAlign_SelectedFrames.Count == 0)
        //        {
        //            S2KStaticMethods.ShowWarningMessageBox("Please select the Frames you wish to align.");

        //            S2KModel.SM.ClearSelection();
        //            return false;
        //        }

        //        S2KModel.SM.ClearSelection();

        //        //progReporter.Report(ProgressData.SetMessage("Searching for Alignment Direction."));

        //        // Pools for the Frame vector alignment
        //        FrameAlign_CurrentVector = PoolForAlignmentDirection_GetVector(cancelOpForm);
        //        if (cancelOpForm.Token.IsCancellationRequested) return false;

        //        foreach (SapFrame Frame in FrameAlign_SelectedFrames)
        //        {
        //            if (!Frame.SetAdvancedAxis(FrameAlign_CurrentPlane.Value, FrameAlign_CurrentVector))
        //                S2KStaticMethods.ShowWarningMessageBox($"Could not align Frame: {Frame.Name}");
        //            // reselects to be nice to the used
        //            S2KModel.SM.FrameMan.SelectElements(Frame);
        //        }

        //        cancelOpForm.Close_FromOtherThread();
        //        return true;
        //    }

        //    // Runs the job async
        //    var task = new Task<bool>(() => work(), cancelOpForm.Token);
        //    task.Start();
        //    await task;

        //    AlignFrameBindings.I.FlipLastButton_IsEnabled = task.Result;
        //    ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

        //    S2KModel.SM.RefreshView();
        //    EnableWindow();
        //}

        private void AlignFrameSwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameAlign_SelectedFrames == null || FrameAlign_SelectedFrames.Count == 0) return;
            if (!FrameAlign_CurrentPlane.HasValue) return;
            if (FrameAlign_CurrentVector == Vector3D.NaN) return;

            // Goes to the next flip
            FrameAlign_CurrentFlipStatus = ++FrameAlign_CurrentFlipStatus == 4 ? 0 : FrameAlign_CurrentFlipStatus;

            if (FrameAlign_CurrentFlipStatus.IsEven())
                switch (FrameAlign_CurrentPlane)
                {
                    case FrameAdvancedAxes_Plane2.Plane12:
                        FrameAlign_CurrentPlane = FrameAdvancedAxes_Plane2.Plane13;
                        break;
                    case FrameAdvancedAxes_Plane2.Plane13:
                        FrameAlign_CurrentPlane = FrameAdvancedAxes_Plane2.Plane12;
                        break;
                    default:
                        break;
                }
            else
                FrameAlign_CurrentVector = FrameAlign_CurrentVector.Negate();

            foreach (SapFrame Frame in FrameAlign_SelectedFrames)
                if (!Frame.SetAdvancedAxis(FrameAlign_CurrentPlane.Value, FrameAlign_CurrentVector))
                    S2KStaticMethods.ShowWarningMessageBox($"Could not align Frame: {Frame.Name}");

            S2KModel.SM.RefreshView();
        }

        #endregion

        #region Align: Area

        private List<SapArea> AreaAlign_SelectedAreas = null;
        private Vector3D AreaAlign_CurrentVector = Vector3D.NaN;
        private AreaAdvancedAxes_Plane? AreaAlign_CurrentPlane = null;
        private int AreaAlign_CurrentFlipStatus = 0;

        //private async void BeginAlignAreaButton_Click(object sender, RoutedEventArgs e)
        //{
        //    DisableWindow();

        //    // Created in the UI thread
        //    CancelOpForm cancelOpForm = new CancelOpForm(this);

        //    bool work()
        //    {
        //        AreaAlign_SelectedAreas = null;
        //        AreaAlign_CurrentVector = Vector3D.NaN;
        //        AreaAlign_CurrentPlane = AlignAreaBindings.I.AreaAlignPlaneOption;
        //        AreaAlign_CurrentFlipStatus = 0;

        //        //progReporter.Report(ProgressData.SetMessage("Getting Selected Areas."));
        //        AreaAlign_SelectedAreas = S2KModel.SM.AreaMan.GetSelected();

        //        if (AreaAlign_SelectedAreas.Count == 0)
        //        {
        //            S2KStaticMethods.ShowWarningMessageBox("Please select the areas you wish to align.");

        //            S2KModel.SM.ClearSelection();
        //            return false;
        //        }

        //        S2KModel.SM.ClearSelection();

        //        //progReporter.Report(ProgressData.SetMessage("Searching for Alignment Direction."));

        //        // Pools for the area vector alignment
        //        AreaAlign_CurrentVector = PoolForAlignmentDirection_GetVector(cancelOpForm);
        //        if (cancelOpForm.Token.IsCancellationRequested) return false;

        //        foreach (SapArea area in AreaAlign_SelectedAreas)
        //        {
        //            if (!area.SetAdvancedAxis(AreaAlign_CurrentPlane.Value, AreaAlign_CurrentVector))
        //                S2KStaticMethods.ShowWarningMessageBox($"Could not align area: {area.Name}");
        //            // reselects to be nice to the used
        //            S2KModel.SM.AreaMan.SelectElements(area);
        //        }

        //        cancelOpForm.Close_FromOtherThread();
        //        return true;
        //    }

        //    // Runs the job async
        //    var task = new Task<bool>(() => work(), cancelOpForm.Token);
        //    task.Start();
        //    await task;

        //    AlignAreaBindings.I.FlipLastButton_IsEnabled = task.Result;

        //    ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

        //    S2KModel.SM.RefreshView();
        //    EnableWindow();
        //}

        private void AlignAreaSwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (AreaAlign_SelectedAreas == null || AreaAlign_SelectedAreas.Count == 0) return;
            if (!AreaAlign_CurrentPlane.HasValue) return;
            if (AreaAlign_CurrentVector == Vector3D.NaN) return;

            // Goes to the next flip
            AreaAlign_CurrentFlipStatus = ++AreaAlign_CurrentFlipStatus == 4 ? 0 : AreaAlign_CurrentFlipStatus;

            if (AreaAlign_CurrentFlipStatus.IsEven())
                switch (AreaAlign_CurrentPlane)
                {
                    case AreaAdvancedAxes_Plane.Plane31:
                        AreaAlign_CurrentPlane = AreaAdvancedAxes_Plane.Plane32;
                        break;
                    case AreaAdvancedAxes_Plane.Plane32:
                        AreaAlign_CurrentPlane = AreaAdvancedAxes_Plane.Plane31;
                        break;
                    default:
                        break;
                }
            else
                AreaAlign_CurrentVector = AreaAlign_CurrentVector.Negate();

            foreach (SapArea area in AreaAlign_SelectedAreas)
                if (!area.SetAdvancedAxis(AreaAlign_CurrentPlane.Value, AreaAlign_CurrentVector))
                    S2KStaticMethods.ShowWarningMessageBox($"Could not align area: {area.Name}");

            S2KModel.SM.RefreshView();
        }

        #endregion

        #region Align: Points

        private List<SapPoint> PointAlign_SelectedPoints = null;
        private CoordinateSystem PointAlign_CurrentCSys = null;
        private PointAdvancedAxes_Plane2? PointAlign_CurrentPlane = null;
        private int PointAlign_CurrentFlipStatus = 0;

        //private async void BeginAlignJointToSelectedFramesButton_Click(object sender, RoutedEventArgs e)
        //{
        //    DisableWindow();

        //    // Created in the UI thread
        //    CancelOpForm cancelOpForm = new CancelOpForm(this);

        //    bool work()
        //    {
        //        PointAlign_SelectedPoints = null;
        //        PointAlign_CurrentCSys = null;
        //        PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane12;
        //        PointAlign_CurrentFlipStatus = 0;

        //        //progReporter.Report(ProgressData.SetMessage("Getting Selected Points."));
        //        PointAlign_SelectedPoints = S2KModel.SM.PointMan.GetSelected();

        //        if (PointAlign_SelectedPoints.Count == 0)
        //        {
        //            S2KStaticMethods.ShowWarningMessageBox("Please select the points you wish to align.");

        //            S2KModel.SM.ClearSelection();
        //            return false;
        //        }

        //        S2KModel.SM.ClearSelection();

        //        //progReporter.Report(ProgressData.SetMessage("Searching for Alignment Direction."));

        //        // Pools for the vector alignment
        //        PointAlign_CurrentCSys = PoolForAlignmentDirection_GetCoordinateSystem(cancelOpForm);
        //        if (cancelOpForm.Token.IsCancellationRequested) return false;

        //        foreach (SapPoint point in PointAlign_SelectedPoints)
        //        {
        //            point.SetAdvancedLocalAxesFromCoordinateSystem(PointAlign_CurrentCSys);

        //            // reselects to be nice to the user
        //            S2KModel.SM.PointMan.SelectElements(point);
        //        }

        //        cancelOpForm.Close_FromOtherThread();
        //        return true;
        //    }

        //    // Runs the job async
        //    var task = new Task<bool>(() => work(), cancelOpForm.Token);
        //    task.Start();
        //    await task;

        //    AlignJointBindings.I.AlignPointFlipLast_IsEnabled = task.Result;

        //    ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

        //    S2KModel.SM.RefreshView();
        //    EnableWindow();
        //}

        private void AlignPointSwapButton_Click(object sender, RoutedEventArgs e)
        {
            if (PointAlign_SelectedPoints == null || PointAlign_SelectedPoints.Count == 0) return;
            if (!PointAlign_CurrentPlane.HasValue) return;
            if (PointAlign_CurrentCSys == null) return;

            // Goes to the next flip
            PointAlign_CurrentFlipStatus = ++PointAlign_CurrentFlipStatus == 4 ? 0 : PointAlign_CurrentFlipStatus;

            //if (PointAlign_CurrentFlipStatus.IsEven())
            //{
            switch (PointAlign_CurrentPlane.Value)
            {
                case PointAdvancedAxes_Plane2.Plane12:
                    PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane13;
                    break;
                case PointAdvancedAxes_Plane2.Plane13:
                    PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane21;
                    break;
                case PointAdvancedAxes_Plane2.Plane21:
                    PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane23;
                    break;
                case PointAdvancedAxes_Plane2.Plane23:
                    PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane31;
                    break;
                case PointAdvancedAxes_Plane2.Plane31:
                    PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane32;
                    break;
                case PointAdvancedAxes_Plane2.Plane32:
                    PointAlign_CurrentPlane = PointAdvancedAxes_Plane2.Plane12;
                    break;
                default:
                    break;
            }
            //}
            //else
            //{
            //    this.AreaAlign_CurrentVector = this.AreaAlign_CurrentVector.Negate();
            //}

            foreach (SapPoint point in PointAlign_SelectedPoints)
                point.SetAdvancedLocalAxesFromCoordinateSystem(PointAlign_CurrentCSys, PointAlign_CurrentPlane.Value);

            S2KModel.SM.RefreshView();
        }

        private async void BeginAlignJointToFramesButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();
            BusyOverlayBindings.I.Title = "Aligning the joints of the selected frames to the direction of the frame.";

            try
            {
                // Will return the points that could not be aligned
                (List<string> FailedJoints, List<(string jointName, double angle)> ChangeAngle) work()
                {
                    var FailedJoints = new List<string>();
                    var ChangeAngle = new List<(string jointName, double angle)>();

                    // Gets the list of Frames
                    var selFrames = S2KModel.SM.FrameMan.GetSelected(true);

                    if (selFrames == null || selFrames.Count == 0)
                        throw new S2KHelperException("You must select at least one frame!");

                    var selPoints = S2KModel.SM.PointMan.GetSelected(true);

                    if (selPoints.Count == 0)
                    {
                        BusyOverlayBindings.I.SetDeterminate("Aligning the I and J joints of the frames.", "Frame");
                        for (int i = 0; i < selFrames.Count; i++)
                        {
                            SapFrame frame = selFrames[i];

                            Vector3D currentIXAxis = frame.iEndPoint.LocalCoordinateSystem.XAxis;
                            Vector3D currentJXAxis = frame.jEndPoint.LocalCoordinateSystem.XAxis;

                            // Sets the progress
                            BusyOverlayBindings.I.UpdateProgress(i, selFrames.Count, frame.Name);

                            // Gets the vector for the alignment
                            CoordinateSystem frameCsys = frame.GetCSysFromAxesDefinitions();
                            if (AlignJointBindings.I.AlwaysUpwards_IsChecked)
                                // The X vector is pointing downwards
                                if (frameCsys.XAxis.Z < 0)
                                    frameCsys = new CoordinateSystem(Point3D.Origin, frameCsys.XAxis.ScaleBy(-1),
                                        frameCsys.YAxis, frameCsys.ZAxis.ScaleBy(-1));

                            frame.iEndPoint.SetAdvancedLocalAxesFromCoordinateSystem(frameCsys);
                            frame.jEndPoint.SetAdvancedLocalAxesFromCoordinateSystem(frameCsys);

                            ChangeAngle.Add((frame.iEndPoint.Name,
                                currentIXAxis.AngleTo(frameCsys.XAxis.Normalize()).Degrees));
                            ChangeAngle.Add((frame.jEndPoint.Name,
                                currentJXAxis.AngleTo(frameCsys.XAxis.Normalize()).Degrees));
                        }
                    }
                    else
                    {
                        BusyOverlayBindings.I.SetDeterminate("Selected joints to their frames.", "Joint");
                        for (int i = 0; i < selPoints.Count; i++)
                        {
                            SapPoint point = selPoints[i];
                            // Sets the progress
                            BusyOverlayBindings.I.UpdateProgress(i, selPoints.Count, point.Name);

                            // Is this point of a selected frame?
                            if ((from a in selFrames where a.IsPointIJ(point) select a).Count() > 1)
                            {
                                FailedJoints.Add(point.Name);
                                continue;
                            }

                            SapFrame alignFrame =
                                (from a in selFrames where a.IsPointIJ(point) select a).FirstOrDefault();

                            if (alignFrame == null)
                                alignFrame = selFrames.MinBy(a => a.PerpendicularDistance(point)).First();

                            Vector3D currentXAxis = point.LocalCoordinateSystem.XAxis;

                            // Gets the vector for the alignment
                            CoordinateSystem frameCsys = alignFrame.GetCSysFromAxesDefinitions();
                            if (AlignJointBindings.I.AlwaysUpwards_IsChecked)
                                // The X vector is pointing downwards
                                if (frameCsys.XAxis.Z < 0)
                                    frameCsys = new CoordinateSystem(Point3D.Origin, frameCsys.XAxis.ScaleBy(-1),
                                        frameCsys.YAxis, frameCsys.ZAxis.ScaleBy(-1));

                            point.SetAdvancedLocalAxesFromCoordinateSystem(frameCsys);
                            ChangeAngle.Add((point.Name, currentXAxis.AngleTo(frameCsys.XAxis.Normalize()).Degrees));
                        }
                    }

                    return (FailedJoints, ChangeAngle);
                }

                // Runs the job async
                var task =
                    new Task<(List<string> FailedJoints, List<(string jointName, double angle)> ChangeAngle)>(() =>
                        work());
                task.Start();
                await task;

                if (task.IsCompleted && !task.IsFaulted)
                {
                    StringBuilder message = new StringBuilder();

                    if (task.Result.FailedJoints.Count != 0)
                    {
                        message.AppendLine(
                            "The following selected joints could not be aligned, as it was connected to two selected frames. Therefore, it was ambiguous.");
                        foreach (string item in task.Result.FailedJoints) message.AppendLine($"{item}");
                        message.AppendLine();
                    }

                    if (task.Result.ChangeAngle.Count != 0)
                    {
                        message.AppendLine(
                            "The following table contains the angles that were changed in each altered joint.");
                        message.AppendLine($"Joint ShortName\tAngle");
                        foreach ((string jointName, double angle) item in task.Result.ChangeAngle)
                            message.AppendLine($"{item.jointName}\t{item.angle}");
                    }

                    MessageOverlay.ShowOverlay("Align Joint Results", message.ToString());
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox(
                    $"Could not align the points to the frame.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        #endregion

        private void TESTER2_Click(object sender, RoutedEventArgs e)
        {
            var files = Directory.GetFiles(
                @"C:\Users\EngRafaelSMacedo\Google Drive\000 - PROJECTS\041 - MBJ Disney Canopy\18 - Resequencing\0 - Updated Docs\Sap Images");

            int pngCounter = 1;
            foreach (string file in files.OrderBy(a => a))
                if (Path.GetExtension(file).ToLower() == ".png")
                {
                    // Renames the file to the mask
                    File.Move(file,
                        Path.Combine(Path.GetDirectoryName(file), $"STEP_{pngCounter:000}{Path.GetExtension(file)}"));

                    pngCounter++;
                }
        }


        private async void TESTER3_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow(true);

            try
            {
                // Input variables - an interface shall be done
                double topElevation = 837.5625;
                double baseElevation = 0;
                double towerHeight = topElevation - baseElevation;
                double dispFactor = 1000;
                double sineFullWaves = 0.5;

                double A = towerHeight / dispFactor;
                double periodT = towerHeight / sineFullWaves;
                double B = 2 * Math.PI / periodT;
                double C = towerHeight; // To the left means POSITIVE
                double D = 0d; // "Vertical" Shift of the sine - meaning shift in amplitude 

                string sineRunDirection = "+Z";
                string sineAmplitudeDirection = "+X";

                void work()
                {
                    // Gets the list of selected joints that will be displaced
                    var selPoints = S2KModel.SM.PointMan.GetSelected();

                    if (selPoints == null || selPoints.Count == 0)
                        throw new S2KHelperException("You must select the points that will be shifted!");

                    //progReporter.Report(ProgressData.SetMessage("Moving the points. [[Point: ***]]"));
                    for (int i = 0; i < selPoints.Count; i++)
                    {
                        SapPoint pnt = selPoints[i];

                        // Sets the progress
                        //progReporter.Report(ProgressData.UpdateProgress(i, selPoints.Count, pnt.Name));

                        double sineAxisValue = 0d;
                        if (sineRunDirection == "+Z") sineAxisValue = pnt.Z;
                        else
                            throw new S2KHelperException(
                                "The sine run direction is unsupported. It has to be implemented!");

                        double sineValue = A * Math.Sin(B * (sineAxisValue + C)) + D;

                        Point3D newPointPos = Point3D.NaN;

                        if (sineAmplitudeDirection == "+X") newPointPos = new Point3D(pnt.X + sineValue, pnt.Y, pnt.Z);
                        else if (sineAmplitudeDirection == "-X")
                            newPointPos = new Point3D(pnt.X - sineValue, pnt.Y, pnt.Z);
                        else
                            throw new S2KHelperException(
                                "The sine amplitude direction is unsupported. It has to be implemented!");

                        pnt.MoveTo(newPointPos, false);
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox(
                    $"Could not make the desired sinusoidal imperfect shape.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private void TESTER4_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TESTER5_Click(object sender, RoutedEventArgs e)
        {
            // Get selected frame
            var temp = S2KModel.SM.FrameMan.GetSelected();

            Regex aidSectRegex = new Regex(@"^AID_.*<(?<origsect>.*)>");
            // Checks if the section is an erection aid
            Match secMatch = aidSectRegex.Match(temp.First().Section.Name);
            if (secMatch.Success) temp.First().SetSection(secMatch.Value);
        }

        private async void MovePointOntoLineButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow(true);

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Frames from Group cc-CutStub."));
                var stubFrames = S2KModel.SM.FrameMan.GetGroup("cc-CutStub");

                //progReporter.Report(ProgressData.SetMessage("Frames from Group ZZ-Lower."));
                var lgsFrames = S2KModel.SM.FrameMan.GetGroup("ZZ-Lower");

                // We will mode the J point to be on the closest frame
                for (int i = 0; i < stubFrames.Count; i++)
                {
                    SapFrame stub = stubFrames[i];
                    //progReporter.Report(ProgressData.UpdateProgress(i, stubFrames.Count, null,$"Working on Stub {stub.Name}"));

                    SapPoint stubJ = stub.jEndPoint;

                    // Gets the closest frame from the ZZ-Lower
                    SapFrame closestLGS = lgsFrames
                        .MinBy(a => a.Line.ClosestPointTo(stubJ.Point, true).DistanceTo(stubJ.Point)).First();

                    // Gets the closest point
                    Point3D pntOnLine = closestLGS.Line.ClosestPointTo(stubJ.Point, true);

                    stubJ.MoveTo(pntOnLine);
                }

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void SelectTesterButton_Click(object sender, RoutedEventArgs e)
        {
            EnableWindow();
            DisableWindow();

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Frames from Group cc-CutStub."));
                var stubFrames = S2KModel.SM.FrameMan.GetGroup("cc-CutStub");

                //progReporter.Report(ProgressData.SetMessage("Frames from Group ZZ-Lower."));
                var lgsFrames = S2KModel.SM.FrameMan.GetGroup("ZZ-Lower");

                S2KModel.SM.ClearSelection();

                // We will mode the J point to be on the closest frame
                for (int i = 0; i < stubFrames.Count; i++)
                {
                    SapFrame stub = stubFrames[i];
                    //progReporter.Report(ProgressData.UpdateProgress(i, stubFrames.Count, null,$"Selecting Point and Closest Frame {stub.Name}"));

                    SapPoint stubJ = stub.jEndPoint;
                    stubJ.Select();

                    //// Gets the closest frame
                    //SapFrame closest = lgsFrames.MinBy(a => a.Line.ClosestPointTo(stubJ.Point, true).DistanceTo(stubJ.Point)).First();

                    //closest.Select();

                    // Gets the LGS linked frames from both sides
                    foreach (SapFrame item in lgsFrames)
                        if (item.IsPointIJ(stubJ))
                            item.Select();
                }

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void TempCanopy_AddAidWeldGrp_Button_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Getting Selected Frames."));
                var selFrames = S2KModel.SM.FrameMan.GetSelected();

                Regex baseSecRegex = new Regex(@"^.*\<(?<sec>.*)\>");

                // We will mode the J point to be on the closest frame
                for (int i = 0; i < selFrames.Count; i++)
                {
                    SapFrame stub = selFrames[i];
                    //progReporter.Report(ProgressData.UpdateProgress(i, selFrames.Count, null, $"Working on {stub.Name}"));

                    string baseSecName = baseSecRegex.Match(stub.Section.Name).Groups["sec"].Value;

                    stub.AddGroup($"AID_LGS_BASE <{baseSecName}>");
                    stub.AddGroup($"STGW{SelectionInfoBindings.I.STGWWeld}_AID_LGS_BASE <{baseSecName}>");
                }

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void TempCanopy_RemoveAidWeldGrp_Button_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Getting Selected Frames."));
                var selFrames = S2KModel.SM.FrameMan.GetSelected();

                // We will mode the J point to be on the closest frame
                for (int i = 0; i < selFrames.Count; i++)
                {
                    SapFrame stub = selFrames[i];
                    //progReporter.Report(ProgressData.UpdateProgress(i, selFrames.Count, null, $"Working on {stub.Name}"));

                    foreach (string group in stub.Groups)
                        if (@group.StartsWith("STGW1_") ||
                            @group.StartsWith("STGW2_") ||
                            @group.StartsWith("STGW3_") ||
                            @group.StartsWith("AID_LGS_"))
                            stub.RemoveGroup(@group);
                }

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void MergeABGroups_Button_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Getting Selected Frames."));
                var selFrames = S2KModel.SM.FrameMan.GetSelected();

                var ABGroups = new HashSet<string>();

                Regex grpA = new Regex(@"^A\d\d.*");
                Regex grpB = new Regex(@"^B\d\d.*");

                foreach (SapFrame stub in selFrames)
                foreach (string group in stub.Groups)
                    if (grpA.IsMatch(@group) || grpB.IsMatch(@group))
                        ABGroups.Add(@group);

                foreach (string group in ABGroups)
                foreach (SapFrame frame in selFrames)
                    if (!frame.Groups.Contains(@group))
                        frame.AddGroup(@group);

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void OnlyAGroup_Button_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Getting Selected Frames."));
                var selFrames = S2KModel.SM.FrameMan.GetSelected();

                Regex grpB = new Regex(@"^B\d\d.*");

                foreach (SapFrame stub in selFrames)
                foreach (string group in stub.Groups)
                    if (grpB.IsMatch(@group))
                        stub.RemoveGroup(@group);

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void OnlyBGroup_Button_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            bool work()
            {
                // Gets the Frames from the Groups
                //progReporter.Report(ProgressData.SetMessage("Getting Selected Frames."));
                var selFrames = S2KModel.SM.FrameMan.GetSelected();

                Regex grpA = new Regex(@"^A\d\d.*");

                foreach (SapFrame stub in selFrames)
                foreach (string group in stub.Groups)
                    if (grpA.IsMatch(@group))
                        stub.RemoveGroup(@group);

                return true;
            }

            // Runs the job async
            var task = new Task<bool>(() => work());
            task.Start();
            await task;

            //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());

            S2KModel.SM.RefreshView();
            EnableWindow();
        }

        private async void SelectMisc_FromClipboard_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // first, reads the clipboard
                string clipText = Clipboard.GetText();

                if (string.IsNullOrWhiteSpace(clipText))
                    S2KStaticMethods.ShowWarningMessageBox("The clipboard does not contain text.");

                var clipVals = clipText.Split(new string[] {"\t", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (clipVals.Count == 0)
                    S2KStaticMethods.ShowWarningMessageBox(
                        "Could not convert get the list of names from the Clipboard's text.");

                DisableWindow();

                // The async body
                List<string> work()
                {
                    if (!SelectionInfoBindings.I.SelectMisc_FromClipboard_SelectNotInListIsChecked)
                    {
                        var notSelected = new List<string>();

                        for (int i = 0; i < clipVals.Count; i++)
                        {
                            string currItem = clipVals[i];

                            if (SelectionInfoBindings.I.SelectMisc_FromClipboard_FrameIsChecked)
                            {
                                //progReporter.Report(ProgressData.UpdateProgress(i, clipVals.Count, null,$"Selecting Frame: {currItem}"));
                                if (!S2KModel.SM.Select(currItem, SapObjectType.Frame))
                                {
                                    notSelected.Add($"Frame: {currItem}");
                                }
                                else
                                {
                                    // Gets the frame and selects its joints
                                    SapFrame frame = S2KModel.SM.FrameMan.GetByName(currItem);
                                    if (frame != null)
                                    {
                                        frame.iEndPoint.Select();
                                        frame.jEndPoint.Select();
                                    }
                                }
                            }

                            if (SelectionInfoBindings.I.SelectMisc_FromClipboard_AreaIsChecked)
                            {
                                //progReporter.Report(ProgressData.UpdateProgress(i, clipVals.Count, null,$"Selecting Area: {currItem}"));
                                if (!S2KModel.SM.Select(currItem, SapObjectType.Area)) notSelected.Add($"Area: {currItem}");
                            }

                            if (SelectionInfoBindings.I.SelectMisc_FromClipboard_CableIsChecked)
                            {
                                //progReporter.Report(ProgressData.UpdateProgress(i, clipVals.Count, null,$"Selecting Cable: {currItem}"));
                                if (!S2KModel.SM.Select(currItem, SapObjectType.Cable))
                                    notSelected.Add($"Cable: {currItem}");
                            }

                            if (SelectionInfoBindings.I.SelectMisc_FromClipboard_JointIsChecked)
                            {
                                //progReporter.Report(ProgressData.UpdateProgress(i, clipVals.Count, null,$"Selecting Joint: {currItem}"));
                                if (!S2KModel.SM.Select(currItem, SapObjectType.Point))
                                    notSelected.Add($"Joint: {currItem}");
                            }

                            if (SelectionInfoBindings.I.SelectMisc_FromClipboard_LinkIsChecked)
                            {
                                //progReporter.Report(ProgressData.UpdateProgress(i, clipVals.Count, null,$"Selecting Link: {currItem}"));
                                if (!S2KModel.SM.Select(currItem, SapObjectType.Link)) notSelected.Add($"Link: {currItem}");
                            }
                        }

                        return notSelected;
                    }
                    else
                    {
                        // Gets all model entities
                        List<SapObject> relatedObjects = new List<SapObject>();

                        if (SelectionInfoBindings.I.SelectMisc_FromClipboard_FrameIsChecked)
                        {
                            relatedObjects.AddRange(S2KModel.SM.FrameMan.GetAll());
                        }

                        if (SelectionInfoBindings.I.SelectMisc_FromClipboard_AreaIsChecked)
                        {
                            relatedObjects.AddRange(S2KModel.SM.AreaMan.GetAll());
                        }

                        if (SelectionInfoBindings.I.SelectMisc_FromClipboard_CableIsChecked)
                        {
                            relatedObjects.AddRange(S2KModel.SM.CableMan.GetAll());
                        }

                        if (SelectionInfoBindings.I.SelectMisc_FromClipboard_JointIsChecked)
                        {
                            relatedObjects.AddRange(S2KModel.SM.PointMan.GetAll());
                        }

                        if (SelectionInfoBindings.I.SelectMisc_FromClipboard_LinkIsChecked)
                        {
                            relatedObjects.AddRange(S2KModel.SM.LinkMan.GetAll());
                        }

                        foreach (SapObject relatedObject in relatedObjects)
                        {
                            if (!clipVals.Contains(relatedObject.Name))
                            {
                                relatedObject.Select();
                            }
                        }

                        return new List<string>();
                    }
                }

                // Runs the job async
                var task = new Task<List<string>>(() => work());
                task.Start();
                await task;

                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted || task.Result.Count != 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("The following items could not be selected:");

                    foreach (string item in task.Result)
                        sb.AppendLine(item);

                    S2KStaticMethods.ShowWarningMessageBox(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox($"Could not select the items from the clipboard.", ex);
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        #region TAB - SQLite

        private void BrowseS2KForTableReadButton_Click(object sender, RoutedEventArgs e)
        {
            // Selects the Excel file in the view thread
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "S2K file (*.s2k)|*.s2k",
                DefaultExt = "*.s2k",
                Title = "Select the s2k text file with the SAP2000 tables",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
            };
            var ofdret = ofd.ShowDialog();

            if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
            {
                S2KStaticMethods.ShowWarningMessageBox($"Please select a proper s2k file!{Environment.NewLine}");
                SQLiteBindings.I.S2KFileName = string.Empty;
                return; // Aborts the Open File
            }

            SQLiteBindings.I.S2KFileName = ofd.FileName;
        }

        private async void ConvertS2KTablesToSQLiteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow(); // And also hides SAP2000

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Reading the contents of the S2K File and sending to the SQLite database."));

                    // First, opens the S2K File
                    string readBuffer;
                    string fullLine = string.Empty;

                    FileInfo fInfo = new FileInfo(SQLiteBindings.I.S2KFileName);
                    long fileTotalSize = fInfo.Length;
                    long alreadyRead = 0L;
                    var tableLines = new List<Match>();

                    DataSet transformedTables = new DataSet("SQLite_Tables");
                    SQLiteTable? currentTableType = null;
                    DataTable currentTable = null;

                    // Opens the SQLite file
                    SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder();
                    connectionStringBuilder.DataSource = SQLiteBindings.I.SQLiteFileName;
                    using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                    {
                        sqliteConn.Open();

                        // Declares a local function to send the buffer to the SQLite file
                        void local_SendBuffer()
                        {
                            if (tableLines.Count > 0)
                            {
                                // Sends to the database
                                SQLiteBindings.I.SQLiteCommand_AddS2KLinesToTable(sqliteConn, tableLines,
                                    currentTableType.Value);
                                tableLines.Clear();
                            }
                        }

                        using (StreamReader file = new StreamReader(SQLiteBindings.I.S2KFileName))
                        {
                            while (true)
                            {
                                // Reads lines and concatenates if added a stupid _
                                readBuffer = file.ReadLine();
                                if (readBuffer is null)
                                {
                                    local_SendBuffer();
                                    break;
                                }

                                alreadyRead += readBuffer.Length + 2;
                                fullLine = readBuffer;

                                // Is it a line to ignore?
                                if (fullLine.StartsWith("File")) continue;
                                if (fullLine.StartsWith("END")) continue;
                                if (string.IsNullOrWhiteSpace(fullLine)) continue;

                                while (true)
                                {
                                    // Does it finish with _?
                                    if (!(fullLine[fullLine.Length - 2] == ' ' && fullLine[fullLine.Length - 1] == '_'))
                                        break;

                                    // Removes the last _
                                    fullLine = fullLine.Substring(0, fullLine.Length - 2);

                                    readBuffer = file.ReadLine();
                                    if (readBuffer is null)
                                        throw new S2KHelperException(
                                            "Unexpected S2K File Format - The last line finished with a < _>");
                                    alreadyRead += readBuffer.Length + 2;
                                    fullLine += readBuffer;
                                }

                                // Is it a table name?
                                Match tableNameMatch = SQLiteBindings.I.tableStartRegex.Match(fullLine);
                                if (tableNameMatch.Success)
                                {
                                    // Saves the current lines in the buffer to the previous table
                                    local_SendBuffer();

                                    currentTableType = SQLiteBindings.MatchTableType(tableNameMatch.Groups["tableName"].Value);

                                    // Adds it to the DataSet
                                    currentTable = SQLiteBindings.I.RegexTableDic[currentTableType.Value].table;
                                    transformedTables.Tables.Add(currentTable);

                                    // Creates the table in the SQLite
                                    SQLiteBindings.I.SQLiteCommand_CreateTable(sqliteConn, currentTableType.Value);

                                    continue;
                                }

                                // The line has table data!
                                Match lineMatch = SQLiteBindings.I.RegexTableDic[currentTableType.Value].regex
                                    .Match(fullLine);
                                if (!lineMatch.Success)
                                    throw new S2KHelperException(
                                        $"Error processing the line {fullLine}. Table attempted: {currentTableType.Value.ToString()}");

                                //SQLiteBindings.AddS2KLineToTable(lineMatch, currentTableType.Value, currentTable);
                                tableLines.Add(lineMatch);

                                if (tableLines.Count > 1000)
                                {
                                    local_SendBuffer();
                                    //progReporter.Report(ProgressData.UpdateProgress(alreadyRead, fileTotalSize));
                                }

                                fullLine = string.Empty;
                            }
                        }

                        //progReporter.Report(ProgressData.Reset());

                        // We have the tables -> send them to the DataBase


                        //foreach (DataTable table in transformedTables.Tables)
                        //{
                        //    //progReporter.Report(ProgressData.SetMessage($"Sending Table {table.TableName} to the SQLite database."));

                        //    SQLiteTransaction transaction = sqliteConn.BeginTransaction();

                        //    for (int i = 0; i < table.Rows.Count; i++)
                        //    {
                        //        using (SQLiteCommand sqlitecommand = new SQLiteCommand(SQLiteBindings.GetSQLiteCommandFromRow(table.Rows[i]), sqliteConn))
                        //        {
                        //            sqlitecommand.ExecuteNonQuery();
                        //        }

                        //        if (i % 5000 == 0)
                        //        {
                        //            transaction.Commit();
                        //            //transaction.Dispose();
                        //            transaction = sqliteConn.BeginTransaction();

                        //            //progReporter.Report(ProgressData.UpdateProgress(i, table.Rows.Count));
                        //        }
                        //    }

                        //    transaction.Commit();
                        //    //transaction.Dispose();
                        //}
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private void BrowseSQLiteForManipButton_Click(object sender, RoutedEventArgs e)
        {
            // Selects the Excel file in the view thread
            SaveFileDialog ofd = new SaveFileDialog
            {
                Filter = "DB file (*.db)|*.db",
                DefaultExt = "*.db",
                Title = "Select the SQLite .db file that will have the tables written",
                CheckPathExists = true,
                OverwritePrompt = false
            };
            var ofdret = ofd.ShowDialog();

            if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
            {
                S2KStaticMethods.ShowWarningMessageBox($"Please select a proper SQLite DB file!{Environment.NewLine}");
                SQLiteBindings.I.SQLiteFileName = string.Empty;
                return; // Aborts the Open File
            }

            // Checks if the file exists
            FileInfo fInfo = new FileInfo(ofd.FileName);

            if (!fInfo.Exists)
            {
                SQLiteConnection.CreateFile(fInfo.FullName);

                // Creates the new SQLite File
                SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder();
                connectionStringBuilder.DataSource = ofd.FileName;

                using (SQLiteConnection connection = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                {
                    connection.Open();
                }
            }

            SQLiteBindings.I.SQLiteFileName = ofd.FileName;
        }

        private void GetSelectedResultTablesToSQLiteButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region TAB - Disney Canopy

        private async void DisneyRebuildFromKHExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Selects the Excel file in the view thread
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "Excel file (*.xls;*.xlsx)|*.xls;*.xlsx",
                    DefaultExt = "*.xls;*.xlsx",
                    Title = "Select the Excel File With The Correct Format!",
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                var ofdret = ofd.ShowDialog();

                if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
                {
                    S2KStaticMethods.ShowWarningMessageBox($"Please select a proper Excel file!{Environment.NewLine}");
                    return; // Aborts the Open File
                }

                DisableWindow(); // And also hides SAP2000

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Reading the Excel Input.", true));

                    DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName);

                    // Reads the Excel
                    using (FileStream stream = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        // Auto-detect format, supports:
                        //  - Binary Excel files (2.0-2003 format; *.xls)
                        //  - OpenXml Excel files (2007 format; *.xlsx)
                        using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            // 2. Use the AsDataSet extension method
                            fromExcel = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                                // property in a second pass.
                                UseColumnDataType = false,

                                // Gets or sets a callback to determine whether to include the current sheet
                                // in the DataSet. Called once per sheet before ConfigureDataTable.
                                FilterSheet = (tableReader, sheetIndex) => sheetIndex <= 4 ? true : false,

                                // Gets or sets a callback to obtain configuration options for a DataTable. 
                                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                                {
                                    // Gets or sets a value indicating the prefix of generated column names.
                                    EmptyColumnNamePrefix = "Column",

                                    // Gets or sets a value indicating whether to use a row from the 
                                    // data as column names.
                                    UseHeaderRow = true,

                                    // Gets or sets a callback to determine which row is the header row. 
                                    // Only called when UseHeaderRow = true.
                                    ReadHeaderRow = (rowReader) =>
                                    {
                                        // F.ex skip the first row and use the 2nd row as column headers:
                                        //rowReader.Read();
                                    },

                                    // Gets or sets a callback to determine whether to include the 
                                    // current row in the DataTable.
                                    FilterRow = (rowReader) => { return true; },

                                    // Gets or sets a callback to determine whether to include the specific
                                    // column in the DataTable. Called once per column after reading the 
                                    // headers.
                                    FilterColumn = (rowReader, columnIndex) => { return true; }
                                }
                            });
                        }
                    }

                    DataTable tJoints = fromExcel.Tables["JOINTS"];
                    DataTable tCurvedTreated = fromExcel.Tables["CURVED_TREATED"];
                    DataTable tStraight = fromExcel.Tables["STRAIGHT"];
                    DataTable tOrient = fromExcel.Tables["ORIENT"];
                    DataTable tSecMap = fromExcel.Tables["SECMAP"];

                    double MergeTol = S2KModel.SM.MergeTolerance;

                    // Fixes the point names as KH has duplicate - same position with different names
                    var namedJoints = new List<dynamic>();
                    foreach (DataRow jRow in tJoints.AsEnumerable())
                    {
                        Point3D currentPoint = new Point3D((double) jRow["X [in.]"], (double) jRow["Y [in.]"],
                            (double) jRow["Z [in.]"]);

                        dynamic alreadySet =
                            (from a in namedJoints where a.Point.DistanceTo(currentPoint) < 0.05 select a)
                            .FirstOrDefault();
                        if (alreadySet == null)
                        {
                            dynamic dynObj = new ExpandoObject() { };
                            dynObj.Name = jRow["S2KName"].ToString();
                            dynObj.Point = currentPoint;
                            namedJoints.Add(dynObj);
                        }
                        else
                        {
                            if (!alreadySet.Name.Contains(jRow["S2KName"].ToString()))
                                for (int i = 0; i < namedJoints.Count; i++)
                                {
                                    dynamic item = (dynamic) namedJoints[i];
                                    if (item.Name == alreadySet.Name) item.Name += "***" + jRow["S2KName"].ToString();
                                }
                        }
                    }


                    // Generates the Group Names
                    //progReporter.Report(ProgressData.SetMessage("Generating the new Group Names", true));
                    var allGrps = (from a in tCurvedTreated.AsEnumerable() select a["S2KClass"].ToString()).Concat(
                        from a in tStraight.AsEnumerable() select a["S2KClass"].ToString());
                    foreach (string grpName in allGrps.Distinct()) S2KModel.SM.GroupMan.AddGroup(grpName);

                    // Inputs all the joints
                    //progReporter.Report(ProgressData.SetMessage("Entering all Joints in SAP2000 Model. [[Joint ShortName ***]]"));
                    var enteredJointName = new HashSet<string>();
                    for (int i = 0; i < namedJoints.Count; i++)
                    {
                        dynamic joint = namedJoints[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, namedJoints.Count, joint.Name));

                        // Does the name already exists?
                        SapPoint addedPnt = S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(joint.Point, joint.Name);
                        if (addedPnt == null || addedPnt.Name != joint.Name)
                            throw new S2KHelperException($"Could not add joint named {joint.Name} to model.");
                    }

                    // Lets input the Straight frames
                    //progReporter.Report(ProgressData.SetMessage("Entering all Straight Frames in SAP2000 Model. [[Frame ShortName ***]]"));
                    var enteredFrameName = new HashSet<string>();
                    for (int i = 0; i < tStraight.Rows.Count; i++)
                    {
                        DataRow straightRow = tStraight.Rows[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, tStraight.Rows.Count,straightRow["S2KName"].ToString()));

                        if (enteredFrameName.Add(straightRow["S2KName"].ToString()))
                        {
                            // Finds the name of the joints
                            string iName = (from a in namedJoints
                                where a.Name.Contains(straightRow["S2KStartName"].ToString())
                                select a.Name).First();
                            string jName = (from a in namedJoints
                                where a.Name.Contains(straightRow["S2KEndName"].ToString())
                                select a.Name).First();

                            SapFrame addedFrame = S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(iName, jName,
                                straightRow["S2KSect"].ToString(), straightRow["S2KName"].ToString());

                            if (addedFrame == null || addedFrame.Name != straightRow["S2KName"].ToString())
                                throw new S2KHelperException(
                                    $"Could not add frame named {straightRow["S2KName"].ToString()} to model.");

                            // Adds it to the Groups
                            addedFrame.AddGroup(straightRow["S2KClass"].ToString());
                        }
                    }

                    // Lets input the Curved frames
                    //progReporter.Report(ProgressData.SetMessage("Entering all \"Curved\" Frames in SAP2000 Model. [[Frame ShortName ***]]"));
                    for (int i = 0; i < tCurvedTreated.Rows.Count; i++)
                    {
                        DataRow curvedRow = tCurvedTreated.Rows[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, tCurvedTreated.Rows.Count,curvedRow["S2KName"].ToString()));

                        if (enteredFrameName.Add(curvedRow["S2KName"].ToString()))
                        {
                            // Finds the name of the joints
                            string iName = (from a in namedJoints
                                where a.Name.Contains(curvedRow["S2KStartName"].ToString())
                                select a.Name).First();
                            string jName = (from a in namedJoints
                                where a.Name.Contains(curvedRow["S2KEndName"].ToString())
                                select a.Name).First();

                            SapFrame addedFrame = S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(iName, jName,
                                curvedRow["S2KSect"].ToString(), curvedRow["S2KName"].ToString());

                            if (addedFrame == null || addedFrame.Name != curvedRow["S2KName"].ToString())
                                throw new S2KHelperException(
                                    $"Could not add frame named {curvedRow["S2KName"].ToString()} to model.");

                            // Adds it to the Groups
                            addedFrame.AddGroup(curvedRow["S2KClass"].ToString());
                        }
                    }

                    //progReporter.Report(ProgressData.SetMessage("Fixing the alignment of the members. [[Frame ShortName ***]]"));
                    for (int i = 0; i < tOrient.Rows.Count; i++)
                    {
                        DataRow orientRow = tOrient.Rows[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, tOrient.Rows.Count,orientRow["S2KName"].ToString()));

                        // Gets the frame
                        SapFrame frame = S2KModel.SM.FrameMan.GetByName(orientRow["S2KName"].ToString());
                        frame.SetAdvancedAxis(FrameAdvancedAxes_Plane2.Plane13,
                            new Vector3D((double) orientRow["X"], (double) orientRow["Y"], (double) orientRow["Z"]));
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyEnsureLGSUpwardsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Lower Gridshell members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_LOWER-LEVEL");

                    foreach (SapFrame item in workingList)
                    {
                        UnitVector3D local1 = item.Line.Direction;

                        Vector3D local3 = item.AdvancedLocalAxes.PlVect_Vector;
                        Vector3D local2 = local3.Rotate(local1, Angle.FromDegrees(-90));

                        // Going down
                        if (local2.Z < 0)
                            item.SetAdvancedAxis(FrameAdvancedAxes_Plane2.Plane13,
                                item.AdvancedLocalAxes.PlVect_Vector.Negate());
                    }

                    S2KModel.SM.RefreshView();
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyFlipSelectedLGSAxisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Selected LGS Frames", true));
                    var workingList = S2KModel.SM.FrameMan.GetSelected();

                    foreach (SapFrame item in workingList)
                        item.SetAdvancedAxis(FrameAdvancedAxes_Plane2.Plane13,
                            item.AdvancedLocalAxes.PlVect_Vector.Negate());

                    S2KModel.SM.RefreshView();
                }


                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyGetCurvedFrameDefButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Selects the Excel file in the view thread
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "Excel file (*.xls;*.xlsx)|*.xls;*.xlsx",
                    DefaultExt = "*.xls;*.xlsx",
                    Title = "Select the Excel File With The Correct Format!",
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                var ofdret = ofd.ShowDialog();

                if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
                {
                    S2KStaticMethods.ShowWarningMessageBox($"Please select a proper Excel file!{Environment.NewLine}");
                    return; // Aborts the Open File
                }

                DisableWindow(true); // And also hides SAP2000

                // The async body
                string work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Reading the Excel Input of Curved Data", true));

                    DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName);

                    // Reads the Excel
                    using (FileStream stream = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        // Auto-detect format, supports:
                        //  - Binary Excel files (2.0-2003 format; *.xls)
                        //  - OpenXml Excel files (2007 format; *.xlsx)
                        using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            // 2. Use the AsDataSet extension method
                            fromExcel = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                                // property in a second pass.
                                UseColumnDataType = false,

                                // Gets or sets a callback to determine whether to include the current sheet
                                // in the DataSet. Called once per sheet before ConfigureDataTable.
                                FilterSheet = (tableReader, sheetIndex) =>
                                    sheetIndex == 5 || sheetIndex == 0 ? true : false,

                                // Gets or sets a callback to obtain configuration options for a DataTable. 
                                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                                {
                                    // Gets or sets a value indicating the prefix of generated column names.
                                    EmptyColumnNamePrefix = "Column",

                                    // Gets or sets a value indicating whether to use a row from the 
                                    // data as column names.
                                    UseHeaderRow = true,

                                    // Gets or sets a callback to determine which row is the header row. 
                                    // Only called when UseHeaderRow = true.
                                    ReadHeaderRow = (rowReader) =>
                                    {
                                        // F.ex skip the first row and use the 2nd row as column headers:
                                        //rowReader.Read();
                                    },

                                    // Gets or sets a callback to determine whether to include the 
                                    // current row in the DataTable.
                                    FilterRow = (rowReader) => { return true; },

                                    // Gets or sets a callback to determine whether to include the specific
                                    // column in the DataTable. Called once per column after reading the 
                                    // headers.
                                    FilterColumn = (rowReader, columnIndex) => { return true; }
                                }
                            });
                        }
                    }

                    DataTable tCurved = fromExcel.Tables["CURVED"];
                    DataTable tJoints = fromExcel.Tables["JOINTS"];

                    StringBuilder sbLines = new StringBuilder();
                    sbLines.AppendLine("S2KName,S2KSect,S2KClass,S2KStartName,S2KEndName");

                    StringBuilder sbPoints = new StringBuilder();
                    sbPoints.AppendLine("X [in.],Y [in.],Z [in.],S2KName");


                    ////progReporter.Report(ProgressData.SetMessage("Opening the Rhino Interface", true));
                    //using (RhinoModel RhinoHelper = new RhinoModel())
                    //{
                    //    //progReporter.Report(ProgressData.SetMessage("Treating the curves. [[CurveName: ***]]", true));
                    //    for (int i = 0; i < tCurved.Rows.Count; i++)
                    //    {
                    //        DataRow curve = tCurved.Rows[i];
                    //        //progReporter.Report(ProgressData.UpdateProgress(i, tCurved.Rows.Count,curve["S2KName"].ToString()));

                    //        // Gets the first joint
                    //        DataRow startJointRow = (from a in tJoints.AsEnumerable()
                    //            where a["S2KName"].ToString() == curve["S2KStartName"].ToString()
                    //            select a).First();
                    //        Point3D startJoint = new Point3D(startJointRow.Field<double>("X [in.]"),
                    //            startJointRow.Field<double>("Y [in.]"), startJointRow.Field<double>("Z [in.]"));

                    //        // Gets the end joint
                    //        DataRow endJointRow = (from a in tJoints.AsEnumerable()
                    //            where a["S2KName"].ToString() == curve["S2KEndName"].ToString()
                    //            select a).First();
                    //        Point3D endJoint = new Point3D(endJointRow.Field<double>("X [in.]"),
                    //            endJointRow.Field<double>("Y [in.]"), endJointRow.Field<double>("Z [in.]"));

                    //        // Gets the mid joint
                    //        Point3D midJoint = new Point3D(curve.Field<double>("MIDPOINT X [in.]"),
                    //            curve.Field<double>("MIDPOINT Y [in.]"), curve.Field<double>("MIDPOINT Z [in.]"));

                    //        // Is it a straight line?
                    //        int numberSegments;
                    //        Point3D[] arcPoints;
                    //        if (startJoint.AddVector(startJoint.VectorTo(endJoint).ScaleBy(0.5)).DistanceTo(midJoint) <=
                    //            0.01)
                    //        {
                    //            numberSegments = 2;
                    //            arcPoints = new Point3D[] {startJoint, midJoint, endJoint};
                    //        }
                    //        else
                    //        {
                    //            numberSegments = 6;
                    //            arcPoints = RhinoHelper.GetPointListAlongArc(startJoint, midJoint, endJoint,
                    //                numberSegments);
                    //        }

                    //        var NamedArcPoints = new List<(string Name, Point3D arcPoint)>();

                    //        // Adds the first joint
                    //        NamedArcPoints.Add((curve["S2KStartName"].ToString(), startJoint));
                    //        // Adds the other joints
                    //        for (int j = 1; j < arcPoints.Length - 1; j++)
                    //            if (arcPoints[j].DistanceTo(midJoint) < 0.1)
                    //                // Adds the mid joint
                    //                NamedArcPoints.Add(($"{curve.Field<string>("S2KName")}_IJ_MID", midJoint));
                    //            else
                    //                // Adds the intermediate joint
                    //                NamedArcPoints.Add(($"{curve.Field<string>("S2KName")}_IJ_{j}", arcPoints[j]));
                    //        // Adds the end joint
                    //        NamedArcPoints.Add((curve["S2KEndName"].ToString(), endJoint));

                    //        // Adds the joints and the lines
                    //        var pntNamesAlreadyAdded = new HashSet<string>();
                    //        for (int j = 0; j < numberSegments; j++)
                    //        {
                    //            // Writes the joint data
                    //            if (pntNamesAlreadyAdded.Add(NamedArcPoints[j].Name))
                    //                sbPoints.AppendLine(
                    //                    $"{NamedArcPoints[j].arcPoint.X:F3},{NamedArcPoints[j].arcPoint.Y:F3},{NamedArcPoints[j].arcPoint.Z:F3},{NamedArcPoints[j].Name}");
                    //            if (pntNamesAlreadyAdded.Add(NamedArcPoints[j + 1].Name))
                    //                sbPoints.AppendLine(
                    //                    $"{NamedArcPoints[j + 1].arcPoint.X:F3},{NamedArcPoints[j + 1].arcPoint.Y:F3},{NamedArcPoints[j + 1].arcPoint.Z:F3},{NamedArcPoints[j + 1].Name}");

                    //            // Writes the Frame Data "S2KName,S2KSect,S2KClass,S2KStartName,S2KEndName"
                    //            sbLines.AppendLine(
                    //                $"{curve.Field<string>("S2KName")}_S{j},{curve.Field<string>("S2KSect")},{curve.Field<string>("S2KClass")},{NamedArcPoints[j].Name},{NamedArcPoints[j + 1].Name}");
                    //        }
                    //    }
                    //}

                    // Merges it all for the clipboard
                    sbLines.AppendLine();
                    sbLines.AppendLine();
                    sbLines.AppendLine();

                    sbLines.Append(sbPoints);

                    return sbLines.ToString();
                }

                // Runs the job async
                var task = new Task<string>(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                    Clipboard.SetText(task.Result);
                    MessageBox.Show("The data is in the Clipboard.", "Data in clipboard", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private void DisneyEnsureConstraints(List<SapFrame> workingList, List<SapFrame> mustHaves, bool considerRestraint = false)
        {
            // Starts the buffer
            S2KModel.SM.JointConstraintMan.ResetPointConstraintBuffer();

            //progReporter.Report(ProgressData.SetMessage("Treating the frames. [[Frame: ***]]"));
            for (int i = 0; i < workingList.Count; i++)
            {
                SapFrame wFrame = workingList[i];
                //progReporter.Report(ProgressData.UpdateProgress(i, workingList.Count, wFrame.Name));

                //// Debug
                //S2KModel.SM.ClearSelection();
                //wFrame.Select();

                foreach (SapPoint point in wFrame.BothPoints)
                {
                    if (considerRestraint)
                        if (point.Restraints.RestraintType != PointRestraintType.HasNone)
                            continue;

                    var linkedFrames = point.GetAllConnectedFramesAlsoLinkedByConstraints();

                    bool oneMatch = mustHaves.Where(c => c.Name != wFrame.Name)
                        .Select(a => a.Name) // Removes self from the list of MustHaves
                        .Intersect(linkedFrames.Select(b => b.Name))
                        .Any();

                    if (!oneMatch)
                    {
                        // Gets closest frame to point
                        SapFrame closestFrame = mustHaves.Where(c => c.Name != wFrame.Name)
                            .MinBy(a => a.PerpendicularDistance(point)).FirstOrDefault();
                        if (closestFrame == null)
                            throw new S2KHelperException(
                                $"Could not get the closest frame in must-have list to point called {point.Name}.");

                        // Gets the closest point to the closest frame
                        Point3D point3DAtFrame = closestFrame.Line.ClosestPointTo(point.Point, true);

                        // The distance is larger than the tolerance - the points won't merge
                        if (point3DAtFrame.DistanceTo(point.Point) > S2KModel.SM.MergeTolerance)
                        {
                            // Adds this new point to SAP2000
                            string newPointName = $"KH_Link_{S2KStaticMethods.UniqueName(6)}";
                            SapPoint sapPointAtFrame =
                                S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(point3DAtFrame, newPointName);

                            // Breaks the frame at this point
                            var framePieces = closestFrame.DivideAtIntersectPoint(sapPointAtFrame, "_P");
                            mustHaves.RemoveAll(a => a.Name == closestFrame.Name);
                            mustHaves.AddRange(framePieces);

                            // Links the two points using a new constraint
                            string cName = "B_" + newPointName;
                            if (!S2KModel.SM.JointConstraintMan.SetBodyConstraint(cName,
                                new bool[] {true, true, true, true, true, true}))
                                throw new S2KHelperException(
                                    $"Could not create body contraint named {cName} that should link points {point.Name} and {sapPointAtFrame.Name}.");

                            if (!point.AddJointConstraint(cName, false))
                                throw new S2KHelperException($"Could not add contraint {cName} to point {point.Name}.");
                            if (!sapPointAtFrame.AddJointConstraint(cName, false))
                                throw new S2KHelperException($"Could not add contraint {cName} to point {point.Name}.");
                        }
                        else // The distance is smaller than the tolerance - the points would merge.
                        {
                            // Breaks the frame at the point
                            var framePieces = closestFrame.DivideAtIntersectPoint(point, "_P");
                            mustHaves.RemoveAll(a => a.Name == closestFrame.Name);
                            mustHaves.AddRange(framePieces);
                        }
                    }
                }
            }
        }

        private async void DisneyConstraintUpstandsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow(true);

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all upstands", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_UPSTANDS");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Upper and Lower Gridshell Frames", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_UPPER-LEVEL");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_LOWER-LEVEL"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintLowerGridshellButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Lower Gridshell members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_LOWER-LEVEL");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Upstand, Inner Perimeter and End Truss Back Chord members", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_UPSTANDS");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_PERIM-INNER"));
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-BACK-CHORD"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintEdgeUpstandButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Edge Upstands members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_EDGE-UPSTANDS");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Upper Gridshell, Perimeter and End Truss Back Chord members", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_UPPER-LEVEL");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_PERIM-INNER"));
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-BACK-CHORD"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintUpperGridshellButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Upper Gridshell members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_UPPER-LEVEL");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Upper Gridshell, Outer Perimeter and End Truss Top Chord members", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_UPPER-LEVEL");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_PERIM-OUTER"));
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-TOP-CHORD"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintPerimeterWebButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Perimeter Web members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_PERIM-WEB");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Inner Perimeter and Outer Perimeter members", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_PERIM-OUTER");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_PERIM-INNER"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintEndTrussBracingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all End Truss Bracing members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-WEBS-BRACINGS");

                    //progReporter.Report(ProgressData.SetMessage("Getting all End Truss Back Chord, End Truss Top Chord and End Truss Bottom Chord  members",true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-BOTTOM-CHORD");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-TOP-CHORD"));
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-BACK-CHORD"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintRidgeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Ridge members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_RIDGE-BEAM");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Upstand and Upper Gridshell members", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_UPSTANDS");
                    mustHaves.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_UPPER-LEVEL"));

                    DisneyEnsureConstraints(workingList, mustHaves);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                ////StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyConstraintColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    //progReporter.Report(ProgressData.SetMessage("Getting all Column members", true));
                    var workingList = S2KModel.SM.FrameMan.GetGroup("KH_COLUMN");

                    //progReporter.Report(ProgressData.SetMessage("Getting all Inner Perimeter members", true));
                    var mustHaves = S2KModel.SM.FrameMan.GetGroup("KH_PERIM-INNER");

                    DisneyEnsureConstraints(workingList, mustHaves, true);
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyCopyAidsInformationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow(true);
                BusyOverlayBindings.I.Title = "Copying Information of the Erection Aids";

                // The async body
                string work()
                {
                    // Selects the files in the view thread
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "SAP2000 file (*.sdb)|*.sdb",
                        DefaultExt = "*.sdb;*.sdb",
                        Title = "Select the **OLD** SAP2000 File!",
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    var ofdret = ofd.ShowDialog();

                    if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
                        throw new S2KHelperException("Please select a proper SAP2000 file!");

                    // Opens the file
                    //progReporter.Report(ProgressData.SetMessage("Opening the OLD SAP2000 file.", true));
                    if (!S2KModel.SM.OpenFile(ofd.FileName))
                        throw new S2KHelperException($"Could not open the OLD file named {ofd.FileName}.");


                    // Gets the list of frames from the old model
                    //progReporter.Report(ProgressData.SetMessage("Getting all aid frames from the OLD model."));
                    var oldAids = S2KModel.SM.FrameMan.GetGroup("TEMPAID");

                    // Filters the list depending on the section
                    //progReporter.Report(ProgressData.SetMessage("Filtering the frames from the OLD model based on their sections.",true));
                    oldAids.RemoveAll(a => !a.Section.Name.StartsWith("AID_"));

                    // Now, gets for all of them their groups.
                    //progReporter.Report(ProgressData.SetMessage("Getting the AID's groups from the OLD model. [[Frame: ***]]"));
                    for (int i1 = 0; i1 < oldAids.Count; i1++)
                    {
                        SapFrame item = (SapFrame) oldAids[i1];
                        //progReporter.Report(ProgressData.UpdateProgress(i1, oldAids.Count, item.Name));
                        item.Groups.Any();
                    }

                    // Selects the files in the view thread
                    OpenFileDialog ofdNew = new OpenFileDialog
                    {
                        Filter = "SAP2000 file (*.sdb)|*.sdb",
                        DefaultExt = "*.sdb;*.sdb",
                        Title = "Select the **NEW** SAP2000 File!",
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    var ofdretNew = ofdNew.ShowDialog();

                    if (!ofdretNew.HasValue || !ofdretNew.Value || string.IsNullOrWhiteSpace(ofdNew.FileName))
                        throw new S2KHelperException("Please select a proper SAP2000 file!");


                    //progReporter.Report(ProgressData.SetMessage("Opening the NEW SAP2000 file.", true));
                    if (!S2KModel.SM.OpenFile(ofdNew.FileName))
                        throw new S2KHelperException($"Could not open the NEW file named {ofdNew.FileName}.");

                    // Gets the canopy frames from the new model.
                    //progReporter.Report(ProgressData.SetMessage("Getting the frames belonging to the 0-CANOPY group of the NEW model. [[Frame: ***]]"));
                    var canopyFrames = S2KModel.SM.FrameMan.GetGroup("0-CANOPY");

                    //progReporter.Report(ProgressData.SetMessage("Getting the frames belonging to the KH_PERIM-INNER and the KH_END-TRUSS-BACK-CHORD group of the NEW model.",true));
                    var perimFrames = S2KModel.SM.FrameMan.GetGroup("KH_PERIM-INNER");
                    perimFrames.AddRange(S2KModel.SM.FrameMan.GetGroup("KH_END-TRUSS-BACK-CHORD"));

                    // Gets the constraint buffer
                    S2KModel.SM.JointConstraintMan.ResetPointConstraintBuffer();

                    double aidLength = 2;
                    double MaxDistance = double.MinValue;

                    // Begins finding and breaking the frames
                    //progReporter.Report(ProgressData.SetMessage("Creating the Aids in the new model."));
                    Regex secRegex = new Regex(@"(?<AID_TYPE>\S*)\s+<(?<FrameSec>.*)\>");
                    for (int i = 0; i < oldAids.Count; i++)
                    {
                        SapFrame oldAid = oldAids[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, oldAids.Count));

                        // Gets the kind of aid to add
                        Match match = secRegex.Match(oldAid.Section.Name);
                        if (!match.Success)
                            throw new S2KHelperException(
                                $"Section {oldAid.Section.Name} of OLD aid {oldAid.Name} did not match the regex pattern.");

                        SapFrame closestNewFrame;
                        if (match.Groups["AID_TYPE"].Value == "AID_LGS_BASE"
                        ) // If it is a base, finds only among the LGS members
                            closestNewFrame = canopyFrames.Where(a => !a.Name.StartsWith("AID"))
                                .Where(a => a.Groups.Contains("KH_LOWER-LEVEL")).Where(b =>
                                {
                                    // Where the frames have some alignment
                                    Angle angle = b.Line.Direction.AngleTo(oldAid.Line.Direction);
                                    if (b.Line.Direction.AngleTo(oldAid.Line.Direction).Degrees < 10 ||
                                        b.Line.Direction.AngleTo(oldAid.Line.Direction).Degrees > 170) return true;
                                    else return false;
                                }).MinBy(a => a.PerpendicularDistance(oldAid.Centroid)).FirstOrDefault();
                        else
                            closestNewFrame = canopyFrames.Where(a => !a.Name.StartsWith("AID")).Where(b =>
                            {
                                // Where the frames have some alignment
                                Angle angle = b.Line.Direction.AngleTo(oldAid.Line.Direction);
                                if (b.Line.Direction.AngleTo(oldAid.Line.Direction).Degrees < 10 ||
                                    b.Line.Direction.AngleTo(oldAid.Line.Direction).Degrees > 170) return true;
                                else return false;
                            }).MinBy(a => a.PerpendicularDistance(oldAid.Centroid)).FirstOrDefault();

                        double distance = closestNewFrame.Line.ClosestPointTo(oldAid.Centroid, true)
                            .DistanceTo(oldAid.Centroid);
                        if (distance > MaxDistance) MaxDistance = distance;

                        if (closestNewFrame.Length < aidLength + 1)
                            throw new S2KHelperException($"The closest frame {closestNewFrame.Name} is too short!");

                        string newAidSection =
                            match.Groups["AID_TYPE"].Value + " <" + closestNewFrame.Section.Name + ">";

                        void Local_BreakAtEnd(SapPoint endToBreak)
                        {
                            var pieces = closestNewFrame.DivideAtDistanceFromPoint(endToBreak, aidLength, "_A");
                            // Fixes the list in the new model's collection
                            canopyFrames.RemoveAll(a => a.Name == closestNewFrame.Name);
                            canopyFrames.AddRange(pieces);
                            if (perimFrames.Any(a => a.Name == closestNewFrame.Name)
                            ) // The broken frame was a perimeter frame
                            {
                                canopyFrames.RemoveAll(a => a.Name == closestNewFrame.Name);
                                canopyFrames.AddRange(pieces);
                            }

                            // Finds the erection aid by length
                            SapFrame newAid = pieces.MinBy(a => Math.Abs(a.Length - aidLength)).First();

                            // Fixes its section
                            newAid.ChangeName("AID_" + newAid.Name);
                            newAid.SetSection(newAidSection);
                            // Copies the aid's selected groups
                            foreach (string group in oldAid.Groups.Where(a => a != "ALL"))
                            {
                                if (group.StartsWith("A") || group.StartsWith("B")) newAid.AddGroup(@group);
                                if (group == "STGW1" || group == "STGW2" || group == "STGW3")
                                {
                                    newAid.AddGroup(group);
                                    newAid.AddGroup(group + "_" + newAidSection);
                                }
                            }
                        }

                        // Makes the erection aid
                        if (match.Groups["AID_TYPE"].Value == "AID_LGS_BASE")
                        {
                            // The breaking must come at the end that is linked to an Inner Perimeter
                            SapPoint endToBreak = null;

                            if (closestNewFrame.iEndPoint.GetAllConnectedFramesAlsoLinkedByConstraints()
                                .Select(a => a.Name)
                                .Intersect(perimFrames.Select(b => b.Name))
                                .Any()) endToBreak = closestNewFrame.iEndPoint;
                            else endToBreak = closestNewFrame.jEndPoint;

                            Local_BreakAtEnd(endToBreak);
                        }
                        else
                        {
                            // Gets the distance from I of the beginning of the erection aid
                            Point3D pntOnFrame;
                            // An upstand must be broken in its middle
                            if (closestNewFrame.Section.Name.StartsWith("AID_UPSTAND"))
                                pntOnFrame = closestNewFrame.Centroid;
                            else pntOnFrame = closestNewFrame.Line.ClosestPointTo(oldAid.Centroid, true);

                            Vector3D midVectorFromI = closestNewFrame.iEndPoint.Point.VectorTo(pntOnFrame);
                            Vector3D midVectorFromJ = closestNewFrame.jEndPoint.Point.VectorTo(pntOnFrame);

                            if (midVectorFromI.Length < aidLength / 2 + 1)
                            {
                                Local_BreakAtEnd(closestNewFrame.iEndPoint);
                            }
                            else if (midVectorFromJ.Length < aidLength / 2 + 1)
                            {
                                Local_BreakAtEnd(closestNewFrame.jEndPoint);
                            }
                            else
                            {
                                double startDistanceFromI = midVectorFromI
                                    .ScaleBy((midVectorFromI.Length - 1) / midVectorFromI.Length).Length;
                                var pieces = closestNewFrame.DivideInsertNewFrameInMiddle(startDistanceFromI, aidLength,
                                    new string[] {"_AP"});

                                // Fixes the list in the new model's collection
                                canopyFrames.RemoveAll(a => a.Name == closestNewFrame.Name);
                                canopyFrames.AddRange(pieces.allFrames);
                                if (perimFrames.Any(a => a.Name == closestNewFrame.Name)
                                ) // The broken frame was a perimeter frame
                                {
                                    canopyFrames.RemoveAll(a => a.Name == closestNewFrame.Name);
                                    canopyFrames.AddRange(pieces.allFrames);
                                }

                                // Finds the erection aid by length

                                // Fixes its section
                                pieces.desired.ChangeName("AID_" + pieces.desired.Name);
                                pieces.desired.SetSection(newAidSection);
                                // Copies the aid's selected groups
                                foreach (string group in oldAid.Groups.Where(a => a != "ALL"))
                                {
                                    if (group.StartsWith("A") || group.StartsWith("B")) pieces.desired.AddGroup(@group);
                                    if (group == "STGW1" || group == "STGW2" || group == "STGW3")
                                    {
                                        pieces.desired.AddGroup(group);
                                        pieces.desired.AddGroup(group + "_" + newAidSection);
                                    }
                                }
                            }
                        }
                    }

                    return MaxDistance.ToString();
                }

                // Runs the job async
                var task = new Task<string>(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                else
                    MessageBox.Show(task.Result);
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void DisneyCopyGroupInformationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow(true);

                // The async body
                (string, string) work()
                {
                    // Selects the files in the view thread
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "SAP2000 file (*.sdb)|*.sdb",
                        DefaultExt = "*.sdb;*.sdb",
                        Title = "Select the **OLD** SAP2000 File!",
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    var ofdret = ofd.ShowDialog();

                    if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
                        throw new S2KHelperException("Please select a proper SAP2000 file!");

                    // Opens the file
                    //progReporter.Report(ProgressData.SetMessage("Opening the OLD SAP2000 file.", true));
                    if (!S2KModel.SM.OpenFile(ofd.FileName))
                        throw new S2KHelperException($"Could not open the OLD file named {ofd.FileName}.");

                    // Gets the list of frames from the old model
                    //progReporter.Report(ProgressData.SetMessage("Getting all canopy frames from the OLD model."));
                    var oldFrames = S2KModel.SM.FrameMan.GetGroup("0-CANOPY");

                    // Filters the list depending on the section
                    //progReporter.Report(ProgressData.SetMessage("Filtering the frames from the OLD model based on their sections.",true));
                    oldFrames.RemoveAll(a => a.Section.Name.StartsWith("AID_"));

                    // Now, gets for all of them their groups.
                    //progReporter.Report(ProgressData.SetMessage("Getting the frame's groups from the OLD model. [[Frame: ***]]"));
                    for (int i1 = 0; i1 < oldFrames.Count; i1++)
                    {
                        SapFrame item = (SapFrame) oldFrames[i1];
                        //progReporter.Report(ProgressData.UpdateProgress(i1, oldFrames.Count, item.Name));
                        item.Groups.Any(); // just ping
                    }

                    // Selects the files in the view thread
                    OpenFileDialog ofdNew = new OpenFileDialog
                    {
                        Filter = "SAP2000 file (*.sdb)|*.sdb",
                        DefaultExt = "*.sdb;*.sdb",
                        Title = "Select the **NEW** SAP2000 File!",
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    var ofdretNew = ofdNew.ShowDialog();

                    if (ofdretNew.HasValue && ofdretNew.Value && string.IsNullOrWhiteSpace(ofdNew.FileName))
                        throw new S2KHelperException("Please select a proper SAP2000 file!");


                    //progReporter.Report(ProgressData.SetMessage("Opening the NEW SAP2000 file.", true));
                    if (!S2KModel.SM.OpenFile(ofdNew.FileName))
                        throw new S2KHelperException($"Could not open the NEW file named {ofdNew.FileName}.");

                    // Gets the canopy frames from the new model.
                    //progReporter.Report(ProgressData.SetMessage("Getting the frames belonging to the 0-CANOPY group of the NEW model. [[Frame: ***]]"));
                    var newFrames = S2KModel.SM.FrameMan.GetGroup("0-CANOPY");
                    // Filters the list depending on the section
                    //progReporter.Report(ProgressData.SetMessage("Filtering the frames from the NEW model based on their sections.",true));
                    newFrames.RemoveAll(a => a.Section.Name.StartsWith("AID_"));

                    double MaxDistance = double.MinValue;

                    int Local_TypeBasedOnSection(string section)
                    {
                        switch (section)
                        {
                            case "HSS 10.75x0.500":
                            case "HSS 10.75x0.625":
                                return 1;
                            case "HSS 16x0.500":
                            case "HSS 16x0.625":
                            case "HSS 16x2.000":
                                return 2;
                            case "HSS 6.625x0.3125":
                            case "HSS 6.625x0.500":
                            case "HSS 6.625x1.000":
                                return 3;
                            case "HSS 7x4x3/8":
                            case "HSS 7x4x1/2":
                            case "HSS 8x4x3/8":
                            case "HSS 8x4x5/8":
                                return 4;
                            case "HSS8.625x0.625":
                            case "HSS 8.625x0.625":
                                return 5;
                            default:
                                throw new S2KHelperException($"Unexpected section type: {section}.");
                        }
                    }

                    // Begins finding and breaking the frames
                    //progReporter.Report(ProgressData.SetMessage("Going after the important groups for the frames."));
                    Regex validGroup = new Regex(@"A\d\d|B\d\d\w?|STGW");

                    string message = "";

                    for (int i = 0; i < newFrames.Count; i++)
                    {
                        SapFrame newFrame = newFrames[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, newFrames.Count));

                        try
                        {
                            // Gets the closest old frame. The "type" of the section must match and the angle must be close
                            SapFrame closestFrame = oldFrames.Where(b =>
                                {
                                    // Where the frames have some alignment
                                    Angle angle = b.Line.Direction.AngleTo(newFrame.Line.Direction);
                                    if (b.Line.Direction.AngleTo(newFrame.Line.Direction).Degrees < 20 ||
                                        b.Line.Direction.AngleTo(newFrame.Line.Direction).Degrees > 150) return true;
                                    else return false;
                                }).Where(a =>
                                    Local_TypeBasedOnSection(a.Section.Name) ==
                                    Local_TypeBasedOnSection(newFrame.Section.Name))
                                .MinBy(a => a.PerpendicularDistance(newFrame.Centroid)).First();

                            double distance = closestFrame.PerpendicularDistance(newFrame.Centroid);
                            if (distance > MaxDistance) MaxDistance = distance;

                            // copies the group definition
                            foreach (string group in closestFrame.Groups)
                                if (validGroup.IsMatch(@group))
                                    newFrame.AddGroup(@group);
                        }
                        catch
                        {
                            message += newFrame.Name + Environment.NewLine;
                        }
                    }

                    return (MaxDistance.ToString(), message);
                }

                // Runs the job async
                var task = new Task<(string, string)>(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                    MessageBox.Show(task.Result.Item1, "Maximum Distance", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    if (!string.IsNullOrWhiteSpace(task.Result.Item2))
                    {
                        MessageBox.Show("There were failed frames. Their names habe been put in the clipboard.",
                            "Failed members", MessageBoxButton.OK, MessageBoxImage.Information);
                        Clipboard.SetText(task.Result.Item2);
                    }
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void UndoErectionAidSectionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow(true);

                // The async body
                void work()
                {
                    // Gets the list of Canopy Frames
                    var canopy = S2KModel.SM.FrameMan.GetGroup("0-CANOPY");

                    // The AID Regex
                    Regex aidRegex = new Regex(@"^AID.*\s<(?<origsect>.*)>");

                    for (int i = 0; i < canopy.Count; i++)
                    {
                        SapFrame item = canopy[i];
                        //progReporter.Report(ProgressData.UpdateProgress(i, canopy.Count));

                        Match match = aidRegex.Match(item.Section.Name);
                        if (match.Success) item.SetSection(match.Groups["origsect"].Value);
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        #endregion

        private async void SwapSurveyPointButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    var points = S2KModel.SM.PointMan.GetSelected();
                    foreach (SapPoint pnt in points)
                        if (pnt.Groups.Contains("0-SurveyPoints"))
                        {
                            pnt.RemoveGroup("0-SurveyPoints");
                            pnt.AddGroup("0-SurveyIgnored");
                        }
                        else if (pnt.Groups.Contains("0-SurveyIgnored"))
                        {
                            pnt.RemoveGroup("0-SurveyIgnored");
                            pnt.AddGroup("0-SurveyPoints");
                        }

                    S2KModel.SM.RefreshView();
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not change the point's groups.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not change the point's groups.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void AddCrownToSiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();

                // The async body
                void work()
                {
                    // Gets the list of groups
                    var groups = S2KModel.SM.GroupMan.GetGroupList();
                    foreach (string item in groups.Where(a => a.Contains("_CROWN") && !a.Contains("_REL_ADD")))
                    {
                        string siGrp = item.Replace("CROWN", "SI");

                        var frames = S2KModel.SM.FrameMan.GetGroup(item);
                        foreach (SapFrame frame in frames) frame.AddGroup(siGrp);
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;


                // There was an error in getting the data or the data table was not acquired
                if (task.IsFaulted)
                {
                    S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", task.Exception);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowErrorMessageBox("Could not work on the SAP2000 model.", ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void FixLocalTopOfPostButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            try
            {
                void work()
                {
                    // Gets list of constraint
                    var constraints = S2KModel.SM.JointConstraintMan.GetConstraintList();

                    foreach (string item in constraints)
                        if (item.Contains("L_OutPerim"))
                            S2KModel.SM.JointConstraintMan.SetLocalConstraint(item,
                                new bool[] {false, true, false, false, false, false});
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox(
                    $"Could not find the geometry that, when the DEAD is applied, goes to the target positions.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void Debug_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();
            BusyOverlayBindings.I.SetBasic(inTitle: "Debugging...");

            try
            {
                void work()
                {
                    S2KModel.SM.InterAuto.FlaUI_Action_Test();
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                EnableWindow();
            }
        }

        #region Rhino Operations

        private async void AlignPointsToRhinoSurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            try
            {
                void work()
                {
                    // Gets list of Joints in the Group to Align
                    var alignJoints = S2KModel.SM.PointMan.GetGroup("RhinoAlignPoints");

                    // Checks if Rhino is open
                    ////progReporter.Report(ProgressData.SetMessage("Getting the normal axes based on the Rhino surface. [[Joint: ***]]"));
                    //using (RhinoModel RhinoHelper = new RhinoModel())
                    //{
                    //    for (int i = 0; i < alignJoints.Count; i++)
                    //    {
                    //        SapPoint joint = alignJoints[i];
                    //        //progReporter.Report(ProgressData.UpdateProgress(i, alignJoints.Count, joint.Name));

                    //        //if (joint.BasicLocalAxes.Advanced == true) continue;

                    //        Vector3D normalVec = RhinoHelper.GetNormalAtSurface(joint.Point, "TargetSurface");

                    //        UnitVector3D vec1 = normalVec.Normalize();

                    //        // Local 1-2 is vertical, towards +Z. If the frame is vertical, then the local axis is towards +X
                    //        UnitVector3D vecP = vec1.IsVectorVertical()
                    //            ? UnitVector3D.Create(1, 0, 0)
                    //            : UnitVector3D.Create(0, 0, 1);

                    //        // V3 is perpendicular to both the axial vector and the reference vector
                    //        UnitVector3D vec3 = vec1.CrossProduct(vecP);

                    //        // V2 is then perpendicular to both
                    //        UnitVector3D vec2 = vec3.CrossProduct(vec1);

                    //        CoordinateSystem cSys = new CoordinateSystem(Point3D.Origin, vec1, vec2, vec3);

                    //        joint.SetAdvancedLocalAxesFromCoordinateSystem(cSys);
                    //    }
                    //}
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void AddPointsAndTriadsToRhinoButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            try
            {
                void work()
                {
                    // Gets list of Joints in the Group to Align
                    var alignJoints = S2KModel.SM.PointMan.GetGroup("RhinoAlignPoints");
                    //List<SapPoint> alignJoints = S2KModel.SM.PointMan.GetSelected();

                    // Checks if Rhino is open
                    ////progReporter.Report(ProgressData.SetMessage("Adding the points and the triads to the Rhino document. [[Joint: ***]]"));
                    //using (RhinoModel RhinoHelper = new RhinoModel())
                    //{
                    //    for (int i = 0; i < alignJoints.Count; i++)
                    //    {
                    //        SapPoint joint = alignJoints[i];
                    //        //progReporter.Report(ProgressData.UpdateProgress(i, alignJoints.Count, joint.Name));

                    //        if (joint.BasicLocalAxes.Advanced)
                    //            RhinoHelper.AddPointWithTriad(joint.Name, joint.Point, joint.LocalCoordinateSystem, 20);
                    //        else
                    //            RhinoHelper.AddPointWithTriad(joint.Name, joint.Point, joint.LocalCoordinateSystem, 10);
                    //    }

                    //    RhinoHelper.MakeSingleView();
                    //    RhinoHelper.Redraw();
                    //}
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                //StatusBarBindings.I.IProgressReporter.Report(ProgressData.Reset());
                EnableWindow();
            }
        }

        private async void AddPointsToRhinoButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();
            BusyOverlayBindings.I.Title = "Adding Points in Rhino.";
            BusyOverlayBindings.I.ShowOverlay();

            StringBuilder sb = new StringBuilder();

            try
            {
                void work()
                {
                    // first, reads the clipboard
                    BusyOverlayBindings.I.SetIndeterminate("Reading Clipboard Text.");

                    string clipText = Dispatcher.Invoke(() => Clipboard.GetText());

                    if (string.IsNullOrWhiteSpace(clipText))
                        S2KStaticMethods.ShowWarningMessageBox("The clipboard does not contain text.");

                    List<string> clipPoints = clipText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    
                    List<(string name, double x, double y, double z)> points = new List<(string, double, double, double)>();

                    foreach (string clipPoint in clipPoints)
                    {
                        string[] vals = clipPoint.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                        points.Add((vals[0], double.Parse(vals[1]), double.Parse(vals[2]), double.Parse(vals[3])));
                    }

                    string rhinoGroupName = RhinoOperationsBindings.I.TargetRhinoGroupForSelectedJoints;

                    using (RhinoModel rhinoHelper = new RhinoModel())
                    {
                        BusyOverlayBindings.I.MessageText = "Working with Clipboard Elements";
                        BusyOverlayBindings.I.ElementType = "Element";
                        for (int i = 0; i < points.Count; i++)
                        {
                            (string name, double x, double y, double z) item = points[i];
                            BusyOverlayBindings.I.UpdateProgress(i, points.Count, item.name);

                            Guid rhinoGuids = rhinoHelper.AddPoint3DWithName(item.name, new Point3D(item.x, item.y, item.z));
                        }
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                EnableWindow();

                BusyOverlayBindings.I.HideOverlayAndReset();

                if (sb.Length > 0)
                {
                    MessageOverlay.ShowOverlay("Joints not found in Rhino.", sb);
                }


            }
        }

        private async void MarkNamesInClipboardRhinoButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();
            BusyOverlayBindings.I.Title = "Marking Points in Rhino.";
            BusyOverlayBindings.I.ShowOverlay();

            StringBuilder sb = new StringBuilder();

            try
            {
                void work()
                {
                    // first, reads the clipboard
                    BusyOverlayBindings.I.SetIndeterminate("Reading Clipboard Text.");

                    string clipText = Dispatcher.Invoke(() => Clipboard.GetText());

                    if (string.IsNullOrWhiteSpace(clipText))
                        S2KStaticMethods.ShowWarningMessageBox("The clipboard does not contain text.");

                    var clipVals = clipText.Split(new string[] {"\t", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    string rhinoGroupName = RhinoOperationsBindings.I.TargetRhinoGroupForSelectedJoints;

                    using (RhinoModel rhinoHelper = new RhinoModel())
                    {
                        BusyOverlayBindings.I.MessageText = "Working with Clipboard Elements";
                        BusyOverlayBindings.I.ElementType = "Element";
                        for (int i = 0; i < clipVals.Count; i++)
                        {
                            string item = (string)clipVals[i];
                            BusyOverlayBindings.I.UpdateProgress(i, clipVals.Count, item);

                            var rhinoGuids = rhinoHelper.GetGuidsByName(item, RhinoObjectType.Point);
                            if (rhinoGuids.Length == 0)
                            {
                                sb.AppendLine($"{item}");
                                continue;
                            }
                                //throw new S2KHelperException($"Could not find the Guid of the element {item} in Rhino");

                            foreach (string guid in rhinoGuids)
                                rhinoHelper.AddIdToGroup(guid, rhinoGroupName, System.Drawing.Color.Black);
                        }
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                EnableWindow();

                BusyOverlayBindings.I.HideOverlayAndReset();

                if (sb.Length > 0)
                {
                    MessageOverlay.ShowOverlay("Joints not found in Rhino.", sb);
                }

                
            }
        }

        #endregion

        private async void TodiaButton_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow();

            void work()
            {
                BusyOverlayBindings.I.MessageText = "Oi!";
                BusyOverlayBindings.I.Title = "ALOW";

                Thread.Sleep(10000);
            }

            Task alow = new Task(() => work());
            alow.Start();
            await alow;

            EnableWindow();
        }

        private async void ManipulateItems_Substitute_LinksToFrames_Button_Click(object sender, RoutedEventArgs e)
        {
            DisableWindow(true);
            BusyOverlayBindings.I.Title = "Substituting Selected Links to Frames";

            try
            {
                void work()
                {
                    var selLinks = S2KModel.SM.LinkMan.GetSelected(BusyOverlay);
                    S2KModel.SM.ClearSelection();

                    if (selLinks.Count == 0)
                        throw new S2KHelperException("Select the links that will be substituted to frames.");

                    string targetFrameSection = FormBasicRefreshingBindings.I
                        .ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection;

                    BusyOverlayBindings.I.SetDeterminate("Working on link.", "Link");
                    for (int i = 0; i < selLinks.Count; i++)
                    {
                        SapLink lnk = selLinks[i];
                        BusyOverlayBindings.I.UpdateProgress(i, selLinks.Count, lnk.Name);

                        SapFrame newFrame =
                            S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(lnk.iEndPoint, lnk.jEndPoint,
                                targetFrameSection);
                        S2KModel.SM.LinkMan.DeleteLink(lnk);
                        newFrame.Select();
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                EnableWindow();
                S2KModel.SM.RefreshView();
            }
        }

        private async void SelectMisc_FramesBasedOnLength_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableWindow();
                BusyOverlayBindings.I.Title = "Selecting frames from the model based on their lengths.";

                // The async body
                void work()
                {
                    double max = SelectionInfoBindings.I.SelectMisc_FramesBasedOnLength_MaxLength;
                    double min = SelectionInfoBindings.I.SelectMisc_FramesBasedOnLength_MinLength;

                    if (max <= min || max <= 0 || min < 0)
                        throw new S2KHelperException("Invalid limits. Please enter valid data.");

                    List<SapFrame> frames;

                    if (SelectionInfoBindings.I.SelectMisc_FramesBasedOnLength_FilterFromSelection)
                        frames = S2KModel.SM.FrameMan.GetSelected(true);
                    else frames = S2KModel.SM.FrameMan.GetAll(true);

                    S2KModel.SM.ClearSelection();

                    BusyOverlayBindings.I.SetDeterminate("Selecting the frames.", "Frame");
                    for (int i = 0; i < frames.Count; i++)
                    {
                        SapFrame item = (SapFrame) frames[i];
                        BusyOverlayBindings.I.UpdateProgress(i, frames.Count, item.Name);

                        if (item.Length >= min && item.Length <= max) item.Select();
                    }
                }

                // Runs the job async
                Task task = new Task(() => work());
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                EnableWindow();
                S2KModel.SM.RefreshView();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}