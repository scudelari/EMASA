using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using MathNet.Spatial.Euclidean;
using Microsoft.Win32;
using Prism.Commands;
using RhinoInterfaceLibrary;
using Sap2000Library;
using Sap2000Library.DataClasses.Results;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class RhinoOperationsBindings : BindableSingleton<RhinoOperationsBindings>
    {
        private RhinoOperationsBindings(){}
        public override void SetOrReset()
        {
            MarkJointsInPurple = true;
            TargetRhinoGroupForSelectedJoints = "";

            
        }

        private bool _MarkJointsInPurple;
        public bool MarkJointsInPurple { get => _MarkJointsInPurple; set => SetProperty(ref _MarkJointsInPurple, value); }

        private string _TargetRhinoGroupForSelectedJoints;
        public string TargetRhinoGroupForSelectedJoints { get => _TargetRhinoGroupForSelectedJoints; set => SetProperty(ref _TargetRhinoGroupForSelectedJoints, value); }

        #region Copy Frames and Joints
        private string _groupRegexFilter = @"^[AB]\d\d\w?";
        public string GroupRegexFilter
        {
            get => _groupRegexFilter;
            set => SetProperty(ref _groupRegexFilter, value);
        }

        private DelegateCommand _copyFramesAndJointsToRhinoCommand;
        public DelegateCommand CopyFramesAndJointsToRhinoCommand => _copyFramesAndJointsToRhinoCommand ?? (_copyFramesAndJointsToRhinoCommand = new DelegateCommand(ExecuteCopyFramesAndJointsToRhinoCommand));
        private async void ExecuteCopyFramesAndJointsToRhinoCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                Regex grpRegex = new Regex(GroupRegexFilter);

                OnBeginCommand();

                void lf_Work()
                {

                    BusyOverlayBindings.I.Title = $"Copying selected elements to a Rhino Model.";

                    // Getting the output points
                    List<SapPoint> selPoints = S2KModel.SM.PointMan.GetSelected(true);
                    // Getting the list of survey links
                    List<SapFrame> selFrames = S2KModel.SM.FrameMan.GetSelected(true);

                    if (selPoints.Count == 0 && selFrames.Count == 0) throw new Exception("No joints nor frames were selected in the SAP2000 model.");

                    List<string> groups = S2KModel.SM.GroupMan.GetGroupList(true);

                    BusyOverlayBindings.I.SetIndeterminate("Opening the Rhino Software.");
                    RhinoModel.Initialize();
                    RhinoModel.RM.RhinoVisible = false;

                    Dictionary<string,(Color color, int rhinoId)> grpColorRhinoCode = new Dictionary<string, (Color color, int rhinoId)>();
                    foreach (string grp in groups.Where(a => grpRegex.IsMatch(a)))
                    {
                        int g1 = RhinoModel.RM.AddGroupIfNew(grp);
                        grpColorRhinoCode.Add(grp, (S2KModel.SM.GroupMan.GetGroupColor(grp), g1));
                    }
                    // Adds a new random Joint Group
                    int g2 = RhinoModel.RM.AddGroupIfNew("Joints");
                    grpColorRhinoCode.Add("Joints", (Color.Black, g2));

                    // Adds the joints to Rhino into the group called Joints
                    BusyOverlayBindings.I.SetDeterminate("Adding points to Rhino.", "Point");
                    for (int index = 0; index < selPoints.Count; index++)
                    {
                        SapPoint sp = selPoints[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selPoints.Count, sp.Name);

                        RhinoModel.RM.AddPointWithGroupAndColor(sp.Name, sp.Point, grpColorRhinoCode["Joints"].rhinoId, grpColorRhinoCode["Joints"].color);
                    }

                    // Adds the frames to Rhino into the groups of their modules
                    BusyOverlayBindings.I.SetDeterminate("Adding frames to Rhino.", "Frame");
                    for (int index = 0; index < selFrames.Count; index++)
                    {
                        SapFrame sf = selFrames[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selFrames.Count, sf.Name);

                        string group = sf.Groups.FirstOrDefault(a => grpRegex.IsMatch(a));
                        if (@group == null) continue; // Ignores frames that have no module group

                        RhinoModel.RM.AddLineWithGroupAndColor(sf.Name, sf.iEndPoint.Point, sf.jEndPoint.Point, grpColorRhinoCode[@group].rhinoId, grpColorRhinoCode[@group].color);
                    }

                    RhinoModel.RM.RhinoVisible = true;
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
                if (endMessages.Length != 0) OnMessage("Could not.", endMessages.ToString());
            }
        }
        #endregion

        #region Stepwise Spheres
        private string _sqliteDatabaseFileName;
        public string SqliteDatabaseFileName
        {
            get => _sqliteDatabaseFileName;
            set => SetProperty(ref _sqliteDatabaseFileName, value);
        }

        private DelegateCommand _stepwiseSpheres_BrowseSqLiteCommand;
        public DelegateCommand StepwiseSpheres_BrowseSqLiteCommand => _stepwiseSpheres_BrowseSqLiteCommand ?? (_stepwiseSpheres_BrowseSqLiteCommand = new DelegateCommand(ExecuteStepwiseSpheres_BrowseSqLiteCommand));
        private void ExecuteStepwiseSpheres_BrowseSqLiteCommand()
        {
            // Selects the Excel file in the view thread
            OpenFileDialog ofd = new OpenFileDialog
                {
                Filter = "Sqlite file (*.sqlite)|*.sqlite",
                DefaultExt = "*.sqlite",
                Title = "Select the Sqlite File",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
                };
            bool? ofdret = ofd.ShowDialog();

            if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
            {
                OnMessage("Sqlite File", "Please select a proper Sqlite File!");
                return; // Aborts the Open File
            }


            // Saves the filename
            SqliteDatabaseFileName = ofd.FileName;

        }

        private DelegateCommand _stepwiseSpheres_MakeTemperatureSpheresCommand;
        public DelegateCommand StepwiseSpheres_MakeTemperatureSpheresCommand => _stepwiseSpheres_MakeTemperatureSpheresCommand ?? (_stepwiseSpheres_MakeTemperatureSpheresCommand = new DelegateCommand(ExecuteStepwiseSpheres_MakeTemperatureSpheresCommand));
        private async void ExecuteStepwiseSpheres_MakeTemperatureSpheresCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                // Creates the sqlite connection string based on the selected file
                SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder {DataSource = SqliteDatabaseFileName};

                OnBeginCommand();

                void lf_Work()
                {
                    // Opens the connection to the SqLite file
                    using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                    {
                        sqliteConn.Open();

                        BusyOverlayBindings.I.Title = $"Creating one Rhino model per Step, containing all the temperature positions of the points.";

                        BusyOverlayBindings.I.MessageText = "Reading the steps and temperatures available in the SqLite file.";
                        DataTable stepsTable, temperatureTable;
                        // Reading the temps
                        using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                        {
                            dbCommand.CommandText = "select distinct [Temp] from JointPositionView order by [Temp];";
                            SQLiteDataReader reader = dbCommand.ExecuteReader();
                            temperatureTable = new DataTable();
                            temperatureTable.Load(reader);
                        }
                        // Reading the steps
                        using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                        {
                            dbCommand.CommandText = "select distinct [Step] from JointPositionView order by [Step];";
                            SQLiteDataReader reader = dbCommand.ExecuteReader();
                            stepsTable = new DataTable();
                            stepsTable.Load(reader);
                        }

                        BusyOverlayBindings.I.MessageText = "Reading the joints available in the SqLite file.";
                        DataTable jointsTable;
                        // Reading the joints
                        using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                        {
                            dbCommand.CommandText = "select distinct [SurveyPoint] from JointPositionView";
                            SQLiteDataReader reader = dbCommand.ExecuteReader();
                            jointsTable = new DataTable();
                            jointsTable.Load(reader);
                        }

                        BusyOverlayBindings.I.SetIndeterminate("Opening the Rhino Software.");
                        RhinoModel.Initialize();
                        //RhinoModel.RM.RhinoVisible = false;

                        // Stores the current rhino document name
                        string baseRhinoFullFileName = RhinoModel.RM.RhinoActiveDocumentFullFileName;
                        
                        for (int i = 0; i < stepsTable.Rows.Count; i++)
                        {
                            int step = (int) stepsTable.Rows[i].Field<double>("Step");
                            BusyOverlayBindings.I.SetIndeterminate($"Working with step {step:000}: Re-opening the Base Rhino Document.");

                            // Opens the base Rhino Document
                            RhinoModel.RM.OpenRhinoDocument(baseRhinoFullFileName);

                            // Adds the temperature groups to the Rhino Document
                            BusyOverlayBindings.I.SetIndeterminate($"Working with step {step:000}: Adding the Temperature Groups.");
                            Dictionary<double, int> tempRhinoGrpIndexDic = new Dictionary<double, int>();
                            foreach (DataRow tRow in temperatureTable.AsEnumerable())
                            {
                                double temperature = tRow.Field<double>("Temp");
                                int grpIndex = RhinoModel.RM.AddGroupIfNew($"Temp_{temperature.ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture)}");
                                tempRhinoGrpIndexDic.Add(temperature, grpIndex);
                            }

                            BusyOverlayBindings.I.SetDeterminate($"Working with step {step:000}: Creating the colored joints.", "Joint");
                            for (int j = 0; j < jointsTable.Rows.Count; j++)
                            {
                                DataRow jointRow = jointsTable.Rows[j];
                                BusyOverlayBindings.I.UpdateProgress(j, jointsTable.Rows.Count, jointRow.Field<string>("SurveyPoint"));

                                // Gets this joint coordinates (of all temps) of this step
                                DataTable jcTable;
                                using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                                {
                                    dbCommand.CommandText = $@"SELECT * FROM [JointPositionView] WHERE [SurveyPoint] = '{jointRow.Field<string>("SurveyPoint")}' AND [Step] = {step} ORDER BY [Temp]";
                                    SQLiteDataReader reader = dbCommand.ExecuteReader();
                                    jcTable = new DataTable();
                                    jcTable.Load(reader);
                                }

                                // Creates the lists for Rhino Transfer of Data
                                string[] sphereNames = (from a in jcTable.AsEnumerable()
                                                        select $"{a["SurveyPoint"]}_T{a.Field<double>("Temp").ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture)}").ToArray();
                                List<Point3D> sphereCenters = (from a in jcTable.AsEnumerable()
                                                               select new Point3D(a.Field<double>("X"), a.Field<double>("Y"), a.Field<double>("Z"))).ToList();
                                int[] groupIndexes = (from a in tempRhinoGrpIndexDic select a.Value).ToArray();

                                RhinoModel.RM.AddSpheresInterpolatedColor(sphereNames, sphereCenters, groupIndexes, Color.Blue, Color.Red, 1d);
                            }

                            BusyOverlayBindings.I.SetIndeterminate($"Working with step {step:000}: Changing Display of Ghost Steps.");
                            // Gets the status of each module at this step
                            DataTable stepModStatus;
                            using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                            {
                                dbCommand.CommandText = $@"select grp.[Group],grp.BirthStep,t.BirthStep as Step, grp.BirthStep<=t.BirthStep as IsAlive from [GroupBirth] as grp,[GroupBirth] as t
where t.BirthStep = (select max(BirthStep) from [GroupBirth] where BirthStep <= {step}) and grp.[Group] is not null;";
                                SQLiteDataReader reader = dbCommand.ExecuteReader();
                                stepModStatus = new DataTable();
                                stepModStatus.Load(reader);
                            }
                            foreach (DataRow modStatusRow in stepModStatus.AsEnumerable())
                            {
                                // Ignores the steps that are alive
                                if (modStatusRow.Field<long>("IsAlive") == 1) continue;
                                RhinoModel.RM.ChangePropertiesOfObjectInGroup(modStatusRow.Field<string>("Group"), Color.White, 4); // index 4 = > dashed
                            }

                            // Saves the rhino file
                            BusyOverlayBindings.I.SetIndeterminate($"Working with step {step:000}: Saving the Rhino File.");
                            string thisRhinoFullFileName = Path.Combine(Path.GetDirectoryName(baseRhinoFullFileName), $"Stepwise_Temps_{step:000}.3dm");
                            RhinoModel.RM.SaveAsActiveRhinoDocument(thisRhinoFullFileName);
                        }


                        //RhinoModel.RM.RhinoVisible = true;
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
                if (endMessages.Length != 0) OnMessage("Could not.", endMessages.ToString());
            }
        }
        #endregion
    }
}