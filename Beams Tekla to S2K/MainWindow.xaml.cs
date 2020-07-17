using MathNet.Spatial.Euclidean;
using Microsoft.Win32;
using Sap2000Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Sap2000Library.SapObjects;

namespace Beams_Tekla_to_S2K
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //this.Icon = S2KHelper.Properties.Resources.EMS_32.GetImageSource();
        }

        private DataTable beamTable = null;
        private void ButtonBrowseXML_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Browse for the XML file that was generated using the Tekla XML Output.",
                Filter = "XML File (.xml)|*.xml",
                Multiselect = false
            };
            ofd.ShowDialog();

            if (string.IsNullOrWhiteSpace(ofd.FileName))
            {
                MessageBox.Show("No file was selected. Please select a XML file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Tries to read the DataSet
                DataSet ds = new DataSet();
                using (TextReader tr = new StreamReader(ofd.FileName))
                {
                    ds.ReadXml(tr);
                }

                // Gets the beam Table
                beamTable = ds.Tables["MyBeamTable"];
                beamTable.AcceptChanges();

                TextBoxXMLFileName.Text = ofd.FileName;
                GroupBoxXMLFile.IsEnabled = false;
                ButtonGenerateSap2000.IsEnabled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("The selected XML is not in the proper format.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        //S2KModel S2KModel.SM = null;
        BackgroundWorker bwGenerateSap2000 = null;
        private void ButtonGenerateSap2000_Click(object sender, RoutedEventArgs e)
        {
            bwGenerateSap2000 = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            bwGenerateSap2000.ProgressChanged += Status_ProgressChanged;
            bwGenerateSap2000.DoWork += BwGenerateSap2000_DoWork;
            bwGenerateSap2000.RunWorkerCompleted += BwGenerateSap2000_RunWorkerCompleted;

            IsEnabled = false;
            bwGenerateSap2000.RunWorkerAsync();
        }

        private void BwGenerateSap2000_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsEnabled = true;

            if (e.Error != null)
            {
                // There was an error in the async execution
                MessageBox.Show($"Error in the Sap2000 Generation. {e.Error.Message} | {e.Error.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Saves the SAP2000 File
            string sapFileName = Path.ChangeExtension(TextBoxXMLFileName.Text, "sdb");
            S2KModel.SM.SaveFile(sapFileName);
            S2KModel.SM.CloseApplication(false);
        }

        private void BwGenerateSap2000_DoWork(object sender, DoWorkEventArgs e)
        {
            // The first thing is to remove from the table the rows that have PL as the frame section
            //bwGenerateSap2000.ReportProgress(0, "Cleaning up Tekla Table.");
            //foreach (DataRow row in this.beamTable.Rows)
            //    if (row.Field<string>("Profile").StartsWith("PL")) row.Delete();
            //this.beamTable.AcceptChanges();
            //bwGenerateSap2000.ReportProgress(100, "Tekla Table Cleaned Up.");

            // First of all, manipulates the data so that we can work with it in a better way
            HashSet<string> uniqueMaterials = new HashSet<string>();
            HashSet<(string Section, string Material)> uniqueFrameSections = new HashSet<(string Section, string Material)>();
            HashSet<Point3D> uniquePoints = new HashSet<Point3D>();

            bwGenerateSap2000.ReportProgress(0, "Getting unique Materials, Profiles and Points from the Tekla Table.");
            int counter = 0;
            foreach (DataRow row in beamTable.Rows)
            {
                uniqueMaterials.Add(row.Field<string>("Material"));
                uniqueFrameSections.Add((row.Field<string>("Profile"), row.Field<string>("Material")));

                // Gets the start point
                string startPoint = row.Field<string>("StartPoint");
                startPoint = startPoint.Trim(new char[] { '(', ')' });
                double[] startPointValues = (from a in startPoint.Split(new char[] { ',' })
                                             select Math.Round(double.Parse(a) / 25.4, 3)).ToArray();
                uniquePoints.Add(new Point3D(startPointValues[0], startPointValues[1], startPointValues[2]));

                // Gets the end point
                string endPoint = row.Field<string>("EndPoint");
                endPoint = endPoint.Trim(new char[] { '(', ')' });
                double[] endPointValues = (from a in endPoint.Split(new char[] { ',' })
                                           select Math.Round(double.Parse(a) / 25.4, 3)).ToArray();
                uniquePoints.Add(new Point3D(endPointValues[0], endPointValues[1], endPointValues[2]));

                bwGenerateSap2000.ReportProgress(S2KStaticMethods.ProgressPercent(counter++,beamTable.Rows.Count));
            }
            bwGenerateSap2000.ReportProgress(100, "Got unique Materials, Profiles and Points from the Tekla Table.");

            bwGenerateSap2000.ReportProgress(0, "Opening SAP2000 Software.");
            // Opens a new Sap2000 empty model
            S2KModel.InitSingleton_NewInstance(UnitsEnum.kip_in_F, false );
            bwGenerateSap2000.ReportProgress(0, "SAP2000 Software Open.");

            // Adds the Materials to Sap2000
            bwGenerateSap2000.ReportProgress(counter = 0, "Sending the Material Definitions to SAP2000.");
            Dictionary<string, string> TeklaToSapMatTranslate = new Dictionary<string, string>();
            foreach (string TeklaMatName in uniqueMaterials)
            {
                string SapMatName = null;
                switch (TeklaMatName)
                {
                    case "A500-GR.B-42":
                        SapMatName = S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A500", "Grade B, Fy 42 (HSS Round)");
                        break;
                    case "A500-GR.B-46":
                        SapMatName = S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A500", "Grade B, Fy 46 (HSS Rect.)");
                        break;
                    case "A36":
                        SapMatName = S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A36", "Grade 36");
                        break;
                    case "A992":
                        SapMatName = S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A992", "Grade 50");
                        break;
                    case "NOT_DEFINED":
                        SapMatName = S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, TeklaMatName);
                        break;
                    case "5000":
                        SapMatName = S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, TeklaMatName);
                        break;
                    case "A53-B":
                        SapMatName = S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A53", "Grade B");
                        break;
                    case "F1554-GR.55":
                        SapMatName = S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, TeklaMatName);
                        break;
                    case "A563":
                        SapMatName = S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, TeklaMatName);
                        break;

                    case "A572-Gr 42":
                        SapMatName = S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, TeklaMatName);
                        break;

                    case "57250_CVNB":
                    case "A572-50":
                    case "A572-Gr 50":
                        if (!TeklaToSapMatTranslate.Values.Contains("A572Gr50"))
                        {
                            SapMatName = S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A572", "Grade 50");
                        }
                        else
                        {
                            SapMatName = "A572Gr50";
                        }
                        break;
                    default:
                        SapMatName = S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, TeklaMatName);
                        break;
                }
                TeklaToSapMatTranslate.Add(TeklaMatName, SapMatName);

                bwGenerateSap2000.ReportProgress(S2KStaticMethods.ProgressPercent(counter++,uniqueMaterials.Count));
            }
            bwGenerateSap2000.ReportProgress(counter = 100, "Sent the Material Definitions to SAP2000.");

            // Adds the Profiles to Sap2000
            bwGenerateSap2000.ReportProgress(counter = 0, "Sending the Section Definitions to SAP2000.");
            foreach ((string Section, string Material) in uniqueFrameSections)
            {
                bool ret = false;

                Regex HSSPipeTeklaRegex = new Regex(@"^HSS\d*X\d*\.\d*");
                // Is it a custom I section from Tekla?
                if (Section.StartsWith("HI"))
                {
                    // It is a custom I beam
                    // HI406.4-25.4-25.4*609.6
                    string tempSectionName = Section.Replace("HI", "");
                    string[] chunks = tempSectionName.Split(new char[] { '-', '*' });

                    ret = S2KModel.SM.FrameSecMan.SetOrAddISection(Section + "_" + TeklaToSapMatTranslate[Material], TeklaToSapMatTranslate[Material],
                        double.Parse(chunks[0])/25.4, double.Parse(chunks[3]) / 25.4, double.Parse(chunks[2]) / 25.4, double.Parse(chunks[1]) / 25.4, double.Parse(chunks[3]) / 25.4, double.Parse(chunks[2]) / 25.4);
                }
                else if (HSSPipeTeklaRegex.IsMatch(Section))
                {
                    ret = S2KModel.SM.FrameSecMan.ImportFrameSection(Section + "_" + TeklaToSapMatTranslate[Material],
                            TeklaToSapMatTranslate[Material],
                            "AISC15.pro",
                            Section.Replace("X0.", "X."));
                }
                else 
                {
                    ret = S2KModel.SM.FrameSecMan.ImportFrameSection(Section + "_" + TeklaToSapMatTranslate[Material],
                            TeklaToSapMatTranslate[Material],
                            "AISC15.pro",
                            Section);
                }

                if (!ret)
                {
                    bwGenerateSap2000.ReportProgress(-1, $"Could not Add Section: {Section}_{TeklaToSapMatTranslate[Material]}{Environment.NewLine}");
                }

                bwGenerateSap2000.ReportProgress(S2KStaticMethods.ProgressPercent(counter++ ,uniqueFrameSections.Count));
            }
            bwGenerateSap2000.ReportProgress(counter = 100, "Sent the Section Definitions to SAP2000.");

            // Adds the points and the frames
            bwGenerateSap2000.ReportProgress(counter = 0, "Sending the Frames to SAP2000.");
            Dictionary<Point3D, SapPoint> pointsInModel = new Dictionary<Point3D, SapPoint>();
            foreach (DataRow row in beamTable.Rows)
            {
                // Gets the start point
                string startPointStr = row.Field<string>("StartPoint");
                startPointStr = startPointStr.Trim(new char[] { '(', ')' });
                double[] startPointValues = (from a in startPointStr.Split(new char[] { ',' })
                                             select Math.Round(double.Parse(a) / 25.4, 3)).ToArray();
                Point3D startPoint = new Point3D(startPointValues[0], startPointValues[1], startPointValues[2]);

                SapPoint sapStartPoint = null;
                if (!pointsInModel.ContainsKey(startPoint))
                {
                    sapStartPoint = S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(startPoint);
                    pointsInModel.Add(startPoint, sapStartPoint);
                }
                else
                {
                    sapStartPoint = pointsInModel[startPoint];
                }

                // Gets the end point
                string endPointStr = row.Field<string>("EndPoint");
                endPointStr = endPointStr.Trim(new char[] { '(', ')' });
                double[] endPointValues = (from a in endPointStr.Split(new char[] { ',' })
                                           select Math.Round(double.Parse(a) / 25.4, 3)).ToArray();
                Point3D endPoint = new Point3D(endPointValues[0], endPointValues[1], endPointValues[2]);

                SapPoint sapEndPoint = null;
                if (!pointsInModel.ContainsKey(endPoint))
                {
                    sapEndPoint = S2KModel.SM.PointMan.AddByPoint3D_ReturnSapEntity(endPoint);
                    pointsInModel.Add(endPoint, sapEndPoint);
                }
                else
                {
                    sapEndPoint = pointsInModel[endPoint];
                }

                // Adds the frame
                SapFrame addedFrame = S2KModel.SM.FrameMan.AddByPoint_ReturnSapEntity(sapStartPoint, sapEndPoint,
                    row.Field<string>("Profile") + "_" + TeklaToSapMatTranslate[row.Field<string>("Material")]);
                List<int> failedFrameRows = new List<int>();

                // Keeps track of the failed frames
                if (addedFrame == null)
                {
                    bwGenerateSap2000.ReportProgress(-1, $"Could not Add Frame. Table Row {beamTable.Rows.IndexOf(row)} | {row.Field<string>("Profile")} | Start: {row.Field<string>("StartPoint")} | End: {row.Field<string>("EndPoint")}{Environment.NewLine}");
                }
                else
                {
                    // the frame has been added

                    // Attempts to rotate the frame in accordance to the angle
                    double rotateAngle = GetRotateAngleFromTeklaString(row.Field<string>("Position"));

                    addedFrame.SetLocalAxes(rotateAngle);
                }
                bwGenerateSap2000.ReportProgress(S2KStaticMethods.ProgressPercent(counter++,beamTable.Rows.Count));
            }
            bwGenerateSap2000.ReportProgress(counter = 100, "Sent the Frames to SAP2000.");

            S2KModel.SM.ShowSapAsync();
        }

        private void Status_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace((e.UserState as string)))
            {
                if (e.ProgressPercentage >= 0) StatusTextBlock.Text = (string)e.UserState;
                else LogTextBox.AppendText((string)e.UserState);
            }
            StatusProgressBar.Value = e.ProgressPercentage;
        }

        private double GetRotateAngleFromTeklaString(string TeklaPosition)
        {
            // Breaks the Tekla Position
            string[] parts = TeklaPosition.Split(new string[] { "%%%" }, StringSplitOptions.RemoveEmptyEntries);

            // Standard from the Tekla Plugin Input Output
            string RotationText = parts[4];
            double RotationNumber = Double.Parse(parts[5]);
            switch (RotationText)
            {

                case "FRONT":
                    RotationNumber += 90;
                    break;
                case "TOP":
                    break;
                case "BACK":
                    RotationNumber += 270;
                    break;
                case "BELOW":
                    RotationNumber += 180;
                    break;
                default:
                    break;
            }

            return RotationNumber;
        }
    }
}
