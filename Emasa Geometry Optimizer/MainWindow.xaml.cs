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
using AccordHelper.Opt.ParamDefinitions;
using BaseWPFLibrary;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using Emasa_Geometry_Optimizer.Bindings;
using Prism.Events;
using RhinoInterfaceLibrary;

namespace Emasa_Geometry_Optimizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                FormGeneralBindings.Start(MainGrid);
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, "Rhino Initialization Issue.");
            }

            EventAggregatorSingleton.I.GetEvent<BindBeginCommandEvent>().Subscribe(BindBeginCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindEndCommandEvent>().Subscribe(BindEndCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindMessageEvent>().Subscribe(BindMessageEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Subscribe(BindGenericCommandEventHandler);

        }

        #region Binder Event Handlers
        private void BindBeginCommandEventHandler(BindCommandEventArgs inObj)
        {
            CustomOverlay.ShowOverlay();
        }
        private void BindEndCommandEventHandler(BindCommandEventArgs inObj)
        {
            CustomOverlay.HideOverlayAndReset();
        }
        private void BindMessageEventHandler(BindMessageEventArgs inObj)
        {
            // If a message came along, we show the message overlay
            if (inObj.Title != null && inObj.Message != null)
            {
                MessageOverlay.ShowOverlay(inObj.Title, inObj.Message);
            }
        }
        private void BindGenericCommandEventHandler(BindCommandEventArgs inObj)
        {
            if (inObj.EventData is string order)
            {
                if (order == "ActivateWindow")
                {
                    Dispatcher.Invoke(() => Activate());
                    return;
                }
            }

            // This means that the given message will not be handled
            throw new NotImplementedException();
        }
        #endregion

        private void FocusFromOtherThread()
        {
            Dispatcher.InvokeAsync(() => { Focus(); });
        }

        private void Input_ListViewItem_LostFocus(object sender, RoutedEventArgs e)
        {
            Control cSender = (Control) sender;

            // Did the ListViewItem lose its focus?
            if (!cSender.GetAnyChildHasFocus(inIncludeSelf: true))
            {
                if (!(cSender.DataContext is Input_ParamDefBase iParamBase)) throw new InvalidCastException($"The ListViewItem's DataContext is not an Input_ParamDefBase.");
                iParamBase.UpdateBindingValues();
            }
        }
        private void Solver_ListViewItem_LostFocus(object sender, RoutedEventArgs e)
        {
            Control cSender = (Control)sender;

            // Did the ListViewItem lose its focus?
            if (!cSender.GetAnyChildHasFocus(inIncludeSelf: true))
            {
                if (!(cSender.DataContext is Output_ParamDefBase oParamBase)) throw new InvalidCastException($"The ListViewItem's DataContext is not an Output_ParamDefBase.");
                oParamBase.UpdateBindingValues();
            }
        }

        private void CancelSolveButton_Click(object sender, RoutedEventArgs e)
        {
            FormGeneralBindings.I.ExecuteCancelSolveCommand();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
