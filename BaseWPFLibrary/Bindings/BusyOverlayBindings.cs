using System;
using System.Threading;
using System.Windows;
using BaseWPFLibrary.Others;
using Prism.Commands;

namespace BaseWPFLibrary.Bindings
{
    public class BusyOverlayBindings : BindableSingleton<BusyOverlayBindings>
    {
        private BusyOverlayBindings()
        {
        }

        public override void SetOrReset()
        {
            LongTextHeight = 200d;

            // Resets all data
            Title = "Working ...";
            MessageText = "Busy.";
            ElementType = "";
            CurrentElementName = "";
            CurrentElementVisibility = Visibility.Collapsed;
            ButtonVisibility = Visibility.Collapsed;
            ButtonCaption = "Cancel";
            ProgressIsIndeterminate = true;
            ProgressCurrentProgress = 0;
            ProgressTextVisibility = Visibility.Collapsed;

            // Including the cancellation source
            _tokenSource = new CancellationTokenSource();

            LongReport_Visibility = Visibility.Collapsed;
            LongReport_Text = "";

            AutomationWarning_Visibility = Visibility.Collapsed;
        }

        // Special link to the FrameworkElement
        public BusyOverlay OverlayElement => (BusyOverlay)GetReferencedFrameworkElement("UserControlInternalName_BusyOverlay");

        private string _title;
        public string Title
        {
            get
            {
                lock (_padLock)
                {
                    return _title;
                }
            }
            set
            {
                lock (_padLock)
                {
                    SetProperty(ref _title, value);
                }
            }
        }

        private string _messageText;
        public string MessageText
        {
            get
            {
                lock (_padLock)
                {
                    return _messageText;
                }
            }
            set
            {
                lock (_padLock)
                {
                    SetProperty(ref _messageText, value);
                }
            }
        }

        private string _currentElementName;
        public string CurrentElementName
        {
            get { lock (_padLock) { return _currentElementName; } }
            set { lock (_padLock) { SetProperty(ref _currentElementName, value); } }
        }

        private string _elementType;
        public string ElementType
        {
            get { lock (_padLock) { return _elementType; } }
            set { lock (_padLock) { SetProperty(ref _elementType, value); } }
        }

        private Visibility _currentElementVisibility;
        public Visibility CurrentElementVisibility
        {
            get { lock (_padLock) { return _currentElementVisibility; } }
            set { lock (_padLock) { SetProperty(ref _currentElementVisibility, value); } }
        }

        private Visibility _buttonVisibility;
        public Visibility ButtonVisibility
        {
            get { lock (_padLock) { return _buttonVisibility; } }
            set { lock (_padLock) { SetProperty(ref _buttonVisibility, value); } }
        }

        private string _buttonCaption;
        public string ButtonCaption
        {
            get { lock (_padLock) { return _buttonCaption; } }
            set { lock (_padLock) { SetProperty(ref _buttonCaption, value); } }
        }

        private bool _buttonIsEnabled;
        public bool ButtonIsEnabled
        {
            get { lock (_padLock) { return _buttonIsEnabled; } }
            set { lock (_padLock) { SetProperty(ref _buttonIsEnabled, value); } }
        }

        private bool _progressIsIndeterminate;
        public bool ProgressIsIndeterminate
        {
            get { lock (_padLock) { return _progressIsIndeterminate; } }
            set { lock (_padLock) { SetProperty(ref _progressIsIndeterminate, value); } }
        }

        private int _progressCurrentProgress;
        public int ProgressCurrentProgress
        {
            get { lock (_padLock) { return _progressCurrentProgress; } }
            set
            {
                if (ProgressCurrentProgress != value) 
                    lock (_padLock) { SetProperty(ref _progressCurrentProgress, value); }
            }
        }

        private Visibility _progressTextVisibility;
        public Visibility ProgressTextVisibility
        {
            get { lock (_padLock) { return _progressTextVisibility; } }
            set { lock (_padLock) { SetProperty(ref _progressTextVisibility, value); } }
        }


        public void HideOverlayAndReset()
        {
            // Changes the visibility directly to the element itself
            OverlayElement.Visibility = Visibility.Collapsed;

            // Resets all data
            Title = "Working ...";
            MessageText = "Busy.";
            ElementType = "";
            CurrentElementName = "";
            CurrentElementVisibility = Visibility.Collapsed;
            ButtonVisibility = Visibility.Collapsed;
            ButtonCaption = "Cancel";
            ProgressIsIndeterminate = true;
            ProgressCurrentProgress = 0;
            ProgressTextVisibility = Visibility.Collapsed;

            // Including the cancellation source
            _tokenSource = new CancellationTokenSource();

            LongReport_Visibility = Visibility.Collapsed;
            LongReport_Text = "";

            AutomationWarning_Visibility = Visibility.Collapsed;
        }
        public void ShowOverlay()
        {
            // Changes the visibility directly to the element itself
            OverlayElement.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Sets the basic information of the Overlay.
        /// </summary>
        /// <param name="inMessage">The message given to the user.</param>
        /// <param name="inTitle">The title. If null, the default title "Working ..." will be used.</param>
        /// <param name="inElementType">The type of elements. If null, the type of elements will be hidden.</param>
        /// <param name="inButtonCaption">The button caption. If null, the button will be hidden.</param>
        public void SetBasic(string inMessage, string inTitle = null, string inElementType = null, string inButtonCaption = null)
        {
            MessageText = inMessage;

            if (!string.IsNullOrEmpty(inTitle)) Title = inTitle;

            if (!string.IsNullOrEmpty(inButtonCaption))
            {
                ButtonCaption = inButtonCaption;
                ButtonVisibility = Visibility.Visible;
            }
            else
            {
                ButtonVisibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(inElementType))
            {
                ElementType = inElementType;
                CurrentElementVisibility = Visibility.Visible;
            }
            else
            {
                CurrentElementVisibility = Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Sets the basic information of the Overlay.
        /// </summary>
        /// <param name="inTitle">The title.</param>
        /// <param name="inButtonCaption">The button caption. If null, the button will be hidden.</param>
        public void SetBasic(string inTitle , string inButtonCaption = null)
        {
            if (!string.IsNullOrEmpty(inTitle)) Title = inTitle;

            if (!string.IsNullOrEmpty(inButtonCaption))
            {
                ButtonCaption = inButtonCaption;
                ButtonVisibility = Visibility.Visible;
            }
            else
            {
                ButtonVisibility = Visibility.Collapsed;
            }
        }

        public void SetDeterminate(string message = null, string elementType = null)
        {
            if (!string.IsNullOrEmpty(message)) MessageText = message;

            if (!string.IsNullOrEmpty(elementType))
            {
                ElementType = elementType;
                if (CurrentElementVisibility != Visibility.Visible) CurrentElementVisibility = Visibility.Visible;
            }
            else
            {
                if (CurrentElementVisibility != Visibility.Collapsed) CurrentElementVisibility = Visibility.Collapsed;
            }

            UpdateProgress(0, 1, null);
        }
        public void UpdateProgress(int current, int maximum, string currentName = null)
        {
            if (ProgressIsIndeterminate) ProgressIsIndeterminate = false;
            if (ProgressTextVisibility != Visibility.Visible) ProgressTextVisibility = Visibility.Visible;

            ProgressCurrentProgress = EmasaWPFLibraryStaticMethods.ProgressPercent(current, maximum);

            if (!string.IsNullOrEmpty(currentName))
            {
                CurrentElementName = currentName;
                if (CurrentElementVisibility != Visibility.Visible) CurrentElementVisibility = Visibility.Visible;
            }
        }
        public void UpdateProgress(long current, long maximum, string currentName = null)
        {
            if (ProgressIsIndeterminate) ProgressIsIndeterminate = false;
            if (ProgressTextVisibility != Visibility.Visible) ProgressTextVisibility = Visibility.Visible;
            ProgressCurrentProgress = EmasaWPFLibraryStaticMethods.ProgressPercent(current, maximum);

            if (!string.IsNullOrEmpty(currentName))
            {
                CurrentElementName = currentName;
                if (CurrentElementVisibility != Visibility.Visible) CurrentElementVisibility = Visibility.Visible;
            }
        }
        public void SetIndeterminate(string message = null)
        {
            if (!string.IsNullOrEmpty(message)) MessageText = message;

            if (!ProgressIsIndeterminate) ProgressIsIndeterminate = true;
            if (CurrentElementVisibility != Visibility.Collapsed) CurrentElementVisibility = Visibility.Collapsed;
            if (ProgressTextVisibility != Visibility.Collapsed) ProgressTextVisibility = Visibility.Collapsed;
        }
        public void Stop()
        {
            MessageText = "Busy.";
            if (ProgressIsIndeterminate) ProgressIsIndeterminate = false;
            ProgressCurrentProgress = 0;

            ElementType = "";
            CurrentElementName = "";
            CurrentElementVisibility = Visibility.Collapsed;
        }

        private CancellationTokenSource _tokenSource;
        public void CancelOperation()
        {
            _tokenSource.Cancel();
        }
        public CancellationToken Token => _tokenSource.Token;

        private Visibility _longReport_Visibility;
        public Visibility LongReport_Visibility
        {
            get { lock (_padLock) { return _longReport_Visibility; } }
            set { lock (_padLock) { SetProperty(ref _longReport_Visibility, value); } }
        }

        private string _longReport_Text;
        public string LongReport_Text
        {
            get { lock (_padLock) { return _longReport_Text; } }
            set { lock (_padLock) { SetProperty(ref _longReport_Text, value); } }
        }

        public void LongReport_AddLine(string line)
        {
            LongReport_Text += line + Environment.NewLine;
        }

        private Visibility _automationWarning_Visibility;
        public Visibility AutomationWarning_Visibility
        {
            get { lock (_padLock) { return _automationWarning_Visibility; } }
            set
            {
                lock (_padLock)
                {
                    switch (value)
                    {
                        case Visibility.Visible:
                            BoxWidth = 500d;
                            break;
                        case Visibility.Hidden:
                            break;
                        case Visibility.Collapsed:
                            BoxWidth = 500d;
                            break;
                        default:
                            break;
                    }
                    SetProperty(ref _automationWarning_Visibility, value);
                }
            }
        }

        #region Styles
        private double _boxWidth;
        public double BoxWidth
        {
            get => _boxWidth;
            set => SetProperty(ref _boxWidth, value);
        }

        private double _longTextHeight;
        public double LongTextHeight
        {
            get => _longTextHeight;
            set => SetProperty(ref _longTextHeight, value);
        }

        private Thickness _overlayMargin;
        public Thickness OverlayMargin
        {
            get => _overlayMargin;
            set => SetProperty(ref _overlayMargin, value);
        }
        #endregion
    }
}
