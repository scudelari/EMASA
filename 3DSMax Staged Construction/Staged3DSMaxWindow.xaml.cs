using Autodesk.Max;
using S2KHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MathNet.Spatial.Euclidean;
using MoreLinq;
using System.IO;
using EmasaSapTools;
using System.Data;
using ExcelDataReader;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace _3DSMax_Staged_Construction
{
    /// <summary>
    /// Interaction logic for Staged3DSMaxWindow.xaml
    /// </summary>
    public partial class Staged3DSMaxWindow : Window
    {
        public S2KModel sModel = null;

        public StatusBarBindings bStatusBarBindings;
        public StagedConstructionBindings bStagedConstructionBindings;
        private void CreateAndBind()
        {
            // Creates the Data Binders
            this.bStatusBarBindings = new StatusBarBindings(this.WindowStatusBar);
            this.WindowStatusBar.DataContext = this.bStatusBarBindings;

            this.bStagedConstructionBindings = new StagedConstructionBindings(this.StagedConstructionGroupBox);
            this.StagedConstructionGroupBox.DataContext = this.bStagedConstructionBindings;
        }

        private string _residualMessage = null;
        public string ResidualMessage
        {
            get { return _residualMessage; }
            set { _residualMessage = value; }
        }

        public Staged3DSMaxWindow()
        {
            InitializeComponent();
            this.Icon = S2KHelper.Properties.Resources.EMS_32.GetImageSource();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                CreateAndBind();

                // Tries to attach to the open Sap2000 Instance otherwise close
                this.sModel = new S2KModel(null, true);
                this.bStatusBarBindings.ModelName = this.sModel.FileName;
            }
            catch (Exception ex)
            {
                this.ResidualMessage = $"Could not find running Sap2000 Instance.{Environment.NewLine}Please open one, and only one, Sap2000 instance and open the model you want to manipulate.";

                ExceptionViewer ev = new ExceptionViewer(ResidualMessage, ex);
                ev.ShowDialog();

                // Closes the current form to give back control to 3DSMax
                this.Close();
            }
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            // Updates the model name
            this.bStatusBarBindings.ModelName = this.sModel.FileName;

            // Are we in the Staged Construction Tab?
            if (!string.IsNullOrWhiteSpace(this.bStagedConstructionBindings.ExcelFileName))
            {
                // Should we read it again?
                FileInfo fileInfo = new FileInfo(this.bStagedConstructionBindings.ExcelFileName);

                try
                {
                    if (fileInfo.LastWriteTime != this.bStagedConstructionBindings.ExcelFileNameModified)
                        this.StagedConstructionReadExcelFile(this.bStagedConstructionBindings.ExcelFileName);
                }
                catch (Exception)
                {
                    // Marks the current file as nothing
                    this.bStagedConstructionBindings.Set_StagedSelectExcelTextBlock(StagedSelectExcelTextBlock_Alternatives.ErrorInFile);
                }
            }
        }
        private void EnableWindow()
        {
            this.IsEnabled = true;
        }
        private void DisableWindow()
        {
            this.IsEnabled = false;
        }

        private async void MatchSap2000ElementsButton_Click(object sender, RoutedEventArgs e)
        {
            this.DisableWindow();
            
            try
            {
                Action<IProgress<ProgressData>> work = (progReporter) =>
                {
                    progReporter.Report(ProgressData.SetMessage("Getting all Nodes from 3DSMax.", true));
                    List<IINode> allNodes = MaxHelper.MaxInterface.GetAllNodesOfTheScene();

                    progReporter.Report(ProgressData.SetMessage("Getting all Frames From SAP2000."));
                    List<SapFrame> allFrames = this.sModel.FrameMan.GetAll(progReporter);

                    progReporter.Report(ProgressData.SetMessage($"Getting group definitions [[for frame: ***]]"));
                    for (int i = 0; i < allFrames.Count; i++)
                    {
                        SapFrame frame = allFrames[i];

                        // Touches to acquire
                        bool a = frame.Groups != null;

                        progReporter.Report(ProgressData.UpdateProgress(i, allFrames.Count, frame.Name));
                    }

                    progReporter.Report(ProgressData.SetMessage("Creating work copy of Frame List."));
                    List<SapFrame> allFramesConsuming = new List<SapFrame>(allFrames);

                    progReporter.Report(ProgressData.SetMessage("Matching 3DSMax Nodes to SAP2000 Elements. [[Current Node: ***]]"));
                    for (int i = 0; i < allNodes.Count; i++)
                    {
                        IINode node = allNodes[i];
                        if (node.IsRootNode) continue;

                        progReporter.Report(ProgressData.UpdateProgress(i, allNodes.Count, node.Name));

                        (double volume, Point3D center)? nodeVolumeAndCentroid = node.GetVolumeAndMassCenter();

                        if (nodeVolumeAndCentroid.HasValue)
                        {
                            // Gets closest frame
                            SapFrame closest = allFramesConsuming.MinBy(a => a.Centroid.DistanceTo(nodeVolumeAndCentroid.Value.center)).First();

                            double distance = closest.Centroid.DistanceTo(nodeVolumeAndCentroid.Value.center);

                            // Renames the 3DSMax Node
                            node.Name = $"Frame: {closest.Name}";
                        }
                    }
                };

                // Runs the job async
                Task task = new Task(() => work(this.bStatusBarBindings.ProgressReporter));
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                S2KStaticMethods.ShowWarningMessageBox($"Could not match the 3DSMax elements to the SAP2000 elements.{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                this.bStatusBarBindings.IProgressReporter.Report(ProgressData.Reset());
                this.EnableWindow();
            }
        }

        private void StagedSelectExcelButton_Click(object sender, RoutedEventArgs e)
        {
            // Selects the Excel file in the view thread
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel file (*.xls;*.xlsx)|*.xls;*.xlsx";
            ofd.DefaultExt = "*.xls;*.xlsx";
            ofd.Title = "Select the Excel File With The Correct Format!";
            ofd.Multiselect = false;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            bool? ofdret = ofd.ShowDialog();

            if (ofdret.HasValue && ofdret.Value && string.IsNullOrWhiteSpace(ofd.FileName))
            {
                S2KStaticMethods.ShowWarningMessageBox($"Please select a proper Excel file!{Environment.NewLine}");
                this.bStagedConstructionBindings.Set_StagedSelectExcelTextBlock(StagedSelectExcelTextBlock_Alternatives.Unloaded);
                return; // Aborts the Open File
            }

            this.StagedConstructionReadExcelFile(ofd.FileName);
        }
        private async void StagedConstructionReadExcelFile(string excelFileName)
        {
            this.DisableWindow();

            try
            {
                this.bStagedConstructionBindings.ExcelFileName = excelFileName;

                // Tries to read the data from excel async

                Func<IProgress<ProgressData>, (List<SCStepsDataGridType> Steps, List<SCLoadCaseDataGridType> Cases)> work = (progReporter) =>
                {
                    progReporter.Report(ProgressData.SetMessage("Reading the Staged Construction Excel."));
                    DataSet fromExcel = default;

                    // Reads the Excel
                    using (var stream = File.Open(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Auto-detect format, supports:
                        //  - Binary Excel files (2.0-2003 format; *.xls)
                        //  - OpenXml Excel files (2007 format; *.xlsx)
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            // 2. Use the AsDataSet extension method
                            fromExcel = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                                // property in a second pass.
                                UseColumnDataType = true,

                                // Gets or sets a callback to determine whether to include the current sheet
                                // in the DataSet. Called once per sheet before ConfigureDataTable.
                                FilterSheet = (tableReader, sheetIndex) => true,

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
                                    FilterRow = (rowReader) =>
                                    {
                                        return true;
                                    },

                                    // Gets or sets a callback to determine whether to include the specific
                                    // column in the DataTable. Called once per column after reading the 
                                    // headers.
                                    FilterColumn = (rowReader, columnIndex) =>
                                    {
                                        return true;
                                    }
                                }
                            });
                        }
                    }

                    // Reads the steps table and puts it in Excel!
                    DataTable stepsTable = fromExcel.Tables["Steps"];
                    List<SCStepsDataGridType> steps = new List<SCStepsDataGridType>();
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
                                throw new InvalidOperationException($"There is an error in the Excel. Steps column. {operationText} does not designate an implemented operation!");
                        }

                        steps.Add(new SCStepsDataGridType
                        {
                            GroupName = row.Field<string>("GroupName"),
                            NamedProp = row.Field<string>("NamedProp"),
                            Operation = op,
                            Order = (int)row.Field<double>("Order"),
                        });
                    }

                    DataTable casesTable = fromExcel.Tables["LoadCases"];
                    List<SCLoadCaseDataGridType> cases = new List<SCLoadCaseDataGridType>();
                    foreach (DataRow row in casesTable.Rows)
                    {
                        cases.Add(new SCLoadCaseDataGridType
                        {
                            Name = row.Field<string>("Name"),
                            DeadMult = row["DEAD"] as double? ?? 0d,
                            LiveMult = row["LIVE"] as double? ?? 0d,
                            WindMult = row["WIND"] as double? ?? 0d,
                            NotionalMult = row["NOTIONAL"] as double? ?? 0d,
                            TemperatureMult = row["TEMP"] as double? ?? 0d,
                            StrainMult = row["STRAIN"] as double? ?? 0d,
                            BaseName = row.Field<string>("BaseName"),
                            Active = row.Field<bool>("Active"),
                            Others = row.Field<string>("OTHERS")
                        });
                    }

                    return (steps, cases);
                };

                // Runs the job async
                Task<(List<SCStepsDataGridType> Steps, List<SCLoadCaseDataGridType> Cases)> task = new Task<(List<SCStepsDataGridType> Steps, List<SCLoadCaseDataGridType> Cases)>(() => work(this.bStatusBarBindings.ProgressReporter));
                task.Start();
                await task;

                (List<SCStepsDataGridType> Steps, List<SCLoadCaseDataGridType> Cases) excelLists = task.Result;

                // Puts them in the DataGridView
                this.bStagedConstructionBindings.SCStepsDataGridItems = new ObservableCollection<SCStepsDataGridType>(excelLists.Steps);
                this.bStagedConstructionBindings.SCCasesDataGridItems = new ObservableCollection<SCLoadCaseDataGridType>(excelLists.Cases);

                // Success!
                this.bStagedConstructionBindings.Set_StagedSelectExcelTextBlock(StagedSelectExcelTextBlock_Alternatives.Loaded);
            }
            catch (Exception ex)
            {
                this.bStagedConstructionBindings.Set_StagedSelectExcelTextBlock(StagedSelectExcelTextBlock_Alternatives.ErrorInFile);
            }
            finally
            {
                this.bStatusBarBindings.IProgressReporter.Report(ProgressData.Reset());
                this.EnableWindow();
            }
        }


    }
}
