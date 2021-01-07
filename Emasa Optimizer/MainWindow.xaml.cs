using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Drawing;
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
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using LiveCharts;
using LiveCharts.Definitions.Series;
using LiveCharts.Wpf;
using NLoptNet;
using Prism.Events;
using RhinoInterfaceLibrary;
using Sap2000Library;

namespace Emasa_Optimizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Forces the DataContext AFTER the initialization. Necessary because some elements in the AppSS reference FrameworkElements within the window.
                this.DataContext = AppSS.I;
                // Sets the Context of the Additional Content
                Window_CustomOverlay.SetAdditionalContent_DataContext(AppSS.I);
            }
            catch (Exception e)
            {
                ExceptionViewer ev = ExceptionViewer.Show(e);
                Application.Current.Shutdown(1);
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {            
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                BusyOverlayBindings.I.ShowOverlay();

                void lf_Work()
                {
                    // Starts the Optimize Manager, initializing the connection with GrassHopper and other variables
                    BusyOverlayBindings.I.Title = "Linking to Grasshopper and Initializing";
                    BusyOverlayBindings.I.SetIndeterminate("Please wait...");
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;


            }
            catch (Exception ex)
            {
                ExceptionViewer ev = ExceptionViewer.Show(ex);
                Application.Current.Shutdown(1);
            }
            finally
            {
                BusyOverlayBindings.I.HideOverlayAndReset();
            }

            EventAggregatorSingleton.I.GetEvent<BindBeginCommandEvent>().Subscribe(BindBeginCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindEndCommandEvent>().Subscribe(BindEndCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindMessageEvent>().Subscribe(BindMessageEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Subscribe(BindGenericCommandEventHandler);

        }

        #region Binder Event Handlers
        private void BindBeginCommandEventHandler(BindCommandEventArgs inObj)
        {
        }
        private void BindEndCommandEventHandler(BindCommandEventArgs inObj)
        {
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

        private void CancelSolveButton_Click(object sender, RoutedEventArgs e)
        {
            // Sends the cancel signal
            AppSS.I.SolveMgr.CancelSource.Cancel();
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

        bool swap = false;

        private async void Debug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                S2KModel.SM.OpenFile(@"C:\Users\EngRafaelSMacedo\Desktop\RhinoTester\Elliptical Arch.gh_data\NlOpt\FeWork\perfect.s2k");
            }
            finally
            {
                // Terminates the Finite Element Solver
                AppSS.I.FeSolver?.Dispose();
                AppSS.I.FeSolver = null;
            }


        }


    }
}
