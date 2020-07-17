using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class BreakFrameBindings : BindableSingleton<BreakFrameBindings>
    {
        private BreakFrameBindings(){}
        public override void SetOrReset()
        {
            BodyConstraintTypeRadioButton_IsChecked = true;
            EqualConstraintTypeRadioButton_IsChecked = false;
            LocalConstraintTypeRadioButton_IsChecked = false;

            ConstraintPrefix_Text = "";

            U1CheckBox_IsChecked = true;
            U2CheckBox_IsChecked = true;
            U3CheckBox_IsChecked = true;
            R1CheckBox_IsChecked = true;
            R2CheckBox_IsChecked = true;
            R3CheckBox_IsChecked = true;

            ClosestPointAddConstraint_IsChecked = true;

            
        }

        private bool _BodyConstraintTypeRadioButton_IsChecked;public bool BodyConstraintTypeRadioButton_IsChecked { get => _BodyConstraintTypeRadioButton_IsChecked; 
            set
            {
                SetProperty(ref _BodyConstraintTypeRadioButton_IsChecked, value);
                UpdateConstraintName_Label();
            }
        }

        private bool _EqualConstraintTypeRadioButton_IsChecked;public bool EqualConstraintTypeRadioButton_IsChecked { get => _EqualConstraintTypeRadioButton_IsChecked; 
            set
            {
                SetProperty(ref _EqualConstraintTypeRadioButton_IsChecked, value);
                UpdateConstraintName_Label();
            }
        }

        private bool _LocalConstraintTypeRadioButton_IsChecked;public bool LocalConstraintTypeRadioButton_IsChecked { get => _LocalConstraintTypeRadioButton_IsChecked; 
            set
            {
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

        private string _ConstraintName_Label;public string ConstraintName_Label { get => _ConstraintName_Label; set => SetProperty(ref _ConstraintName_Label, value); }

        private string _ConstraintPrefix_Text;public string ConstraintPrefix_Text { get => _ConstraintPrefix_Text; 
            set
            {
                SetProperty(ref _ConstraintPrefix_Text, value);
                UpdateConstraintName_Label();
            }
        }

        private bool _U1CheckBox_IsChecked;public bool U1CheckBox_IsChecked { get => _U1CheckBox_IsChecked; 
            set
            {
                SetProperty(ref _U1CheckBox_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _U2CheckBox_IsChecked;public bool U2CheckBox_IsChecked { get => _U2CheckBox_IsChecked; 
            set
            {
                SetProperty(ref _U2CheckBox_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _U3CheckBox_IsChecked;public bool U3CheckBox_IsChecked { get => _U3CheckBox_IsChecked; 
            set
            {
                SetProperty(ref _U3CheckBox_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _R1CheckBox_IsChecked;public bool R1CheckBox_IsChecked { get => _R1CheckBox_IsChecked; 
            set
            {
                SetProperty(ref _R1CheckBox_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _R2CheckBox_IsChecked;public bool R2CheckBox_IsChecked { get => _R2CheckBox_IsChecked; 
            set
            {
                SetProperty(ref _R2CheckBox_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _R3CheckBox_IsChecked;public bool R3CheckBox_IsChecked { get => _R3CheckBox_IsChecked; 
            set
            {
                SetProperty(ref _R3CheckBox_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private string _U1CheckBox_Label;public string U1CheckBox_Label { get => _U1CheckBox_Label; set => SetProperty(ref _U1CheckBox_Label, value); }

        private string _U2CheckBox_Label;public string U2CheckBox_Label { get => _U2CheckBox_Label; set => SetProperty(ref _U2CheckBox_Label, value); }

        private string _U3CheckBox_Label;public string U3CheckBox_Label { get => _U3CheckBox_Label; set => SetProperty(ref _U3CheckBox_Label, value); }

        private string _R1CheckBox_Label;public string R1CheckBox_Label { get => _R1CheckBox_Label; set => SetProperty(ref _R1CheckBox_Label, value); }

        private string _R2CheckBox_Label;public string R2CheckBox_Label { get => _R2CheckBox_Label; set => SetProperty(ref _R2CheckBox_Label, value); }

        private string _R3CheckBox_Label;public string R3CheckBox_Label { get => _R3CheckBox_Label; set => SetProperty(ref _R3CheckBox_Label, value); }

        private bool _ClosestPointAddConstraint_IsChecked;public bool ClosestPointAddConstraint_IsChecked { get => _ClosestPointAddConstraint_IsChecked; 
            set
            {
                SetProperty(ref _ClosestPointAddConstraint_IsChecked, value);
                BreakFrameClosestPointButtonUpdate_IsEnabled();
            }
        }

        private bool _BreakFrameClosestPointButton_IsEnabled;public bool BreakFrameClosestPointButton_IsEnabled { get => _BreakFrameClosestPointButton_IsEnabled; set => SetProperty(ref _BreakFrameClosestPointButton_IsEnabled, value); }

        public void BreakFrameClosestPointButtonUpdate_IsEnabled()
        {
            if (ClosestPointAddConstraint_IsChecked)
                if (!U1CheckBox_IsChecked && !U2CheckBox_IsChecked && !U3CheckBox_IsChecked &&
                    !R1CheckBox_IsChecked && !R2CheckBox_IsChecked && !R3CheckBox_IsChecked)
                {
                    BreakFrameClosestPointButton_IsEnabled = false;
                    return;
                }

            BreakFrameClosestPointButton_IsEnabled = true;
            return;
        }
    }
}