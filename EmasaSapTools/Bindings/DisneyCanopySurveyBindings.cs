using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using EmasaSapTools.Resources;
using MathNet.Spatial.Euclidean;
using Microsoft.Win32;
using MoreLinq;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.DataClasses.Results;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class DisneyCanopySurveyBindings : BindableSingleton<DisneyCanopySurveyBindings>
    {
        private DisneyCanopySurveyBindings(){}
        public override void SetOrReset()
        {
            Areas = "1_2_3_4_5";
            SurveyRadio_IsChecked = false;
            WorkRadio_IsChecked = true;

            CurrentUnits = S2KModel.SM.PresentationUnitsStringFormat;
            _duplicateAndSplit_SeparatorText = "***";

            ResultsDisplacements_Cases_Selected = new ObservableCollection<string>();
            ResultsDisplacements_Groups_Selected = new ObservableCollection<string>();
        }

        private ICollectionView _resultsDisplacements_Cases_ViewItems;
        public ICollectionView ResultsDisplacements_Cases_ViewItems
        {
            get => _resultsDisplacements_Cases_ViewItems;
            set
            {
                SetProperty(ref _resultsDisplacements_Cases_ViewItems, value);
                _resultsDisplacements_Cases_ViewItems.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
        }
        public ObservableCollection<string> ResultsDisplacements_Cases_Selected { get; set; }

        private ICollectionView _resultsDisplacements_Groups_ViewItems;
        public ICollectionView ResultsDisplacements_Groups_ViewItems
        {
            get => _resultsDisplacements_Groups_ViewItems;
            set
            {
                SetProperty(ref _resultsDisplacements_Groups_ViewItems, value);
                _resultsDisplacements_Groups_ViewItems.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
        }
        public ObservableCollection<string> ResultsDisplacements_Groups_Selected { get; set; }

        private string _areas;
        public string Areas { get => _areas; set => SetProperty(ref _areas, value); }

        private bool _surveyRadio_IsChecked;
        public bool SurveyRadio_IsChecked { get => _surveyRadio_IsChecked; set => SetProperty(ref _surveyRadio_IsChecked, value); }

        private bool _workRadio_IsChecked;
        public bool WorkRadio_IsChecked { get => _workRadio_IsChecked; set => SetProperty(ref _workRadio_IsChecked, value); }


        private string _currentUnits;
        public string CurrentUnits { get => _currentUnits; set => SetProperty(ref _currentUnits, value); }

        private bool _duplicateAndSplit_IsChecked =false;
        public bool DuplicateAndSplit_IsChecked { get => _duplicateAndSplit_IsChecked; set => SetProperty(ref _duplicateAndSplit_IsChecked, value); }

        private string _duplicateAndSplit_SeparatorText;
        public string DuplicateAndSplit_SeparatorText { get => _duplicateAndSplit_SeparatorText; set => SetProperty(ref _duplicateAndSplit_SeparatorText, value); }

        private string _deadMatchSolverMessages;
        public string DeadMatchSolverMessages { get => _deadMatchSolverMessages; set => SetProperty(ref _deadMatchSolverMessages, value); }

        private readonly StringBuilder deadMatchSolverMessages_sb = new StringBuilder();

        public void DeadMatchSolverMessages_AddLine(string inLine)
        {
            deadMatchSolverMessages_sb.AppendLine(inLine);
            DeadMatchSolverMessages = deadMatchSolverMessages_sb.ToString();
        }

        private DelegateCommand _surveyReadPointsCommand;
        public DelegateCommand SurveyReadPointsCommand =>
            _surveyReadPointsCommand ?? (_surveyReadPointsCommand = new DelegateCommand(ExecuteSurveyReadPointsCommand));
        async void ExecuteSurveyReadPointsCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    if (WorkRadio_IsChecked) BusyOverlayBindings.I.Title = "Adding WORK points definitions from the selected Excel file into the SAP2000 model.";
                    if (SurveyRadio_IsChecked) BusyOverlayBindings.I.Title = "Adding SURVEY points definitions from the selected Excel file into the SAP2000 model.";

                    // Gets the areas
                    int[] areas;
                    try
                    {
                        areas = Areas.Split('_').Select(int.Parse).ToArray();
                    }
                    catch (Exception)
                    {
                        throw new S2KHelperException($"Please enter a proper area string, which should be <int>_<int>...");
                    }

                    if (areas.Length == 0) throw new S2KHelperException($"Please enter a proper area string, which should be <int>_<int>...");

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
                        throw new S2KHelperException($"Please select a proper Excel file!{Environment.NewLine}");
                    }

                    BusyOverlayBindings.I.SetIndeterminate("Reading the Excel Input.");

                    // Reads the excel sheets into the dataset
                    DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName, new int[] { 0 });
                    DataTable surveyPoints = fromExcel.Tables["SURVEY"];

                    BusyOverlayBindings.I.SetIndeterminate("Making sure target groups exist.");
                    string flag = WorkRadio_IsChecked ? "Work" : "Survey";
                    string groupNamePoint = $"0-SurveyPoints_{Areas}_{flag}";
                    S2KModel.SM.GroupMan.DeleteGroup(groupNamePoint);
                    S2KModel.SM.GroupMan.AddGroup(groupNamePoint);

                    BusyOverlayBindings.I.SetDeterminate("Adding Survey Information to Model.");
                    var addedPoints = new List<SapPoint>();
                    for (int i = 0; i < surveyPoints.Rows.Count; i++)
                    {
                        DataRow row = surveyPoints.Rows[i];
                        BusyOverlayBindings.I.UpdateProgress(i, surveyPoints.Rows.Count);

                        // Ignores the items that are not part of the selected area
                        if (!areas.Any(a => a == (int)row.Field<double>("Area"))) continue;

                        if (WorkRadio_IsChecked)
                        {
                            // Adding the work points
                            if (row["Type"].ToString().Contains("Srv")) continue;

                            // Means that the work points location were not given
                            if ((double)row["NX"] == -166800d && (double)row["NY"] == -763200d && (double)row["NZ"] == 0) continue;

                            SapPoint newPoint = S2KModel.SM.PointMan.AddByCoord_ReturnSapEntity((double)row["NX"], (double)row["NY"], (double)row["NZ"], row["SAPPointName"].ToString(), 57);
                            newPoint.AddGroup(groupNamePoint);
                            addedPoints.Add(newPoint);
                        }
                        else if (SurveyRadio_IsChecked)
                        {
                            // Adding the survey points
                            if (row["Type"].ToString().Contains("Work")) continue;

                            // Means that the surveyed values were not given
                            if ((double)row["SX"] == -166800d && (double)row["SY"] == -763200d && (double)row["SZ"] == 0) continue;

                            SapPoint newPoint = S2KModel.SM.PointMan.AddByCoord_ReturnSapEntity((double)row["SX"], (double)row["SY"], (double)row["SZ"], row["SAPPointName"].ToString(), 57);
                            newPoint.AddGroup(groupNamePoint);
                            addedPoints.Add(newPoint);
                        }
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
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) OnMessage("Could not add the points to the SAP2000 file.", endMessages.ToString());
            }
        }

        private DelegateCommand _surveyLinkToCanopyCommand;
        public DelegateCommand SurveyLinkToCanopyCommand =>
            _surveyLinkToCanopyCommand ?? (_surveyLinkToCanopyCommand = new DelegateCommand(ExecuteSurveyLinkToCanopyCommand));
        async void ExecuteSurveyLinkToCanopyCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                // And hides SAP2000
                OnBeginCommand(true);

                void lf_Work()
                {
                    string flag = WorkRadio_IsChecked ? "Work" : "Survey";
                    BusyOverlayBindings.I.Title = $"Link {flag} Points to Canopy";

                    // Gets the areas
                    int[] areas;
                    try
                    {
                        areas = Areas.Split('_').Select(int.Parse).ToArray();
                    }
                    catch (Exception)
                    {
                        throw new S2KHelperException($"Please enter a proper area string, which should be <int>_<int>...");
                    }
                    if (areas.Length == 0) throw new S2KHelperException($"Please enter a proper area string, which should be <int>_<int>...");

                    // changes the name of the current thread for debugging purposes
                    //Thread.CurrentThread.Name = "Worker Thread";

                    Regex moduleGroupRegex = new Regex(@"^A\d{2}|^B\d{2}a?");

                    List<SapPoint> canopyPoints = S2KModel.SM.PointMan.GetGroup("0-CANOPY_POINTS", true);
                    if (canopyPoints.Count == 0) throw new S2KHelperException($"Group 0-CANOPY_POINTS has no joint.");

                    string GroupNamePoint = $"0-SurveyPoints_{Areas}_{flag}";
                    List<SapPoint> surveyPoints = S2KModel.SM.PointMan.GetGroup(GroupNamePoint, true);
                    if (surveyPoints.Count == 0) throw new S2KHelperException($"Group {GroupNamePoint} has no joint.");

                    List<SapFrame> canopyFrames = S2KModel.SM.FrameMan.GetGroup("0-CANOPY", true);
                    if (canopyFrames.Count == 0) throw new S2KHelperException("Could not get list of canopy frames.");

                    BusyOverlayBindings.I.SetIndeterminate("Making sure groups exist.");
                    string GroupNameFrame = $"0-SurveyLinks_{Areas}_{flag}";
                    S2KModel.SM.GroupMan.DeleteGroup(GroupNameFrame);
                    S2KModel.SM.GroupMan.AddGroup(GroupNameFrame);

                    BusyOverlayBindings.I.SetDeterminate("Linking survey points to the canopy.", "Survey Point");
                    double maxDistance = 1; // The maximum distance allowed to just link the survey to the canopy
                    for (int i = 0; i < surveyPoints.Count; i++)
                    {
                        SapPoint surveyPoint = surveyPoints[i];
                        BusyOverlayBindings.I.UpdateProgress(i, surveyPoints.Count, surveyPoint.Name);

                        // Gets closest point in the canopy
                        SapPoint closest = canopyPoints.MinBy(a => surveyPoint.Point.DistanceTo(a.Point)).First();

                        double distance = closest.Point.DistanceTo(surveyPoint.Point);
                        if (distance < maxDistance)
                        {
                            // Adds a frame
                            string frameName = $"F_{flag}_{S2KStaticMethods.UniqueName(6)}";
                            SapFrame newFrame = S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(surveyPoint, closest, "Z-LINK", frameName);
                            if (newFrame == null) throw new S2KHelperException($"Could not add rigid frame linking {surveyPoint.Name} to {closest.Name}.");
                            newFrame.AddGroup(GroupNameFrame);
                            newFrame.EndReleases = new FrameEndRelease(FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionI);

                            // Links them by an equal constraint
                            string constName = "E_" + frameName;
                            if (!S2KModel.SM.JointConstraintMan.SetEqualConstraint(constName, new[] { true, true, true, false, false, false }))
                                throw new S2KHelperException($"Could not create constraint called {constName}.");
                            surveyPoint.AddJointConstraint(constName, false);
                            closest.AddJointConstraint(constName, false);
                        }
                        else
                        {
                            // Gets closest frame to point
                            SapFrame closestFrame = canopyFrames.MinBy(a => a.PerpendicularDistance(surveyPoint)).FirstOrDefault();
                            if (closestFrame == null) throw new S2KHelperException($"Could not get the closest frame in the canopy to point called {surveyPoint.Name}.");

                            // Gets the closest point at the closest frame
                            Point3D point3DAtFrame = closestFrame.Line.ClosestPointTo(surveyPoint.Point, true);

                            // If it is an extreme joint, just link
                            if (closestFrame.IsPointIJ(point3DAtFrame))
                            {
                                closest = closestFrame.GetIOrJClosestTo(point3DAtFrame);

                                // Adds a frame
                                string frameName = $"F_{flag}_{S2KStaticMethods.UniqueName(6)}";
                                SapFrame newFrame = S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(surveyPoint, closest, "Z-LINK", frameName);
                                if (newFrame == null) throw new S2KHelperException($"Could not add rigid frame linking {surveyPoint.Name} to {closest.Name}.");
                                newFrame.AddGroup(GroupNameFrame);
                                newFrame.EndReleases = new FrameEndRelease(FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionI);

                                // Links them by an equal constraint
                                string constName = "E_" + frameName;
                                if (!S2KModel.SM.JointConstraintMan.SetEqualConstraint(constName, new[] { true, true, true, false, false, false }))
                                    throw new S2KHelperException($"Could not create constraint called {constName}.");
                                surveyPoint.AddJointConstraint(constName, false);
                                closest.AddJointConstraint(constName, false);
                            }
                            else
                            {
                                // The distance is larger than the tolerance - the points won't merge
                                if (point3DAtFrame.DistanceTo(surveyPoint.Point) > S2KModel.SM.MergeTolerance)
                                {
                                    // Adds this new point to SAP2000
                                    string newPointName = $"KH_SurveyLink_{S2KStaticMethods.UniqueName(6)}";
                                    SapPoint sapPointAtFrame = S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(point3DAtFrame, newPointName);

                                    // Breaks the frame at this point
                                    var framePieces = closestFrame.DivideAtIntersectPoint(sapPointAtFrame, "_P");
                                    canopyFrames.RemoveAll(a => a.Name == closestFrame.Name);
                                    canopyFrames.AddRange(framePieces);

                                    // Adds a frame
                                    string frameName =
                                        $"F_{flag}_{S2KStaticMethods.UniqueName(6)}";
                                    SapFrame newFrame = S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(surveyPoint, sapPointAtFrame, "Z-LINK", frameName);
                                    if (newFrame == null) throw new S2KHelperException($"Could not add rigid frame linking {surveyPoint.Name} to {sapPointAtFrame.Name}.");
                                    newFrame.AddGroup(GroupNameFrame);
                                    newFrame.EndReleases = new FrameEndRelease(FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionI);

                                    // Links them by an equal constraint
                                    string constName = "E_" + frameName;
                                    if (!S2KModel.SM.JointConstraintMan.SetEqualConstraint(constName, new[] { true, true, true, false, false, false }))
                                        throw new S2KHelperException($"Could not create constraint called {constName}.");
                                    surveyPoint.AddJointConstraint(constName, false);
                                    sapPointAtFrame.AddJointConstraint(constName, false);
                                }
                                else // The distance is smaller than the tolerance - the points would merge.
                                {
                                    // Breaks the frame at the point
                                    var framePieces = closestFrame.DivideAtIntersectPoint(surveyPoint, "_P");
                                    canopyFrames.RemoveAll(a => a.Name == closestFrame.Name);
                                    canopyFrames.AddRange(framePieces);
                                }
                            }
                        }
                    }

                    BusyOverlayBindings.I.SetDeterminate("Adding the canopy groups to the links.", "Survey Link");
                    var links = S2KModel.SM.FrameMan.GetGroup(GroupNameFrame, false);
                    for (int i = 0; i < links.Count; i++)
                    {
                        SapFrame link = links[i];
                        BusyOverlayBindings.I.UpdateProgress(i, links.Count, link.Name);

                        SapFrame closestFrame = canopyFrames.MinBy(a => a.PerpendicularDistance(link.jEndPoint))
                            .FirstOrDefault();

                        foreach (string item in closestFrame.Groups.Where(a => moduleGroupRegex.IsMatch(a)))
                            link.AddGroup(item);
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
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) OnMessage("Could not link the points to the SAP2000 file.", endMessages.ToString());
            }
        }

        private DelegateCommand _surveyAddFixedSupportWithDisplacementsCommand;
        public DelegateCommand SurveyAddFixedSupportWithDisplacementsCommand =>
            _surveyAddFixedSupportWithDisplacementsCommand ?? (_surveyAddFixedSupportWithDisplacementsCommand = new DelegateCommand(ExecuteSurveyAddFixedSupportWithDisplacementsCommand));
        async void ExecuteSurveyAddFixedSupportWithDisplacementsCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    string flag = WorkRadio_IsChecked ? "Work" : "Survey";
                    BusyOverlayBindings.I.Title = $"Adding Fixed Support and Displacement Information to the {flag} Joints";

                    // Gets the areas
                    int[] areas;
                    try
                    {
                        areas = Areas.Split('_').Select(int.Parse).ToArray();
                    }
                    catch (Exception)
                    {
                        throw new S2KHelperException($"Please enter a proper area string, which should be <int>_<int>...");
                    }
                    if (areas.Length == 0) throw new S2KHelperException($"Please enter a proper area string, which should be <int>_<int>...");
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
                        throw new S2KHelperException($"Please select a proper Excel file!{Environment.NewLine}");
                    }

                    BusyOverlayBindings.I.SetIndeterminate("Reading the Excel Input.");

                    DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName, new int[] { 0 });
                    DataTable surveyTable = fromExcel.Tables["SURVEY"];

                    string GroupNamePoint = $"0-SurveyPoints_{Areas}_{flag}";
                    var surveyPoints = S2KModel.SM.PointMan.GetGroup(GroupNamePoint, true);
                    if (surveyPoints.Count == 0) throw new S2KHelperException("Could not get list of survey joints.");

                    BusyOverlayBindings.I.SetDeterminate("Treating the joints to add the support and the displacements.");
                    for (int i = 0; i < surveyTable.Rows.Count; i++)
                    {
                        DataRow dispRow = surveyTable.Rows[i];
                        BusyOverlayBindings.I.UpdateProgress(i, surveyTable.Rows.Count);

                        // Ignores the items that are not part of the selected area
                        if (!areas.Any(a => a == (int)dispRow.Field<double>("Area"))) continue;

                        // Adding the work points
                        if (WorkRadio_IsChecked)
                        {
                            if (dispRow["Type"].ToString().Contains("Srv")) continue;

                            // Finds the point
                            SapPoint modelPoint =
                                surveyPoints.FirstOrDefault(a => a.Name == dispRow["SAPPointName"].ToString());
                            if (modelPoint == null)
                                continue; // Not finding the point is not a bug - it just means that the data was incomplete and thus it wasn't created

                            // Adds the restraint
                            modelPoint.Restraints = new PointRestraintDef(PointRestraintType.FullyFixed);

                            double delta1 = (double)dispRow["SX"] - (double)dispRow["NX"];
                            double delta2 = (double)dispRow["SY"] - (double)dispRow["NY"];
                            double delta3 = (double)dispRow["SZ"] - (double)dispRow["NZ"];

                            if (delta1 != 0 || delta2 != 0 || delta3 != 0)
                            {
                                // Adds the Joint Load
                                modelPoint.AddDisplacementLoad(new JointDisplacementLoad()
                                    {
                                    LoadPatternName = "SURVEY",
                                    U1 = delta1,
                                    U2 = delta2,
                                    U3 = delta3
                                    });
                            }
                        }
                        else if (SurveyRadio_IsChecked)
                        {
                            if (dispRow["Type"].ToString().Contains("Work")) continue;

                            // Finds the point
                            SapPoint modelPoint =
                                surveyPoints.FirstOrDefault(a => a.Name == dispRow["SAPPointName"].ToString());
                            if (modelPoint == null) continue;

                            // Adds the restraint
                            modelPoint.Restraints = new PointRestraintDef(PointRestraintType.FullyFixed);

                            double delta1 = (double)dispRow["FieldX"] - (double)dispRow["SX"];
                            double delta2 = (double)dispRow["FieldY"] - (double)dispRow["SY"];
                            double delta3 = (double)dispRow["FieldZ"] - (double)dispRow["SZ"];

                            if (delta1 != 0 || delta2 != 0 || delta3 != 0)
                            {
                                // Adds the Joint Load
                                modelPoint.AddDisplacementLoad(new JointDisplacementLoad()
                                    {
                                    LoadPatternName = "SURVEY",
                                    U1 = delta1,
                                    U2 = delta2,
                                    U3 = delta3
                                    });
                            }
                        }
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
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) OnMessage("Could not link the points to the SAP2000 file.", endMessages.ToString());
            }
        }

        private DelegateCommand _getResultDisplacementCommand;
        public DelegateCommand GetResultDisplacementCommand =>
            _getResultDisplacementCommand ?? (_getResultDisplacementCommand = new DelegateCommand(ExecuteGetResultDisplacementCommand));
        async void ExecuteGetResultDisplacementCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = $"Getting the Displacement Results";

                    BusyOverlayBindings.I.SetIndeterminate($"Gathering basic load case data and validation.");

                    // First, checks if the model is run
                    if (!S2KModel.SM.Locked) throw new S2KHelperException($"There is no load case that is run in the model (model unlocked). Please run the load cases to obtain the results.");

                    // Gets the selected list
                    if (ResultsDisplacements_Cases_Selected.Count == 0) throw new S2KHelperException($"Please select an output case.");
                    if (ResultsDisplacements_Groups_Selected.Count == 0) throw new S2KHelperException($"Please select a group from which to get the results.");

                    // Are the selected cases finished?
                    List<LoadCase> selectedCases = S2KModel.SM.LCMan.GetAll().Where(a => ResultsDisplacements_Cases_Selected.Contains(a.Name)).ToList();
                    if (selectedCases.Any(a => a.Status != LCStatus.Finished)) throw new S2KHelperException($"All selected load cases must be run. Please run the load cases to obtain the results. ");

                    S2KModel.SM.ClearSelection();

                    BusyOverlayBindings.I.SetIndeterminate($"Selecting the groups.");
                    foreach (string group in ResultsDisplacements_Groups_Selected)
                        S2KModel.SM.GroupMan.SelectGroup(group);

                    ////progReporter.Report(ProgressData.SetMessage($"Ensuring that the points of the frames in the group are also selected.", true));
                    //List<SapFrame> selFrames = S2KModel.SM.FrameMan.GetSelected();
                    //foreach (SapFrame frame in selFrames) frame.SelectJoints();

                    // Gets the selected joints
                    var selPoints = S2KModel.SM.PointMan.GetSelected(true);

                    BusyOverlayBindings.I.SetIndeterminate($"Preparing the output.");
                    S2KModel.SM.ResultMan.DeselectAllCasesAndCombosForOutput();
                    S2KModel.SM.ResultMan.SetOptionMultiStepStatic(OptionMultiStepStatic.LastStep);
                    S2KModel.SM.ResultMan.SetOptionMultiValuedCombo(OptionMultiValuedCombo.Correspondence);
                    S2KModel.SM.ResultMan.SetOptionNLStatic(OptionNLStatic.LastStep);

                    foreach (LoadCase item in selectedCases) S2KModel.SM.ResultMan.SetCaseSelectedForOutput(item);

                    BusyOverlayBindings.I.SetIndeterminate($"Getting the Joint Displacement Results Data from SAP2000.");
                    var jointDisplacementDatas = S2KModel.SM.ResultMan.GetJointDisplacement("", ItemTypeElmEnum.SelectionElm);

                    BusyOverlayBindings.I.SetDeterminate($"Filling the global coordinate transformed data.", "Joint");
                    for (int i = 0; i < jointDisplacementDatas.Count; i++)
                    {
                        JointDisplacementData currDispData = jointDisplacementDatas[i];

                        BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);

                        currDispData.FillGlobalCoordinates();
                    }

                    if (DuplicateAndSplit_IsChecked)
                    {
                        #region Displacement Data

                        // Changes the list to account for various points in the same point
                        BusyOverlayBindings.I.SetDeterminate($"Fixing the Joint Displacement to Account for Various Points.", "Object");
                        int currentListSize = jointDisplacementDatas.Count;
                        var toRemove1 = new List<JointDisplacementData>();
                        for (int i = 0; i < currentListSize; i++)
                        {
                            JointDisplacementData currDispData = jointDisplacementDatas[i];
                            BusyOverlayBindings.I.UpdateProgress(i, currentListSize, currDispData.Obj);

                            if (currDispData.Obj != null && currDispData.Obj.Contains(DuplicateAndSplit_SeparatorText))
                            {
                                foreach (string subname in currDispData.Obj.Split(
                                    new[] { DuplicateAndSplit_SeparatorText },
                                    StringSplitOptions.RemoveEmptyEntries))
                                    jointDisplacementDatas.Add(currDispData.DuplicateDataWithNewObj(subname));

                                toRemove1.Add(currDispData);
                            }
                        }

                        foreach (JointDisplacementData item in toRemove1) jointDisplacementDatas.Remove(item);

                        #endregion

                        #region Joint Coordinates

                        BusyOverlayBindings.I.SetDeterminate($"Fixing the Joint Coordinates to Account for Various Points.", "Point");
                        currentListSize = selPoints.Count;
                        var toRemove2 = new List<SapPoint>();
                        for (int i = 0; i < currentListSize; i++)
                        {
                            SapPoint currPoint = selPoints[i];
                            BusyOverlayBindings.I.UpdateProgress(i, currentListSize, currPoint.Name);
                            
                            if (currPoint.Name != null && currPoint.Name.Contains(DuplicateAndSplit_SeparatorText))
                            {
                                foreach (string subname in currPoint.Name.Split(
                                    new[] { DuplicateAndSplit_SeparatorText },
                                    StringSplitOptions.RemoveEmptyEntries))
                                    selPoints.Add(currPoint.DuplicateCoordinatesWithNewName(subname));

                                toRemove2.Add(currPoint);
                            }
                        }

                        foreach (SapPoint item in toRemove2) selPoints.Remove(item);

                        #endregion
                    }

                    // Now, saves it in a new SQLite database
                    BusyOverlayBindings.I.SetIndeterminate("Creating the new SQLite file.");

                    string sqlFileName = Path.Combine(S2KModel.SM.ModelDir, Path.GetFileNameWithoutExtension(S2KModel.SM.FullFileName) + $"_Disp_{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}.sqlite");

                    SQLiteConnection.CreateFile(sqlFileName);

                    SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder();
                    connectionStringBuilder.DataSource = sqlFileName;
                    using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                    {
                        sqliteConn.Open();

                        // Creates the tables
                        using (SQLiteCommand createTable = new SQLiteCommand(JointDisplacementData.SQLite_CreateTableStatement, sqliteConn))
                        {
                            createTable.ExecuteNonQuery();
                        }

                        // Creates the tables
                        using (SQLiteCommand createTable = new SQLiteCommand(SapPoint.SQLite_CreateCoordinateTableStatement, sqliteConn))
                        {
                            createTable.ExecuteNonQuery();
                        }

                        // Creates a view to expedite the reading of the data
                        string ViewCommand = @"CREATE VIEW JointNewCoordinates AS
SELECT 
       [main].[JointCoordinates].[ShortName], 
       [main].[JointCoordinates].[X], 
       [main].[JointCoordinates].[Y], 
       [main].[JointCoordinates].[Z], 
       [main].[JointDisplacement].[GlobalU1], 
       [main].[JointDisplacement].[GlobalU2], 
       [main].[JointDisplacement].[GlobalU3],
       [main].[JointCoordinates].[X] + [main].[JointDisplacement].[GlobalU1] AS NEWX,
       [main].[JointCoordinates].[Y] + [main].[JointDisplacement].[GlobalU2] AS NEWY,
       [main].[JointCoordinates].[Z] + [main].[JointDisplacement].[GlobalU3] AS NEWZ
FROM   [main].[JointCoordinates]
       INNER JOIN [main].[JointDisplacement] ON [main].[JointCoordinates].[ShortName] = [main].[JointDisplacement].[Obj];";
                        // Creates the View
                        using (SQLiteCommand createView = new SQLiteCommand(ViewCommand, sqliteConn))
                        {
                            createView.ExecuteNonQuery();
                        }

                        ViewCommand = @"CREATE VIEW ExcelImport AS 
SELECT 
ShortName,
'GLOBAL' as CoordSys,
'Cartesian' as CoordType,
NEWX as XorR,
NEWY as Y,
NEWZ as Z
FROM JointNewCoordinates;";
                        // Creates the View
                        using (SQLiteCommand createView = new SQLiteCommand(ViewCommand, sqliteConn))
                        {
                            createView.ExecuteNonQuery();
                        }

                        int bufferCounter = 1;
                        BusyOverlayBindings.I.SetDeterminate("Saving the joint displacement table.", "Object");
                        SQLiteTransaction transaction = sqliteConn.BeginTransaction();
                        for (int i = 0; i < jointDisplacementDatas.Count; i++)
                        {
                            JointDisplacementData currDispData = jointDisplacementDatas[i];

                            using (SQLiteCommand insertCommand = new SQLiteCommand(currDispData.SQLite_InsertStatement, sqliteConn))
                            {
                                insertCommand.ExecuteNonQuery();
                            }

                            if (bufferCounter % 1000 == 0)
                            {
                                // Commits and add a new transaction
                                transaction.Commit();
                                transaction.Dispose();
                                transaction = sqliteConn.BeginTransaction();
                            }

                            bufferCounter++;

                            BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);
                        }

                        // Commits last transaction
                        transaction.Commit();
                        transaction.Dispose();

                        bufferCounter = 1;
                        BusyOverlayBindings.I.SetDeterminate("Saving the joint coordinate table.", "Point");
                        transaction = sqliteConn.BeginTransaction();
                        for (int i = 0; i < selPoints.Count; i++)
                        {
                            SapPoint currPoint = selPoints[i];

                            using (SQLiteCommand insertCommand =
                                new SQLiteCommand(currPoint.SQLite_InsertStatement, sqliteConn))
                            {
                                insertCommand.ExecuteNonQuery();
                            }

                            if (bufferCounter % 1000 == 0)
                            {
                                // Commits and add a new transaction
                                transaction.Commit();
                                transaction.Dispose();
                                transaction = sqliteConn.BeginTransaction();
                            }

                            bufferCounter++;

                            BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currPoint.Name);
                        }

                        // Commits last transaction
                        transaction.Commit();
                        transaction.Dispose();
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
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) OnMessage("Could not export the displacement data of the points to the SQLite database.", endMessages.ToString());
            }
        }

        private DelegateCommand _solverMatchDEADCasePositionCommand;
        public DelegateCommand SolverMatchDEADCasePositionCommand =>
            _solverMatchDEADCasePositionCommand ?? (_solverMatchDEADCasePositionCommand = new DelegateCommand(ExecuteSolverMatchDEADCasePositionCommand));
        async void ExecuteSolverMatchDEADCasePositionCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = $"Finding geometry that will reach given positions when loaded with DEAD case";
                    BusyOverlayBindings.I.AutomationWarning_Visibility = Visibility.Visible;
                    BusyOverlayBindings.I.LongReport_Visibility = Visibility.Visible;

                    // Selects the files in the view thread
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "SQLite Database (*.db;*.sqlite)|*.db;*.sqlite",
                        DefaultExt = "*.db;*.sqlite",
                        Title = "Select SQLite Database!",
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    var ofdret = ofd.ShowDialog();

                    if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName)) throw new S2KHelperException("Please select a proper SQLite file!");

                    DataTable targetCoordTable = new DataTable();

                    // Opens the file
                    BusyOverlayBindings.I.SetIndeterminate("Reading the data from the SQLite file.");
                    SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder();
                    connectionStringBuilder.DataSource = ofd.FileName;
                    using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                    {
                        sqliteConn.Open();

                        // Creates the tables
                        using (SQLiteCommand dbCommand =
                            new SQLiteCommand("SELECT * FROM JointNewCoordinates;", sqliteConn))
                        {
                            SQLiteDataReader executeReader = dbCommand.ExecuteReader(CommandBehavior.SingleResult);
                            targetCoordTable.Load(executeReader);
                        }
                    }


                    int iterationCount = 0;
                    DateTime startAll = DateTime.Now;
                    while (true)
                    {
                        DateTime startIteration = DateTime.Now;

                        BusyOverlayBindings.I.SetIndeterminate("Running SAP2000 DEAD case.");
                        // Runs the DAED case in SAP2000
                        S2KModel.SM.AnalysisMan.DeleteAllResults();
                        S2KModel.SM.AnalysisMan.ModelLocked = false;
                        S2KModel.SM.AnalysisMan.SetAllNotToRun();
                        S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);
                        S2KModel.SM.AnalysisMan.RunAnalysis();

                        BusyOverlayBindings.I.SetIndeterminate("Results Acquire = Select Group.");
                        S2KModel.SM.ClearSelection();
                        string jointGroup = "0-CANOPY_POINTS";
                        S2KModel.SM.GroupMan.SelectGroup(jointGroup);

                        // Gets the selected joints
                        List<SapPoint> selPoints = S2KModel.SM.PointMan.GetSelected(true);

                        S2KModel.SM.ResultMan.DeselectAllCasesAndCombosForOutput();
                        S2KModel.SM.ResultMan.SetOptionMultiStepStatic(OptionMultiStepStatic.LastStep);
                        S2KModel.SM.ResultMan.SetOptionMultiValuedCombo(OptionMultiValuedCombo.Correspondence);
                        S2KModel.SM.ResultMan.SetOptionNLStatic(OptionNLStatic.LastStep);

                        S2KModel.SM.ResultMan.SetCaseSelectedForOutput("DEAD");

                        BusyOverlayBindings.I.SetIndeterminate($"Results Acquire = Acquire Joint Displacement Data.");
                        List<JointDisplacementData> jointDisplacementDatas =  S2KModel.SM.ResultMan.GetJointDisplacement("", ItemTypeElmEnum.SelectionElm);

                        BusyOverlayBindings.I.SetDeterminate($"Results Acquire = Linking Joint Displacement Data to their Joints.", "Joint");
                        for (int i = 0; i < jointDisplacementDatas.Count; i++)
                        {
                            JointDisplacementData currDispData = jointDisplacementDatas[i];
                            BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);

                            SapPoint linkedPoint = selPoints.FirstOrDefault(a => a.Name == currDispData.Obj);
                            if (linkedPoint == null) throw new S2KHelperException($"Could not find the SapPoint linked to the Displacement Data with Obj={currDispData.Obj}.");

                            currDispData.LinkToPoint(linkedPoint);
                        }

                        BusyOverlayBindings.I.SetDeterminate($"Results Acquire = Filling the transformed data.", "Joint");
                        for (int i = 0; i < jointDisplacementDatas.Count; i++)
                        {
                            JointDisplacementData currDispData = jointDisplacementDatas[i];
                            BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);

                            currDispData.FillGlobalCoordinates();
                        }

                        double tolerance = 0.001;
                        var distances = new List<double>();

                        var toUpdate = new List<(string JointName, Point3D newCoords)>();
                        BusyOverlayBindings.I.SetDeterminate($"Compare = Checking if we are near the target.", "Joint");
                        for (int i = 0; i < jointDisplacementDatas.Count; i++)
                        {
                            JointDisplacementData currDispData = jointDisplacementDatas[i];
                            BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);

                            DataRow targetRow = targetCoordTable.AsEnumerable().First(a => a.Field<string>("Name") == currDispData.Obj);
                            if (targetRow == null) throw new S2KHelperException($"Could not find the target row for joint displacement Obj={currDispData.Obj}.");

                            Point3D targetCoord = new Point3D(targetRow.Field<double>("NEWX"), targetRow.Field<double>("NEWY"), targetRow.Field<double>("NEWZ"));

                            // It is too far
                            double distance = targetCoord.DistanceTo(currDispData.FinalCoordinates);
                            distances.Add(distance);
                            if (distance > tolerance)
                            {
                                toUpdate.Add((currDispData.Obj, new Point3D(
                                    currDispData.OriginalCoordinates.X + targetCoord.X -
                                    currDispData.FinalCoordinates.X,
                                    currDispData.OriginalCoordinates.Y + targetCoord.Y -
                                    currDispData.FinalCoordinates.Y,
                                    currDispData.OriginalCoordinates.Z + targetCoord.Z -
                                    currDispData.FinalCoordinates.Z)));
                            }
                        }


                        // If no need to update, great - it is finished!
                        if (toUpdate.Count == 0) break;

                        // Writes the dummy s2k
                        BusyOverlayBindings.I.SetIndeterminate($"Import New Coordinates = Writing the s2k joint coordinates table.");
                        StringBuilder fileTextBuilder = new StringBuilder();
                        fileTextBuilder.AppendLine(@"File C:\Users\EngRafaelSMacedo\Desktop\00 CANOPY\21 - Site Data Matching\3 - DEAD Search Interative End Positions\debug.s2k was saved on m/d/yy at h:mm:ss");
                        fileTextBuilder.AppendLine();
                        fileTextBuilder.AppendLine(@"TABLE:  ""PROGRAM CONTROL""");
                        fileTextBuilder.AppendLine(@"   ProgramName=SAP2000   Version=21.1.0   ProgLevel=Ultimate   LicenseNum=3010*1R4S46VQ9SBZJEY   LicenseOS=Yes   LicenseSC=Yes   LicenseHT=No   CurrUnits=""Kip, in, F""   SteelCode=""AISC 360-10""   ConcCode=""ACI 318-14"" _");
                        fileTextBuilder.AppendLine(@"        AlumCode=""AA-ASD 2000""   ColdCode=AISI-ASD96   RegenHinge=Yes");
                        fileTextBuilder.AppendLine();
                        fileTextBuilder.AppendLine(@"TABLE:  ""JOINT COORDINATES""");

                        foreach ((string JointName, Point3D newCoords) item in toUpdate) fileTextBuilder.AppendLine($"   Joint={item.JointName}   CoordSys=GLOBAL   CoordType=Cartesian   XorR={item.newCoords.X}   Y={item.newCoords.Y}   Z={item.newCoords.Z}   SpecialJt=   GlobalX={item.newCoords.X}   GlobalY={item.newCoords.Y}   GlobalZ={item.newCoords.Z}   GUID= ");

                        fileTextBuilder.AppendLine();
                        fileTextBuilder.AppendLine(@"END TABLE DATA");

                        string tempTextFilename = "tempInterationJoints.s2k";
                        string textFullPath = Path.Combine(S2KModel.SM.ModelDir, tempTextFilename);
                        File.WriteAllText(textFullPath, fileTextBuilder.ToString());

                        BusyOverlayBindings.I.SetIndeterminate($"Import New Coordinates = Unlocking model.");
                        S2KModel.SM.AnalysisMan.DeleteAllResults();
                        S2KModel.SM.AnalysisMan.ModelLocked = false;

                        BusyOverlayBindings.I.SetIndeterminate($"Import New Coordinates = Importing New Coordinates into SAP2000.");
                        //S2KModel.SM.InterAuto.Action_ImportCoordinateUpdateFromS2KTextFile(textFullPath);
                        S2KModel.SM.InterAuto.FlaUI_Action_ImportTablesFromS2K(textFullPath);
                        // Reports

                        // Tells the main window to Activate
                        OnGenericCommand("ActivateWindow");

                        BusyOverlayBindings.I.SetIndeterminate($"Saving File.");
                        S2KModel.SM.SaveFile();

                        BusyOverlayBindings.I.LongReport_AddLine($"Iteration {iterationCount}:");
                        BusyOverlayBindings.I.LongReport_AddLine($"--- Count far joints: {toUpdate.Count} | Max Distance: {distances.Max()} | Average: {distances.Sum() / distances.Count}");
                        BusyOverlayBindings.I.LongReport_AddLine($"--- Iteration Time: {(DateTime.Now - startIteration).TotalSeconds}s");
                        BusyOverlayBindings.I.LongReport_AddLine($"--- Total Time: {(DateTime.Now - startAll).TotalSeconds}s");
                        BusyOverlayBindings.I.LongReport_AddLine($"-----------------");
                        BusyOverlayBindings.I.LongReport_AddLine($"");

                        iterationCount++;
                    }

                    S2KModel.SM.SaveFile();

                    BusyOverlayBindings.I.LongReport_AddLine($"FINISHED");
                    BusyOverlayBindings.I.LongReport_AddLine($"--- Total Time: {(DateTime.Now - startAll).TotalSeconds}s");
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
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) OnMessage("Could not match the deflections at the end of the DEAD case with the deflections specified in the SQLite file.", endMessages.ToString());
            }
        }
    }
}