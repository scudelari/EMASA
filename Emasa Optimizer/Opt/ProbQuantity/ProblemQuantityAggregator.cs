using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.WpfResources;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public class ProblemQuantityAggregator : BindableBase, IEquatable<ProblemQuantityAggregator>
    {
        public ProblemQuantityAggregator()
        {
        }


        public Dictionary<Quantity_AggregateTypeEnum, string> Quantity_AggregateTypeEnumDescriptions => ListDescSH.I.Quantity_AggregateTypeEnumDescriptions;
        private Quantity_AggregateTypeEnum _aggregateType = Quantity_AggregateTypeEnum.Max;
        public Quantity_AggregateTypeEnum AggregateType
        {
            get => _aggregateType;
            set => SetProperty(ref _aggregateType, value);
        }


        private bool _useTableAbsoluteValues = true;
        public bool UseTableAbsoluteValues
        {
            get => _useTableAbsoluteValues;
            set => SetProperty(ref _useTableAbsoluteValues, value);
        }


        private bool _hasScale;
        public bool HasScale
        {
            get => _hasScale;
            set
            {
                if (value)
                {
                    SetDefaultScale();
                }
                else
                {
                    ScaleRange = new DoubleValueRange(0d, 1d);
                }

                SetProperty(ref _hasScale, value);
            }
        }
        private DoubleValueRange _scaleRange;
        public DoubleValueRange ScaleRange
        {
            get => _scaleRange;
            set => SetProperty(ref _scaleRange, value);
        }
        public void SetDefaultScale(FeResultTypeEnum? inFeResultType = null)
        {
            if (inFeResultType.HasValue)
            {
                switch (inFeResultType.Value)
                {
                    // Stresses are from 0Mpa to 1.2 max Material Fu
                    case FeResultTypeEnum.ElementNodal_Stress_SDir:
                    case FeResultTypeEnum.ElementNodal_Stress_SByT:
                    case FeResultTypeEnum.ElementNodal_Stress_SByB:
                    case FeResultTypeEnum.ElementNodal_Stress_SBzT:
                    case FeResultTypeEnum.ElementNodal_Stress_SBzB:

                    case FeResultTypeEnum.SectionNode_Stress_S1:
                    case FeResultTypeEnum.SectionNode_Stress_S2:
                    case FeResultTypeEnum.SectionNode_Stress_S3:
                    case FeResultTypeEnum.SectionNode_Stress_SInt:
                    case FeResultTypeEnum.SectionNode_Stress_SEqv:
                        ScaleRange = new DoubleValueRange(0d, FeMaterial.GetAllMaterials().Max(a => a.Fu) * 1.2d);
                        break;

                    // Strains are from 0 to 5%
                    case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                        ScaleRange = new DoubleValueRange(0d, 0.05d);
                        break;


                    // Code check is from 0 to 1.5
                    case FeResultTypeEnum.ElementNodal_CodeCheck:
                        ScaleRange = new DoubleValueRange(0d, 1.5d);
                        break;


                    // Default Range
                    case FeResultTypeEnum.Element_StrainEnergy:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:

                    default:
                        break;
                }
            }
            
            ScaleRange = new DoubleValueRange(0d, 1d);

        }
        

        private bool _useEntityFilter = false;
        public bool UseEntityFilter
        {
            get => _useEntityFilter;
            set
            {
                SetProperty(ref _useEntityFilter, value);
                RaisePropertyChanged("EntityFilter_ComboBoxVisibility");
            }
        }
        private GhGeom_ParamDefBase _entityToFilter;
        public GhGeom_ParamDefBase EntityToFilter
        {
            get => _entityToFilter;
            set => SetProperty(ref _entityToFilter, value);
        }
        public Visibility EntityFilter_ComboBoxVisibility => UseEntityFilter ? Visibility.Visible : Visibility.Hidden;


        #region IEquitable based on values of all fields

        public bool Equals(ProblemQuantityAggregator other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _aggregateType == other._aggregateType && _useTableAbsoluteValues == other._useTableAbsoluteValues && _hasScale == other._hasScale && Equals(_scaleRange, other._scaleRange) && _useEntityFilter == other._useEntityFilter && Equals(_entityToFilter, other._entityToFilter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProblemQuantityAggregator) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int) _aggregateType;
                hashCode = (hashCode * 397) ^ _useTableAbsoluteValues.GetHashCode();
                hashCode = (hashCode * 397) ^ _hasScale.GetHashCode();
                hashCode = (hashCode * 397) ^ (_scaleRange != null ? _scaleRange.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _useEntityFilter.GetHashCode();
                hashCode = (hashCode * 397) ^ (_entityToFilter != null ? _entityToFilter.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ProblemQuantityAggregator left, ProblemQuantityAggregator right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProblemQuantityAggregator left, ProblemQuantityAggregator right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region WPF
        public string WpfTextDescription
        {
            get
            {
                string toRet = $"{Quantity_AggregateTypeEnumDescriptions[AggregateType]}";
                if (UseEntityFilter) toRet += $" [{EntityToFilter.Name}]";
                if (UseTableAbsoluteValues) toRet += $" |x| ";
                if (HasScale) toRet += " S";
                return toRet;
            }
        }

        public Visibility Wpf_AbsoluteValues => UseTableAbsoluteValues ? Visibility.Visible : Visibility.Collapsed;
        public string Wpf_FilterEntityName => UseEntityFilter && EntityToFilter != null ? EntityToFilter.Name : "All";
        public Visibility Wpf_Scaled => HasScale ? Visibility.Visible : Visibility.Collapsed;
        public string Wpf_AggregateTypeName => Quantity_AggregateTypeEnumDescriptions[AggregateType];

        public string Wpf_SummaryToolTip
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (UseEntityFilter) sb.AppendLine($"Filtered by: {EntityToFilter.Name}");
                if (UseTableAbsoluteValues) sb.AppendLine($"Aggregate Absolute Values");
                if (HasScale) sb.AppendLine($"Has Scale: {ScaleRange.Range.Min:+0.000e+000;-0.000e+000;0.0} - {ScaleRange.Range.Max:+0.000e+000;-0.000e+000;0.0}");
                return sb.ToString();
            }
        }
        #endregion
    }

    public enum Quantity_AggregateTypeEnum
    {
        Max,
        Min,
        Mean,
        StandardDeviation,
        Sum,
        Product,
    }
}
