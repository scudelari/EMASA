using System.Text;
using System.Windows;

namespace BaseWPFLibrary.Bindings
{
    internal class MessageOverlayBindings : BindableSingleton<MessageOverlayBindings>
    {
        private MessageOverlayBindings() { }
        public override void SetOrReset()
        {
            HideOverlayAndReset();
        }

        private Visibility _overlayVisibility;
        public Visibility OverlayVisibility
        {
            get { lock (_padLock) { return _overlayVisibility; } }
            set { lock (_padLock) { SetProperty(ref _overlayVisibility, value); } }
        }

        private string _title;
        public string Title
        {
            get { lock (_padLock) { return _title; } }
            set { lock (_padLock) { SetProperty(ref _title, value); } }
        }

        private string _messageText;
        public string MessageText
        {
            get { lock (_padLock) { return _messageText; } }
            set { lock (_padLock) { SetProperty(ref _messageText, value); } }
        }

        public void HideOverlayAndReset()
        {
            OverlayVisibility = Visibility.Collapsed;

            Title = "Results";
            MessageText = "";
        }

        public void ShowOverlay(string title, string message)
        {
            OverlayVisibility = Visibility.Visible;

            Title = title;
            MessageText = message;
        }
        public void ShowOverlay(string title, StringBuilder message)
        {
            OverlayVisibility = Visibility.Visible;

            Title = title;
            MessageText = message.ToString();
        }
    }
}
