using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Serialization;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using RhinoInterfaceLibrary;
using Image = System.Drawing.Image;

namespace WpfApp1
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

        public SeriesCollection SeriesCollection { get; set; } = new SeriesCollection();
        public AxesCollection AxesCollection1 { get; set; } = new AxesCollection();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Random r = new Random();

            AxesCollection1 = new AxesCollection
                    {
                    new Axis() {Title = "First Axes", Position = AxisPosition.LeftBottom},
                    new Axis() {Title = "Second Axes", Position = AxisPosition.RightTop},
                    };

            SeriesCollection = new SeriesCollection
                {
                new LineSeries
                    {
                    Title = "Series 1",
                    Values = new ChartValues<ObservablePoint>
                        {
                        new ObservablePoint(1, r.NextDouble()),
                        new ObservablePoint(2, r.NextDouble()),
                        new ObservablePoint(3, r.NextDouble()),
                        new ObservablePoint(4, r.NextDouble()),
                        new ObservablePoint(5, r.NextDouble()),
                        new ObservablePoint(6, r.NextDouble()),
                        },
                    LineSmoothness = 0,
                    StrokeThickness = 1,
                    ScalesYAt = 0,
                    },
                new LineSeries
                    {
                    Title = "Series 2",
                    Values = new ChartValues<ObservablePoint>
                        {
                        new ObservablePoint(1, r.NextDouble()),
                        new ObservablePoint(2, r.NextDouble()),
                        new ObservablePoint(3, r.NextDouble()),
                        new ObservablePoint(4, r.NextDouble()),
                        new ObservablePoint(5, r.NextDouble()),
                        new ObservablePoint(6, r.NextDouble()),
                        },
                    LineSmoothness = 0,
                    StrokeThickness = 1,
                    ScalesYAt = 1,
                    },
                };

            //YFormatter = value => $"{value:G5}";
            
            DataContext = this;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            RhinoModel.Initialize();
        }


        private void bntRhinoByCommand_Click(object sender, RoutedEventArgs e)
        {

            List<(string,string)> list = new List<(string, string)>()
                {
                ("1","2"),("1", "2")
                };

            XmlSerializer serializer = new XmlSerializer(typeof(List<(string, string)>));

            string xmldata;
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.Serialize(ms, list);
                xmldata = System.Text.Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            }

            List<(string, string)> list2;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmldata)))
            {
                XmlSerializer serializer2 = new XmlSerializer(typeof(List<(string, string)>));
                list2 = serializer2.Deserialize(ms) as List<(string, string)>;
            }




            int a = 0;
            a++;
            //try
            //{
            //    RhinoModel.RM.PrepareRhinoView_ByCommand();
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}

        }

        private void bntRhinoByCOM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GrasshopperAllEmasaOutputWrapper_AsRhino3dm a =  RhinoModel.RM.Grasshopper_GetAllEmasaOutputs();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
