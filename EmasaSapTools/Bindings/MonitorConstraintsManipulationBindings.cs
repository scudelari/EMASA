using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using EmasaSapTools.Monitors;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Prism.Commands;
using Prism.Events;
using Sap2000Library;
using Sap2000Library.SapObjects;
using Sap2000Library.DataClasses;

namespace EmasaSapTools.Bindings
{
    public class MonitorConstraintsManipulationBindings : BindableSingleton<MonitorConstraintsManipulationBindings>, IMonitorInterfaceItems
    {
        private MonitorConstraintsManipulationBindings()
        {
            EventAggregatorSingleton.I.GetEvent<PauseMonitorEvent>().Subscribe(PauseMonitorEventHandler, ThreadOption.PublisherThread);
            EventAggregatorSingleton.I.GetEvent<ResumeMonitorEvent>().Subscribe(ResumeMonitorEventHandler, ThreadOption.PublisherThread);
        }

        private void PauseMonitorEventHandler(PauseMonitorEventArgs inObj)
        {
            // If the name was given, only this
            if (string.IsNullOrWhiteSpace(inObj.MonitorName))
            {
                MyMonitor.StopMonitor(true);
                Constraint_IsEnabled = false;
            }
            else
            {
                if (inObj.MonitorName == Name)
                {
                    MyMonitor.StopMonitor(true);
                    Constraint_IsEnabled = false;
                }
            }
        }

        private void ResumeMonitorEventHandler(ResumeMonitorEventArgs inObj)
        {
            // If the name was given, only this
            if (string.IsNullOrWhiteSpace(inObj.MonitorName))
            {
                MyMonitor.StartMonitor(true);
                Constraint_IsEnabled = MyMonitor.IsRunning;
            }
            else
            {
                if (inObj.MonitorName == Name)
                {
                    MyMonitor.StartMonitor(true);
                    Constraint_IsEnabled = MyMonitor.IsRunning;
                }
            }
        }

        public override void SetOrReset()
        {
            MyMonitor = new ConstraintMonitor();
            MyMonitor.MonitorDataChanged += MyMonitor_MonitorDataChanged;

            _selectedProbePoints.CollectionChanged += _selectedProbePoints_CollectionChanged;

            Name = "Constraint";
            ShortName = "C";

            BodyConstraintTypeRadioButton_IsChecked = true;
            EqualConstraintTypeRadioButton_IsChecked = false;
            LocalConstraintTypeRadioButton_IsChecked = false;

            Constraint_IsEnabled = false;

            CoordinateSystemTextBox_Text = "Global";
            ConstraintPrefix_Text = "";

            U1CheckBox_IsChecked = true;
            U2CheckBox_IsChecked = true;
            U3CheckBox_IsChecked = true;
            R1CheckBox_IsChecked = true;
            R2CheckBox_IsChecked = true;
            R3CheckBox_IsChecked = true;

            SelectedMonitor = null;
        }

        private ConstraintMonitor MyMonitor = null;
        private readonly ObservableCollection<SapPoint> _selectedProbePoints = new ObservableCollection<SapPoint>();

        private string _name;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _shortName;

        public string ShortName
        {
            get => _shortName;
            set => SetProperty(ref _shortName, value);
        }

        private bool _BodyConstraintTypeRadioButton_IsChecked;

        public bool BodyConstraintTypeRadioButton_IsChecked
        {
            get => _BodyConstraintTypeRadioButton_IsChecked;
            set
            {
                SetProperty(ref _BodyConstraintTypeRadioButton_IsChecked, value);
                UpdateConstraintName_Label();
            }
        }

        private bool _EqualConstraintTypeRadioButton_IsChecked;

        public bool EqualConstraintTypeRadioButton_IsChecked
        {
            get => _EqualConstraintTypeRadioButton_IsChecked;
            set
            {
                SetProperty(ref _EqualConstraintTypeRadioButton_IsChecked, value);
                UpdateConstraintName_Label();
            }
        }

        private bool _LocalConstraintTypeRadioButton_IsChecked;

        public bool LocalConstraintTypeRadioButton_IsChecked
        {
            get => _LocalConstraintTypeRadioButton_IsChecked;
            set
            {
                CoordinateSystemTextBox_IsEnabled = !value;

                if (value)
                {
                    U1CheckBox_Label = "U1";
                    U2CheckBox_Label = "U2";
                    U3CheckBox_Label = "U3";
                    R1CheckBox_Label = "R1";
                    R2CheckBox_Label = "R2";
                    R3CheckBox_Label = "R3";
                }
                else
                {
                    U1CheckBox_Label = "TX";
                    U2CheckBox_Label = "TY";
                    U3CheckBox_Label = "TZ";
                    R1CheckBox_Label = "RX";
                    R2CheckBox_Label = "RY";
                    R3CheckBox_Label = "RZ";
                }

                SetProperty(ref _LocalConstraintTypeRadioButton_IsChecked, value);
                UpdateConstraintName_Label();
            }
        }

        public string OutConstraintName { get; private set; }

        private void UpdateConstraintName_Label()
        {
            string typePrefix = "";

            if (BodyConstraintTypeRadioButton_IsChecked) typePrefix = "B_";
            if (EqualConstraintTypeRadioButton_IsChecked) typePrefix = "E_";
            if (LocalConstraintTypeRadioButton_IsChecked) typePrefix = "L_";

            OutConstraintName = $"{typePrefix}{ConstraintPrefix_Text}";
            ConstraintName_Label = OutConstraintName + "<RandomId>";
        }

        private string _ConstraintName_Label;

        public string ConstraintName_Label
        {
            get => _ConstraintName_Label;
            set => SetProperty(ref _ConstraintName_Label, value);
        }

        private bool _U1CheckBox_IsChecked;

        public bool U1CheckBox_IsChecked
        {
            get => _U1CheckBox_IsChecked;
            set
            {
                SetProperty(ref _U1CheckBox_IsChecked, value);
            }
        }

        private bool _U2CheckBox_IsChecked;

        public bool U2CheckBox_IsChecked
        {
            get => _U2CheckBox_IsChecked;
            set
            {
                SetProperty(ref _U2CheckBox_IsChecked, value);
            }
        }

        private bool _U3CheckBox_IsChecked;

        public bool U3CheckBox_IsChecked
        {
            get => _U3CheckBox_IsChecked;
            set
            {
                SetProperty(ref _U3CheckBox_IsChecked, value);
            }
        }

        private bool _R1CheckBox_IsChecked;

        public bool R1CheckBox_IsChecked
        {
            get => _R1CheckBox_IsChecked;
            set
            {
                SetProperty(ref _R1CheckBox_IsChecked, value);
            }
        }

        private bool _R2CheckBox_IsChecked;

        public bool R2CheckBox_IsChecked
        {
            get => _R2CheckBox_IsChecked;
            set
            {
                SetProperty(ref _R2CheckBox_IsChecked, value);
            }
        }

        private bool _R3CheckBox_IsChecked;

        public bool R3CheckBox_IsChecked
        {
            get => _R3CheckBox_IsChecked;
            set
            {
                SetProperty(ref _R3CheckBox_IsChecked, value);
            }
        }

        private string _U1CheckBox_Label;

        public string U1CheckBox_Label
        {
            get => _U1CheckBox_Label;
            set => SetProperty(ref _U1CheckBox_Label, value);
        }

        private string _U2CheckBox_Label;

        public string U2CheckBox_Label
        {
            get => _U2CheckBox_Label;
            set => SetProperty(ref _U2CheckBox_Label, value);
        }

        private string _U3CheckBox_Label;

        public string U3CheckBox_Label
        {
            get => _U3CheckBox_Label;
            set => SetProperty(ref _U3CheckBox_Label, value);
        }

        private string _R1CheckBox_Label;

        public string R1CheckBox_Label
        {
            get => _R1CheckBox_Label;
            set => SetProperty(ref _R1CheckBox_Label, value);
        }

        private string _R2CheckBox_Label;

        public string R2CheckBox_Label
        {
            get => _R2CheckBox_Label;
            set => SetProperty(ref _R2CheckBox_Label, value);
        }

        private string _R3CheckBox_Label;

        public string R3CheckBox_Label
        {
            get => _R3CheckBox_Label;
            set => SetProperty(ref _R3CheckBox_Label, value);
        }

        private bool _CoordinateSystemTextBox_IsEnabled;

        public bool CoordinateSystemTextBox_IsEnabled
        {
            get => _CoordinateSystemTextBox_IsEnabled;
            set => SetProperty(ref _CoordinateSystemTextBox_IsEnabled, value);
        }

        private string _CoordinateSystemTextBox_Text;

        public string CoordinateSystemTextBox_Text
        {
            get => _CoordinateSystemTextBox_Text;
            set => SetProperty(ref _CoordinateSystemTextBox_Text, value);
        }

        private string _ConstraintPrefix_Text;

        public string ConstraintPrefix_Text
        {
            get => _ConstraintPrefix_Text;
            set
            {
                SetProperty(ref _ConstraintPrefix_Text, value);
                UpdateConstraintName_Label();
            }
        }

        private bool _constraint_IsEnabled;

        public bool Constraint_IsEnabled
        {
            get => _constraint_IsEnabled;
            set => SetProperty(ref _constraint_IsEnabled, value);
        }

        // NEW SECTION
        public FastObservableCollection<SapPoint> SapSelectedPointList { get; set; } =
            new FastObservableCollection<SapPoint>();

        public FastObservableCollection<JointConstraintMonitorData> SapSelectedPointConstraintList { get; set; } =
            new FastObservableCollection<JointConstraintMonitorData>();

        public ObservableCollection<SapPoint> SelectedProbePoints => _selectedProbePoints;

        private void _selectedProbePoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // The input joint list is empty
            if (SapSelectedPointList.Count == 0) return;

            // The selected entry is empty
            if (SelectedProbePoints.Count == 0)
            {
                foreach (JointConstraintMonitorData constraintMonitorData in SapSelectedPointConstraintList)
                {
                    constraintMonitorData.SelPointHasIt = TriState.NotSet;
                }
            }
            else
            {
                foreach (JointConstraintMonitorData constraintMonitorData in SapSelectedPointConstraintList)
                {
                    if (SelectedProbePoints.All(inPoint => inPoint.JointConstraintNames.Contains(constraintMonitorData.ConstraintName)))
                        constraintMonitorData.SelPointHasIt = TriState.True;
                    else
                        constraintMonitorData.SelPointHasIt = TriState.False;
                }
            }

            SapSelectedPointConstraintList.NotifyChanges();
        }

        private void MyMonitor_MonitorDataChanged(object sender, EventArgs e)
        {
            if (MyMonitor.CurrentMonitorData is List<SapPoint> monitorData)
            {
                // It was a new list
                if (!SapSelectedPointList.ReplaceItemsIfNew(monitorData.OrderBy(a => a.Name).ToList()))
                {
                    // Gets a list with the unique names of the constraints
                    var allConstraints = new HashSet<JointConstraintMonitorData>();
                    foreach (SapPoint joint in monitorData)
                    foreach (string cName in joint.JointConstraintNames)
                    {
                        TriState allJointsHaveIt = TriState.False;

                        if (monitorData.All(a => a.JointConstraintNames.Contains(cName)))
                            allJointsHaveIt = TriState.True;

                        JointConstraintMonitorData data = new JointConstraintMonitorData(cName) {AllPointsHaveIt = allJointsHaveIt};

                        allConstraints.Add(data);
                    }

                    // Adds the constraints if they are new; sorted.
                    SapSelectedPointConstraintList.AddItems(allConstraints.OrderBy(a => a.ConstraintName).ToList(), true);
                }
            }
            else
            {
                SapSelectedPointList.Clear();
                SapSelectedPointConstraintList.Clear();

                SelConstraint_cSys = String.Empty;
                SelConstraint_HasDoFVis = Visibility.Collapsed;
                SelConstraint_HasAxisXVis = Visibility.Collapsed;
                SelConstraint_HasAxisYVis = Visibility.Collapsed;
                SelConstraint_HasAxisZVis = Visibility.Collapsed;
                SelConstraint_HasAxisAutoVis = Visibility.Collapsed;
                SelConstraint_HasCSysVis = Visibility.Collapsed;
            }
        }

        private JointConstraintMonitorData _SelectedMonitor;

        public JointConstraintMonitorData SelectedMonitor
        {
            get => _SelectedMonitor;
            set
            {
                SetProperty(ref _SelectedMonitor, value);

                if (value != null)
                {
                    SelConstraint_Type = value.ConstraintDef.ConstraintType;

                    switch (value.ConstraintDef.ConstraintType)
                    {
                        case ConstraintTypeEnum.Local:
                            SelConstraint_HasDoFVis = Visibility.Visible;
                            SelConstraint_HasAxisXVis = Visibility.Collapsed;
                            SelConstraint_HasAxisYVis = Visibility.Collapsed;
                            SelConstraint_HasAxisZVis = Visibility.Collapsed;
                            SelConstraint_HasAxisAutoVis = Visibility.Collapsed;
                            SelConstraint_HasCSysVis = Visibility.Collapsed;

                            SelConstraint_U1 = value.ConstraintDef.U1;
                            SelConstraint_U2 = value.ConstraintDef.U2;
                            SelConstraint_U3 = value.ConstraintDef.U3;
                            SelConstraint_R1 = value.ConstraintDef.R1;
                            SelConstraint_R2 = value.ConstraintDef.R2;
                            SelConstraint_R3 = value.ConstraintDef.R3;
                            break;

                        case ConstraintTypeEnum.Equal:
                        case ConstraintTypeEnum.Body:
                        case ConstraintTypeEnum.Line:
                        case ConstraintTypeEnum.Weld:
                            SelConstraint_HasDoFVis = Visibility.Visible;
                            SelConstraint_HasAxisXVis = Visibility.Collapsed;
                            SelConstraint_HasAxisYVis = Visibility.Collapsed;
                            SelConstraint_HasAxisZVis = Visibility.Collapsed;
                            SelConstraint_HasAxisAutoVis = Visibility.Collapsed;
                            SelConstraint_HasCSysVis = Visibility.Visible;

                            SelConstraint_U1 = value.ConstraintDef.U1;
                            SelConstraint_U2 = value.ConstraintDef.U2;
                            SelConstraint_U3 = value.ConstraintDef.U3;
                            SelConstraint_R1 = value.ConstraintDef.R1;
                            SelConstraint_R2 = value.ConstraintDef.R2;
                            SelConstraint_R3 = value.ConstraintDef.R3;
                            SelConstraint_cSys = value.ConstraintDef.CSys;
                            break;

                        case ConstraintTypeEnum.Beam:
                        case ConstraintTypeEnum.Diaphragm:
                        case ConstraintTypeEnum.Plate:
                        case ConstraintTypeEnum.Rod:
                            SelConstraint_HasDoFVis = Visibility.Collapsed;
                            SelConstraint_HasAxisXVis = Visibility.Visible;
                            SelConstraint_HasAxisYVis = Visibility.Visible;
                            SelConstraint_HasAxisZVis = Visibility.Visible;
                            SelConstraint_HasAxisAutoVis = Visibility.Visible;
                            SelConstraint_HasCSysVis = Visibility.Visible;

                            switch (value.ConstraintDef.Axis)
                            {
                                case ConstraintAxisEnum.X:
                                    SelConstraint_AxisX = true;
                                    SelConstraint_AxisY = false;
                                    SelConstraint_AxisZ = false;
                                    SelConstraint_AxisAuto = false;
                                    break;

                                case ConstraintAxisEnum.Y:
                                    SelConstraint_AxisX = false;
                                    SelConstraint_AxisY = true;
                                    SelConstraint_AxisZ = false;
                                    SelConstraint_AxisAuto = false;
                                    break;

                                case ConstraintAxisEnum.Z:
                                    SelConstraint_AxisX = false;
                                    SelConstraint_AxisY = false;
                                    SelConstraint_AxisZ = true;
                                    SelConstraint_AxisAuto = false;
                                    break;

                                case ConstraintAxisEnum.Auto:
                                    SelConstraint_AxisX = false;
                                    SelConstraint_AxisY = false;
                                    SelConstraint_AxisZ = false;
                                    SelConstraint_AxisAuto = true;
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            SelConstraint_cSys = value.ConstraintDef.CSys;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                else
                {
                    SelConstraint_cSys = String.Empty;
                    SelConstraint_HasDoFVis = Visibility.Collapsed;
                    SelConstraint_HasAxisXVis = Visibility.Collapsed;
                    SelConstraint_HasAxisYVis = Visibility.Collapsed;
                    SelConstraint_HasAxisZVis = Visibility.Collapsed;
                    SelConstraint_HasAxisAutoVis = Visibility.Collapsed;
                    SelConstraint_HasCSysVis = Visibility.Collapsed;
                }
            }
        }

        private Visibility _SelConstraint_HasDoFVis;

        public Visibility SelConstraint_HasDoFVis
        {
            get => _SelConstraint_HasDoFVis;
            set => SetProperty(ref _SelConstraint_HasDoFVis, value);
        }

        private Visibility _SelConstraint_HasAxisXVis;

        public Visibility SelConstraint_HasAxisXVis
        {
            get => _SelConstraint_HasAxisXVis;
            set => SetProperty(ref _SelConstraint_HasAxisXVis, value);
        }

        private Visibility _SelConstraint_HasAxisYVis;

        public Visibility SelConstraint_HasAxisYVis
        {
            get => _SelConstraint_HasAxisYVis;
            set => SetProperty(ref _SelConstraint_HasAxisYVis, value);
        }

        private Visibility _SelConstraint_HasAxisZVis;

        public Visibility SelConstraint_HasAxisZVis
        {
            get => _SelConstraint_HasAxisZVis;
            set => SetProperty(ref _SelConstraint_HasAxisZVis, value);
        }

        private Visibility _SelConstraint_HasAxisAutoVis;

        public Visibility SelConstraint_HasAxisAutoVis
        {
            get => _SelConstraint_HasAxisAutoVis;
            set => SetProperty(ref _SelConstraint_HasAxisAutoVis, value);
        }

        private Visibility _SelConstraint_HasCSysVis;

        public Visibility SelConstraint_HasCSysVis
        {
            get => _SelConstraint_HasCSysVis;
            set => SetProperty(ref _SelConstraint_HasCSysVis, value);
        }

        private ConstraintTypeEnum _SelConstraint_Type;

        public ConstraintTypeEnum SelConstraint_Type
        {
            get => _SelConstraint_Type;
            set => SetProperty(ref _SelConstraint_Type, value);
        }

        private bool _SelConstraint_U1;

        public bool SelConstraint_U1
        {
            get => _SelConstraint_U1;
            set => SetProperty(ref _SelConstraint_U1, value);
        }

        private bool _SelConstraint_U2;

        public bool SelConstraint_U2
        {
            get => _SelConstraint_U2;
            set => SetProperty(ref _SelConstraint_U2, value);
        }

        private bool _SelConstraint_U3;

        public bool SelConstraint_U3
        {
            get => _SelConstraint_U3;
            set => SetProperty(ref _SelConstraint_U3, value);
        }

        private bool _SelConstraint_R1;

        public bool SelConstraint_R1
        {
            get => _SelConstraint_R1;
            set => SetProperty(ref _SelConstraint_R1, value);
        }

        private bool _SelConstraint_R2;

        public bool SelConstraint_R2
        {
            get => _SelConstraint_R2;
            set => SetProperty(ref _SelConstraint_R2, value);
        }

        private bool _SelConstraint_R3;

        public bool SelConstraint_R3
        {
            get => _SelConstraint_R3;
            set => SetProperty(ref _SelConstraint_R3, value);
        }

        private bool _SelConstraint_AxisX;

        public bool SelConstraint_AxisX
        {
            get => _SelConstraint_AxisX;
            set => SetProperty(ref _SelConstraint_AxisX, value);
        }

        private bool _SelConstraint_AxisY;

        public bool SelConstraint_AxisY
        {
            get => _SelConstraint_AxisY;
            set => SetProperty(ref _SelConstraint_AxisY, value);
        }

        private bool _SelConstraint_AxisZ;

        public bool SelConstraint_AxisZ
        {
            get => _SelConstraint_AxisZ;
            set => SetProperty(ref _SelConstraint_AxisZ, value);
        }

        private bool _SelConstraint_AxisAuto;

        public bool SelConstraint_AxisAuto
        {
            get => _SelConstraint_AxisAuto;
            set => SetProperty(ref _SelConstraint_AxisAuto, value);
        }

        private string _SelConstraint_cSys;

        public string SelConstraint_cSys
        {
            get => _SelConstraint_cSys;
            set => SetProperty(ref _SelConstraint_cSys, value);
        }

        #region Actions

        public void ConstraintMonitorCheckBox_Checked()
        {
            if (MyMonitor.IsRunning) return;
            MyMonitor.StartMonitor();
        }
        public void ConstraintMonitorCheckBox_Unchecked()
        {
            if (!MyMonitor.IsRunning) return;
            MyMonitor.StopMonitor();
        }

        private DelegateCommand _addConstraintButtonCommand;
        public DelegateCommand AddConstraintButtonCommand =>
            _addConstraintButtonCommand ?? (_addConstraintButtonCommand = new DelegateCommand(ExecuteAddConstraintButtonCommand)).ObservesCanExecute(() => Constraint_IsEnabled);
        public async void ExecuteAddConstraintButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = "Add New Constraint to Joints";

                    // Gets Selected Points
                    List<SapPoint> selPoints = S2KModel.SM.PointMan.GetSelected(true);

                    bool[] constVals =
                        {
                        U1CheckBox_IsChecked,
                        U2CheckBox_IsChecked,
                        U3CheckBox_IsChecked,
                        R1CheckBox_IsChecked,
                        R2CheckBox_IsChecked,
                        R3CheckBox_IsChecked
                        };

                    string constName = OutConstraintName + S2KStaticMethods.UniqueName(10);

                    BusyOverlayBindings.I.SetIndeterminate("Adding the new Constraint Definition.");
                    if (BodyConstraintTypeRadioButton_IsChecked)
                    {
                        // Creates a joint constraint
                        if (!S2KModel.SM.JointConstraintMan.SetBodyConstraint(constName, constVals))
                            throw new S2KHelperException($"Could not create BODY constraint called {constName}");
                    }
                    else if (LocalConstraintTypeRadioButton_IsChecked)
                    {
                        // Creates a joint constraint
                        if (!S2KModel.SM.JointConstraintMan.SetLocalConstraint(constName, constVals))
                            throw new S2KHelperException($"Could not create LOCAL constraint called {constName}");
                    }
                    else if (EqualConstraintTypeRadioButton_IsChecked)
                    {
                        // Creates a joint constraint
                        if (!S2KModel.SM.JointConstraintMan.SetEqualConstraint(constName, constVals))
                            throw new S2KHelperException($"Could not create EQUAL constraint called {constName}");
                    }

                    // Sets the constraint to all points
                    BusyOverlayBindings.I.SetDeterminate($"Adding constraint {constName}.", "Joint");
                    for (int index = 0; index < selPoints.Count; index++)
                    {
                        SapPoint pntToConst = selPoints[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selPoints.Count, pntToConst.Name);
                        if (!pntToConst.AddJointConstraint(constName)) // Failed
                        {
                            endMessages.AppendLine(pntToConst.Name);
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
                if (endMessages.Length != 0)
                    OnMessage("Could not add the new constraint to the following joints", endMessages.ToString());
            }
        }

        private DelegateCommand _mergeConstraintButtonCommand;
        public DelegateCommand MergeConstraintButtonCommand =>
            _mergeConstraintButtonCommand ?? (_mergeConstraintButtonCommand = new DelegateCommand(ExecuteMergeConstraintButtonCommand)).ObservesCanExecute(() => Constraint_IsEnabled);
        public async void ExecuteMergeConstraintButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Merge Constraints";

                    // Gets Selected Points
                    var selPoints = S2KModel.SM.PointMan.GetSelected(true);

                    //bool differentList = !selPoints.TrueForAll(sP => this.MonitorConstraintsManipulationBindings.I.SelectedPoints.Any(b => b.ShortName == sP.ShortName));

                    // Gets the number of distinct constraints from the point group
                    var cteNames = new HashSet<string>();
                    var cteNames2 = selPoints.SelectMany(a => a.JointConstraintNames).Distinct().ToList();
                    foreach (SapPoint pnt in selPoints)
                    foreach (string item in pnt.JointConstraintNames)
                        cteNames.Add(item);

                    if (cteNames.Count > 1)
                    {
                        throw new S2KHelperException("You can only group points with ONE constraint already set.");
                    }

                    string constName = cteNames.First();

                    // Sets the constraint to all points
                    BusyOverlayBindings.I.SetDeterminate($"Merging constraint {constName}.", "Joint");
                    for (int index = 0; index < selPoints.Count; index++)
                    {
                        SapPoint pntToConst = selPoints[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selPoints.Count, pntToConst.Name);

                        if (pntToConst.JointConstraintNames.Contains(constName)) continue;
                        if (!pntToConst.AddJointConstraint(constName)) // Failed
                        {
                            endMessages.AppendLine(pntToConst.Name);
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
                if (endMessages.Length != 0)
                    OnMessage("Could not add the new constraint to the following joints", endMessages.ToString());
            }
        }

        private DelegateCommand _deleteConstraintButtonCommand;
        public DelegateCommand DeleteConstraintButtonCommand =>
            _deleteConstraintButtonCommand ?? (_deleteConstraintButtonCommand = new DelegateCommand(ExecuteDeleteConstraintButtonCommand)).ObservesCanExecute(() => Constraint_IsEnabled);
        public async void ExecuteDeleteConstraintButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Deleting All Constraints from Selected Joints";

                    // Gets the selected points
                    var selPoints = S2KModel.SM.PointMan.GetSelected(true);

                    BusyOverlayBindings.I.SetDeterminate("Deleting Constraints", "Joint");
                    for (int index = 0; index < selPoints.Count; index++)
                    {
                        SapPoint pnt = selPoints[index];
                        BusyOverlayBindings.I.UpdateProgress(index, selPoints.Count, pnt.Name);

                        if (!pnt.RemoveAllJointConstraints())
                            endMessages.AppendLine(pnt.Name);
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
                if (endMessages.Length != 0)
                    OnMessage("Could not add the constraints from the following joints", endMessages.ToString());
            }
        }

        #endregion
    }

    public class ConstraintMonitor : MonitorStatusBase
    {
        public ConstraintMonitor() : base("C", "Constraint")
        {
        }

        public override Action MonitorAction
        {
            get
            {
                // The async body
                Action work = () =>
                {
                    do
                    {
                        if (StopToken.IsCancellationRequested) return;

                        // Gets the selected points
                        var selPoints = S2KModel.SM.PointMan.GetSelected();

                        // Gets their constraints
                        foreach (SapPoint pnt in selPoints)
                        {
                            foreach (string item in pnt.JointConstraintNames)
                            {
                            }
                        }

                        CurrentMonitorData = selPoints;

                        Thread.Sleep(Properties.Settings.Default.MonitorSleep);
                    } while (true);
                };
                return work;
            }
        }
    }

    public class JointConstraintMonitorData
    {
        private JointConstraintDef _constraintDef;

        public JointConstraintMonitorData(string inConstraintName)
        {
            ConstraintName = inConstraintName;
        }

        public string ConstraintName { get; }
        public TriState SelPointHasIt { get; set; } = TriState.NotSet;
        public TriState AllPointsHaveIt { get; set; } = TriState.False;

        public JointConstraintDef ConstraintDef
        {
            get
            {
                if (_constraintDef == null)
                {
                    _constraintDef = S2KModel.SM.JointConstraintMan.GetJointConstraintDef(ConstraintName, true);
                }

                return _constraintDef;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is JointConstraintMonitorData cObj)
                if (ConstraintName == cObj.ConstraintName)
                    return true;
            return false;
        }

        public override int GetHashCode()
        {
            return ConstraintName.GetHashCode();
        }
    }
}