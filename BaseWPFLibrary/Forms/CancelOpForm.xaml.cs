using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Others;

namespace BaseWPFLibrary.Forms
{
    /// <summary>
    /// Interaction logic for CancelOpForm.xaml
    /// </summary>
    public partial class CancelOpForm : Window
    {
        private readonly Window owner;

        public CancelOpForm(Window inOwner)
        {
            InitializeComponent();
            CancelFormBindings.Start();

            CancelFormBindings.I.TitleText = "Cancel Operation";
            CancelFormBindings.I.MessageText = "Message";
            CancelFormBindings.I.ButtonCaption = "Cancel";

            tokenSource = new CancellationTokenSource();

            // Makes it impossible for the owner to get focus
            owner = inOwner;
            owner.GotFocus += Owner_GotFocus;
        }

        private void Owner_GotFocus(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        public CancelOpForm(Window inOwner, string message, string title = null) : this(inOwner)
        {
            if (!string.IsNullOrWhiteSpace(title)) CancelFormBindings.I.TitleText = title;
            CancelFormBindings.I.MessageText = message;
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            DataContext = CancelFormBindings.I;

            try
            {
                // Gets the monitor of the parent window
                IntPtr windowHandle = new WindowInteropHelper(owner).Handle;
                IntPtr monitorHandle = PInvokeWrappers.MonitorFromWindow(windowHandle, PInvokeWrappers.MONITOR_DEFAULTTONEAREST);

                PInvokeWrappers.MonitorInfoEx mon_info = new PInvokeWrappers.MonitorInfoEx();
                mon_info.Size = (int)Marshal.SizeOf(mon_info);
                PInvokeWrappers.GetMonitorInfo(monitorHandle, ref mon_info);

                Left = 30;
                Top = (mon_info.WorkArea.Bottom - Height) / 2;
            }
            catch (Exception)
            {
                // Moves the window
                Left = 30;
                Top = (SystemParameters.WorkArea.Height - Height) / 2;
            }
        }

        public string MessageText
        {
            get => CancelFormBindings.I.MessageText;
            set => CancelFormBindings.I.MessageText = value;
        }
        public string TitleText
        {
            get => CancelFormBindings.I.TitleText;
            set => CancelFormBindings.I.TitleText = value;
        }
        public string ButtonCaption
        {
            get => CancelFormBindings.I.ButtonCaption;
            set => CancelFormBindings.I.ButtonCaption = value;
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

    public class CancelFormBindings : BindableSingleton<CancelFormBindings>
    {
        private string _MessageText;public string MessageText { get => _MessageText; set => SetProperty(ref _MessageText, value); }
        private string _TitleText;public string TitleText { get => _TitleText; set => SetProperty(ref _TitleText, value); }
        private string _ButtonCaption;public string ButtonCaption { get => _ButtonCaption; set => SetProperty(ref _ButtonCaption, value); }

        public override void SetOrReset()
        {
        }
    }
}
