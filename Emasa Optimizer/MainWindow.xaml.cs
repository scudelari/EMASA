using System;
using System.CodeDom;
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
using Emasa_Optimizer.WpfResources;
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

        private async void Solve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomOverlay.ShowOverlay();

                void lf_Work()
                {
                    // Starts the Solve Manager, initializing the connection with GrassHopper and other variables
                    CustomOverlayBindings.I.Title = "Solving";
                    CustomOverlayBindings.I.MessageText = "Please Wait";

                    FormGeneralBindings.I.SolveMgr.NlOpt_SolveSelectedProblem();
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
                CustomOverlay.HideOverlayAndReset();
            }
        }

        private void CancelSolveButton_Click(object sender, RoutedEventArgs e)
        {
            // Sends the cancel signal
            FormGeneralBindings.I.SolveMgr.NlOptManager.CancelSource.Cancel();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is TabItem tab)
            {
                if (tab.HasHeader && tab.Header is string tabString && tabString == "Optimize")
                {
                    FormGeneralBindings.I.SolveMgr.FeOptions.WpfAllSelectedOutputResults.Refresh();
                    // TODO: Checks if the added list has items that are not in the selected output list
                }
            }
        }


        private void DataGrid_AutoGeneratingColumn_AddDoubleFormats(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Handles the event of creating the columns of the Result Data - Table DataGrid
            // Sets the custom converter to the Columns
            if (e.Column is DataGridTextColumn textCol)
            {
                if (textCol.Binding is Binding binding)
                {
                    binding.Converter = new DefaultNumberConverter();
                }
            }
        }
    }
}
