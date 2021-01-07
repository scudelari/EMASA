using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using EmasaSapTools.DataGridTypes;
using EmasaSapTools.Resources;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Microsoft.Win32;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class StagedConstructionBindings : BindableSingleton<StagedConstructionBindings>
    {
        private StagedConstructionBindings()
        {
        }

        public override void SetOrReset()
        {
            //Set_StagedSelectExcelTextBlock(StagedSelectExcelTextBlock_Alternatives.Unloaded);

            // Sets the Views for the Grids
            SCCasesDataGrid_ViewItems = CollectionViewSource.GetDefaultView(SCCasesDataGrid_RawItems);
            SCStepsDataGrid_ViewItems = CollectionViewSource.GetDefaultView(SCStepsDataGrid_RawItems);
            //StifferComboBox_ViewItems = CollectionViewSource.GetDefaultView(StifferComboBox_RawItems);

            DeadPatternName = "DEAD";
            LivePatternName = "LIVE";
            TempPatternName = "TEMP_#";
            StrainPatternName = "STRAIN";
            WindPatternName = "W_#";
            NotionalPatternName = "N_#";

            DeadOnlyIsChecked = true;
            DeadAndOthersIsChecked = false;

            WindIsChecked = true;
            At45IsChecked = false;
            At90IsChecked = true;

            LoadRemoveStagesIsChecked = false;

            NotionalIsChecked = false;

            TemperatureIsChecked = false;
            TempWithLateralIsChecked = false;
            TempWithBaseDeadIsChecked = true;

            ReducedWindIsChecked = false;
            ReducedWind_Threshold = 1;
            ReducedWind_Threshold_IsEnabled = false;

            // Sets the CollectionViewSource on the gridview
            //if (SCStepsDataGrid != null) SCStepsDataGrid.DataContext = SCStepsDataGrid_CVS;
            //if (SCCasesDataGrid != null) SCCasesDataGrid.DataContext = SCCasesDataGrid_CVS;

            StagedCreateCasesMakeTheirDesignCombos = true;
            StagedCreateCasesMakeWithEventToEventStepping = true;

            SelectStepGroupText = "1";

            GhostOptions_GroupName = "*** GHOST ***";
            GhostOptions_AxialReductionValue = 1e-5;
            GhostOptions_OthersReductionValue = 1e-4;
            GhostOptions_MakeGroupStiffer = true;
            GhostOptions_ReductionValue_GroupMult = 10d;
            GhostOptions_AddCablesAsFrames = false;
        }

        private TextBlock StagedSelectExcelTextBlock
        {
            get
            {
                try
                {
                    //return (TextBlock) BoundTo.FindName("StagedSelectExcelTextBlock");
                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void Set_StagedSelectExcelTextBlock(Run inStatus = null)
        {
            // Clears the text
            if (inStatus == null)
            {
                StagedSelectExcelTextBlock.Text = string.Empty;
                return;
            }

            StagedSelectExcelTextBlock.Inlines.Add(inStatus);
        }

        private void Set_StagedSelectExcelTextBlock(string inMessage, Brush inBrush,
            TextDecorationCollection inDec = null)
        {
            Run tmpRun = new Run(inMessage)
                {
                Foreground = inBrush
                };

            if (inDec != null) tmpRun.TextDecorations.Add(inDec);

            Set_StagedSelectExcelTextBlock(tmpRun);
        }

        public void Set_StagedSelectExcelTextBlock(StagedSelectExcelTextBlock_Alternatives inOption)
        {
            switch (inOption)
            {
                case StagedSelectExcelTextBlock_Alternatives.Loaded:
                    Set_StagedSelectExcelTextBlock(null); // Clears the text
                    Set_StagedSelectExcelTextBlock("Loaded!", new SolidColorBrush(Colors.Green));
                    ExcelFile_IsOk = true;
                    break;

                case StagedSelectExcelTextBlock_Alternatives.ErrorInFile:
                    Set_StagedSelectExcelTextBlock(null); // Clears the text
                    Set_StagedSelectExcelTextBlock("Error in Excel File!", new SolidColorBrush(Colors.Red),
                        TextDecorations.Underline);
                    ExcelFile_IsOk = false;
                    break;

                case StagedSelectExcelTextBlock_Alternatives.Unloaded:
                default:
                    Set_StagedSelectExcelTextBlock(null); // Clears the text
                    Set_StagedSelectExcelTextBlock("Unloaded", new SolidColorBrush(Colors.Red),
                        TextDecorations.Underline);
                    ExcelFileName = null;
                    ExcelFile_IsOk = false;
                    break;
            }
        }

        private bool _ExcelFile_IsOk;

        public bool ExcelFile_IsOk
        {
            get => _ExcelFile_IsOk;
            set => SetProperty(ref _ExcelFile_IsOk, value);
        }

        private TriState _excelFileStatus;

        public TriState ExcelFileStatus
        {
            get => _excelFileStatus;
            set => SetProperty(ref _excelFileStatus, value);
        }

        private string _ExcelFileName;

        public string ExcelFileName
        {
            get => _ExcelFileName;
            set
            {
                try
                {
                    if (value != null)
                    {
                        FileInfo fileInfo = new FileInfo(value);
                        ExcelFileNameModified = fileInfo.LastWriteTime;
                    }
                    else
                    {
                        ExcelFileNameModified = default;
                    }
                }
                catch (Exception)
                {
                    throw new InvalidOperationException(
                        "Could not get the modified date of the Excel file. Does it still exist?");
                }

                SetProperty(ref _ExcelFileName, value);
            }
        }

        public DateTime ExcelFileNameModified { get; private set; }

        private string _DeadPatternName;

        public string DeadPatternName
        {
            get => _DeadPatternName;
            set => SetProperty(ref _DeadPatternName, value);
        }

        private string _LivePatternName;

        public string LivePatternName
        {
            get => _LivePatternName;
            set => SetProperty(ref _LivePatternName, value);
        }

        private string _WindPatternName;

        public string WindPatternName
        {
            get => _WindPatternName;
            set => SetProperty(ref _WindPatternName, value);
        }

        private string _TempPatternName;

        public string TempPatternName
        {
            get => _TempPatternName;
            set => SetProperty(ref _TempPatternName, value);
        }

        private string _StrainPatternName;

        public string StrainPatternName
        {
            get => _StrainPatternName;
            set => SetProperty(ref _StrainPatternName, value);
        }

        private string _NotionalPatternName;

        public string NotionalPatternName
        {
            get => _NotionalPatternName;
            set => SetProperty(ref _NotionalPatternName, value);
        }

        private bool _DeadOnlyIsChecked;

        public bool DeadOnlyIsChecked
        {
            get => _DeadOnlyIsChecked;
            set
            {
                SetProperty(ref _DeadOnlyIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _DeadAndOthersIsChecked;

        public bool DeadAndOthersIsChecked
        {
            get => _DeadAndOthersIsChecked;
            set
            {
                SetProperty(ref _DeadAndOthersIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _WindIsChecked;

        public bool WindIsChecked
        {
            get => _WindIsChecked;
            set
            {
                SetProperty(ref _WindIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _At45IsChecked;

        public bool At45IsChecked
        {
            get => _At45IsChecked;
            set
            {
                SetProperty(ref _At45IsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _At90IsChecked;

        public bool At90IsChecked
        {
            get => _At90IsChecked;
            set
            {
                SetProperty(ref _At90IsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        public int AngleStep
        {
            get
            {
                if (At45IsChecked) return 45;
                else if (At90IsChecked) return 90;
                else throw new InvalidOperationException("Could not define the step angle for lateral loads.");
            }
        }

        private bool _NotionalIsChecked;

        public bool NotionalIsChecked
        {
            get => _NotionalIsChecked;
            set
            {
                SetProperty(ref _NotionalIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _TemperatureIsChecked;

        public bool TemperatureIsChecked
        {
            get => _TemperatureIsChecked;
            set
            {
                SetProperty(ref _TemperatureIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _TempWithLateralIsChecked;

        public bool TempWithLateralIsChecked
        {
            get => _TempWithLateralIsChecked;
            set
            {
                SetProperty(ref _TempWithLateralIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _TempWithBaseDeadIsChecked;

        public bool TempWithBaseDeadIsChecked
        {
            get => _TempWithBaseDeadIsChecked;
            set
            {
                SetProperty(ref _TempWithBaseDeadIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private void Update_InternalIsEnabled()
        {
            if (DeadOnlyIsChecked)
            {
                WindIsEnabled = false;
                NotionalIsEnabled = false;
                TemperatureIsEnabled = false;

                TempWithLateralIsEnabled = false;
                At45IsEnabled = false;
                At90IsEnabled = false;
                TempWithBaseDeadIsEnabled = false;

                ReducedWind_Threshold_IsEnabled = false;
            }
            else
            {
                WindIsEnabled = true;
                NotionalIsEnabled = true;
                TemperatureIsEnabled = true;

                At45IsEnabled = NotionalIsChecked || WindIsChecked;
                At90IsEnabled = NotionalIsChecked || WindIsChecked;

                TempWithLateralIsEnabled = TemperatureIsChecked && (NotionalIsChecked || WindIsChecked);
                TempWithBaseDeadIsEnabled = TemperatureIsChecked;

                ReducedWind_Threshold_IsEnabled = ReducedWindIsChecked;

                LoadRemoveStagesIsEnabled = NotionalIsChecked || WindIsChecked || TemperatureIsEnabled;
            }
        }

        private bool _WindIsEnabled;

        public bool WindIsEnabled
        {
            get => _WindIsEnabled;
            set => SetProperty(ref _WindIsEnabled, value);
        }

        private bool _NotionalIsEnabled;

        public bool NotionalIsEnabled
        {
            get => _NotionalIsEnabled;
            set => SetProperty(ref _NotionalIsEnabled, value);
        }

        private bool _TemperatureIsEnabled;

        public bool TemperatureIsEnabled
        {
            get => _TemperatureIsEnabled;
            set => SetProperty(ref _TemperatureIsEnabled, value);
        }

        private bool _At45IsEnabled;

        public bool At45IsEnabled
        {
            get => _At45IsEnabled;
            set => SetProperty(ref _At45IsEnabled, value);
        }

        private bool _At90IsEnabled;

        public bool At90IsEnabled
        {
            get => _At90IsEnabled;
            set => SetProperty(ref _At90IsEnabled, value);
        }

        private bool _TempWithLateralIsEnabled;

        public bool TempWithLateralIsEnabled
        {
            get => _TempWithLateralIsEnabled;
            set => SetProperty(ref _TempWithLateralIsEnabled, value);
        }

        private bool _TempWithBaseDeadIsEnabled;

        public bool TempWithBaseDeadIsEnabled
        {
            get => _TempWithBaseDeadIsEnabled;
            set => SetProperty(ref _TempWithBaseDeadIsEnabled, value);
        }

        private bool _LoadRemoveStagesIsChecked;

        public bool LoadRemoveStagesIsChecked
        {
            get => _LoadRemoveStagesIsChecked;
            set => SetProperty(ref _LoadRemoveStagesIsChecked, value);
        }

        private bool _LoadRemoveStagesIsEnabled;

        public bool LoadRemoveStagesIsEnabled
        {
            get => _LoadRemoveStagesIsEnabled;
            set => SetProperty(ref _LoadRemoveStagesIsEnabled, value);
        }

        private bool _ReducedWindIsChecked;

        public bool ReducedWindIsChecked
        {
            get => _ReducedWindIsChecked;
            set
            {
                SetProperty(ref _ReducedWindIsChecked, value);
                Update_InternalIsEnabled();
            }
        }

        private bool _ReducedWind_Threshold_IsEnabled;

        public bool ReducedWind_Threshold_IsEnabled
        {
            get => _ReducedWind_Threshold_IsEnabled;
            set => SetProperty(ref _ReducedWind_Threshold_IsEnabled, value);
        }

        private int _ReducedWind_Threshold;

        public int ReducedWind_Threshold
        {
            get => _ReducedWind_Threshold;
            set => SetProperty(ref _ReducedWind_Threshold, value);
        }

        private string _GhostOptions_GroupName;

        public string GhostOptions_GroupName
        {
            get => _GhostOptions_GroupName;
            set => SetProperty(ref _GhostOptions_GroupName, value);
        }

        private double _GhostOptions_AxialReductionValue;

        public double GhostOptions_AxialReductionValue
        {
            get => _GhostOptions_AxialReductionValue;
            set => SetProperty(ref _GhostOptions_AxialReductionValue, value);
        }

        private double _GhostOptions_OthersReductionValue;

        public double GhostOptions_OthersReductionValue
        {
            get => _GhostOptions_OthersReductionValue;
            set => SetProperty(ref _GhostOptions_OthersReductionValue, value);
        }

        private bool _GhostOptions_MakeGroupStiffer;

        public bool GhostOptions_MakeGroupStiffer
        {
            get => _GhostOptions_MakeGroupStiffer;
            set => SetProperty(ref _GhostOptions_MakeGroupStiffer, value);
        }

        private double _GhostOptions_ReductionValue_GroupMult;

        public double GhostOptions_ReductionValue_GroupMult
        {
            get => _GhostOptions_ReductionValue_GroupMult;
            set => SetProperty(ref _GhostOptions_ReductionValue_GroupMult, value);
        }

        private bool _GhostOptions_AddCablesAsFrames;

        public bool GhostOptions_AddCablesAsFrames
        {
            get => _GhostOptions_AddCablesAsFrames;
            set => SetProperty(ref _GhostOptions_AddCablesAsFrames, value);
        }

        #region DataGrids

        private CollectionViewSource SCStepsDataGrid_CVS = new CollectionViewSource();

        private ObservableCollection<SCStepsDataGridType> SCStepsDataGrid_OC;
        public ObservableCollection<SCStepsDataGridType> SCStepsDataGridItems
        {
            get => SCStepsDataGrid_OC;
            set
            {
                // Creates a new observable collection
                SCStepsDataGrid_OC = value ?? new ObservableCollection<SCStepsDataGridType>();

                SCStepsDataGrid_CVS.Source = SCStepsDataGrid_OC;

                SCStepsDataGrid_CVS.SortDescriptions.Clear();
                SortDescription sortingOrder = new SortDescription("Order", ListSortDirection.Ascending);
                SCStepsDataGrid_CVS.SortDescriptions.Add(sortingOrder);
                SortDescription sortingGroup = new SortDescription("GroupName", ListSortDirection.Ascending);
                SCStepsDataGrid_CVS.SortDescriptions.Add(sortingGroup);
            }
        }

        private CollectionViewSource SCCasesDataGrid_CVS = new CollectionViewSource();

        private ObservableCollection<ScLoadCaseDataGridType> SCCasesDataGrid_OC;

        public ObservableCollection<ScLoadCaseDataGridType> SCCasesDataGridItems
        {
            get => SCCasesDataGrid_OC;
            set
            {
                // Creates a new observable collection
                SCCasesDataGrid_OC = value ?? new ObservableCollection<ScLoadCaseDataGridType>();

                SCCasesDataGrid_CVS.Source = SCCasesDataGrid_OC;

                SCCasesDataGrid_CVS.SortDescriptions.Clear();
                SortDescription sorting = new SortDescription("ShortName", ListSortDirection.Ascending);
                SCCasesDataGrid_CVS.SortDescriptions.Add(sorting);
            }
        }

        #endregion

        private bool _StagedCreateCasesMakeTheirDesignCombos;

        public bool StagedCreateCasesMakeTheirDesignCombos
        {
            get => _StagedCreateCasesMakeTheirDesignCombos;
            set => SetProperty(ref _StagedCreateCasesMakeTheirDesignCombos, value);
        }

        private bool _StagedCreateCasesMakeWithEventToEventStepping;

        public bool StagedCreateCasesMakeWithEventToEventStepping
        {
            get => _StagedCreateCasesMakeWithEventToEventStepping;
            set => SetProperty(ref _StagedCreateCasesMakeWithEventToEventStepping, value);
        }

        private string _SelectStepGroupText;

        public string SelectStepGroupText
        {
            get => _SelectStepGroupText;
            set => SetProperty(ref _SelectStepGroupText, value);
        }

        #region New Items

        public ICollectionView SCStepsDataGrid_ViewItems { get; private set; }
        public FastObservableCollection<SCStepsDataGridType> SCStepsDataGrid_RawItems { get; } = new FastObservableCollection<SCStepsDataGridType>();

        public ICollectionView SCCasesDataGrid_ViewItems { get; private set; }
        public FastObservableCollection<ScLoadCaseDataGridType> SCCasesDataGrid_RawItems { get; } = new FastObservableCollection<ScLoadCaseDataGridType>();

        private ICollectionView _stifferComboBox_ViewItems;
        public ICollectionView StifferComboBox_ViewItems
        {
            get => _stifferComboBox_ViewItems;
            set
            {
                SetProperty(ref _stifferComboBox_ViewItems, value);
                _stifferComboBox_ViewItems.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
            }
        }
        private string _stifferComboBox_SelectedItem;
        public string StifferComboBox_SelectedItem
        {
            get => _stifferComboBox_SelectedItem;
            set => SetProperty(ref _stifferComboBox_SelectedItem, value);
        }

        #endregion

        #region Commands

        private DelegateCommand _selectExcelFileButtonCommand;
        public DelegateCommand SelectExcelFileButtonCommand =>
            _selectExcelFileButtonCommand ?? (_selectExcelFileButtonCommand = new DelegateCommand(ExecuteSelectExcelFileButtonCommand));
        public async void ExecuteSelectExcelFileButtonCommand()
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
                ExcelFileName = String.Empty;
                ExcelFileStatus = TriState.NotSet;
                return; // Aborts the Open File
            }

            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Clears both lists
                    SCCasesDataGrid_RawItems.Clear();
                    SCStepsDataGrid_RawItems.Clear();

                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Reading Staged Construction Excel File";

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
                            Order = (int) row.Field<double>("Order")
                            });
                    }

                    DataTable casesTable = fromExcel.Tables["LoadCasesAndCombos"];
                    List<ScLoadCaseDataGridType> cases = new List<ScLoadCaseDataGridType>();
                    BusyOverlayBindings.I.SetIndeterminate("Reading Load Case Table.");
                    foreach (DataRow row in casesTable.Rows)
                        cases.Add(new ScLoadCaseDataGridType
                            {
                            Name = row.Field<string>("ShortName"),
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

                    SCStepsDataGrid_RawItems.AddItems(steps);
                    SCCasesDataGrid_RawItems.AddItems(cases);

                    ExcelFileName = ofd.FileName;
                    ExcelFileStatus = TriState.True;
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExcelFileName = ofd.FileName;
                ExcelFileStatus = TriState.False;
                ExceptionViewer.Show(ex);
            }
            finally
            {
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) OnMessage("Excel File", endMessages.ToString());
            }
        }

        private DelegateCommand _generateLoadCasesButtonCommand;
        public DelegateCommand GenerateLoadCasesButtonCommand =>
            _generateLoadCasesButtonCommand ?? (_generateLoadCasesButtonCommand = new DelegateCommand(ExecuteGenerateLoadCasesButtonCommand));
        public async void ExecuteGenerateLoadCasesButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Generating Staged Construction Load Cases";

                    // Gets the list of the steps - gets here because it will be used in loops
                    List<SCStepsDataGridType> stepList = (from a in SCStepsDataGrid_RawItems
                        orderby a.Order ascending
                        select a).ToList();

                    #region Creating the Base Deads

                    BusyOverlayBindings.I.SetIndeterminate("Generating the Base Dead Cases.");

                    List<ScLoadCaseDataGridType> baseDeadOnlyList = (from a in SCCasesDataGrid_RawItems
                        where string.IsNullOrWhiteSpace(a.BaseName) &&
                              a.Active &&
                              a.DeadMult != 0d
                        select a).ToList();

                    // This is the total number of base dead iterations
                    int max_step = stepList.Max(a => a.Order) * baseDeadOnlyList.Count;

                    // This list will help us to work on the lateral loads afterwards
                    var baseDeadCasesWithStagesToLoadLaterally = new Dictionary<string, List<(int stage, string name)>>();

                    // Iterates on the list of Base Deads
                    for (int i_baseDead = 0; i_baseDead < baseDeadOnlyList.Count; i_baseDead++)
                    {
                        ScLoadCaseDataGridType bdLoadCaseExcelDef = baseDeadOnlyList[i_baseDead];

                        // The list Of created dead LCs that were created for this base dead - used to add other loads on top of this.
                        List<(int stage, string name)> deadStageListToLoadLater = new List<(int stage, string name)>();

                        // Iterating on the list of STEPS and grouping all their actions
                        string currentStageName = null;
                        string previousStageName = null;

                        int i_step = 1;
                        BusyOverlayBindings.I.SetDeterminate($"Generating Base Dead: {bdLoadCaseExcelDef.Name}", "Stage");
                        while (true)
                        {
                            // Gets the actions in this step
                            var actionsInStepExcelDef = (from a in stepList where a.Order == i_step select a).ToList();
                            // Ended
                            if (actionsInStepExcelDef.Count == 0) break;

                            previousStageName = currentStageName;
                            currentStageName = $"{i_step:000}_{bdLoadCaseExcelDef.Name}";
                            BusyOverlayBindings.I.UpdateProgress((i_step + 1) * (i_baseDead + 1), max_step, currentStageName);

                            // Creates the list of actions that must be inserted in this stage
                            List<LoadCaseNLStagedStageData> sapActionsInStep = new List<LoadCaseNLStagedStageData>();
                            bool shouldLoadLater = true; // Will be made false if there is a remove and the checkbox is selected
                            foreach (SCStepsDataGridType item in actionsInStepExcelDef)
                            {
                                #region Large Switch that adds the actions depending on the excel steps table

                                switch (item.Operation)
                                {
                                    case StagedConstructionOperation.AddStructure:
                                        // The action to ADD the group to the model
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.AddStructure)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName
                                                });

                                        // The action to load with DEAD the just added group
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.LoadObjects)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Load",
                                                MyName = DeadPatternName,
                                                ScaleFactor = bdLoadCaseExcelDef.DeadMult
                                                });

                                        // The action to load with LIVE the just added group
                                        if (bdLoadCaseExcelDef.LiveMult != 0d)
                                            sapActionsInStep.Add(
                                                new LoadCaseNLStagedStageData(StagedConstructionOperation.LoadObjects)
                                                    {
                                                    ObjectType = "Group",
                                                    ObjectName = item.GroupName,
                                                    MyType = "Load",
                                                    MyName = LivePatternName,
                                                    ScaleFactor = bdLoadCaseExcelDef.LiveMult
                                                    });

                                        // The action to load with STRAIN the just added group
                                        if (bdLoadCaseExcelDef.StrainMult != 0d)
                                            sapActionsInStep.Add(
                                                new LoadCaseNLStagedStageData(StagedConstructionOperation.LoadObjects)
                                                    {
                                                    ObjectType = "Group",
                                                    ObjectName = item.GroupName,
                                                    MyType = "Load",
                                                    MyName = StrainPatternName,
                                                    ScaleFactor = bdLoadCaseExcelDef.StrainMult
                                                    });

                                        break;

                                    case StagedConstructionOperation.RemoveStructure:
                                        // The action to REMOVE the group from the model
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.RemoveStructure)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName
                                                });

                                        // If the user does not want remove cases to be loaded, they will not be
                                        shouldLoadLater = LoadRemoveStagesIsChecked;
                                        break;

                                    case StagedConstructionOperation.AddGuideStructure:
                                        // The action to ADD Guide Structure to the model
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.AddGuideStructure)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName
                                                });

                                        break;

                                    // These two work by adding a possible other LOAD
                                    case StagedConstructionOperation.LoadObjectsIfNew:
                                        // Finds the other load multiplier
                                        (string CaseName, double Mult) = bdLoadCaseExcelDef.OtherCaseList.First(a => a.CaseName == item.NamedProp);

                                        // Adds action to the OTHER load
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.LoadObjectsIfNew)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Load",
                                                MyName = CaseName,
                                                ScaleFactor = Mult
                                                });
                                        break;

                                    case StagedConstructionOperation.LoadObjects:
                                        // Finds the other load multiplier
                                        (string CaseName, double Mult) otherLoadDef_b = bdLoadCaseExcelDef.OtherCaseList.First(a => a.CaseName == item.NamedProp);

                                        // Adds action to the OTHER load
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.LoadObjects)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Load",
                                                MyName = otherLoadDef_b.CaseName,
                                                ScaleFactor = otherLoadDef_b.Mult
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeSectionPropertyModifiers_Area:
                                        // Adds action to change the section property modifiers
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation
                                                .ChangeSectionPropertyModifiers_Area)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Area",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeSectionPropertyModifiers_Frame:
                                        // Adds action to change the section property modifiers
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation
                                                .ChangeSectionPropertyModifiers_Frame)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Frame",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeReleases:
                                        // Adds action to change the releases
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation.ChangeReleases)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Frame",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeSectionPropertiesAndAge:
                                        throw new NotImplementedException("Changing Age is Still not Supported!");

                                    case StagedConstructionOperation.ChangeSectionProperties_Area:
                                        // Adds action to change the section properties
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation
                                                .ChangeSectionProperties_Area)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Area",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeSectionProperties_Frame:
                                        // Adds action to change the section properties
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation
                                                .ChangeSectionProperties_Frame)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Frame",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeSectionProperties_Cable:
                                        // Adds action to change the section properties
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation
                                                .ChangeSectionProperties_Cable)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Cable",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    case StagedConstructionOperation.ChangeSectionProperties_Link:
                                        // Adds action to change the section properties
                                        sapActionsInStep.Add(
                                            new LoadCaseNLStagedStageData(StagedConstructionOperation
                                                .ChangeSectionProperties_Link)
                                                {
                                                ObjectType = "Group",
                                                ObjectName = item.GroupName,
                                                MyType = "Link",
                                                MyName = item.NamedProp
                                                });
                                        break;

                                    default:
                                        break;
                                }

                                #endregion
                            }

                            // Creates the NL *Staged* case
                            if (!S2KModel.SM.LCMan.AddNew_LCStagedNonLinear(currentStageName, sapActionsInStep,
                                StagedCreateCasesMakeWithEventToEventStepping,
                                previousStageName))
                                throw new S2KHelperException($"Could not add Staged Non-Linear case named {currentStageName}. Aborting the whole operation!");

                            // Should we also add a Combination?
                            if (StagedCreateCasesMakeTheirDesignCombos)
                                S2KModel.SM.CombMan.AddComb($"C_{currentStageName}", new List<(string CaseName, int ScaleFactor)>() {(currentStageName, 1)});

                            // Only adds to the list if the conditions are met
                            // -> Adds all stages except:
                            // -> Is the user choses not to give additional loads the stages with remove operations, they will not be added to this list. 
                            if (shouldLoadLater) deadStageListToLoadLater.Add((i_step, currentStageName));

                            // Iterates 
                            i_step++;
                        }

                        baseDeadCasesWithStagesToLoadLaterally.Add(bdLoadCaseExcelDef.Name, deadStageListToLoadLater);
                    }

                    #endregion

                    #region We have other loads

                    if (DeadAndOthersIsChecked)
                    {
                        // This list will help us to work on the temperature loads afterwards
                        Dictionary<string, List<(int stage, string name)>> lateralCreatedStages = new Dictionary<string, List<(int stage, string name)>>();

                        #region We have Lateral loads

                        if (NotionalIsChecked || WindIsChecked)
                        {
                            BusyOverlayBindings.I.SetDeterminate($"Generating the Cases for Lateral Loads", "Load Case | Stage");

                            List<ScLoadCaseDataGridType> lateralCases = (from a in SCCasesDataGrid_RawItems
                                where !string.IsNullOrWhiteSpace(a.BaseName) &&
                                      a.Active &&
                                      (a.WindMult != 0d || a.NotionalMult != 0d)
                                select a).ToList();

                            // This is the total number of lateral iterations - used to report
                            int maxLateral = 360 / AngleStep * lateralCases.Count * baseDeadCasesWithStagesToLoadLaterally.First().Value.Count();

                            // Iterates on the lateral loads
                            for (int i_lateralCase = 0; i_lateralCase < lateralCases.Count; i_lateralCase++)
                            {
                                ScLoadCaseDataGridType lateralCase = lateralCases[i_lateralCase];

                                // Gets the base stages related to this lateral load
                                var baseStages = (from a in baseDeadCasesWithStagesToLoadLaterally
                                    where a.Key == lateralCase.BaseName
                                    select a.Value).First();

                                List<(int stage, string name)> createdLateralStageList = new List<(int stage, string name)>();

                                // For all directions
                                for (int k_angle = 0; k_angle < 360; k_angle += AngleStep)
                                {
                                    double notXMult = Math.Cos(Math.PI * k_angle / 180d);
                                    if (Math.Abs(notXMult) <= 0.000001d) notXMult = 0d;
                                    double notYMult = Math.Sin(Math.PI * k_angle / 180d);
                                    if (Math.Abs(notYMult) <= 0.000001d) notYMult = 0d;

                                    // For all base stages -> Creates a lateral static non linear stage that has the base as initial

                                    foreach ((int stage, string name) in baseStages)
                                    {
                                        string lateralName = $"{k_angle:000}_{stage:000}_{lateralCase.Name}";
                                        BusyOverlayBindings.I.UpdateProgress(lateralCreatedStages.Count + 1 * createdLateralStageList.Count + 1, maxLateral, $"{lateralCase.Name} | {lateralName}");

                                        List<LoadCaseNLLoadData> loads = new List<LoadCaseNLLoadData>();

                                        #region Adding the loads that must be added to the cases - Note: temperature comes afterwards

                                        // Do we add Live?
                                        if (lateralCase.LiveMult != 0d)
                                            loads.Add(new LoadCaseNLLoadData()
                                                {
                                                LoadType = "Load",
                                                LoadName = LivePatternName,
                                                ScaleFactor = lateralCase.LiveMult
                                                });

                                        // Do we add strain?
                                        if (lateralCase.StrainMult != 0d)
                                            loads.Add(new LoadCaseNLLoadData()
                                                {
                                                LoadType = "Load",
                                                LoadName = StrainPatternName,
                                                ScaleFactor = lateralCase.StrainMult
                                                });

                                        // Do we add wind?
                                        if (WindIsChecked && lateralCase.WindMult != 0d)
                                        {
                                            string lnTemp =
                                                WindPatternName.Replace("#",
                                                    $"{k_angle:000}");
                                            if (ReducedWindIsChecked)
                                                if (stage <= ReducedWind_Threshold)
                                                    lnTemp += "_RED";

                                            loads.Add(new LoadCaseNLLoadData()
                                                {
                                                LoadType = "Load",
                                                LoadName = lnTemp,
                                                ScaleFactor = lateralCase.WindMult
                                                });
                                        }

                                        // Do we add notional?
                                        if (NotionalIsChecked &&
                                            lateralCase.NotionalMult != 0d)
                                        {
                                            if (notXMult != 0d)
                                                loads.Add(new LoadCaseNLLoadData()
                                                    {
                                                    LoadType = "Load",
                                                    LoadName =
                                                        NotionalPatternName.Replace("#",
                                                            "X"),
                                                    ScaleFactor = lateralCase.NotionalMult * notXMult
                                                    });
                                            if (notYMult != 0d)
                                                loads.Add(new LoadCaseNLLoadData()
                                                    {
                                                    LoadType = "Load",
                                                    LoadName =
                                                        NotionalPatternName.Replace("#",
                                                            "Y"),
                                                    ScaleFactor = lateralCase.NotionalMult * notYMult
                                                    });
                                        }

                                        // Do we have any other load to add?
                                        foreach ((string CaseName, double Mult) in lateralCase.OtherCaseList)
                                            loads.Add(new LoadCaseNLLoadData()
                                                {
                                                LoadType = "Load",
                                                LoadName = CaseName,
                                                ScaleFactor = Mult
                                                });

                                        #endregion

                                        // Adds the case to the model
                                        // Creates the NL case
                                        if (!S2KModel.SM.LCMan.AddNew_LCNonLinear(lateralName, loads,
                                            StagedCreateCasesMakeWithEventToEventStepping,
                                            name))
                                            throw new S2KHelperException(
                                                $"Could not add Non-Linear case named {lateralName}. Aborting the whole operation!");

                                        // Should we also add a Combination?
                                        if (StagedCreateCasesMakeTheirDesignCombos)
                                            S2KModel.SM.CombMan.AddComb($"C_{lateralName}",
                                                new List<(string CaseName, int ScaleFactor)>() {(lateralName, 1)});

                                        // Adds to the created list
                                        createdLateralStageList.Add((stage, lateralName));
                                    }
                                }

                                lateralCreatedStages.Add(lateralCase.Name, createdLateralStageList);
                            }
                        }

                        #endregion

                        #region We have Temperature loads

                        if (TemperatureIsChecked)
                        {
                            BusyOverlayBindings.I.SetDeterminate($"Generating the Temperature Cases", "[Sign] | Load Case | Stage");

                            // Gets the cases that shall add temperature
                            var temperatureCases = new List<ScLoadCaseDataGridType>();

                            int maximumIterations = 0; // The total counter - for reporting to the user
                            foreach (ScLoadCaseDataGridType tempCase in from a in SCCasesDataGrid_RawItems
                                where !string.IsNullOrWhiteSpace(a.BaseName) &&
                                      a.Active &&
                                      a.TemperatureMult != 0d
                                select a)
                            {
                                // Should we consider them to be added depending on the user's options?
                                if (TempWithBaseDeadIsChecked)
                                    if (baseDeadCasesWithStagesToLoadLaterally.ContainsKey(tempCase.BaseName))
                                    {
                                        maximumIterations += baseDeadCasesWithStagesToLoadLaterally[tempCase.BaseName]
                                            .Count;
                                        temperatureCases.Add(tempCase);
                                    }

                                // Should we consider them to be added depending on the user's options?
                                if (TempWithLateralIsChecked)
                                    if (lateralCreatedStages.ContainsKey(tempCase.BaseName))
                                    {
                                        maximumIterations += lateralCreatedStages[tempCase.BaseName].Count;
                                        temperatureCases.Add(tempCase);
                                    }
                            }

                            maximumIterations *= 2;

                            int iterationCounter = 0;
                            foreach (ScLoadCaseDataGridType tCase in temperatureCases)
                            {
                                // Gets the list with the cases to which temperature will be added in this case
                                List<(int stage, string name)> baseTempCases;
                                if (baseDeadCasesWithStagesToLoadLaterally.ContainsKey(tCase.BaseName))
                                    baseTempCases = baseDeadCasesWithStagesToLoadLaterally[tCase.BaseName];
                                else if (lateralCreatedStages.ContainsKey(tCase.BaseName))
                                    baseTempCases = lateralCreatedStages[tCase.BaseName];
                                else
                                    throw new InvalidOperationException(
                                        $"Could not find the {tCase.BaseName} for case {tCase.Name} in the dictionaries that have been created!");

                                foreach ((int stage, string name) in baseTempCases)
                                {
                                    string tempCaseNamePos = $"{name}_+{tCase.TemperatureMult}T";

                                    BusyOverlayBindings.I.UpdateProgress(iterationCounter, maximumIterations, $"[+] | {tempCaseNamePos} | {stage}");
                                    var loadsPos = new List<LoadCaseNLLoadData>
                                        {
                                        new LoadCaseNLLoadData()
                                            {
                                            LoadType = "Load",
                                            LoadName = TempPatternName.Replace("#", "+"),
                                            ScaleFactor = tCase.TemperatureMult
                                            }
                                        };

                                    // Adds the case to the model
                                    // Creates the NL case
                                    if (!S2KModel.SM.LCMan.AddNew_LCNonLinear(tempCaseNamePos, loadsPos, StagedCreateCasesMakeWithEventToEventStepping, name))
                                        throw new S2KHelperException($"Could not add Non-Linear case named {tempCaseNamePos}. Aborting the whole operation!");

                                    // Should we also add a Combination?
                                    if (StagedCreateCasesMakeTheirDesignCombos) S2KModel.SM.CombMan.AddComb($"C_{tempCaseNamePos}", new List<(string CaseName, int ScaleFactor)>() {(tempCaseNamePos, 1)});
                                    iterationCounter++;

                                    string tempCaseNameNeg = $"{name}_-{tCase.TemperatureMult}T";

                                    BusyOverlayBindings.I.UpdateProgress(iterationCounter, maximumIterations, $"[-] | {tempCaseNameNeg} | {stage}");
                                    var loadsNeg = new List<LoadCaseNLLoadData>
                                        {
                                        new LoadCaseNLLoadData()
                                            {
                                            LoadType = "Load",
                                            LoadName = TempPatternName.Replace("#", "-"),
                                            ScaleFactor = tCase.TemperatureMult
                                            }
                                        };

                                    // Adds the case to the model
                                    // Creates the NL case
                                    if (!S2KModel.SM.LCMan.AddNew_LCNonLinear(tempCaseNameNeg, loadsNeg,
                                        StagedCreateCasesMakeWithEventToEventStepping, name)
                                    )
                                        throw new S2KHelperException(
                                            $"Could not add Non-Linear case named {tempCaseNameNeg}. Aborting the whole operation!");

                                    // Should we also add a Combination?
                                    if (StagedCreateCasesMakeTheirDesignCombos)
                                        S2KModel.SM.CombMan.AddComb($"C_{tempCaseNameNeg}",
                                            new List<(string CaseName, int ScaleFactor)>() {(tempCaseNameNeg, 1)});
                                    iterationCounter++;
                                }
                            }
                        }

                        #endregion
                    }

                    #endregion
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
                if (endMessages.Length != 0) OnMessage("Message Title", endMessages.ToString());
            }
        }
        
        private DelegateCommand _generateGhostButtonCommand;
        public DelegateCommand GenerateGhostButtonCommand =>
            _generateGhostButtonCommand ?? (_generateGhostButtonCommand = new DelegateCommand(ExecuteGenerateGhostButtonCommand));
        public async void ExecuteGenerateGhostButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Generating the Ghost Structure";
                    BusyOverlayBindings.I.AutomationWarning_Visibility = Visibility.Visible;

                    S2KModel.SM.InterAuto.FlaUI_Action_CloseAllOtherSAP2000Windows(true);
                    // Focus on the main window
                    EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "ActivateWindow"));

                    // Handling the new ghost group
                    BusyOverlayBindings.I.SetIndeterminate("Cleaning up Previous Ghost Structure.");
                    string ghostGroup = GhostOptions_GroupName;
                    string stifferGhostGroup = GhostOptions_GroupName + "_Stiffer";

                    List<string> grpNames = S2KModel.SM.GroupMan.GetGroupList();
                    if (grpNames.Contains(ghostGroup))
                    {
                        S2KModel.SM.ClearSelection();
                        S2KModel.SM.GroupMan.SelectGroup(ghostGroup);
                        S2KModel.SM.InterAuto.FlaUI_Action_SendDeleteKey();
                        S2KModel.SM.GroupMan.DeleteGroup(ghostGroup);
                        S2KModel.SM.GroupMan.CreateGroup(ghostGroup);
                    }
                    if (grpNames.Contains(stifferGhostGroup))
                    {
                        S2KModel.SM.ClearSelection();
                        S2KModel.SM.GroupMan.SelectGroup(stifferGhostGroup);
                        S2KModel.SM.InterAuto.FlaUI_Action_SendDeleteKey();
                        S2KModel.SM.GroupMan.DeleteGroup(stifferGhostGroup);
                    }

                    string outputTempFileName = $"tempGhostFrame_{S2KStaticMethods.UniqueName(5)}.s2k";
                    string textFullPath = Path.Combine(S2KModel.SM.ModelDir, outputTempFileName);

                    S2KModel.SM.InterAuto.FlaUI_Action_ExportTablesToS2K(textFullPath,
                        new List<Sap2000ExportTable>()
                            {
                            Sap2000ExportTable.Connectivity_MINUS_Frame,
                            Sap2000ExportTable.Connectivity_MINUS_Cable,
                            Sap2000ExportTable.Frame_Section_Assignments,
                            Sap2000ExportTable.Cable_Section_Assignments,
                            Sap2000ExportTable.Frame_Local_Axes_Assignments_1_MINUS_Typical,
                            Sap2000ExportTable.Frame_Local_Axes_Assignments_2_MINUS_Advanced,
                            Sap2000ExportTable.Cable_Section_Definitions,
                            Sap2000ExportTable.Groups_2_MINUS_Assignments,
                            Sap2000ExportTable.Frame_Section_Properties_01_MINUS_General
                            }, inUpdateInterface: true);

                    // Focus on the main window
                    EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "ActivateWindow"));

                    BusyOverlayBindings.I.SetIndeterminate("Reading the S2kK into memory.");
                    string[] originalS2KFile = File.ReadAllLines(textFullPath);
                    
                    DataSet sapDataSet = S2KModel.SM.InterAuto.GetDataSetFromS2K(originalS2KFile, true);

                    // Getting the original tables
                    DataTable frameConnTable = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Connectivity_MINUS_Frame)];
                    DataTable frameSecAssignTable = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Frame_Section_Assignments)];
                    DataTable frameAxis1Table = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Frame_Local_Axes_Assignments_1_MINUS_Typical)];
                    DataTable frameAxis2Table = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Frame_Local_Axes_Assignments_2_MINUS_Advanced)];
                    DataTable inGroupAssignTable = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Groups_2_MINUS_Assignments)];

                    DataSet outputDataSet = new DataSet();

                    // Gets a filtered list of the group assignments that (1 - Is in the frame list and 2 - Is of type frame)
                    //DataTable outGroupAssignTable = Sap2000ExportTable.Groups_2_MINUS_Assignments.GetTableFormat();
                    var frameFilteredGroups = from fconn in frameConnTable.AsEnumerable() join gass in inGroupAssignTable.AsEnumerable().Where(a => a.Field<string>("ObjectType") == "Frame")
                                                on fconn.Field<string>("Frame") equals gass.Field<string>("ObjectLabel")
                                            select new {GroupName = gass["GroupName"], ObjectType = gass["ObjectType"], ObjectLabel = gass["ObjectLabel"]};

                    // Copies so that the ghost FRAMES will also share the same groups as the original items
                    foreach (var item in frameFilteredGroups)
                    {
                        inGroupAssignTable.Rows.Add(item.GroupName, item.ObjectType, "GST_F#" + item.ObjectLabel);
                    }
                    sapDataSet.Tables.Remove(inGroupAssignTable);
                    outputDataSet.Tables.Add(inGroupAssignTable);

                    BusyOverlayBindings.I.SetIndeterminate("Treating the DataSet - Connectivity - Frame");
                    sapDataSet.Tables.Remove(frameConnTable);
                    foreach (DataRow row in frameConnTable.Rows)
                    {
                        row["Frame"] = "GST_F#" + row["Frame"];
                        row["GUID"] = string.Empty;
                    }
                    outputDataSet.Tables.Add(frameConnTable);

                    // Also adds the items to the new ghost group
                    foreach (DataRow row in frameConnTable.Rows)
                    {
                        inGroupAssignTable.Rows.Add(ghostGroup, "Frame", row["Frame"]);
                    }

                    BusyOverlayBindings.I.SetIndeterminate("Treating the DataSet - Frame Section Assignments Table");
                    sapDataSet.Tables.Remove(frameSecAssignTable);
                    foreach (DataRow row in frameSecAssignTable.Rows)
                    {
                        row["Frame"] = "GST_F#" + row["Frame"];
                    }
                    outputDataSet.Tables.Add(frameSecAssignTable);

                    BusyOverlayBindings.I.SetIndeterminate("Treating the DataSet - Frame Local Axes Assignments 1 - Typical");
                    sapDataSet.Tables.Remove(frameAxis1Table);
                    foreach (DataRow row in frameAxis1Table.Rows)
                    {
                        row["Frame"] = "GST_F#" + row["Frame"];
                    }
                    outputDataSet.Tables.Add(frameAxis1Table);

                    BusyOverlayBindings.I.SetIndeterminate("Treating the DataSet - Frame Local Axes Assignments 2 - Advanced");
                    sapDataSet.Tables.Remove(frameAxis2Table);
                    foreach (DataRow row in frameAxis2Table.Rows)
                    {
                        row["Frame"] = "GST_F#" + row["Frame"];
                    }
                    outputDataSet.Tables.Add(frameAxis2Table);

                    #region Add Cables as Frames
                    if (GhostOptions_AddCablesAsFrames)
                    {
                        BusyOverlayBindings.I.SetIndeterminate("Handling the new material and frame sections that will be assigned to the ghost's cables.");

                        string ghostCableMatName = "GST_CABLE_MAT";
                        // First, adds the cable ghost material if it doesn't exist
                        List<string> materials = S2KModel.SM.MaterialMan.GetMaterialList();
                        if (!materials.Contains(ghostCableMatName))
                        {
                            S2KModel.SM.MaterialMan.AddNewMaterial(MatTypeEnum.Steel, "United States", "ASTM A572", "Grade 50", ghostCableMatName);
                            S2KModel.SM.MaterialMan.SetIsotropicMaterialProperties(ghostCableMatName, 29000d / 2d);
                            
                            materials = S2KModel.SM.MaterialMan.GetMaterialList();
                            if (!materials.Contains(ghostCableMatName)) throw new S2KHelperException($"Could not add dummy material called {ghostCableMatName} that will be used to turn cables into frames for the ghost structure.");
                        }

                        // Checks if the frame sections of the cables already exist in the table
                        DataTable cableSecDef = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Cable_Section_Definitions)];
                        DataTable frameSecDef = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Frame_Section_Properties_01_MINUS_General)];

                        foreach (DataRow row in cableSecDef.Rows)
                        {
                            string cableAsFrameSectName = "GSTc_" + row.Field<string>("CableSect");

                            if (frameSecDef.AsEnumerable().Any(a => a.Field<string>("SectionName") == cableAsFrameSectName)) continue;
                            else
                            { // The section does not exist in the frame section table
                                double cableDiameter = row.Field<double>("Diameter");
                                if (!S2KModel.SM.FrameSecMan.SetOrAddCircle(cableAsFrameSectName, ghostCableMatName, cableDiameter)) throw new S2KHelperException($"Could not add the dummy frame section called {cableAsFrameSectName} that will be used to add the ghost's cables.");
                            }
                        }

                        BusyOverlayBindings.I.SetIndeterminate("Adding the definition of the ghost's cables to the frame tables.");

                        DataTable cableConnTable = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Connectivity_MINUS_Cable)];
                        DataTable cableSectAssignTable = sapDataSet.Tables[SapAutoExtensions.EnumToString(Sap2000ExportTable.Cable_Section_Assignments)];

                        // The cables also need to be with:
                        // 1) Tension-Compression Limits
                        DataTable frameTensionCompLimitsTable = Sap2000ExportTable.Frame_Tension_And_Compression_Limits.GetTableFormat();

                        // 2) Releases
                        DataTable frameEndReleasesTable = Sap2000ExportTable.Frame_Release_Assignments_1_MINUS_General.GetTableFormat();

                        // Adds the cables to the frame tables
                        foreach (DataRow cableRow in cableConnTable.Rows)
                        {
                            string cName = "GST_C#" + cableRow["Cable"];

                            // Adds the cable to the frame connectivity table
                            DataRow newFrame = frameConnTable.NewRow();
                            newFrame["Frame"] = cName;
                            newFrame["JointI"] = cableRow["JointI"];
                            newFrame["JointJ"] = cableRow["JointJ"];
                            newFrame["IsCurved"] = "No";
                            frameConnTable.Rows.Add(newFrame);

                            // Adds the TC limits
                            DataRow tcRow = frameTensionCompLimitsTable.NewRow();
                            tcRow["Frame"] = cName;
                            tcRow["TensLimit"] = "No";
                            tcRow["CompLimit"] = "Yes";
                            tcRow["Compression"] = 0d;
                            frameTensionCompLimitsTable.Rows.Add(tcRow);

                            // Adds the frame releases
                            DataRow relRow = frameEndReleasesTable.NewRow();
                            relRow["Frame"] = cName;
                            relRow["M2I"] = "Yes";
                            relRow["M3I"] = "Yes";
                            relRow["M2J"] = "Yes";
                            relRow["M3J"] = "Yes";
                            relRow["TJ"] = "Yes";
                            relRow["PartialFix"] = "No";
                            frameEndReleasesTable.Rows.Add(relRow);

                            // Adds also a definition to the groups table putting them in the Ghost's base group
                            inGroupAssignTable.Rows.Add(ghostGroup, "Frame", cName);
                        }

                        // Replicating the original groups of these cables
                        // Gets a filtered list of the group assignments that (1 - Is in the cable list and 2 - Is of type frame - cables are set here as Frames)
                        var cableFilteredGroups = from cconn in cableConnTable.AsEnumerable()
                            join gass in inGroupAssignTable.AsEnumerable().Where(a => a.Field<string>("ObjectType") == "Frame")
                                on cconn.Field<string>("Cable") equals gass.Field<string>("ObjectLabel")
                            select new { GroupName = gass["GroupName"], ObjectType = gass["ObjectType"], ObjectLabel = gass["ObjectLabel"] };

                        // Copies so that the ghost CABLE-FRAMES will also share the same groups as the original items
                        foreach (var item in cableFilteredGroups)
                        {
                            inGroupAssignTable.Rows.Add(item.GroupName, item.ObjectType, "GST_C#" + item.ObjectLabel);
                        }

                        outputDataSet.Tables.Add(frameTensionCompLimitsTable);
                        outputDataSet.Tables.Add(frameEndReleasesTable);

                        // Sets up their sections
                        foreach (DataRow cableSectRow in cableSectAssignTable.Rows)
                        {
                            DataRow newFrameSectAssign = frameSecAssignTable.NewRow();
                            newFrameSectAssign["Frame"] = "GST_C#" + cableSectRow["Cable"];
                            newFrameSectAssign["AutoSelect"] = "N.A.";
                            newFrameSectAssign["AnalSect"] = "GSTc_" + cableSectRow["CableSect"];
                            newFrameSectAssign["MatProp"] = "Default";
                            frameSecAssignTable.Rows.Add(newFrameSectAssign);
                        }
                    }
                    #endregion

                    #region Make Groups Stiffer
                    
                    if (GhostOptions_MakeGroupStiffer && !string.IsNullOrWhiteSpace(StifferComboBox_SelectedItem))
                    {
                        BusyOverlayBindings.I.SetIndeterminate("Handling the items that should be made stiffer in the Ghost Structure.");

                        S2KModel.SM.GroupMan.CreateGroup(stifferGhostGroup);

                        List<DataRow> newRows = new List<DataRow>();
                        foreach (DataRow row in inGroupAssignTable.AsEnumerable().Where(a => a.Field<string>("GroupName") == StifferComboBox_SelectedItem))
                        {
                            DataRow newRow = inGroupAssignTable.NewRow();
                            newRow["GroupName"] = stifferGhostGroup;
                            newRow["ObjectType"] = row["ObjectType"];
                            newRow["ObjectLabel"] = row["ObjectLabel"];
                            newRows.Add(newRow);
                        }

                        foreach (DataRow nRow in newRows)
                        {
                            inGroupAssignTable.Rows.Add(nRow);
                        }
                    }
                    #endregion

                    // Dumps the DataSet into the S2K
                    S2KModel.SM.InterAuto.WriteDataSetToS2K(textFullPath + "_temp", outputDataSet, true);

                    // Imports the S2K into the SAP2000 program
                    S2KModel.SM.InterAuto.FlaUI_Action_ImportTablesFromS2K(textFullPath + "_temp", inUpdateInterface: true);

                    // Focus on the main window
                    EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "ActivateWindow"));

                    // Fixes the stiffness of the items
                    BusyOverlayBindings.I.SetIndeterminate("Fixing the modifiers of the Ghost Structure.");
                    S2KModel.SM.FrameMan.SetModifiers_Group(ghostGroup
                        , GhostOptions_AxialReductionValue
                        , GhostOptions_OthersReductionValue
                        , GhostOptions_OthersReductionValue
                        , GhostOptions_OthersReductionValue
                        , GhostOptions_OthersReductionValue
                        , GhostOptions_OthersReductionValue
                        , 0
                        , 0);

                    BusyOverlayBindings.I.SetIndeterminate("Fixing the modifiers of the Ghost Structure - Make Stiffer Groups.");
                    if (GhostOptions_MakeGroupStiffer)
                        S2KModel.SM.FrameMan.SetModifiers_Group(stifferGhostGroup
                            , GhostOptions_AxialReductionValue *
                              GhostOptions_ReductionValue_GroupMult
                            , GhostOptions_OthersReductionValue *
                              GhostOptions_ReductionValue_GroupMult
                            , GhostOptions_OthersReductionValue *
                              GhostOptions_ReductionValue_GroupMult
                            , GhostOptions_OthersReductionValue *
                              GhostOptions_ReductionValue_GroupMult
                            , GhostOptions_OthersReductionValue *
                              GhostOptions_ReductionValue_GroupMult
                            , GhostOptions_OthersReductionValue *
                              GhostOptions_ReductionValue_GroupMult
                            , 0
                            , 0);

                    // Cleansup - Deletes the files
                    BusyOverlayBindings.I.SetIndeterminate("Cleaning up temporary files.");
                    FileInfo originalDumpFile = new FileInfo(textFullPath);
                    if (originalDumpFile.Exists) originalDumpFile.Delete();
                    FileInfo inputFile = new FileInfo(textFullPath + "_temp");
                    if (inputFile.Exists) inputFile.Delete();
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
                if (endMessages.Length != 0) OnMessage("Message Title", endMessages.ToString());
            }
        }

        private DelegateCommand _clearAllLoadCasesAndCombosButtonCommand;
        public DelegateCommand ClearAllLoadCasesAndCombosButtonCommand =>
            _clearAllLoadCasesAndCombosButtonCommand ?? (_clearAllLoadCasesAndCombosButtonCommand = new DelegateCommand(ExecuteClearAllLoadCasesAndCombosButtonCommand));
        public async void ExecuteClearAllLoadCasesAndCombosButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Clearing All Load Cases and Combos";
                    BusyOverlayBindings.I.AutomationWarning_Visibility = Visibility.Visible;

                    S2KModel.SM.InterAuto.PInvoke_MakeSap2000ActiveWindow(true);

                    //progReporter.Report(ProgressData.SetMessage("Trying to close all secondary windows."));
                    if (S2KModel.SM.InterAuto.SAP2000HasOtherWindows)
                        throw new S2KHelperException($"Please close all Sap2000 dialogs before proceeding.");

                    //progReporter.Report(ProgressData.SetMessage("Automating the interface to delete all Load Cases and Combinations.",true));
                    S2KModel.SM.InterAuto.Action_ClearAllLoadCasesAndCombos();

                    // TODO: Add things to the StringBuilder for the messages.
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
                if (endMessages.Length != 0) OnMessage("Message Title", endMessages.ToString());
            }
        }

        private DelegateCommand _selectStepRangeButtonCommand;
        public DelegateCommand SelectStepRangeButtonCommand =>
            _selectStepRangeButtonCommand ?? (_selectStepRangeButtonCommand = new DelegateCommand(ExecuteSelectStepRangeButtonCommand));
        public async void ExecuteSelectStepRangeButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                // Parses the textbox
                int start;
                int end;
                if (SelectStepGroupText.Contains("-"))
                {
                    var parts = SelectStepGroupText.Split(new char[] { '-' });
                    if (parts.Length != 2) throw new InvalidOperationException("Could not parse the desired range. Please check the input format.");

                    if (!int.TryParse(parts[0], out start)) throw new InvalidOperationException("Could not parse the desired range. Please check the input format.");

                    if (!int.TryParse(parts[1], out end)) throw new InvalidOperationException("Could not parse the desired range. Please check the input format.");
                }
                else
                {
                    if (!int.TryParse(SelectStepGroupText, out start)) throw new InvalidOperationException("Could not parse the desired range. Please check the input format.");
                    end = start;
                }

                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Selecting the Steps";

                    // Gets the list of the steps - gets here because it will be used in loops
                    var stepList = (from a in SCStepsDataGridItems
                        where a.Order >= start && a.Order <= end
                        orderby a.Order ascending
                        select a).ToList();

                    // Clear selection
                    S2KModel.SM.ClearSelection();

                    BusyOverlayBindings.I.SetDeterminate("Selecting the Groups.", "Group");

                    for (int i = 0; i < stepList.Count; i++)
                    {
                        SCStepsDataGridType step = (SCStepsDataGridType)stepList[i];
                        BusyOverlayBindings.I.UpdateProgress(i, stepList.Count, step.GroupName);

                        // Selects the group
                        if (!S2KModel.SM.GroupMan.SelectGroup(step.GroupName)) throw new S2KHelperException($"Could not select the items of group {step.GroupName}.");
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
                if (endMessages.Length != 0) OnMessage("Message Title", endMessages.ToString());
            }
        }
        #endregion
    }

    public enum StagedSelectExcelTextBlock_Alternatives
    {
        Unloaded,
        Loaded,
        ErrorInFile
    }
}