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
using System.Windows.Navigation;
using System.Windows.Shapes;
using BaseWPFLibrary.Bindings;

namespace BaseWPFLibrary
{
    /// <summary>
    /// Interaction logic for MessageOverlay.xaml
    /// </summary>
    public partial class MessageOverlay : UserControl
    {
        public MessageOverlay()
        {
            InitializeComponent();
        }

        public Visibility OverlayVisibility
        {
            get => MessageOverlayBindings.I.OverlayVisibility;
            set => MessageOverlayBindings.I.OverlayVisibility = value;
        }

        public string Title
        {
            get => MessageOverlayBindings.I.Title;
            set => MessageOverlayBindings.I.Title = value;
        }

        public string MessageText
        {
            get => MessageOverlayBindings.I.MessageText;
            set => MessageOverlayBindings.I.MessageText = value;
        }

        public void HideOverlayAndReset()
        {
            MessageOverlayBindings.I.HideOverlayAndReset();
        }
        public void ShowOverlay(string title, string message)
        {
            MessageOverlayBindings.I.ShowOverlay(title, message);
        }
        public void ShowOverlay(string title, StringBuilder stringBuilder)
        {
            MessageOverlayBindings.I.ShowOverlay(title, stringBuilder);
        }

        private void MessageOverlayGrid_CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(MessageOverlayBindings.I.MessageText);
        }

        private void MessageOverlayGrid_CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageOverlayBindings.I.HideOverlayAndReset();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Binding binding = new Binding("OverlayVisibility");
            SetBinding(VisibilityProperty, binding);
        }
    }
}
