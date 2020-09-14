using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Color = Autodesk.Revit.DB.Color;
using Material = Autodesk.Revit.DB.Material;

namespace StepwiseSpheres
{
    /// <remarks>
    /// This inApplication's main class. The class must be Public.
    /// </remarks>
    public class StepwiseSphereExternalApp : IExternalApplication
    {
        // Both OnStartup and OnShutdown must be implemented as public method
        public Result OnStartup(UIControlledApplication inApplication)
        {
            // Add a new ribbon panel
            RibbonPanel ribbonPanel = inApplication.CreateRibbonPanel("Emasa");

            // Create a push button to trigger a command add it to the ribbon panel.
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("cmdEmsStepwiseSphere", "Stepwise Spheres", thisAssemblyPath, "StepwiseSpheres.StepwiseSphereExternalCmd");

            PushButton pushButton = ribbonPanel.AddItem(buttonData) as PushButton;

            // Optionally, other properties may be assigned to the button
            // a) tool-tip
            pushButton.ToolTip = "Create stepwise spheres based on the data contained in the SqLite file.";

            // b) large bitmap
            BitmapImage largeImage = ConvertImage(Properties.Resources.Revit_E);
            pushButton.LargeImage = largeImage;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication inApplication)
        {
            // nothing to clean up in this simple case
            return Result.Succeeded;
        }

        #region Icon Helpers

        public BitmapImage ConvertImage(Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        #endregion
    }
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class StepwiseSphereExternalCmd : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            #region Asks for the IO data
            // Getting the SqLite File
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Sqlite file (*.sqlite)|*.sqlite",
                DefaultExt = "*.sqlite",
                Title = "Select the Sqlite File",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            DialogResult ofdret = ofd.ShowDialog();

            if (string.IsNullOrWhiteSpace(ofd.FileName) || ofdret == DialogResult.Cancel || ofdret == DialogResult.Abort)
            {
                return Result.Failed; // Aborts the Open File
            }

            // Saves the filename
            string SqliteDatabaseFileName = ofd.FileName;
            SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder { DataSource = SqliteDatabaseFileName };

            // Getting the Output Folder
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                Description = "Select the output folder.",
                RootFolder = Environment.SpecialFolder.Desktop,
                ShowNewFolderButton = true
            };

            DialogResult fbdret = fbd.ShowDialog();

            if (string.IsNullOrWhiteSpace(fbd.SelectedPath) || fbdret == DialogResult.Cancel || fbdret == DialogResult.Abort)
            {
                return Result.Failed; // Aborts the Open File
            }

            // Saves the output folder
            string outputFolder = fbd.SelectedPath;
            #endregion

            // Makes the log
            string logFileName = Path.Combine(outputFolder, "log.txt");
            if (File.Exists(logFileName)) File.Delete(logFileName);
            using (StreamWriter logWriter = new StreamWriter(logFileName))
            {
                // Facilitates access
                UIApplication Revit = revit.Application;
                UIDocument UIDoc = Revit.ActiveUIDocument;
                Document Doc = UIDoc.Document;

                // Loads cube the family if it doesn't exist
                string familyName = "Mass_Box";
                Family family;
                FamilySymbol cubeSymbol;

                using (Transaction t = new Transaction(Doc, "Add Cube Family"))
                {
                    t.Start();

                    // Either gets or loads the family of the cube
                    FilteredElementCollector fec = new FilteredElementCollector(Doc).OfClass(typeof(Family));
                    family = fec.FirstOrDefault(e => e.Name.Equals("Mass_Box")) as Family;
                    if (family == null)
                    {
                        string fn = @"C:\ProgramData\Autodesk\RVT 2020\Libraries\UK\Mass\Mass_Box.rfa";
                        Doc.LoadFamily(fn, out family);
                    }

                    // Gets the Family Symbol so that we can replicate afterwards
                    ElementId cubeSymbolId = family.GetFamilySymbolIds().First();
                    cubeSymbol = Doc.GetElement(cubeSymbolId) as FamilySymbol;

                    t.Commit();
                }
                logWriter.WriteLine("Cube Family Added");

                // Opens the connection to the SqLite file
                using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                {
                    sqliteConn.Open();

                    DataTable stepsTable, temperatureTable;
                    // Reading the temps
                    using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                    {
                        dbCommand.CommandText = "select distinct [Temp] from JointPositionView_Buffer where [Temp] in (53,68,88) order by [Temp];";
                        SQLiteDataReader reader = dbCommand.ExecuteReader();
                        temperatureTable = new DataTable();
                        temperatureTable.Load(reader);
                    }

                    // Reading the steps
                    using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                    {
                        dbCommand.CommandText = "select distinct [Step] from JointPositionView_Buffer where [Temp] in (53,68,88) order by [Step];";
                        SQLiteDataReader reader = dbCommand.ExecuteReader();
                        stepsTable = new DataTable();
                        stepsTable.Load(reader);
                    }
                    logWriter.WriteLine("Read the Temp and Steps Distinct Tables");

                    // Adds the materials
                    List<string> matNames = (from a in temperatureTable.AsEnumerable() select $"mat_{a["Temp"]}").ToList();
                    int totalMatCount = matNames.Count;

                    Dictionary<string, Material> colorMats = new Dictionary<string, Material>();
                    using (Transaction t = new Transaction(UIDoc.Document, "Adding the materials"))
                    {
                        t.Start();

                        for (int index = 0; index < matNames.Count; index++)
                        {
                            string matName = matNames[index];

                            ElementId matId = Material.Create(Doc, matName);
                            Material mat = Doc.GetElement(matId) as Material;
                            // Sets the colors
                            mat.Color = LerpRGB(red, blue, (double)index / (totalMatCount - 1));

                            // Adds to the dictionary for easy reference in the future
                            colorMats.Add(matName, mat);
                        }

                        t.Commit();
                    }
                    logWriter.WriteLine("Added the materials");

                    DataTable jointsTable;
                    // Reading the joints
                    using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                    {
                        dbCommand.CommandText = "select distinct [SurveyPoint] from JointPositionView_Buffer where [Temp] in (53,68,88)";
                        SQLiteDataReader reader = dbCommand.ExecuteReader();
                        jointsTable = new DataTable();
                        jointsTable.Load(reader);
                    }

                    // For each step
                    for (int i = 0; i < stepsTable.Rows.Count; i++)
                    {
                        int step = (int)stepsTable.Rows[i].Field<double>("Step");

                        using (Transaction t = new Transaction(UIDoc.Document, $"Clean-up Previous Cubes {step:000}"))
                        {
                            t.Start();

                            FilteredElementCollector fec = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance));
                            Element[] toDel = fec.ToArray();
                            foreach (Element element in toDel) Doc.Delete(element.Id);

                            t.Commit();
                        }
                        logWriter.WriteLine($"{step:000} Cleaned up previous step.");

                        using (Transaction t = new Transaction(UIDoc.Document, $"Add Cubes {step:000}"))
                        {
                            t.Start();

                            // Makes sure the cube symbol is active
                            if (!cubeSymbol.IsActive) cubeSymbol.Activate();

                            foreach (DataRow jointRow in jointsTable.AsEnumerable())
                            {
                                // Gets this joint coordinates (of all temps) of this step
                                DataTable jcTable;
                                using (SQLiteCommand dbCommand = sqliteConn.CreateCommand())
                                {
                                    dbCommand.CommandText = $@"SELECT * FROM [JointPositionView_Buffer] WHERE [SurveyPoint] = '{jointRow.Field<string>("SurveyPoint")}' AND [Step] = {step} AND [Temp] in (53,68,88) ORDER BY [Temp]";
                                    SQLiteDataReader reader = dbCommand.ExecuteReader();
                                    jcTable = new DataTable();
                                    jcTable.Load(reader);
                                }

                                // For each temp based joint coordinate in this row - adds a coloured element
                                foreach (DataRow jcRow in jcTable.AsEnumerable())
                                {
                                    string mat = $"mat_{jcRow["Temp"]}";

                                    XYZ location = new XYZ(
                                        ( jcRow.Field<double>("X") / 12d) + 13900,
                                        ( jcRow.Field<double>("Y") / 12d) + 63600, 
                                        ( (jcRow.Field<double>("Z") - cubeEdgeSize / 2) / 12d) );
                                    FamilyInstance fi = Doc.Create.NewFamilyInstance(location, cubeSymbol, StructuralType.UnknownFraming);
                                    fi.GetParameters("Width").First().Set(cubeEdgeSize); // The database units are in feet
                                    fi.GetParameters("Height").First().Set(cubeEdgeSize); // The database units are in feet
                                    fi.GetParameters("Depth").First().Set(cubeEdgeSize); // The database units are in feet

                                    fi.GetParameters("Mass Material").First().Set(colorMats[mat].Id); // Sets the parameter
                                }
                                logWriter.WriteLine($"{step:000} Added items for joint {jointRow.Field<string>("SurveyPoint")}.");
                            }

                            t.Commit();
                        }

                        using (Transaction t = new Transaction(UIDoc.Document, $"Export to IFC {step:000}"))
                        {
                            t.Start();

                            // Exports it to an IFC
                            Doc.Export(outputFolder, $"Stepwise Temperature Variations - Step {step:000}.ifc", new IFCExportOptions() { FileVersion = IFCVersion.IFC2x3CV2 });

                            t.Commit();
                        }
                        logWriter.WriteLine($"{step:000} Exported IFC.");

                    }
                }

                return Autodesk.Revit.UI.Result.Succeeded;
            }
        }

        private double cubeEdgeSize = 3d / 12d;

        private Color red = new Color(255, 0, 0);
        private Color blue = new Color(0, 0, 255);
        private Color black = new Color(255, 255, 255);
        private Color white = new Color(0, 0, 0);
        private Color green = new Color(0, 255, 0);

        private Color LerpRGB(Color a, Color b, double t)
        {
            return new Color(
                (byte)(a.Red + (b.Red - a.Red) * t),
                (byte)(a.Green + (b.Green - a.Green) * t),
                (byte)(a.Blue + (b.Blue - a.Blue) * t)
            );
        }
    }
}
