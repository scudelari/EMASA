extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;

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
        public NlOpt_Point LastPoint => _points?.Last();

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
                List<Wpf_CalculatingInputDisplay_Data> list = new List<Wpf_CalculatingInputDisplay_Data>();

                for (int i = 0; i < AppSS.I.Gh_Alg.InputDefs_VarCount; i++)
                {
                    list.Add(new Wpf_CalculatingInputDisplay_Data(i, 
                        AppSS.I.Gh_Alg.GetInputParameterNameByIndex(i),
                        LastPoint != null ? LastPoint.InputValuesAsDoubleArray[i] : double.NaN,
                        CurrentlyCalculatingInput[i],
                        LastPoint != null ? CurrentlyCalculatingInput[i] - LastPoint.InputValuesAsDoubleArray[i] : CurrentlyCalculatingInput[i]));
                }

                return CollectionViewSource.GetDefaultView(list);
            }
        }

        #endregion
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
        public Wpf_CalculatingInputDisplay_Data(int inIndex, string inParameter, double inLast, double inCalculating, double inDelta)
        {
            Index = inIndex;
            Parameter = inParameter;
            Last = inLast;
            Calculating = inCalculating;
            Delta = inDelta;
        }

        public int Index { get; }
        public string Parameter { get; }
        public double Last { get; }
        public double Calculating { get; }
        public double Delta { get; }
    }
}
