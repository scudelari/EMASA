using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
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
using EmasaSapTools.DataGridTypes;
using EmasaSapTools.Resources;
using ExcelDataReader.Exceptions;
using MathNet.Spatial.Euclidean;
using Microsoft.Win32;
using MoreLinq;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.DataClasses.Results;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using DataTable = System.Data.DataTable;
using Excel = Microsoft.Office.Interop.Excel;

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

        private bool _appendPoints_IsChecked = true;
        public bool AppendPoints_IsChecked
        {
            get => _appendPoints_IsChecked;
            set => SetProperty(ref _appendPoints_IsChecked, value);
        }


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

                    string flag = WorkRadio_IsChecked ? "Work" : "Survey";
                    string groupNamePoint = $"0-SurveyPoints_{Areas}_{flag}";
                    if (!AppendPoints_IsChecked)
                    {
                        BusyOverlayBindings.I.SetIndeterminate("Making sure target groups exist.");
                        S2KModel.SM.GroupMan.DeleteGroup(groupNamePoint);
                        S2KModel.SM.GroupMan.AddGroup(groupNamePoint); 
                    }

                    List<SapPoint> existingPoints = null;
                    if (AppendPoints_IsChecked) existingPoints = S2KModel.SM.PointMan.GetAll(true);

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

                            // The point already exists
                            if (AppendPoints_IsChecked) 
                                if (existingPoints.Any(a => a.Name == row["SAPPointName"].ToString())) continue;

                            SapPoint newPoint = S2KModel.SM.PointMan.AddByCoord_ReturnSapEntity((double)row["NX"], (double)row["NY"], (double)row["NZ"], row["SAPPointName"].ToString(), 57);
                            newPoint.AddGroup(groupNamePoint);
                            addedPoints.Add(newPoint);
                        }
                        else if (SurveyRadio_IsChecked)
                        {
                            // Adding the survey points
                            if (row["Type"].ToString().Contains("Work")) continue;

                            // Filters unwanted joints
                            if (row["SAPPointName"].ToString().EndsWith("T")) continue;

                            // Means that the surveyed values were not given
                            if ((double)row["NX"] == -166800d && (double)row["NY"] == -763200d && (double)row["NZ"] == 0) continue;

                            // The point already exists
                            if (AppendPoints_IsChecked) 
                                if (existingPoints.Any(a => a.Name == row["SAPPointName"].ToString())) continue;

                            SapPoint newPoint = S2KModel.SM.PointMan.AddByCoord_ReturnSapEntity((double)row["NX"], (double)row["NY"], (double)row["NZ"], row["SAPPointName"].ToString(), 57);
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

                    string GroupNameFrame = $"0-SurveyLinks_{Areas}_{flag}";
                    if (!AppendPoints_IsChecked)
                    {
                        BusyOverlayBindings.I.SetIndeterminate("Making sure groups exist.");
                        S2KModel.SM.GroupMan.DeleteGroup(GroupNameFrame);
                        S2KModel.SM.GroupMan.AddGroup(GroupNameFrame);
                    }

                    List<SapFrame> previousLinks = null;
                    if (AppendPoints_IsChecked) previousLinks = S2KModel.SM.FrameMan.GetGroup(GroupNameFrame, true);

                    BusyOverlayBindings.I.SetDeterminate("Linking survey points to the canopy.", "Survey Point");
                    double maxDistance = 1; // The maximum distance allowed to just link the survey to the canopy
                    for (int i = 0; i < surveyPoints.Count; i++)
                    {
                        SapPoint surveyPoint = surveyPoints[i];
                        BusyOverlayBindings.I.UpdateProgress(i, surveyPoints.Count, surveyPoint.Name);

                        // Ignoring points that have already been connected
                        if (AppendPoints_IsChecked)
                            if (previousLinks.Any(a => a.IsPointIJ(surveyPoint))) continue;

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

                    DataTable surveyTable = ExcelHelper.GetDataSetFromExcel(ofd.FileName, "TEMP_FIXED_SURVEY");

                    string GroupNamePoint = $"0-SurveyPoints_{Areas}_{flag}";
                    var surveyPoints = S2KModel.SM.PointMan.GetGroup(GroupNamePoint, true);
                    if (surveyPoints.Count == 0) throw new S2KHelperException("Could not get list of survey joints.");

                    BusyOverlayBindings.I.SetDeterminate("Treating the joints to add the support and the displacements.");
                    for (int i = 0; i < surveyTable.Rows.Count; i++)
                    {
                        DataRow dispRow = surveyTable.Rows[i];
                        BusyOverlayBindings.I.UpdateProgress(i, surveyTable.Rows.Count);

                        //if (dispRow["Type"].ToString().Contains("Work")) continue;

                        // Finds the point
                        SapPoint modelPoint = surveyPoints.FirstOrDefault(a => a.Name == dispRow["Joint"].ToString());
                        if (modelPoint == null) continue;

                        // Adds the restraint
                        modelPoint.Restraints = new PointRestraintDef(PointRestraintType.FullyFixed);

                        double delta1 = (double)dispRow["ApplyDeltaX"];
                        double delta2 = (double)dispRow["ApplyDeltaY"];
                        double delta3 = (double)dispRow["ApplyDeltaZ"];

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

                        // Ignores the items that are not part of the selected area
                        //if (!areas.Any(a => a == (int)dispRow.Field<double>("Area"))) continue;

                        //// Adding the work points
                        //if (WorkRadio_IsChecked)
                        //{
                        //    if (dispRow["Type"].ToString().Contains("Srv")) continue;

                        //    // Finds the point
                        //    SapPoint modelPoint =
                        //        surveyPoints.FirstOrDefault(a => a.Name == dispRow["SAPPointName"].ToString());
                        //    if (modelPoint == null)
                        //        continue; // Not finding the point is not a bug - it just means that the data was incomplete and thus it wasn't created

                        //    // Adds the restraint
                        //    modelPoint.Restraints = new PointRestraintDef(PointRestraintType.FullyFixed);

                        //    double delta1 = (double)dispRow["SX"] - (double)dispRow["NX"];
                        //    double delta2 = (double)dispRow["SY"] - (double)dispRow["NY"];
                        //    double delta3 = (double)dispRow["SZ"] - (double)dispRow["NZ"];

                        //    if (delta1 != 0 || delta2 != 0 || delta3 != 0)
                        //    {
                        //        // Adds the Joint Load
                        //        modelPoint.AddDisplacementLoad(new JointDisplacementLoad()
                        //            {
                        //            LoadPatternName = "SURVEY",
                        //            U1 = delta1,
                        //            U2 = delta2,
                        //            U3 = delta3
                        //            });
                        //    }
                        //}
                        //else if (SurveyRadio_IsChecked)
                        //{
                        //    if (dispRow["Type"].ToString().Contains("Work")) continue;

                        //    // Finds the point
                        //    SapPoint modelPoint =
                        //        surveyPoints.FirstOrDefault(a => a.Name == dispRow["SAPPointName"].ToString());
                        //    if (modelPoint == null) continue;

                        //    // Adds the restraint
                        //    modelPoint.Restraints = new PointRestraintDef(PointRestraintType.FullyFixed);

                        //    double delta1 = (double)dispRow["FieldX"] - (double)dispRow["SX"];
                        //    double delta2 = (double)dispRow["FieldY"] - (double)dispRow["SY"];
                        //    double delta3 = (double)dispRow["FieldZ"] - (double)dispRow["SZ"];

                        //    if (delta1 != 0 || delta2 != 0 || delta3 != 0)
                        //    {
                        //        // Adds the Joint Load
                        //        modelPoint.AddDisplacementLoad(new JointDisplacementLoad()
                        //            {
                        //            LoadPatternName = "SURVEY",
                        //            U1 = delta1,
                        //            U2 = delta2,
                        //            U3 = delta3
                        //            });
                        //    }
                        //}
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
       [main].[JointCoordinates].[Name], 
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
       INNER JOIN [main].[JointDisplacement] ON [main].[JointCoordinates].[Name] = [main].[JointDisplacement].[Obj];";
                        // Creates the View
                        using (SQLiteCommand createView = new SQLiteCommand(ViewCommand, sqliteConn))
                        {
                            createView.ExecuteNonQuery();
                        }

                        ViewCommand = @"CREATE VIEW ExcelImport AS 
SELECT 
Name,
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





        private string _solverMatchCasePosition_SelectedCase;
        public string SolverMatchCasePosition_SelectedCase
        {
            get => _solverMatchCasePosition_SelectedCase;
            set => SetProperty(ref _solverMatchCasePosition_SelectedCase, value);
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
                    BusyOverlayBindings.I.Title = $"Finding geometry that will reach given positions when loaded with {SolverMatchCasePosition_SelectedCase} case";
                    BusyOverlayBindings.I.AutomationWarning_Visibility = Visibility.Visible;
                    BusyOverlayBindings.I.LongReport_Visibility = Visibility.Visible;

                    // Selects the files in the view thread
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "SQLite Database (*.db;*.sqlite)|*.db;*.sqlite|Excel File (*.xlsx)|*.xlsx",
                        DefaultExt = "*.db;*.sqlite;*.xlsx",
                        Title = "Select SQLite Database or Excel Sheet!",
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    var ofdret = ofd.ShowDialog();

                    if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName)) throw new S2KHelperException("Please select a proper SQLite file or Excel Sheet!");

                    DataTable targetCoordTable = new DataTable();

                    if (Path.GetExtension(ofd.FileName) == ".db" || Path.GetExtension(ofd.FileName) == ".sqlite")
                    {
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
                    }
                    else if (Path.GetExtension(ofd.FileName) == ".xlsx")
                    {
                        // Reads the excel file
                        BusyOverlayBindings.I.SetIndeterminate("Reading the Excel Input.");

                        DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName, new int[] { 0 });
                        targetCoordTable = fromExcel.Tables[0];
                    }
                    else throw new S2KHelperException($"The file is not a sqlite database nor an excel file.");

                    int iterationCount = 0;
                    DateTime startAll = DateTime.Now;
                    while (true)
                    {
                        DateTime startIteration = DateTime.Now;

                        BusyOverlayBindings.I.SetIndeterminate($"Running SAP2000 {SolverMatchCasePosition_SelectedCase} case.");
                        // Runs the selected case in SAP2000
                        S2KModel.SM.AnalysisMan.DeleteAllResults();
                        S2KModel.SM.AnalysisMan.ModelLocked = false;
                        S2KModel.SM.AnalysisMan.SetAllNotToRun();
                        S2KModel.SM.AnalysisMan.SetCaseRunFlag(SolverMatchCasePosition_SelectedCase, true);
                        S2KModel.SM.AnalysisMan.RunAnalysis();

                        // Selects the joints listed in the target coordinate table
                        BusyOverlayBindings.I.SetIndeterminate("Selecting the joints that are listed in the SQLite file.");
                        S2KModel.SM.ClearSelection();

                        // Tries to hide for faster selection
                        S2KModel.SM.WindowVisible = false;
                        foreach (DataRow dataRow in targetCoordTable.Rows)
                        {
                            S2KModel.SM.PointMan.SelectElements(dataRow["Name"].ToString());
                        }
                        S2KModel.SM.WindowVisible = true;

                        // Gets the selected joints
                        List<SapPoint> selPoints = S2KModel.SM.PointMan.GetSelected(true);

                        S2KModel.SM.ResultMan.DeselectAllCasesAndCombosForOutput();
                        S2KModel.SM.ResultMan.SetOptionMultiStepStatic(OptionMultiStepStatic.LastStep);
                        S2KModel.SM.ResultMan.SetOptionMultiValuedCombo(OptionMultiValuedCombo.Correspondence);
                        S2KModel.SM.ResultMan.SetOptionNLStatic(OptionNLStatic.LastStep);

                        S2KModel.SM.ResultMan.SetCaseSelectedForOutput(SolverMatchCasePosition_SelectedCase);

                        BusyOverlayBindings.I.SetIndeterminate($"Results Acquire = Acquire Joint Displacement Data.");
                        List<JointDisplacementData> jointDisplacementDatas =  S2KModel.SM.ResultMan.GetJointDisplacement("", ItemTypeElmEnum.SelectionElm);

                        BusyOverlayBindings.I.SetDeterminate($"Results Acquire = Linking Joint Displacement Data to their Joints.", "Joint");
                        for (int i = 0; i < jointDisplacementDatas.Count; i++)
                        {
                            JointDisplacementData currDispData = jointDisplacementDatas[i];
                            BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);

                            SapPoint linkedPoint = selPoints.FirstOrDefault(a => a.Name == currDispData.Obj);
                            if (linkedPoint == null) throw new S2KHelperException($"Could not find the SapPoint linked to the Displacement Data with Obj={currDispData.Obj}.");

                            currDispData.LinkedPoint = linkedPoint;
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

                            DataRow targetRow = targetCoordTable.AsEnumerable().First(a => a["Name"].ToString() == currDispData.Obj);
                            if (targetRow == null) throw new S2KHelperException($"Could not find the target row for joint displacement Obj={currDispData.Obj}.");

                            Point3D targetCoord;
                            if (Path.GetExtension(ofd.FileName) == ".db" || Path.GetExtension(ofd.FileName) == ".sqlite")
                            {
                                targetCoord = new Point3D(targetRow.Field<double>("NEWX"), targetRow.Field<double>("NEWY"), targetRow.Field<double>("NEWZ"));
                            }
                            else if (Path.GetExtension(ofd.FileName) == ".xlsx")
                            {
                                targetCoord = new Point3D(targetRow.Field<double>("TargetX"), targetRow.Field<double>("TargetY"), targetRow.Field<double>("TargetZ"));
                            }
                            else throw new S2KHelperException($"The file is not a sqlite database nor an excel file.");
                                

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






        private DelegateCommand _getResultDisplacementSupportFall;
        public DelegateCommand GetResultDisplacementSupportFall =>
            _getResultDisplacementSupportFall ?? (_getResultDisplacementSupportFall = new DelegateCommand(ExecuteGetResultDisplacementSupportFall));
        async void ExecuteGetResultDisplacementSupportFall()
        {
            StringBuilder endMessages = new StringBuilder();

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
            bool? ofdret = ofd.ShowDialog();

            if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
            {
                OnMessage("Excel File", "Please select a proper Excel File!");
                return; // Aborts the Open File
            }

            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    
                    
                    BusyOverlayBindings.I.Title = $"Getting support fall results";

                    BusyOverlayBindings.I.SetIndeterminate($"Gathering basic load case data and validation.");
                    DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName);

                    // Reads the steps table and puts it in Excel!
                    DataTable stepsTable = fromExcel.Tables["Steps"];
                    List<SCStepsDataGridType> steps = new List<SCStepsDataGridType>();
                    BusyOverlayBindings.I.SetIndeterminate("Reading Steps Table.");
                    foreach (DataRow row in stepsTable.Rows)
                    {
                        StagedConstructionOperation op = default;
                        string operationText = row.Field<string>("Operation");
                        switch (operationText)
                        {
                            case "Add Guide Structure":
                                op = StagedConstructionOperation.AddGuideStructure;
                                break;

                            case "Add Structure":
                                op = StagedConstructionOperation.AddStructure;
                                break;

                            case "Change Releases":
                                op = StagedConstructionOperation.ChangeReleases;
                                break;

                            case "Change Section/Area":
                                op = StagedConstructionOperation.ChangeSectionProperties_Area;
                                break;

                            case "Change Section/Frame":
                                op = StagedConstructionOperation.ChangeSectionProperties_Frame;
                                break;

                            case "Change Section/Link":
                                op = StagedConstructionOperation.ChangeSectionProperties_Link;
                                break;

                            case "Change Section/Cable":
                                op = StagedConstructionOperation.ChangeSectionProperties_Cable;
                                break;

                            case "Load Structure (OTHER LIST)":
                                op = StagedConstructionOperation.LoadObjects;
                                break;

                            case "Load Structure if Added (OTHER LIST)":
                                op = StagedConstructionOperation.LoadObjectsIfNew;
                                break;

                            case "Remove Structure":
                                op = StagedConstructionOperation.RemoveStructure;
                                break;

                            case "Section Modifier/Area":
                                op = StagedConstructionOperation.ChangeSectionPropertyModifiers_Area;
                                break;

                            case "Section Modifier/Frame":
                                op = StagedConstructionOperation.ChangeSectionPropertyModifiers_Frame;
                                break;

                            default:
                                throw new InvalidOperationException(
                                    $"There is an error in the Excel. Steps column. {operationText} does not designate an implemented operation!");
                        }

                        steps.Add(new SCStepsDataGridType
                        {
                            GroupName = row.Field<string>("GroupName"),
                            NamedProp = row.Field<string>("NamedProp"),
                            Operation = op,
                            Order = (int)row.Field<double>("Order")
                        });
                    }

                    throw new NotImplementedException();

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

        #region Stepwise Report

        private DataTable _stepwiseReport_MatchDataTable = null;

        private string _stepwiseReport_ExcelMatchFileTextBox;
        public string StepwiseReport_ExcelMatchFileTextBox
        {
            get => _stepwiseReport_ExcelMatchFileTextBox;
            set => SetProperty(ref _stepwiseReport_ExcelMatchFileTextBox, value);
        }
        private string _stepwiseReport_DesiredTemperatureTextBox = "68 -5[3] +5[4]";
        public string StepwiseReport_DesiredTemperatureTextBox
        {
            get => _stepwiseReport_DesiredTemperatureTextBox;
            set => SetProperty(ref _stepwiseReport_DesiredTemperatureTextBox, value);
        }

        private ICollectionView _stepwiseReport_OutputGroups_ViewItems;
        public ICollectionView StepwiseReport_OutputGroups_ViewItems
        {
            get => _stepwiseReport_OutputGroups_ViewItems;
            set
            {
                SetProperty(ref _stepwiseReport_OutputGroups_ViewItems, value);
                _stepwiseReport_OutputGroups_ViewItems.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
        }
        private string _stepwiseReport_SelectedOutputGroup;
        public string StepwiseReport_SelectedOutputGroup
        {
            get => _stepwiseReport_SelectedOutputGroup;
            set => SetProperty(ref _stepwiseReport_SelectedOutputGroup, value);
        }


        private DelegateCommand _stepwiseReport_SelectMatchExcelCommand;
        public DelegateCommand StepwiseReport_SelectMatchExcelCommand =>
            _stepwiseReport_SelectMatchExcelCommand ?? (_stepwiseReport_SelectMatchExcelCommand = new DelegateCommand(ExecuteStepwiseReport_SelectMatchExcelCommand));
        void ExecuteStepwiseReport_SelectMatchExcelCommand()
        {
            // Selects the Excel file in the view thread
            OpenFileDialog ofd = new OpenFileDialog
                {
                Filter = "Excel file (*.xls;*.xlsx)|*.xls;*.xlsx",
                DefaultExt = "*.xls;*.xlsx",
                Title = "Select the Match Excel File With Match Sheet in the Correct Format!",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
                };
            bool? ofdret = ofd.ShowDialog();

            if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
            {
                OnMessage("Excel File", "Please select a proper Excel File!");
                return; // Aborts the Open File
            }

            try
            {
                // Tries to read the selected excel for the Match sheet
                DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName);
                // Reads the steps table and puts it in Excel!
                DataTable matchDataTable = fromExcel.Tables["Match"];
                _stepwiseReport_MatchDataTable = matchDataTable;
            }
            catch (Exception)
            {
                OnMessage("Excel File", "Could not find the Match table in the excel file.");
                return; // Aborts the Open File
            }

            // Saves the filename
            StepwiseReport_ExcelMatchFileTextBox = ofd.FileName;
        }

        private DelegateCommand _stepwiseReport_GenerateAndRunCasesCommand;
        public DelegateCommand StepwiseReport_GenerateAndRunCasesCommand =>
            _stepwiseReport_GenerateAndRunCasesCommand ?? (_stepwiseReport_GenerateAndRunCasesCommand = new DelegateCommand(ExecuteStepwiseReport_GenerateAndRunCasesCommand));
        async void ExecuteStepwiseReport_GenerateAndRunCasesCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                if (string.IsNullOrWhiteSpace(StepwiseReport_DesiredTemperatureTextBox)) throw new ArgumentNullException("Match DataTable", "Please select the Excel file with the matching table.");
                if (_stepwiseReport_MatchDataTable == null) throw new ArgumentNullException("Match DataTable", "The Match Excel DataTable cannot be null.");

                double ref_temp = double.NaN;
                Dictionary<int, double> tempStepDictionary = new Dictionary<int, double>();

                try
                {
                    Regex chunkReader = new Regex(@"(?<ref>\d+)\s((?<tvar>[\-\+\d\.]*)\[(?<tcount>\d*)]\s?)*");
                    Match m = chunkReader.Match(StepwiseReport_DesiredTemperatureTextBox);
                    if (!m.Success) throw new Exception();

                    ref_temp = double.Parse(m.Groups["ref"].Value);

                    for (int i = 0; i < m.Groups["tcount"].Captures.Count; i++)
                    {
                        tempStepDictionary.Add(int.Parse(m.Groups["tcount"].Captures[i].Value),double.Parse(m.Groups["tvar"].Captures[i].Value));
                    }
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Desired Temperatures", "You did not specify the temperatures properly.");
                }

                OnBeginCommand();

                void lf_Work()
                {

                    BusyOverlayBindings.I.Title = $"Generating the Stepwise (with temperature) Load Cases and Run";

                    BusyOverlayBindings.I.SetIndeterminate($"Getting the list of Staged Construction cases with format <###_BD_1D>.");
                    List<LCNonLinear> allNlCases = S2KModel.SM.LCMan.GetNonLinearStaticLoadCaseList(inRegexFilter: @"\d\d\d_BD_1D");
                    allNlCases.RemoveAll(a => a.NLSubType != LCNonLinear_SubType.StagedConstruction);

                    BusyOverlayBindings.I.SetDeterminate("Generating Report the Temperature Load Cases for Each Step.", "Temperature");

                    int currentCase = 0;
                    int totalCases = tempStepDictionary.Aggregate(1, (inD, inPair) => inD * inPair.Key) * allNlCases.Count;

                    double shift = 0d;

                    for (int i = 0; i < tempStepDictionary.Count; i++)
                    {
                        KeyValuePair<int, double> pair = tempStepDictionary.ElementAt(i);

                        for (int j = 0; j < pair.Key; j++)
                        {
                            double tval = ref_temp + (pair.Value * (j + 1));

                            string tval_string = tval.ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture);
                            BusyOverlayBindings.I.UpdateProgress(currentCase++, totalCases, tval_string);

                            string pattern_name = $"RepT";

                            string currentCaseNamePrefix = $"T_{tval_string}_";
                            //string previousCaseNamePrefix = j == 0 ? "" : "T_" + (tval - pair.Value).ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture) + "_";
                            string previousCaseNamePrefix = "T_" + (tval - pair.Value).ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture) + "_";

                            string refTempPrefix = "T_" + (ref_temp).ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture) + "_";

                            foreach (LCNonLinear bdcase in allNlCases)
                            {
                                // Sets the temperature case
                                S2KModel.SM.LCMan.AddNew_LCNonLinear(inCaseName: bdcase.Name.Replace(refTempPrefix, currentCaseNamePrefix),
                                    inLoads: new List<LoadCaseNLLoadData>()
                                        {
                                    new LoadCaseNLLoadData() {LoadType = "Load", LoadName = pattern_name, ScaleFactor = pair.Value},
                                        },
                                    inUseStepping: true,
                                    inPreviousCase: bdcase.Name.Replace(refTempPrefix, previousCaseNamePrefix),
                                    inGeomNonLinearType: LCNonLinear_NLGeomType.None);
                            }
                        } 
                    }

                    // Renames the base dead cases

                    //foreach (LCNonLinear lcNonLinear in allNlCases)
                    //{
                    //    S2KModel.SM.LCMan.RenameCase(lcNonLinear.Name, $"T_{ref_temp.ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture)}_{lcNonLinear.Name}");
                    //}

                    //// Sets all cases to Run
                    S2KModel.SM.AnalysisMan.SetAllToRun();
                    BusyOverlayBindings.I.SetIndeterminate($"Running the SAP2000 Analysis");
                    S2KModel.SM.AnalysisMan.RunAnalysis();
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
                if (endMessages.Length != 0) OnMessage("Could not export create and run the desired temperatures.", endMessages.ToString());
            }
        }

        private DelegateCommand _stepwiseReport_GenerateReportCommand;
        public DelegateCommand StepwiseReport_GenerateReportCommand =>
            _stepwiseReport_GenerateReportCommand ?? (_stepwiseReport_GenerateReportCommand = new DelegateCommand(ExecuteStepwiseReport_GenerateReportCommand));
        async void ExecuteStepwiseReport_GenerateReportCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                if (string.IsNullOrWhiteSpace(StepwiseReport_DesiredTemperatureTextBox)) throw new ArgumentNullException("Match DataTable", "Please select the Excel file with the matching table.");
                if (_stepwiseReport_MatchDataTable == null) throw new ArgumentNullException("Match DataTable", "The Match Excel DataTable cannot be null.");

                OnBeginCommand();

                void lf_Work()
                {

                    BusyOverlayBindings.I.Title = $"Generating the Stepwise (with temperature) Report";

                    if (!S2KModel.SM.AnalysisMan.ModelLocked) throw new Exception("The model must be locked and the cases must be run!");

                    // Getting the output points
                    S2KModel.SM.ClearSelection();
                    S2KModel.SM.GroupMan.SelectGroup(StepwiseReport_SelectedOutputGroup);
                    List<SapPoint> selPoints = S2KModel.SM.PointMan.GetSelected(true);

                    // Getting the list of survey links
                    S2KModel.SM.ClearSelection();
                    //S2KModel.SM.GroupMan.SelectGroup("0-SurveyPoints_1_2_3_4_5_Survey");
                    S2KModel.SM.GroupMan.SelectGroup("0-SurveyLinks_1_2_3_4_5_Survey");
                    S2KModel.SM.GroupMan.DeselectGroup("*** GHOST ***");
                    List<SapFrame> linkFrames = S2KModel.SM.FrameMan.GetSelected(true);

                    // Finds a match 
                    BusyOverlayBindings.I.SetIndeterminate("Finding the modules related to each survey point.");
                    Regex grpRegex = new Regex(@"^[AB]\d\d\w?");
                    Dictionary<SapPoint, string> pntModuleDic = new Dictionary<SapPoint, string>();
                    foreach (SapPoint pnt in selPoints)
                    {
                        // Finds the frame
                        SapFrame frame = (from a in linkFrames
                            where a.IsPointIJ(pnt)
                            select a).First();

                        // Gets the frame's group that is a module
                        string grpName = (from a in frame.Groups
                            where grpRegex.IsMatch(a)
                            select a).First();

                        // Adds the match to the dictionary
                        pntModuleDic.Add(pnt, grpName);
                    }

                    BusyOverlayBindings.I.SetIndeterminate($"Setting the results output configuration");

                    // Performs basic setup for output
                    S2KModel.SM.ClearSelection();
                    S2KModel.SM.GroupMan.SelectGroup(StepwiseReport_SelectedOutputGroup);

                    S2KModel.SM.ResultMan.DeselectAllCasesAndCombosForOutput();
                    S2KModel.SM.ResultMan.SetOptionMultiStepStatic(OptionMultiStepStatic.LastStep);
                    S2KModel.SM.ResultMan.SetOptionMultiValuedCombo(OptionMultiValuedCombo.Correspondence);
                    S2KModel.SM.ResultMan.SetOptionNLStatic(OptionNLStatic.LastStep);

                    List<LCNonLinear> runNlCases = S2KModel.SM.LCMan.GetNonLinearStaticLoadCaseList().Where(a => a.Status == LCStatus.Finished).ToList();
                    foreach (LCNonLinear item in runNlCases) S2KModel.SM.ResultMan.SetCaseSelectedForOutput(item);

                    BusyOverlayBindings.I.SetIndeterminate($"Getting the Joint Displacement Results Data from SAP2000.");
                    List<JointDisplacementData> jointDisplacementDatas = S2KModel.SM.ResultMan.GetJointDisplacement("", ItemTypeElmEnum.SelectionElm);

                    BusyOverlayBindings.I.SetDeterminate($"Linking the Joint Displacement Data to the Joints (for global coord. transform).");
                    for (int i = 0; i < jointDisplacementDatas.Count; i++)
                    {
                        JointDisplacementData jdd = jointDisplacementDatas[i];
                        BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count);
                        
                        jdd.LinkedPoint = selPoints.First(a => a.Name == jdd.Obj);
                    }

                    BusyOverlayBindings.I.SetDeterminate($"Filling the global coordinate transformed data for each result.");
                    for (int i = 0; i < jointDisplacementDatas.Count; i++)
                    {
                        JointDisplacementData currDispData = jointDisplacementDatas[i];

                        BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count);

                        currDispData.FillGlobalCoordinates();
                    }

                    Regex prefixRegex = new Regex(@"^S_\dSrv._");
                    BusyOverlayBindings.I.SetDeterminate($"Fixing the names at the joint coordinates data.");
                    for (int index = 0; index < selPoints.Count; index++)
                    {
                        SapPoint selPoint = selPoints[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selPoints.Count);

                        selPoint.Name = prefixRegex.Replace(selPoint.Name, "");
                    }
                    BusyOverlayBindings.I.SetDeterminate($"Fixing the names at the joint displacement data.");
                    for (int index = 0; index < jointDisplacementDatas.Count; index++)
                    {
                        JointDisplacementData currDispData = jointDisplacementDatas[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selPoints.Count);

                        currDispData.Obj = prefixRegex.Replace(currDispData.Obj, "");
                    }
                    // Check if we need to change the names at the module dictionary - probably not as they have references to the same point object.

                    #region SQLite File

                    string sqlFileName = Path.Combine(S2KModel.SM.ModelDir, Path.GetFileNameWithoutExtension(S2KModel.SM.FullFileName) + $"_StepwiseReport_{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}.sqlite");
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

                        int bufferCounter = 1;
                        BusyOverlayBindings.I.SetDeterminate("Saving the joint displacement table to the aggregate SQlite file.", "Object");
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
                        BusyOverlayBindings.I.SetDeterminate("Saving the joint coordinate table to the aggregate SQlite file.", "Point");
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

                        // Saving the joint module dictionary to the table
                        using (SQLiteCommand createTable = new SQLiteCommand(@"CREATE TABLE JointModule ( Name TEXT, Module TEXT);", sqliteConn))
                        {
                            createTable.ExecuteNonQuery();
                        }

                        bufferCounter = 1;
                        BusyOverlayBindings.I.SetDeterminate("Saving the joint module dictionary table to the aggregate SQlite file.", "Point");
                        transaction = sqliteConn.BeginTransaction();
                        for (int i = 0; i < selPoints.Count; i++)
                        {
                            SapPoint currPoint = selPoints[i];

                            using (SQLiteCommand insertCommand = new SQLiteCommand($@"INSERT INTO JointModule (Name , Module) VALUES('{currPoint.Name ?? "NULL"}','{pntModuleDic.Where(a => a.Key.Name == currPoint.Name).First().Value}');", sqliteConn))
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

                    #endregion
                    
                    BusyOverlayBindings.I.SetDeterminate("Generating the Separated Excel Files.", "Load Case");

                    Regex caseRegex = new Regex(@"^T_(?<temp>[\+\-]?\d+)[FC]?_(?<step>\d*)_BD_1D");

                    // Opens the Excel application that will be used to handle the excel data
                    Excel._Application excelApp = new Excel.Application();
                    Excel._Workbook excelWorkbook = null;
                    Excel._Worksheet excelWorksheetFinalCoordsInFt = null;


                    for (int i = 0; i < runNlCases.Count; i++)
                    {
                        LCNonLinear nlCase = runNlCases[i];
                        BusyOverlayBindings.I.UpdateProgress(i, runNlCases.Count, nlCase.Name);

                        Match m = caseRegex.Match(nlCase.Name);
                        if (!m.Success) throw new Exception($"Case {nlCase.Name} did not match the expected temperature Regex.");

                        string caseFolderName = Path.Combine(S2KModel.SM.ModelDir, Path.GetFileNameWithoutExtension(S2KModel.SM.FullFileName), $"TEMP {m.Groups["temp"].Value}F");
                        if (!Directory.Exists(caseFolderName)) Directory.CreateDirectory(caseFolderName);

                        // Getting the step number
                        int step = Int32.MinValue;
                        try
                        {
                            step = int.Parse(m.Groups["step"].Value);
                        }
                        catch
                        {
                            throw new Exception($"Could not parse the step number from the load case case name.");
                        }

                        double temperature = double.NaN;
                        try
                        {
                            temperature = double.Parse(m.Groups["temp"].Value);
                        }
                        catch
                        {
                            throw new Exception($"Could not parse the temperature from the load case case name.");
                        }


                        // Getting the friendly name from the match table
                        string stepFriendlyName = _stepwiseReport_MatchDataTable.AsEnumerable().First(a => a.Field<double>("Step") == step).Field<string>("ProjectStepName");

                        // Configures the output Table for the excel output
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Survey Point Name", typeof(string)); // 0
                        dt.Columns.Add("Northing (Y) (ft)", typeof(double));
                        dt.Columns.Add("Easting (X) (ft)", typeof(double));
                        dt.Columns.Add("Elevation (Z) (ft)", typeof(double));
                        dt.Columns.Add("Description", typeof(string));

                        //dt.Columns.Add("Temperature (F)", typeof(double));
                        //dt.Columns.Add("Step", typeof(string));
                        //dt.Columns.Add("Module", typeof(string)); // 6
                        
                        //dt.Columns.Add("Delta Y (ft)", typeof(double)); // 7
                        //dt.Columns.Add("Delta X (ft)", typeof(double));
                        //dt.Columns.Add("Delta Z (ft)", typeof(double));

                        //dt.Columns.Add("As-Fab Y (ft)", typeof(double)); // 10
                        //dt.Columns.Add("As-Fab X (ft)", typeof(double));
                        //dt.Columns.Add("As-Fab Z (ft)", typeof(double));

                        foreach (JointDisplacementData dispData in jointDisplacementDatas.Where(a => a.LoadCase == nlCase.Name))
                        {
                            DataRow r = dt.NewRow();
                            int count = 0;
                            r[count++] = dispData.Obj;

                            r[count++] = (dispData.FinalCoordinates.Y / 12d) + 63600; // Y Final Coordinate
                            r[count++] = (dispData.FinalCoordinates.X / 12d) + 13900; // X Final Coordinate
                            r[count++] = (dispData.FinalCoordinates.Z / 12d); // Z Final Coordinate

                            r[count++] = $"PNTMOD[{pntModuleDic.Where(a => a.Key.Name == dispData.LinkedPoint.Name).First().Value}]_STEP[{stepFriendlyName}]_TEMP[{temperature}F]";

                            //r[count++] = temperature;
                            //r[count++] = stepFriendlyName;
                            //r[count++] = pntModuleDic.Where(a => a.Key.Name == dispData.LinkedPoint.Name).First().Value; // Saves the module name

                            //r[count++] = (dispData.GlobalU2.Value / 12d); // Y Delta
                            //r[count++] = (dispData.GlobalU1.Value / 12d); // X Delta
                            //r[count++] = (dispData.GlobalU3.Value / 12d); // Z Delta

                            //r[count++] = (dispData.LinkedPoint.Y / 12d) + 63600; // Y Final Coordinate
                            //r[count++] = (dispData.LinkedPoint.X / 12d) + 13900; // X Final Coordinate
                            //r[count++] = (dispData.LinkedPoint.Z / 12d); // Z Final Coordinate

                            dt.Rows.Add(r);
                        }


                        // Adding the data to a new excel workbook
                        // Starts a new workbook
                        excelWorkbook = excelApp.Workbooks.Add();
                        excelWorksheetFinalCoordsInFt = (Excel.Worksheet)excelWorkbook.Worksheets[1]; // First worksheet is in the first index
                        excelWorksheetFinalCoordsInFt.Name = $"Data";

                        object[,] tableAsObjects = dt.ConvertToObjectArray2(true);
                        excelWorksheetFinalCoordsInFt.Range[excelWorksheetFinalCoordsInFt.Cells[1, 1], excelWorksheetFinalCoordsInFt.Cells[tableAsObjects.GetLength(0), tableAsObjects.GetLength(1)]].Value
                            = tableAsObjects;

                        string excelFullFileName = Path.Combine(caseFolderName, $"{stepFriendlyName} - TEMP {m.Groups["temp"].Value}F.xlsx");
                        excelWorkbook.Close(true, excelFullFileName);

                    }

                    // Closes the excel app that was handling the creation of the files
                    excelApp.Quit();

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
                if (endMessages.Length != 0) OnMessage("Could not export the displacement data of the points to the SQLite database and the infinite excel files.", endMessages.ToString());
            }
        }
        #endregion


        #region Add Prescribed Displacements

        private bool _addPrescribedDisplacements_ContainsDeltas = false;
        public bool AddPrescribedDisplacements_ContainsDeltas
        {
            get => _addPrescribedDisplacements_ContainsDeltas;
            set => SetProperty(ref _addPrescribedDisplacements_ContainsDeltas, value);
        }
        private bool _addPrescribedDisplacements_ContainsFinalPositions = true;
        public bool AddPrescribedDisplacements_ContainsFinalPositions
        {
            get => _addPrescribedDisplacements_ContainsFinalPositions;
            set => SetProperty(ref _addPrescribedDisplacements_ContainsFinalPositions, value);
        }

        private DelegateCommand _addPrescribedDisplacementsCommand;
        public DelegateCommand AddPrescribedDisplacementsCommand => _addPrescribedDisplacementsCommand ?? (_addPrescribedDisplacementsCommand = new DelegateCommand(ExecuteAddPrescribedDisplacementsCommand));
        async void ExecuteAddPrescribedDisplacementsCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    string flag = AddPrescribedDisplacements_ContainsDeltas ? "Work" : "Survey";
                    BusyOverlayBindings.I.Title = $"Adding Fixed Support and Displacement Information to the joints. ";

                    // Selects the Excel file in the view thread
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "Excel file (*.xls;*.xlsx)|*.xls;*.xlsx",
                        DefaultExt = "*.xls;*.xlsx",
                        Title = $"Select the Excel File. First sheet name is the load pattern. Columns must contain joint name, x, y, z in this order.",
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

                    DataSet fromExcel = ExcelHelper.GetDataSetFromExcel(ofd.FileName);

                    string sheetName = fromExcel.Tables[0].TableName;
                    DataTable surveyData = fromExcel.Tables[0];

                    // Makes sure that the load pattern exists
                    if (!S2KModel.SM.LPMan.GetAllNames().Contains(sheetName))
                    {
                        S2KModel.SM.LPMan.Add(new LoadPatternData()
                            {
                            Name = sheetName,
                            PatternType = LoadPatternType.LTYPE_DEAD,
                            SelfWeightMultiplier = 0d,
                        }, false);
                    }

                    // Gets all SAP points
                    List<SapPoint> allPoints = S2KModel.SM.PointMan.GetAll(true);

                    BusyOverlayBindings.I.SetDeterminate("Adding the prescribed displacements.", "Excel Line");

                    for (int index = 0; index < surveyData.Rows.Count; index++)
                    {
                        DataRow excelRow = surveyData.Rows[index];

                        BusyOverlayBindings.I.UpdateProgress(index, surveyData.Rows.Count, $"{index + 2}");

                        string jointName;
                        double excelX, excelY, excelZ;

                        try
                        {
                            jointName = excelRow[0].ToString();
                            excelX = excelRow.Field<double>(1);
                            excelY = excelRow.Field<double>(2);
                            excelZ = excelRow.Field<double>(3);
                        }
                        catch
                        {
                            endMessages.AppendLine($"Could not parse excel data - excel row number {index + 2}");
                            continue;
                        }

                        SapPoint p = allPoints.FirstOrDefault(a => a.Name == jointName);
                        if (p == null)
                        {
                            endMessages.AppendLine($"Could not find joint named {jointName} in the model - excel row number {index + 2}");
                            continue;
                        }

                        // Adds the prescribed displacement
                        double dx, dy, dz;
                        if (AddPrescribedDisplacements_ContainsDeltas)
                        {
                            dx = excelX;
                            dy = excelY;
                            dz = excelZ;
                        }
                        else
                        {
                            dx = excelX - p.X;
                            dy = excelY - p.Y;
                            dz = excelZ - p.Z;
                        }

                        // Adds the restraints to the joints
                        p.Restraints = new PointRestraintDef(PointRestraintType.FullyFixed);

                        // Adds the load
                        p.AddDisplacementLoad(new JointDisplacementLoad()
                            {
                            LoadPatternName = sheetName,
                            U1 = dx,
                            U2 = dy,
                            U3 = dz
                            });
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
                if (endMessages.Length != 0) OnMessage("Some issues occured in the following items.", endMessages.ToString());
            }
        }

        #endregion
    }
}