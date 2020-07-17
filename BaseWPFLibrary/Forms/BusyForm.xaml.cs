using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Others;

namespace BaseWPFLibrary.Forms
{
    /// <summary>
    /// Interaction logic for BusyForm.xaml
    /// </summary>
    public partial class BusyForm : Window
    {
        private readonly Window owner;

        public BusyForm(Window inOwner)
        {
            InitializeComponent();

            // Creates the binging object
            BusyFormBindings.Start();

            // Makes it impossible for the owner to get focus
            owner = inOwner;
            owner.GotFocus += Owner_GotFocus;
        }

        private void Owner_GotFocus(object sender, RoutedEventArgs e)
        {
            Focus();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            DataContext = BusyFormBindings.I;
        }

        public event EventHandler OperationCanceled;
        protected virtual void OnEventHandler(EventArgs e)
        {
            // If there is a subscriber, call it
            OperationCanceled?.Invoke(this, e);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            // Marks the token as cancelled
            tokenSource.Cancel();
            OnEventHandler(new EventArgs());
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;

                Close();
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private CancellationTokenSource tokenSource;
        public void CancelOperation()
        {
            tokenSource.Cancel();
        }
        public CancellationToken Token => tokenSource.Token;

        #region Async Caller Helpers 
        public void ResetForm()
        {
            tokenSource = new CancellationTokenSource();
            BusyFormBindings.I.ButtonCaption = "Cancel";
            BusyFormBindings.I.ButtonVisibility = Visibility.Collapsed;
            BusyFormBindings.I.TitleText = "Working ...";
            BusyFormBindings.I.ProgressIsIntermediate = false;
            BusyFormBindings.I.ProgressCurrentProgress = 0;
            BusyFormBindings.I.MessageText = null;
            BusyFormBindings.I.ElementType = null;
            BusyFormBindings.I.CurrentElementName = null;
            BusyFormBindings.I.CurrentElementVisibility = Visibility.Collapsed;
        }

        public string ButtonCaption
        {
            get => BusyFormBindings.I.ButtonCaption;
            set => BusyFormBindings.I.ButtonCaption = value;
        }
        public Visibility ButtonVisibility
        {
            get => BusyFormBindings.I.ButtonVisibility;
            set => BusyFormBindings.I.ButtonVisibility = value;
        }
        public string TitleText
        {
            get => BusyFormBindings.I.TitleText;
            set => BusyFormBindings.I.TitleText = value;
        }
        public bool ProgressIsIntermediate
        {
            get => BusyFormBindings.I.ProgressIsIntermediate;
            set => BusyFormBindings.I.ProgressIsIntermediate = value;
        }
        public int ProgressCurrentProgress
        {
            get => BusyFormBindings.I.ProgressCurrentProgress;
            set => BusyFormBindings.I.ProgressCurrentProgress = value;
        }
        public void UpdateCurrentProgress(int current, int max)
        {
            ProgressCurrentProgress = EmasaWPFLibraryStaticMethods.ProgressPercent(current, max);
        }
        public void UpdateCurrentProgress(long current, long max)
        {
            ProgressCurrentProgress = EmasaWPFLibraryStaticMethods.ProgressPercent(current, max);
        }
        public string MessageText
        {
            get => BusyFormBindings.I.MessageText;
            set => BusyFormBindings.I.MessageText = value;
        }
        public string ElementType
        {
            get => BusyFormBindings.I.ElementType;
            set 
            { 
                BusyFormBindings.I.ElementType = value;
                CurrentElementVisibility = Visibility.Visible;
            }
        }
        public string CurrentElementName
        {
            get => BusyFormBindings.I.CurrentElementName;
            set 
            {
                BusyFormBindings.I.CurrentElementName = value;
                CurrentElementVisibility = Visibility.Visible;
            }
        }
        public Visibility CurrentElementVisibility
        {
            get => BusyFormBindings.I.CurrentElementVisibility;
            set => BusyFormBindings.I.CurrentElementVisibility = value;
        }

        public void Show_FromOtherThread()
        {
            Dispatcher.Invoke(() => {
                Show();
                Focus();
            });
        }
        public void ShowDialog_OtherThread()
        {
            Dispatcher.Invoke(() => {
                ShowDialog();
            });
        }
        public void Close_FromOtherThread()
        {
            Dispatcher.InvokeAsync(() => { Close(); });
        }
        public void Hide_FromOtherThreat()
        {
            Dispatcher.InvokeAsync(() => { Hide(); });
        }
        #endregion
    }

    public class BusyFormBindings : BindableSingleton<BusyFormBindings>
    {
        private string _TitleText;public string TitleText { get => _TitleText; set => SetProperty(ref _TitleText, value); }

        private string _MessageText;public string MessageText { get => _MessageText; set => SetProperty(ref _MessageText, value); }

        private string _ElementType;public string ElementType { get => _ElementType; set => SetProperty(ref _ElementType, value); }
        private string _CurrentElementName;public string CurrentElementName { get => _CurrentElementName; set => SetProperty(ref _CurrentElementName, value); }
        private Visibility _CurrentElementVisibility;public Visibility CurrentElementVisibility { get => _CurrentElementVisibility; set => SetProperty(ref _CurrentElementVisibility, value); }

        private Visibility _ButtonVisibility;public Visibility ButtonVisibility { get => _ButtonVisibility; set => SetProperty(ref _ButtonVisibility, value); }
        private string _ButtonCaption;public string ButtonCaption { get => _ButtonCaption; set => SetProperty(ref _ButtonCaption, value); }

        private bool _ProgressIsIntermediate;public bool ProgressIsIntermediate { get => _ProgressIsIntermediate; set => SetProperty(ref _ProgressIsIntermediate, value); }
        private int _ProgressCurrentProgress;public int ProgressCurrentProgress { get => _ProgressCurrentProgress; set => SetProperty(ref _ProgressCurrentProgress, value); }

        public override void SetOrReset()
        {
        }
    }
}
