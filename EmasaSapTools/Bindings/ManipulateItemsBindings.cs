using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class ManipulateItemsBindings : BindableSingleton<ManipulateItemsBindings>
    {
        private ManipulateItemsBindings(){}
        public override void SetOrReset()
        {
            #region Break Frames
            BreakFrame_BodyConstraintTypeRadioButton_IsChecked = true;
            BreakFrame_EqualConstraintTypeRadioButton_IsChecked = false;
            BreakFrame_LocalConstraintTypeRadioButton_IsChecked = false;

            BreakFrame_ConstraintPrefix_Text = "";

            BreakFrame_U1CheckBox_IsChecked = true;
            BreakFrame_U2CheckBox_IsChecked = true;
            BreakFrame_U3CheckBox_IsChecked = true;
            BreakFrame_R1CheckBox_IsChecked = true;
            BreakFrame_R2CheckBox_IsChecked = true;
            BreakFrame_R3CheckBox_IsChecked = true;

            BreakFrame_ClosestPointAddConstraint_IsChecked = true; 
            #endregion
        }

        #region Break Frame
        private bool _breakFrameBodyConstraintTypeRadioButton_IsChecked;
        public bool BreakFrame_BodyConstraintTypeRadioButton_IsChecked
        {
            get => _breakFrameBodyConstraintTypeRadioButton_IsChecked;
            set
            {
                SetProperty(ref _breakFrameBodyConstraintTypeRadioButton_IsChecked, value);
                BreakFrame_UpdateConstraintName_Label();
            }
        }

        private bool _breakFrameEqualConstraintTypeRadioButton_IsChecked;
        public bool BreakFrame_EqualConstraintTypeRadioButton_IsChecked
        {
            get => _breakFrameEqualConstraintTypeRadioButton_IsChecked;
            set
            {
                SetProperty(ref _breakFrameEqualConstraintTypeRadioButton_IsChecked, value);
                BreakFrame_UpdateConstraintName_Label();
            }
        }

        private bool _breakFrameLocalConstraintTypeRadioButton_IsChecked;
        public bool BreakFrame_LocalConstraintTypeRadioButton_IsChecked
        {
            get => _breakFrameLocalConstraintTypeRadioButton_IsChecked;
            set
            {
                if (value)
                {
                    BreakFrame_U1CheckBox_Label = "U1";
                    BreakFrame_U2CheckBox_Label = "U2";
                    BreakFrame_U3CheckBox_Label = "U3";
                    BreakFrame_R1CheckBox_Label = "R1";
                    BreakFrame_R2CheckBox_Label = "R2";
                    BreakFrame_R3CheckBox_Label = "R3";
                }
                else
                {
                    BreakFrame_U1CheckBox_Label = "TX";
                    BreakFrame_U2CheckBox_Label = "TY";
                    BreakFrame_U3CheckBox_Label = "TZ";
                    BreakFrame_R1CheckBox_Label = "RX";
                    BreakFrame_R2CheckBox_Label = "RY";
                    BreakFrame_R3CheckBox_Label = "RZ";
                }

                SetProperty(ref _breakFrameLocalConstraintTypeRadioButton_IsChecked, value);
                BreakFrame_UpdateConstraintName_Label();
            }
        }

        public string BreakFrame_OutConstraintName { get; private set; }
        private void BreakFrame_UpdateConstraintName_Label()
        {
            string typePrefix = "";

            if (BreakFrame_BodyConstraintTypeRadioButton_IsChecked) typePrefix = "B_";
            if (BreakFrame_EqualConstraintTypeRadioButton_IsChecked) typePrefix = "E_";
            if (BreakFrame_LocalConstraintTypeRadioButton_IsChecked) typePrefix = "L_";

            BreakFrame_OutConstraintName = $"{typePrefix}{BreakFrame_ConstraintPrefix_Text}";
            BreakFrame_ConstraintName_Label = BreakFrame_OutConstraintName + "<RandomId>";
        }

        private string _breakFrameConstraintName_Label;
        public string BreakFrame_ConstraintName_Label
        {
            get => _breakFrameConstraintName_Label;
            set => SetProperty(ref _breakFrameConstraintName_Label, value);
        }

        private string _breakFrameConstraintPrefix_Text;
        public string BreakFrame_ConstraintPrefix_Text
        {
            get => _breakFrameConstraintPrefix_Text;
            set
            {
                SetProperty(ref _breakFrameConstraintPrefix_Text, value);
                BreakFrame_UpdateConstraintName_Label();
            }
        }

        private bool _breakFrameU1CheckBox_IsChecked;
        public bool BreakFrame_U1CheckBox_IsChecked
        {
            get => _breakFrameU1CheckBox_IsChecked;
            set
            {
                SetProperty(ref _breakFrameU1CheckBox_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _breakFrameU2CheckBox_IsChecked;
        public bool BreakFrame_U2CheckBox_IsChecked
        {
            get => _breakFrameU2CheckBox_IsChecked;
            set
            {
                SetProperty(ref _breakFrameU2CheckBox_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _breakFrameU3CheckBox_IsChecked;
        public bool BreakFrame_U3CheckBox_IsChecked
        {
            get => _breakFrameU3CheckBox_IsChecked;
            set
            {
                SetProperty(ref _breakFrameU3CheckBox_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _breakFrameR1CheckBox_IsChecked;
        public bool BreakFrame_R1CheckBox_IsChecked
        {
            get => _breakFrameR1CheckBox_IsChecked;
            set
            {
                SetProperty(ref _breakFrameR1CheckBox_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _breakFrameR2CheckBox_IsChecked;
        public bool BreakFrame_R2CheckBox_IsChecked
        {
            get => _breakFrameR2CheckBox_IsChecked;
            set
            {
                SetProperty(ref _breakFrameR2CheckBox_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _breakFrameR3CheckBox_IsChecked;
        public bool BreakFrame_R3CheckBox_IsChecked
        {
            get => _breakFrameR3CheckBox_IsChecked;
            set
            {
                SetProperty(ref _breakFrameR3CheckBox_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private string _breakFrameU1CheckBox_Label;
        public string BreakFrame_U1CheckBox_Label
        {
            get => _breakFrameU1CheckBox_Label;
            set => SetProperty(ref _breakFrameU1CheckBox_Label, value);
        }

        private string _breakFrameU2CheckBox_Label;
        public string BreakFrame_U2CheckBox_Label
        {
            get => _breakFrameU2CheckBox_Label;
            set => SetProperty(ref _breakFrameU2CheckBox_Label, value);
        }

        private string _breakFrameU3CheckBox_Label;
        public string BreakFrame_U3CheckBox_Label
        {
            get => _breakFrameU3CheckBox_Label;
            set => SetProperty(ref _breakFrameU3CheckBox_Label, value);
        }

        private string _breakFrameR1CheckBox_Label;
        public string BreakFrame_R1CheckBox_Label
        {
            get => _breakFrameR1CheckBox_Label;
            set => SetProperty(ref _breakFrameR1CheckBox_Label, value);
        }

        private string _breakFrameR2CheckBox_Label;
        public string BreakFrame_R2CheckBox_Label
        {
            get => _breakFrameR2CheckBox_Label;
            set => SetProperty(ref _breakFrameR2CheckBox_Label, value);
        }

        private string _breakFrameR3CheckBox_Label;
        public string BreakFrame_R3CheckBox_Label
        {
            get => _breakFrameR3CheckBox_Label;
            set => SetProperty(ref _breakFrameR3CheckBox_Label, value);
        }

        private bool _breakFrameClosestPointAddConstraint_IsChecked;
        public bool BreakFrame_ClosestPointAddConstraint_IsChecked
        {
            get => _breakFrameClosestPointAddConstraint_IsChecked;
            set
            {
                SetProperty(ref _breakFrameClosestPointAddConstraint_IsChecked, value);
                BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _breakFrame_ClosestPointButton_IsEnabled;
        public bool BreakFrame_ClosestPointButton_IsEnabled
        {
            get => _breakFrame_ClosestPointButton_IsEnabled;
            set => SetProperty(ref _breakFrame_ClosestPointButton_IsEnabled, value);
        }

        private void BreakFrame_Helper_ClosestPointButtonUpdate_IsEnabled()
        {
            if (BreakFrame_ClosestPointAddConstraint_IsChecked)
                if (!BreakFrame_U1CheckBox_IsChecked && !BreakFrame_U2CheckBox_IsChecked && !BreakFrame_U3CheckBox_IsChecked &&
                    !BreakFrame_R1CheckBox_IsChecked && !BreakFrame_R2CheckBox_IsChecked && !BreakFrame_R3CheckBox_IsChecked)
                {
                    BreakFrame_ClosestPointButton_IsEnabled = false;
                    return;
                }

            BreakFrame_ClosestPointButton_IsEnabled = true;
            return;
        }

        private DelegateCommand _breakFrameCommand;
        public DelegateCommand BreakFrameCommand =>
            _breakFrameCommand ?? (_breakFrameCommand = new DelegateCommand(ExecuteBreakFrameCommand));
        async void ExecuteBreakFrameCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                OnBeginCommand();

                void lf_Work()
                {

                    BusyOverlayBindings.I.Title = $"Breaking the Frames Relative Closest to the Selected Joints";
                    var sPoints = S2KModel.SM.PointMan.GetSelected(true);

                    if (sPoints.Count != 1) throw new S2KHelperException("You must select one - and only one point. It is the point closest to which the frame will be broken.");

                    var sFrames = S2KModel.SM.FrameMan.GetSelected(true);

                    if (sFrames.Count != 1) throw new S2KHelperException("You must select one - and only one frame. It is the frame that will be broken.");

                    BusyOverlayBindings.I.SetIndeterminate($"Breaking the Frame {sFrames.First().Name}.");

                    // Puts a point in the frame that is closest to the selected point
                    SapPoint onFrame = sFrames[0].AddPointInFrameClosestToGiven(sPoints[0], $"KH_Link_{S2KStaticMethods.UniqueName(6)}");

                    // Checks if is one of the extreme frames
                    if (sFrames[0].IsPointIJ(onFrame)) throw new S2KHelperException("The frame will not be broken as the closest point is one of the end joints of the frame.");

                    // Breaks the frame at the either the given or added point - depends on the results of the AddPointInFrameClosestToGiven function.
                    List<SapFrame> sapFrames = null;
                    try
                    {
                        sapFrames = sFrames[0].DivideAtIntersectPoint(onFrame, "P");
                    }
                    catch (Exception)
                    {
                        throw new S2KHelperException("Cannot break the frame at the given point. Probably the closest point is too close (considering Sap2000 Merge Tolerance) to the frame as to prevent a point to be added all the while being too far to be captured as being on the line to break the frame. Try reducing Sap2000's Merge Tolerance.");
                    }

                    // Should we add a constraint?
                    if (BreakFrame_ClosestPointAddConstraint_IsChecked)
                    {
                        BusyOverlayBindings.I.SetIndeterminate($"Adding a joint constraint.");

                        bool[] constVals =
                        {
                        BreakFrame_U1CheckBox_IsChecked,
                        BreakFrame_U2CheckBox_IsChecked,
                        BreakFrame_U3CheckBox_IsChecked,
                        BreakFrame_R1CheckBox_IsChecked,
                        BreakFrame_R2CheckBox_IsChecked,
                        BreakFrame_R3CheckBox_IsChecked
                        };

                        string constName = BreakFrame_OutConstraintName + S2KStaticMethods.UniqueName(10);

                        if (BreakFrame_BodyConstraintTypeRadioButton_IsChecked)
                        {
                            // Creates a joint constraint
                            if (!S2KModel.SM.JointConstraintMan.SetBodyConstraint(constName, constVals)) throw new S2KHelperException($"Could not create constraint called {constName}.");
                        }
                        else if (BreakFrame_LocalConstraintTypeRadioButton_IsChecked)
                        {
                            // Creates a joint constraint
                            if (!S2KModel.SM.JointConstraintMan.SetLocalConstraint(constName, constVals)) throw new S2KHelperException($"Could not create constraint called {constName}.");
                        }
                        else if (BreakFrame_EqualConstraintTypeRadioButton_IsChecked)
                        {
                            // Creates a joint constraint
                            if (!S2KModel.SM.JointConstraintMan.SetEqualConstraint(constName, constVals)) throw new S2KHelperException($"Could not create constraint called {constName}.");
                        }

                        sPoints[0].AddJointConstraint(constName, false);
                        onFrame.AddJointConstraint(constName, false);
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
                if (endMessages.Length != 0) OnMessage("Could not break the selected frame close to the selected joint.", endMessages.ToString());
            }
        }
        #endregion
    }
}