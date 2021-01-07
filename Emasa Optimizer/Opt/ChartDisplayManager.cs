extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BaseWPFLibrary;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt.ProbQuantity;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Definitions.Series;
using LiveCharts.Wpf;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.Opt
{
    public class ChartDisplayManager
    {
        public ChartDisplayManager()
        {
        }

        public void InitializeCharts()
        {
            if (!(AppSS.FirstReferencedWindow is MainWindow mw)) throw new Exception($"{this.GetType()} could not get a reference of {typeof(MainWindow)}");

            PointPlotSummaryEvalPlot_CartesianChart = mw.PointPlotSummaryEvalPlot_CartesianChart;
            //PointPlotSummaryEvalPlot_CartesianChart.LegendLocation = LegendLocation.Top;
            PointPlotSummaryEvalPlot_CartesianChart.ChartLegend = null;
            PointPlotSummaryEvalPlot_CartesianChart.DisableAnimations = true;

            PointPlotSummaryEvalPlot_CartesianChart.AxisY.Add(new Axis()
            {
                Title = "Objective Function Eval",
                LabelFormatter = inD => BaseWPFStaticMembers.DoubleToEngineering(inD, "3"),
                Foreground = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
            });
            PointPlotSummaryEvalPlot_CartesianChart.AxisX.Add(new Axis()
            {
                Title = "Optimization Point",
            });
            PointPlotSummaryEvalPlot_CartesianChart.Series.Add(new LineSeries()
            {
                //Title = "Objective Function Eval",
                Values = new ChartValues<double>(),
                PointForeground = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                Foreground = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                Stroke = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                StrokeThickness = 1d,
                Fill = new SolidColorBrush() { Opacity = 0d },
                PointGeometrySize = 5d,
                LineSmoothness = 0d,
            });


            if (!(AppSS.I.GetDescendantOfElement_ByTag("CalculatingEvalPlot_CartesianChart", mw.Window_CustomOverlay) is CartesianChart calEvalPlot)) throw new Exception($"{this.GetType()} could not get find CalculatingEvalPlot_CartesianChart when searching by Tag.");
            CalculatingEvalPlot_CartesianChart = calEvalPlot;
            CalculatingEvalPlot_CartesianChart.ChartLegend = null;
            //CalculatingEvalPlot_CartesianChart.LegendLocation = LegendLocation.Top;
            CalculatingEvalPlot_CartesianChart.DisableAnimations = true;

            CalculatingEvalPlot_CartesianChart.AxisY.Add(new Axis()
            {
                Title = "Objective Function Eval",
                LabelFormatter = inD => BaseWPFStaticMembers.DoubleToEngineering(inD, "3"),
                Foreground = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
            });
            CalculatingEvalPlot_CartesianChart.AxisX.Add(new Axis()
            {
                Title = "Optimization Point",
            });
            CalculatingEvalPlot_CartesianChart.Series.Add(new LineSeries()
            {
                //Title = "Objective Function Eval",
                Values = new ChartValues<double>(),
                PointForeground = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                Foreground = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                Stroke = mw.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                StrokeThickness = 1d,
                Fill = new SolidColorBrush() { Opacity = 0d },
                PointGeometrySize = 5d,
                LineSmoothness = 0d,
            });



            ProblemConfigDetailPlot_CartesianChart = mw.ProblemConfigDetailPlot_CartesianChart;
            ProblemConfigDetailPlot_CartesianChart.LegendLocation = LegendLocation.Top;
            ProblemConfigDetailPlot_CartesianChart.DisableAnimations = true;
            //ProblemConfigDetailPlot_CartesianChart.AxisY[0].LabelFormatter = inD => $"{inD:+0.0e+00;-0.0e+00;0.0}";

            // Makes them visible
            PointPlotSummaryEvalPlot_CartesianChart.Visibility = Visibility.Visible;
            CalculatingEvalPlot_CartesianChart.Visibility = Visibility.Visible;
            ProblemConfigDetailPlot_CartesianChart.Visibility = Visibility.Visible;

            NlOptPointDetails_CartesianChart = mw.NlOptPointDetails_CartesianChart;
            NlOptPointDetails_CartesianChart.DisableAnimations = true;
            NlOptPointDetails_CartesianChart.DataTooltip = new DefaultTooltip() { SelectionMode = TooltipSelectionMode.OnlySender };
            NlOptPointDetails_CartesianChart.HideLegend();

            NlOptPointDetails_CartesianChart.AxisX.Add(new Axis()
            {
                Labels = new List<string>(),
            });

            NlOptPointDetails_CartesianChart.AxisY.Add(new Axis()
            {
                Title = "Count of Elements",
                LabelFormatter = inD => BaseWPFStaticMembers.DoubleToEngineering(inD, "3"),
                Position = AxisPosition.LeftBottom,
                Foreground = mw.Resources["EmsPanelBorder_Gray"] as SolidColorBrush,
            });

            NlOptPointDetails_CartesianChart.Series.Add(new ColumnSeries()
            {
                Values = new ChartValues<int>(),
                Foreground = mw.Resources["EmsPanelBackground_Gray"] as SolidColorBrush,
                Stroke = mw.Resources["EmsPanelBorder_Gray"] as SolidColorBrush,
                StrokeThickness = 1d,
                Fill = mw.Resources["EmsPanelBackground_Gray"] as SolidColorBrush,
                ScalesYAt = 0,
                ScalesXAt = 0,
            });
        }

        public CartesianChart PointPlotSummaryEvalPlot_CartesianChart { get; private set; }
        public CartesianChart CalculatingEvalPlot_CartesianChart { get; private set; }

        public CartesianChart ProblemConfigDetailPlot_CartesianChart { get; private set; }

        public CartesianChart NlOptPointDetails_CartesianChart { get; private set; }

        // Management of data displayed in the plots
        public void ClearSeriesValues(CartesianChart inChart)
        {
            foreach (ISeriesView chartSeries in inChart.Series)
            {
                chartSeries.Values.Clear();
            }

            //inChart.Dispatcher.Invoke(() => {
            //    inChart.Update(true, true);
            //    inChart.InvalidateArrange();
            //    inChart.UpdateLayout();
            //});
        }

        public void AddSeriesValue(CartesianChart inChart, double inVal, int? inSeriesIndex = null)
        {
            if (inSeriesIndex.HasValue) inChart.Series[inSeriesIndex.Value].Values.Add(inVal);
            else
                foreach (ISeriesView chartSeries in inChart.Series) 
                {
                    chartSeries.Values.Add(inVal);
                }
        }
        public void AddSeriesValue(CartesianChart inChart, IEnumerable<double> inVals, int? inSeriesIndex = null)
        {
            if (inSeriesIndex.HasValue) inChart.Series[inSeriesIndex.Value].Values.AddRange(inVals.Cast<object>());
            else
                foreach (ISeriesView chartSeries in inChart.Series)
                {
                    chartSeries.Values.AddRange(inVals.Cast<object>());
                }

        }

        public void UpdateChart(CartesianChart inChart, List<ChartDisplayData> inData)
        {
            // Checks if chart is already invalidated...

            // Completely clears the chart's series
            inChart.Series.Clear();
            inChart.AxisY.Clear();

            // Get all visible pairs
            IEnumerable<ChartDisplayData_Series> allVisible = inData.SelectMany(a => a.SeriesData).Where(a => a.IsVisible);

            //double lf_getClosest(double inValue, double inShift)
            //{
            //    double target = inValue + inShift;
            //    string s = $"{target:+0.0e+000;-0.0e+000}";
            //    string[] chunks = s.Split(new[] {'e'});
            //    return double.Parse($"{chunks[0]}e{chunks[1]}");
            //}

            int counter = 0;
            foreach (ChartDisplayData_Series chartDisplayData_SeriesAxisPair in allVisible)
            {
                //double min = chartDisplayData_SeriesAxisPair.Min;
                //double max = chartDisplayData_SeriesAxisPair.Max;
                //int lineCount = 10;
                //double shift = (max - min) / lineCount;
                //double axisMin = lf_getClosest(min, -shift);
                //double axisMax = lf_getClosest(max, shift);

                // Creates an Axis to handle this list
                inChart.AxisY.Add(new Axis()
                    {
                    Title = chartDisplayData_SeriesAxisPair.Series.Title,
                    LabelFormatter = inD => BaseWPFStaticMembers.DoubleToEngineering(inD, "3"),
                    Position = chartDisplayData_SeriesAxisPair.IsPairSelected_AxisOnLeftSide ? AxisPosition.LeftBottom : AxisPosition.RightTop,
                    Foreground = chartDisplayData_SeriesAxisPair.Series.Stroke,

                    //MaxValue = axisMax,
                    //MinValue = axisMin,
                    //Separator = new Separator(){Step = (axisMax - axisMin)/lineCount}
                });

                // Adds this series to the list
                inChart.Series.Add(chartDisplayData_SeriesAxisPair.Series);

                // Tells the series to plot in the right axis
                chartDisplayData_SeriesAxisPair.Series.ScalesYAt = counter;


                counter++;
            }


            // Matches the axis' colors


            //inChart.Dispatcher.Invoke(() =>
            //{
            //    if (inChart == null)
            //    {
            //        int a = 0;
            //        a++;
            //    }

            //    inChart.Update();
            //    inChart.InvalidateArrange();
            //    inChart.UpdateLayout();
            //});

            // TODO: Check assigned color and try to match the color of the axes with the color of the series
            //int b = 0;
            //b++;
            
        }

        public void UpdateDistributionChart(CartesianChart inChart, List<double> inVals)
        {
            // Clears the chart's X Labels
            inChart.AxisX[0].Labels.Clear();

            ColumnSeries colSeries = inChart.Series.FirstOrDefault(a => a is ColumnSeries) as ColumnSeries;

            // Clears the chart's Series' data
            colSeries.Values.Clear();

            // Nothing in the list
            if (inVals.Count < 2) // 0 or 1
            {
                // Hides the chart.
                inChart.Visibility = Visibility.Hidden;
                return;
            }
            
            double mean = inVals.Mean();
            double stDev = inVals.StandardDeviation();
            double max = inVals.Max();
            double min = inVals.Min();

            if (double.IsNaN(mean) || double.IsNaN(stDev) || max == min)
            {
                // Hides the chart.
                inChart.Visibility = Visibility.Hidden;
                return;
            }

            // Displays the chart
            inChart.Visibility = Visibility.Visible;

            double fullRange = max - min;
            int colDivs = 20;
            Dictionary<double, int> colValDict = new Dictionary<double, int>();

            // Initializes the dictionary
            for (int i = 0; i < colDivs; i++)
            {
                double rMin = min + ((fullRange / colDivs) * (double)i);
                double rMax = min + ((fullRange / colDivs) * (double)(i + 1));
                double rMid = ((rMax - rMin) / 2d) + rMin;

                colValDict.Add(rMid, 0);
            }

            // Counts the number of elements in each bracket
            foreach (double val in inVals)
            {
                // Finds the closest column
                var kvp = colValDict.AsEnumerable().OrderBy(inPair => Math.Abs(val - inPair.Key)).First();
                colValDict[kvp.Key]++;
            }

            // Sets the labels of the X axis
            inChart.AxisX[0].Separator.Step = 1;
            inChart.AxisX[0].LabelsRotation = -90d;
            ((List<string>)inChart.AxisX[0].Labels).AddRange(colValDict.Keys.Select(b => $"{b:+0.0e+00;-0.0e+00;0.0}"));
            
            // Sets the column chart' data
            colSeries.Values.AddRange(colValDict.Values.Cast<object>());

            
            
            // Deletes the previous Normal Distribution Line Data From the Chart
            if (inChart.Series.Count > 1) inChart.Series.RemoveAt(1);
            if (inChart.AxisY.Count > 1) inChart.AxisY.RemoveAt(1);
            if (inChart.AxisX.Count > 1) inChart.AxisX.RemoveAt(1);
            
            // Working with the normal distribution
            Normal n = new Normal(mean, stDev);
            ChartValues<ObservablePoint> normalValDict = new ChartValues<ObservablePoint>();

            // Fills the dictionary
            int normCount = (colDivs * 2);
            double normStepSize = (max - min) / normCount;
            for (int i = 0; i < (normCount + 1); i++)
            {
                double val = min + normStepSize*i;
                normalValDict.Add(new ObservablePoint(val, n.Density(val)));
            }

            inChart.AxisX.Add(new Axis()
                {
                //Labels = new List<string>(),
                ShowLabels = false,
                Separator = new Separator() { IsEnabled = false },
                Sections = new SectionsCollection(){
                    new AxisSection()
                        {
                        Value = mean - stDev,
                        SectionWidth = stDev,
                        Fill = new SolidColorBrush(){ Color = Colors.DarkOrange, Opacity = 0.3},
                        //Stroke = new SolidColorBrush(){ Color = Colors.DarkRed, Opacity = 1d},
                        //StrokeThickness = 2d,
                        },
                        new AxisSection()
                        {
                        Value = mean + stDev,
                        SectionWidth = -stDev,
                        Fill = new SolidColorBrush(){ Color = Colors.DarkOrange, Opacity = 0.3},
                        //Stroke = new SolidColorBrush(){ Color = Colors.DarkRed, Opacity = 1d},
                        //StrokeThickness = 2d,
                        },
                        new AxisSection()
                        {
                        Value = mean,
                        Stroke = new SolidColorBrush(){ Color = Colors.DarkRed, Opacity = 1d},
                        StrokeThickness = 2d,
                        }
                    },

                MinValue = min,
                MaxValue = max,
                
            });

            inChart.AxisY.Add(new Axis()
                {
                Position = AxisPosition.RightTop,
                ShowLabels = false,
                Separator = new Separator() { IsEnabled = false },
            });

            inChart.Series.Add(new LineSeries()
                {
                Values = normalValDict,
                PointForeground = AppSS.FirstReferencedWindow.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                Foreground = AppSS.FirstReferencedWindow.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                Stroke = AppSS.FirstReferencedWindow.Resources["EmsPanelBorder_Green"] as SolidColorBrush,
                StrokeThickness = 1d,
                Fill = new SolidColorBrush() {Opacity = 0d},
                PointGeometrySize = 0d,
                LineSmoothness = 0.5d,
                ScalesYAt = 1,
                ScalesXAt = 1,
                });

        }
    }

    public class ChartDisplayData
    {
        /// <summary>
        /// The quantity to be plotted in the chart - Can be Objective Function Eval, NL Input such as Double/Point and a Problem Quantity
        /// </summary>
        public object RelatedQuantity { get; set; }
        
        /// <summary>
        /// A list of series and axis to be plotted
        /// </summary>
        public ChartDisplayData_Series[] SeriesData { get; set; }
    }

    public class ChartDisplayData_Series : BindableBase
    {
        private bool _isPairSelected_AxisOnRightSide = true;
        public bool IsPairSelected_AxisOnRightSide
        {
            get => _isPairSelected_AxisOnRightSide;
            set
            {
                _isPairSelected_AxisOnRightSide = value;

                // Ensures the other is false
                if (_isPairSelected_AxisOnRightSide && _isPairSelected_AxisOnLeftSide)
                {
                    _isPairSelected_AxisOnLeftSide = false;
                    RaisePropertyChanged("IsPairSelected_AxisOnLeftSide");
                }

                // Warns about a changes in visibility
                RaisePropertyChanged("IsPairSelected_AxisOnRightSide");
                RaisePropertyChanged("IsVisible");
                VisibilityUpdated?.Invoke(this, null);
            }
        }

        private bool _isPairSelected_AxisOnLeftSide = false;
        public bool IsPairSelected_AxisOnLeftSide
        {
            get => _isPairSelected_AxisOnLeftSide;
            set
            {
                _isPairSelected_AxisOnLeftSide = value;

                // Ensures the other is false
                if (_isPairSelected_AxisOnRightSide && _isPairSelected_AxisOnLeftSide)
                {
                    _isPairSelected_AxisOnRightSide = false;
                    RaisePropertyChanged("IsPairSelected_AxisOnRightSide");
                }

                // Warns about a changes in visibility
                RaisePropertyChanged("IsPairSelected_AxisOnLeftSide");
                RaisePropertyChanged("IsVisible");
                VisibilityUpdated?.Invoke(this, null);


            }
        }

        public bool IsVisible => IsPairSelected_AxisOnRightSide || IsPairSelected_AxisOnLeftSide;
        public event EventHandler VisibilityUpdated;

        private string _designation;
        /// <summary>
        /// A string designation of the plot for when we have various plots - such as when we have input points
        /// </summary>
        public string Designation
        {
            get => _designation;
            set => SetProperty(ref _designation, value);
        }
        
        public ChartDisplayData_Series([NotNull] LineSeries inSeries)
        {
            Series = inSeries ?? throw new ArgumentNullException(nameof(inSeries));
        }

        public LineSeries Series { get; private set; }

        public double Max => Series.Values.OfType<double>().Max();
        public double Min => Series.Values.OfType<double>().Min();

        public override int GetHashCode()
        {
            return Series.GetHashCode();
        }
    }
}
