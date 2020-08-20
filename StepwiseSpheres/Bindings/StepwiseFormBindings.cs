using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using Microsoft.Win32;
using Prism.Commands;
using Color = System.Drawing.Color;

namespace StepwiseSpheres.Bindings
{
    public class StepwiseFormBindings : BindableSingleton<StepwiseFormBindings>
    {
        private StepwiseFormBindings() { }

        public override void SetOrReset()
        {

        }

        #region Revit Communication Objects
        public ExternalCommandData Revit_ExternalCommandData { get; set; }
        public ElementSet Revit_ElementSet { get; set; }
        public string Revit_OutputMessage { get; private set; } = null;

        private UIApplication Revit => Revit_ExternalCommandData.Application;
        private UIDocument Doc => Revit.ActiveUIDocument;
        #endregion

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
                SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder { DataSource = SqliteDatabaseFileName };

                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = $"Creating one IFC output model per Step, containing all the temperature positions of the points.";

                    // Opens the connection to the SqLite file
                    using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                    {

                        sqliteConn.Open();

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

                        // Stores the current rhino document name
                        string baseRevitFileName = Doc.Document.PathName;

                        for (int i = 0; i < stepsTable.Rows.Count; i++)
                        {
                            int step = (int)stepsTable.Rows[i].Field<double>("Step");
                            BusyOverlayBindings.I.SetIndeterminate($"Working with step {step:000}: Re-opening the Base Rhino Document.");

                            // Opens the base Rhino Document
                            Doc.SaveAndClose();
                            Revit.OpenAndActivateDocument(baseRevitFileName);

                            // Adds the temperature groups to the Rhino Document
                            //BusyOverlayBindings.I.SetIndeterminate($"Working with step {step:000}: Adding the Temperature Groups.");
                            //Dictionary<double, int> tempRhinoGrpIndexDic = new Dictionary<double, int>();
                            //foreach (DataRow tRow in temperatureTable.AsEnumerable())
                            //{
                            //    double temperature = tRow.Field<double>("Temp");
                            //    int grpIndex = RhinoModel.RM.AddGroupIfNew($"Temp_{temperature.ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture)}");
                            //    tempRhinoGrpIndexDic.Add(temperature, grpIndex);
                            //}

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

                                //// Creates the lists for Rhino Transfer of Data
                                //string[] sphereNames = (from a in jcTable.AsEnumerable()
                                //                        select $"{a["SurveyPoint"]}_T{a.Field<double>("Temp").ToString("+###.###F;-###.###F", CultureInfo.InvariantCulture)}").ToArray();
                                //List<Point3D> sphereCenters = (from a in jcTable.AsEnumerable()
                                //                               select new Point3D(a.Field<double>("X"), a.Field<double>("Y"), a.Field<double>("Z"))).ToList();
                                //int[] groupIndexes = (from a in tempRhinoGrpIndexDic select a.Value).ToArray();

                                //RhinoModel.RM.AddSpheresInterpolatedColor(sphereNames, sphereCenters, groupIndexes, Color.Blue, Color.Red, 1d);
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
                            string thisRhinoFullFileName = Path.Combine(Path.GetDirectoryName(baseRevitFileName), $"Stepwise_Temps_{step:000}.3dm");
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

        #region HelperFunctions
        #endregion
    }
}
