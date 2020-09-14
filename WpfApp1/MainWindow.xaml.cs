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
using Accord.Math;
using Accord.Math.Differentiation;
using MathNet.Numerics.Differentiation;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

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

        private void IfcGenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string inputpath = @"C:\Users\EngRafaelSMacedo\Desktop\Revit Test\IfcGen\Project1.ifc";
            string outputpath = $@"C:\Users\EngRafaelSMacedo\Desktop\Revit Test\IfcGen\Out_{DateTime.Now:dd_MM_HH_mm_ss}.ifc";

            PropertyTranformDelegate semanticFilter = (property, parentObject) =>
            {
                //leave out geometry and placement
                if (parentObject is IIfcProduct &&
                    (property.PropertyInfo.Name == "Representation" || // nameof() removed to allow for VS2013 compatibility
                    property.PropertyInfo.Name == "ObjectPlacement")) // nameof() removed to allow for VS2013 compatibility
                    return null;

                //leave out mapped geometry
                if (parentObject is IIfcTypeProduct &&
                    property.PropertyInfo.Name == "RepresentationMaps") // nameof() removed to allow for VS2013 compatibility
                    return null;


                //only bring over IsDefinedBy and IsTypedBy inverse relationships which will take over all properties and types
                if (property.EntityAttribute.Order < 0 && !(
                    property.PropertyInfo.Name == "IsDefinedBy" || // nameof() removed to allow for VS2013 compatibility
                    property.PropertyInfo.Name == "IsTypedBy"      // nameof() removed to allow for VS2013 compatibility
                    ))
                    return null;

                return property.PropertyInfo.GetValue(parentObject, null);
            };

            using (var model = IfcStore.Open(inputpath))
            {
                var walls = model.Instances.OfType<IIfcBuildingElementProxy>();
                using (var iModel = IfcStore.Create(((IModel)model).SchemaVersion, XbimStoreType.InMemoryModel))
                {
                    using (var txn = iModel.BeginTransaction("Insert copy"))
                    {
                        //single map should be used for all insertions between two models
                        var map = new XbimInstanceHandleMap(model, iModel);

                        foreach (var wall in walls)
                        {
                            iModel.InsertCopy(wall, map, semanticFilter, true, false);
                        }

                        txn.Commit();
                    }

                    iModel.SaveAs(outputpath);
                }
            }
        }

        private void TestFiniteDifferences_Click(object sender, RoutedEventArgs e)
        {
            double[] point = new[] { 2.0, -1.0 };
            
            // Create a new finite differences calculator
            FiniteDifferences calculator = new FiniteDifferences(2, TestFiniteDifferences_Accord, 1, 0.001);

            double[] accordResult = calculator.Gradient(point);

            // test points
            NumericalDerivative nd = new NumericalDerivative(2,1);

            double[] MathNetResult = new double[2];
            double centerVal = TestFiniteDifferences_MathNet(point);
            for (int i = 0; i < MathNetResult.Length; i++)
            {
                MathNetResult[i] = nd.EvaluatePartialDerivative(TestFiniteDifferences_MathNet, point, i, 1, centerVal);
            }

        }

        private int fd_AccordCount = 0;
        private List<(double[], double)> fd_AccordPoints = new List<(double[], double)>();
        
        private int fd_MathNetCount = 0;
        private List<(double[], double)> fd_MathNetPoints = new List<(double[], double)>();
        private double TestFiniteDifferences_Accord(double[] x)
        {
            fd_AccordCount++;
            double d = Math.Pow(x[0], 2) + x[1];
            fd_AccordPoints.Add((x.Copy(), d));
            return d;
        }
        private double TestFiniteDifferences_MathNet(double[] x)
        {
            fd_MathNetCount++;
            double d = Math.Pow(x[0], 2) + x[1];
            fd_MathNetPoints.Add((x.Copy(), d));
            return d;
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
