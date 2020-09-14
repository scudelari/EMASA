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
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Prism.Events;

namespace Emasa_Optimizer
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
        private async void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                BusyOverlay.ShowOverlay();
                
                void lf_Work()
                {
                    // Starts the Solve Manager, initializing the connection with GrassHopper and other variables
                    BusyOverlayBindings.I.Title = "Linking to Grasshopper and Initializing";
                    BusyOverlayBindings.I.SetIndeterminate("Please wait...");
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;

                FormGeneralBindings.Start(MainGrid);
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
                Application.Current.Shutdown(1);
            }
            finally
            {
                BusyOverlay.HideOverlayAndReset();
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
                //MessageOverlay.ShowOverlay(inObj.Title, inObj.Message);
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

        private void Input_ListViewItem_LostFocus(object sender, RoutedEventArgs e)
        {
            Control cSender = (Control)sender;

            // Did the ListViewItem lose its focus?
            if (!cSender.GetAnyChildHasFocus(inIncludeSelf: true))
            {
            }
        }
        private void CancelSolveButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
