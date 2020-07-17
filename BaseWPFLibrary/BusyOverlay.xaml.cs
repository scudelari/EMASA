using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class BusyOverlay : UserControl
    {
        public BusyOverlay()
        {
            InitializeComponent();
            BusyOverlayBindings.Start(this);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BusyOverlayBindings.I.CancelOperation();
        }

        public Visibility OverlayVisibility {
            get => BusyOverlayBindings.I.OverlayVisibility;
            set => BusyOverlayBindings.I.OverlayVisibility = value;
        }

        public string Title
        {
            get => BusyOverlayBindings.I.Title;
            set => BusyOverlayBindings.I.Title = value;
        }

        public string MessageText
        {
            get => BusyOverlayBindings.I.MessageText;
            set => BusyOverlayBindings.I.MessageText = value;
        }

        public string ElementType
        {
            get => BusyOverlayBindings.I.ElementType;
            set => BusyOverlayBindings.I.ElementType = value;
        }

        public void HideOverlayAndReset()
        {
            BusyOverlayBindings.I.HideOverlayAndReset();
        }
        public void ShowOverlay()
        {
            BusyOverlayBindings.I.ShowOverlay();
        }
        public void SetBasic(string message, string title = null, string elementType = null, string buttonCaption = null)
        {
            BusyOverlayBindings.I.SetBasic(message, inTitle: title, inElementType: elementType, inButtonCaption: buttonCaption);
        }

        public void SetDeterminate(string message = null, string elementType = null)
        {
            BusyOverlayBindings.I.SetDeterminate(message: message, elementType: elementType);
        }
        public void UpdateProgress(int current, int maximum, string currentName = null)
        {
            BusyOverlayBindings.I.UpdateProgress(current, maximum, currentName: currentName);
        }
        public void UpdateProgress(long current, long maximum, string currentName = null)
        {
            BusyOverlayBindings.I.UpdateProgress(current, maximum, currentName: currentName);
        }
        public void SetIndeterminate(string message = null)
        {
            BusyOverlayBindings.I.SetIndeterminate(message: message);
        }
        public void Stop()
        {
            BusyOverlayBindings.I.Stop();
        }

        public void CancelOperation()
        {
            BusyOverlayBindings.I.CancelOperation();
        }
        public CancellationToken Token => BusyOverlayBindings.I.Token;
        public bool IsCancellationRequested => Token.IsCancellationRequested;

        public Visibility LongReport_Visibility
        {
            get => BusyOverlayBindings.I.LongReport_Visibility;
            set => BusyOverlayBindings.I.LongReport_Visibility = value;
        }
        public string LongReport_Text
        {
            get => BusyOverlayBindings.I.LongReport_Text;
            set => BusyOverlayBindings.I.LongReport_Text = value;
        }
        public void LongReport_AddLine(string line)
        {
            BusyOverlayBindings.I.LongReport_AddLine(line);
        }

        public Visibility AutomationWarning_Visibility
        {
            get => BusyOverlayBindings.I.AutomationWarning_Visibility;
            set => BusyOverlayBindings.I.AutomationWarning_Visibility = value;
        }

        private void UpdateSize(double newSize)
        {
            double newLongTextHeight = newSize;

            double margin = 100d;
            double redbar = 26d;
            double titlebar = 26d;
            double button = 30d + 2 * 5d;
            double progress = 30d + 2 * 10d;
            double textborderpadding = 2 * 5d + 2 * 5d;
            double messagetext = 14d;
            double elementtext = 14d + 5d;
            double longtextborder = 6d;
            double scrollmargin = 2 * 5d;

            newLongTextHeight -= 2 * margin;
            if (BusyOverlayBindings.I.AutomationWarning_Visibility == Visibility.Visible) newLongTextHeight -= 2 * redbar;
            newLongTextHeight -= titlebar;
            if (BusyOverlayBindings.I.ButtonVisibility == Visibility.Visible) newLongTextHeight -= button;
            newLongTextHeight -= progress;
            newLongTextHeight -= messagetext;
            if (BusyOverlayBindings.I.CurrentElementVisibility == Visibility.Visible) newLongTextHeight -= elementtext;
            newLongTextHeight -= longtextborder;
            newLongTextHeight -= textborderpadding;
            newLongTextHeight -= scrollmargin;

            BusyOverlayBindings.I.LongTextHeight = newLongTextHeight;
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize(e.NewSize.Height);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Binding binding = new Binding("OverlayVisibility");
            SetBinding(VisibilityProperty, binding);
        }
    }
}