using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using LibOptimization.Optimization;
using LibOptimization.Util;
using Prism.Events;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //WindowBindings binds = new WindowBindings();
        public MainWindow()
        {
            InitializeComponent();

            Thread.CurrentThread.Name = "Main";

            // Initializing the Bindings
            WindowBindings.Start(this);

            // Subscribes to the GLOBAL events from the binders
            EventAggregatorSingleton.I.GetEvent<BindBeginCommandEvent>().Subscribe(BindBeginCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindEndCommandEvent>().Subscribe(BindEndCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Subscribe(BindGenericCommandEvent, ThreadOption.UIThread);
        }

        private void BindGenericCommandEvent(BindCommandEventArgs inObj)
        {
            MessageBox.Show($"Got {inObj.EventData} from {inObj.Sender}");
        }

        private void BindEndCommandEventHandler(BindCommandEventArgs inObj)
        {
            BusyOverlay.HideOverlayAndReset();
        }

        private void BindBeginCommandEventHandler(BindCommandEventArgs inObj)
        {
            BusyOverlay.ShowOverlay();
        }



        private async void TestBusyOverLayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                void work()
                {
                    BusyOverlay.ShowOverlay();
                    BusyOverlay.SetBasic("Message", "Title", "Element", "Cancel");

                    BusyOverlay.LongReport_Visibility = Visibility.Visible;
                    BusyOverlay.AutomationWarning_Visibility = Visibility.Visible;

                    for (int i = 0; i < 100; i++)
                    {
                        BusyOverlay.UpdateProgress(i, 100, $"Value {i}");
                        BusyOverlay.LongReport_AddLine($"Value {i}");
                        Thread.Sleep(50);
                        if (BusyOverlay.Token.IsCancellationRequested) break;
                    }

                    BusyOverlay.HideOverlayAndReset();
                }

                Task task = new Task(work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {

            }
        }

        private async void TestMessageButton_Click(object sender, RoutedEventArgs e)
        {
            void work()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 1000; i++)
                {
                    sb.AppendLine($"{i}");
                }

                MessageOverlay.ShowOverlay("Title", sb.ToString());

                //for (int i = 0; i < 100; i++)
                //{
                //    Thread.Sleep(50);
                //}

                //MessageOverlay.HideOverlayAndReset();
            }

            Task task = new Task(work);
            task.Start();
            await task;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //this.binds.debug();
        }

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Event Called");
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //MessageBox.Show($"{MethodBase.GetCurrentMethod()} Event Called");
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"{MethodBase.GetCurrentMethod()} Event Called");
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"{MethodBase.GetCurrentMethod()} Event Called");
        }

        private void TestRhino_Click(object sender, RoutedEventArgs e)
        {
            string fullname = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                "TempFile.bin");


            clsOptDE h1 = new clsOptDE(new RafaObjectiveFunction());
            h1.DEStrategy = clsOptDE.EnumDEStrategyType.DE_best_1_bin;

            h1.Init();

            //clsUtil.DebugValue(valueTuple.opt);
            int counter = 0;

            h1.DoIteration(10);

            IFormatter formatter1 = new BinaryFormatter();
            Stream stream1 = new FileStream(fullname, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter1.Serialize(stream1, h1);
            stream1.Close();

            IFormatter formatter2 = new BinaryFormatter();
            Stream stream2 = new FileStream(fullname, FileMode.Open, FileAccess.Read, FileShare.Read);
            clsOptDE h2 = (clsOptDE)formatter2.Deserialize(stream2);
            stream2.Close();

            h2.DoIteration(10);
        }

        private void TestCustomOverlay_Click(object sender, RoutedEventArgs e)
        {
            CustomOverlay.ShowOverlay();
        }
    }

    [Serializable]
    class RafaObjectiveFunction : absObjectiveFunction
    {
        public RafaObjectiveFunction()
        {
        }

        public int _counter = 0;

        public override int NumberOfVariable()
        {
            return 2;
        }

        private double A = 100;
        private double B = 1;

        public override double F(List<double> inputVars)
        {
            _counter++;

            double ret;

            double x = inputVars[0];
            double y = inputVars[1];

            ret = Math.Pow(A - x, 2) + B * Math.Pow(y - x * x, 2d) + 50;

            //Console.WriteLine($"Evaluated {_counter}.{Environment.NewLine}\t\tInput: x:{x:f3} y:{y:f3}{Environment.NewLine}\t\tOutput: {ret:f3}");

            return ret;
        }

        public override List<double> Gradient(List<double> x)
        {
            throw new NotImplementedException();
        }

        public override List<List<double>> Hessian(List<double> x)
        {
            throw new NotImplementedException();
        }
    }
}
