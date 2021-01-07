extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xaml;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using LiveCharts;
using LiveCharts.Wpf;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;
using Point = System.Windows.Point;

namespace Emasa_Optimizer.Opt
{
    /// <summary>
    /// Contains the evaluation of a single Problem Configuration.
    /// </summary>
    public class ProblemConfig : BindableBase
    {
        public ProblemConfig([NotNull] List<ProblemConfig_ElementCombinationValue> inElementValues, int inIndex)
        {
            ConfigElementValues = inElementValues ?? throw new ArgumentNullException(nameof(inElementValues));

            Index = inIndex;

            Wpf_ElementValues_View = CollectionViewSource.GetDefaultView(ConfigElementValues);

            #region Initializes the Solution Points
            _points = new FastObservableCollection<NlOpt_Point>();

            // Sets the views for the function points
            Wpf_FunctionPoints = CollectionViewSource.GetDefaultView(_points);
            #endregion

            #region Configures the NlOpt_SolverWrapper
            _nlOptSolverWrapper = new NlOpt_SolverWrapper(this);
            _nlOptSolverWrapper.PropertyChanged += (inSender, inArgs) => { RaisePropertyChanged("NlOptSolverWrapper"); };
            #endregion
        }

        private int _index;
        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        #region Inputs that define this configuration
        public List<ProblemConfig_ElementCombinationValue> ConfigElementValues { get; }
        public ICollectionView Wpf_ElementValues_View { get; }

        public FeSection GetGhLineListSection(LineList_GhGeom_ParamDef inGhLineList)
        {
            ProblemConfig_GhLineListConfigValues pair = ConfigElementValues.OfType<ProblemConfig_GhLineListConfigValues>().FirstOrDefault(a => a.GhLineList == inGhLineList);
            if (pair == null) throw new Exception($"Could not find the FeSection of Grasshopper LineList {inGhLineList.Name}.");
            else return pair.Section;
        }
        public int GetGhIntegerConfig(Integer_GhConfig_ParamDef inGhIntegerConfig)
        {
            ProblemConfig_GhIntegerInputConfigValues pair = ConfigElementValues.OfType<ProblemConfig_GhIntegerInputConfigValues>().FirstOrDefault(a => a.GhIntegerGhConfig == inGhIntegerConfig);
            if (pair == null) throw new Exception($"Could not find the integer value of Grasshopper Integer Config {inGhIntegerConfig.Name}.");
            else return pair.Value;
        }
        #endregion

        private TimeSpan _problemConfigOptimizeElapsedTime;
        public TimeSpan ProblemConfigOptimizeElapsedTime
        {
            get => _problemConfigOptimizeElapsedTime;
            set => SetProperty(ref _problemConfigOptimizeElapsedTime, value);
        }

        private double[] _lowerBounds;
        public double[] LowerBounds
        {
            get => _lowerBounds;
            set => SetProperty(ref _lowerBounds, value);
        }
        private double[] _upperBounds;
        public double[] UpperBounds
        {
            get => _upperBounds;
            set => SetProperty(ref _upperBounds, value);
        }
        public void Reset()
        {
            foreach (NlOpt_Point p in _points) p.ReleaseManagedResources();
            _points.Clear();

            NlOptSolverWrapper.OptimizeTerminationException = new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.NotStarted, "Optimization not started.");

            CurrentlyCalculatingInput = null;
            
            Wpf_SelectedDisplayFunctionPoint = null;
            _chartData = null;
            _plotGrasshopperInputList = null;
            _plotProblemQuantityList = null;

            // Clears the chart
            AppSS.I.ChartDisplayMgr.ClearSeriesValues(AppSS.I.ChartDisplayMgr.CalculatingEvalPlot_CartesianChart);
            RaisePropertyChanged("LastPoint");
        }
        public void Optimize()
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                NlOptSolverWrapper.RunOptimization();
            }
            finally
            {
                sw.Stop();
                ProblemConfigOptimizeElapsedTime = sw.Elapsed;
            }
        }
        
        private readonly NlOpt_SolverWrapper _nlOptSolverWrapper;
        public NlOpt_SolverWrapper NlOptSolverWrapper { get => _nlOptSolverWrapper; }

        #region NlOpt Optimization Point Management
        private readonly FastObservableCollection<NlOpt_Point> _points;
        public void AddFunctionPoint(NlOpt_Point inPoint)
        {
            if (inPoint.NlOptPointCalcType == NlOpt_Point_CalcTypeEnum.Gradient) throw new Exception($"Cannot add points used for gradient calculation to the function point list - they are to be added to the gradients of the main function point.");

            inPoint.PointIndex = _points.Count;
            if (inPoint.PointIndex != 0) inPoint.PreviousPoint = _points.Last();
            _points.Add(inPoint);

            // If this is currently calculating, adds the eval value to the chart
            if (AppSS.I.SolveMgr.CurrentCalculatingProblemConfig == this)
            {
                AppSS.I.ChartDisplayMgr.AddSeriesValue(AppSS.I.ChartDisplayMgr.CalculatingEvalPlot_CartesianChart, inPoint.ObjectiveFunctionEval);
            }

            RaisePropertyChanged("TotalPointCount");
            RaisePropertyChanged("LastPoint");
        }
        public NlOpt_Point GetFunctionPointIfExists(double[] inPointVars)
        {
            // Will return the first it finds; or null if failed.
            return _points.FirstOrDefault(a => a.InputValuesAsDoubleArray.SequenceEqual(inPointVars));
        }
        public ICollectionView Wpf_FunctionPoints { get; }

        public int TotalPointCount => _points != null ? _points.Count : 0;
        public NlOpt_Point LastPoint => _points != null && _points.Count > 0 ? _points.Last() : null;

        // Calculating input data display
        private double[] _currentlyCalculatingInput;
        public double[] CurrentlyCalculatingInput
        {
            get => _currentlyCalculatingInput;
            set
            {
                SetProperty(ref _currentlyCalculatingInput, value);
                RaisePropertyChanged("Wpf_CalculatingInputDisplay");
            }
        }

        public ICollectionView Wpf_CalculatingInputDisplay
        {
            get
            {
                if (LastPoint == null) return null;

                List<Wpf_CalculatingInputDisplay_Data> list = new List<Wpf_CalculatingInputDisplay_Data>();

                for (int i = 0; i < AppSS.I.Gh_Alg.InputDefs_VarCount; i++)
                {
                    //list.Add(new Wpf_CalculatingInputDisplay_Data(i, 
                    //    AppSS.I.Gh_Alg.GetInputParameterNameByIndex(i),
                    //    LastPoint != null ? LastPoint.InputValuesAsDoubleArray[i] : double.NaN,
                    //    CurrentlyCalculatingInput[i],
                    //    LastPoint != null ? CurrentlyCalculatingInput[i] - LastPoint.InputValuesAsDoubleArray[i] : CurrentlyCalculatingInput[i]));

                    list.Add(new Wpf_CalculatingInputDisplay_Data(inIndex: i, 
                        inParameter: AppSS.I.Gh_Alg.GetInputParameterNameByIndex(i),
                        inLowerBoundary: LowerBounds[i],
                        inUpperBoundary: UpperBounds[i],
                        inCalculating: CurrentlyCalculatingInput[i]));
                }

                return CollectionViewSource.GetDefaultView(list);
            }
        }

        private NlOpt_Point _wpf_SelectedDisplayFunctionPoint;
        public NlOpt_Point Wpf_SelectedDisplayFunctionPoint
        {
            get => _wpf_SelectedDisplayFunctionPoint;
            set
            {
                SetProperty(ref _wpf_SelectedDisplayFunctionPoint, value);

                // Sets the Point to display the results
                AppSS.I.NlOptDetails_DisplayAggregator.WpfDisplayPoint = Wpf_SelectedDisplayFunctionPoint;
                // Tells the interface that the display aggregator changed
                AppSS.I.NlOptDetails_DisplayAggregator_Changed();
            }
        }

        #endregion

        #region Chart Data

        public IEnumerable<double> ObjectiveFunctionEvals => _points.Select(a =>
        {
            try
            {
                return a.ObjectiveFunctionEval;
            }
            catch
            {
                return double.NaN;
            }
        });

        private List<ChartDisplayData> _chartData;
        public List<ChartDisplayData> ChartData
        {
            get
            {
                void lf_chartDisplayData_SeriesAxisPair_VisibilityChanged(object sender, EventArgs e)
                {
                    if (sender is ChartDisplayData_Series)
                    {
                        AppSS.I.ChartDisplayMgr.UpdateChart(AppSS.I.ChartDisplayMgr.ProblemConfigDetailPlot_CartesianChart, ChartData);
                    }
                }

                int colorIndex = 0;
                SolidColorBrush lf_chartColor()
                {
                    SolidColorBrush toRet = ListDescSH.I.ChartAvailableColors[colorIndex];
                    colorIndex++;
                    if (colorIndex == ListDescSH.I.ChartAvailableColors.Length) colorIndex = 0;
                    return toRet;
                }

                if (_chartData == null)
                {
                    List<ChartDisplayData> tmpList = new List<ChartDisplayData>();

                    int seriesCount = 0;

                    // The GhInput Collection
                    foreach (Input_ParamDefBase input_ParamDefBase in AppSS.I.Gh_Alg.InputDefs)
                    {
                        switch (input_ParamDefBase)
                        {
                            case Double_Input_ParamDef double_Input_ParamDef:
                                {
                                    LineSeries l = new LineSeries()
                                        {
                                        Title = input_ParamDefBase.Name,
                                        Values = new ChartValues<double>(from a in _points select (double)a.GhInput_Values[double_Input_ParamDef]),

                                        StrokeThickness = 1d,
                                        Fill = new SolidColorBrush() { Opacity = 0d },
                                        PointGeometrySize = 5d,
                                        LineSmoothness = 0d,

                                        PointForeground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Foreground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Stroke = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        StrokeDashArray = new DoubleCollection(new[] { 10d, 5d, 10d }),
                                        };

                                    ChartDisplayData_Series pair = new ChartDisplayData_Series(l);
                                    pair.VisibilityUpdated += lf_chartDisplayData_SeriesAxisPair_VisibilityChanged;

                                    tmpList.Add(new ChartDisplayData()
                                        {
                                        RelatedQuantity = double_Input_ParamDef,
                                        SeriesData = new []{ pair }
                                        });

                                    break;
                                }

                            case Point_Input_ParamDef point_Input_ParamDef:
                                {
                                    LineSeries x = new LineSeries()
                                        {
                                        Title = point_Input_ParamDef.Name + " - X",
                                        Values = new ChartValues<double>(from a in _points select ((Point3d)a.GhInput_Values[point_Input_ParamDef]).X),

                                        StrokeThickness = 1d,
                                        Fill = new SolidColorBrush() { Opacity = 0d },
                                        PointGeometrySize = 5d,
                                        LineSmoothness = 0d,

                                        PointForeground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Foreground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Stroke = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        StrokeDashArray = new DoubleCollection(new[] {10d, 5d, 10d}),
                                        };

                                    LineSeries y = new LineSeries()
                                        {
                                        Title = point_Input_ParamDef.Name + " - Y",
                                        Values = new ChartValues<double>(from a in _points select ((Point3d)a.GhInput_Values[point_Input_ParamDef]).Y),

                                        StrokeThickness = 1d,
                                        Fill = new SolidColorBrush() { Opacity = 0d },
                                        PointGeometrySize = 5d,
                                        LineSmoothness = 0d,

                                        PointForeground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Foreground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Stroke = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        StrokeDashArray = new DoubleCollection(new[] { 1d, 5d, 1d }),
                                        };

                                    LineSeries z = new LineSeries()
                                        {
                                        Title = point_Input_ParamDef.Name + " - Z",
                                        Values = new ChartValues<double>(from a in _points select ((Point3d)a.GhInput_Values[point_Input_ParamDef]).Z),

                                        StrokeThickness = 1d,
                                        Fill = new SolidColorBrush() { Opacity = 0d },
                                        PointGeometrySize = 5d,
                                        LineSmoothness = 0d,

                                        PointForeground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Foreground = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        Stroke = AppSS.FirstReferencedWindow.Resources["EmsDarkBlueColor"] as SolidColorBrush,
                                        StrokeDashArray = new DoubleCollection(new[] { 5d, 5d, 5d }),
                                        };

                                    ChartDisplayData_Series xPair = new ChartDisplayData_Series(x) {Designation = "X"};
                                    xPair.VisibilityUpdated += lf_chartDisplayData_SeriesAxisPair_VisibilityChanged;
                                    ChartDisplayData_Series yPair = new ChartDisplayData_Series(y) { Designation = "Y" };
                                    yPair.VisibilityUpdated += lf_chartDisplayData_SeriesAxisPair_VisibilityChanged;
                                    ChartDisplayData_Series zPair = new ChartDisplayData_Series(z) { Designation = "Z" };
                                    zPair.VisibilityUpdated += lf_chartDisplayData_SeriesAxisPair_VisibilityChanged;

                                    tmpList.Add(new ChartDisplayData()
                                        {
                                        RelatedQuantity = point_Input_ParamDef,
                                        SeriesData = new[] { xPair, yPair, zPair},
                                        });

                                    break;
                                }

                            default:
                                throw new ArgumentOutOfRangeException(nameof(input_ParamDefBase));
                        }
                    }

                    // Complete Quantity List
                    foreach (ProblemQuantity pQuantity in AppSS.I.ProbQuantMgn.WpfProblemQuantities_All.OfType<ProblemQuantity>())
                    {
                        SolidColorBrush lCol;
                        if (pQuantity.IsConstraint) lCol = AppSS.FirstReferencedWindow.Resources["EmsPanelBorder_Yellow"] as SolidColorBrush;
                        else if (pQuantity.IsObjectiveFunctionMinimize) lCol = AppSS.FirstReferencedWindow.Resources["EmsPanelBorder_Green"] as SolidColorBrush;
                        else lCol = AppSS.FirstReferencedWindow.Resources["EmsPanelBorder_Gray"] as SolidColorBrush;

                        LineSeries l = new LineSeries()
                            {
                            Title = $"{pQuantity.QuantitySource.Wpf_ProblemQuantityGroup} - {pQuantity.QuantitySource.Wpf_ProblemQuantityName}",
                            Values = new ChartValues<double>(from a in _points select a.ProblemQuantityOutputs[pQuantity].AggregatedValue ?? Double.NaN),

                            StrokeThickness = 1d,
                            Fill = new SolidColorBrush() {Opacity = 0d},
                            PointGeometrySize = 5d,
                            LineSmoothness = 0d,

                            PointForeground = lCol,
                            Foreground = lCol,
                            Stroke = lCol,
                        };

                        ChartDisplayData_Series pair = new ChartDisplayData_Series(l);
                        pair.VisibilityUpdated += lf_chartDisplayData_SeriesAxisPair_VisibilityChanged;

                        tmpList.Add(new ChartDisplayData()
                            {
                            RelatedQuantity = pQuantity,
                            SeriesData = new[] { pair }
                            });
                    }

                    _chartData = tmpList;
                }

                return _chartData;
            }
        }
        
        private ICollectionView _plotGrasshopperInputList;
        public ICollectionView ChartGrasshopperInputList
        {
            get
            {
                if (_plotGrasshopperInputList == null)
                {
                    _plotGrasshopperInputList = (new CollectionViewSource() {Source = ChartData }).View;
                    _plotGrasshopperInputList.Filter = inO => (inO is ChartDisplayData cData) && (cData.RelatedQuantity is Input_ParamDefBase);
                    _plotGrasshopperInputList.SortDescriptions.Add(new SortDescription("RelatedQuantity.Name", ListSortDirection.Ascending));
                }

                return _plotGrasshopperInputList;
            }
        }
        private ICollectionView _plotProblemQuantityList;
        public ICollectionView ChartProblemQuantityList
        {
            get
            {
                if (_plotProblemQuantityList == null)
                {
                    _plotProblemQuantityList = (new CollectionViewSource() { Source = ChartData }).View;
                    _plotProblemQuantityList.Filter = inO => (inO is ChartDisplayData cData) && (cData.RelatedQuantity is ProblemQuantity);
                    _plotProblemQuantityList.SortDescriptions.Add(new SortDescription("RelatedQuantity.InternalId", ListSortDirection.Ascending));
                }

                return _plotProblemQuantityList;
            }
        }
        #endregion

        public void WpfCommand_DeleteProblemConfiguration()
        {
            this.Reset();
            AppSS.I.SolveMgr.ProblemConfigs.Remove(this);
        }
        public void WpfCommand_ClearOptimizationData()
        {
            Reset();
        }
    }

    public abstract class ProblemConfig_ElementCombinationValue
    {
        public sealed class ProblemConfig_ElementCombinationValue_ListComparer : IEqualityComparer<List<ProblemConfig_ElementCombinationValue>>
        {
            public bool Equals(List<ProblemConfig_ElementCombinationValue> x, List<ProblemConfig_ElementCombinationValue> y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;

                // Based on the HashCodes of the lists, which in turn is based on the contents of the list
                return GetHashCode(x) == GetHashCode(y); 
            }

            public int GetHashCode(List<ProblemConfig_ElementCombinationValue> obj)
            {
                unchecked
                {
                    int hash = 19;
                    foreach (ProblemConfig_ElementCombinationValue foo in obj)
                    {
                        hash = hash * 31 + foo.GetHashCode();
                    }
                    return hash;
                }
            }
        }
        public sealed class ProblemConfig_ElementCombinationValue_Comparer : IComparer<ProblemConfig_ElementCombinationValue>
        {
            public int Compare(ProblemConfig_ElementCombinationValue x, ProblemConfig_ElementCombinationValue y)
            {
                if (x == null || y == null) return 0;

                int typeNameCompare = x.Element_TypeName.CompareTo(y.Element_TypeName);
                if (typeNameCompare != 0) return typeNameCompare;

                return x.Element_Name.CompareTo(y.Element_Name);
            }
        }

        public virtual string Element_Name { get; }
        public virtual string Element_TypeName { get; }
        public virtual string Wpf_ConfigValue_Display { get; }
    }
    public class ProblemConfig_GhLineListConfigValues : ProblemConfig_ElementCombinationValue
    {
        public ProblemConfig_GhLineListConfigValues([NotNull] LineList_GhGeom_ParamDef inGhLineList, [NotNull] FeSection inSection)
        {
            GhLineList = inGhLineList ?? throw new ArgumentNullException(nameof(inGhLineList));
            Section = inSection ?? throw new ArgumentNullException(nameof(inSection));
        }

        public LineList_GhGeom_ParamDef GhLineList { get; set; }
        public FeSection Section { get; set; }

        public override string Element_Name => GhLineList.Name;
        public override string Element_TypeName => GhLineList.TypeName;
        public override string Wpf_ConfigValue_Display => Section.NameFixed;

        public override string ToString()
        {
            return $"{GhLineList} - {Section}";
        }
    }
    public class ProblemConfig_GhIntegerInputConfigValues : ProblemConfig_ElementCombinationValue
    {
        public ProblemConfig_GhIntegerInputConfigValues([NotNull] Integer_GhConfig_ParamDef inGhIntegerGhConfig, int inValue)
        {
            GhIntegerGhConfig = inGhIntegerGhConfig ?? throw new ArgumentNullException(nameof(inGhIntegerGhConfig));
            Value = inValue;
        }

        public Integer_GhConfig_ParamDef GhIntegerGhConfig { get; set; }
        public int Value { get; set; }

        public override string Element_Name => GhIntegerGhConfig.Name;
        public override string Element_TypeName => GhIntegerGhConfig.TypeName;
        public override string Wpf_ConfigValue_Display => Value.ToString();

        public override string ToString()
        {
            return $"{GhIntegerGhConfig} - {Value}";
        }
    }

    public class Wpf_CalculatingInputDisplay_Data
    {
        public Wpf_CalculatingInputDisplay_Data(int inIndex, string inParameter, double inLowerBoundary, double inUpperBoundary, double inCalculating)
        {
            Index = inIndex;
            Parameter = inParameter;
            LowerBoundary = inLowerBoundary;
            UpperBoundary = inUpperBoundary;
            Calculating = inCalculating;
        }

        public int Index { get; }
        public string Parameter { get; }
        public double Last { get; } = 0d;
        public double Calculating { get; }
        public double Delta { get; } = 0d;

        public double LowerBoundary { get; }
        public double UpperBoundary { get; }
    }
    
}
