using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.WpfResources;
using MathNet.Numerics.Statistics;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public class ProblemQuantityAggregator : BindableBase, IEquatable<ProblemQuantityAggregator>
    {
        public ProblemQuantityAggregator()
        {
            // Monitors changes in the Range
            ScaleRange.PropertyChanged += ScaleRangeOnPropertyChanged;
        }

        private bool _isDisplayOnly = false;
        /// <summary>
        /// This is a flag that is used when raising events when options change.
        /// If this is true, it will Raise the change data events when the options are changed by the user.
        /// Use set this to true when the aggregator is being used for display report.
        /// </summary>
        public bool IsDisplayOnly
        {
            get => _isDisplayOnly;
            set
            {
                _isDisplayOnly = value;
            }
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
            set
            {
                SetProperty(ref _useTableAbsoluteValues, value);

                if (IsDisplayOnly)
                {
                    // Marks that an update in the data must be made
                    IsUpdatePending = true;

                    // Tells the interface that the display aggregator changed
                    AppSS.I.NlOptDetails_DisplayAggregator_Changed();
                }
            }
        }

        private bool _hasScale;
        public bool HasScale
        {
            get => _hasScale;
            set
            {
                SetProperty(ref _hasScale, value);

                if (IsDisplayOnly)
                {
                    // Marks that an update in the data must be made
                    IsUpdatePending = true;

                    // Tells the interface that the display aggregator changed
                    AppSS.I.NlOptDetails_DisplayAggregator_Changed();
                }
            }
        }
        private DoubleValueRange _scaleRange = new DoubleValueRange(0d, 1d);
        public DoubleValueRange ScaleRange
        {
            get => _scaleRange;
            private set => SetProperty(ref _scaleRange, value);
        }
        private void ScaleRangeOnPropertyChanged(object inSender, PropertyChangedEventArgs inE)
        {
            if (IsDisplayOnly)
            {
                // Marks that an update in the data must be made
                IsUpdatePending = true;

                // Tells the interface that the display aggregator changed
                AppSS.I.NlOptDetails_DisplayAggregator_Changed();
            }
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
                        ScaleRange.Range = new DoubleRange(0d, FeMaterial.GetAllMaterials().Max(a => a.Fu) * 1.2d);
                        break;

                    // Strains are from 0 to 5%
                    case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                        ScaleRange.Range = new DoubleRange(0d, 0.05d);
                        break;


                    // Code check is from 0 to 1.5
                    case FeResultTypeEnum.ElementNodal_CodeCheck:
                        ScaleRange.Range = new DoubleRange(0d, 1.5d);
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
            
            // ScaleRange.Range = new DoubleRange(0d, 1d);
        }
        
        private bool _useEntityFilter = false;
        public bool UseEntityFilter
        {
            get => _useEntityFilter;
            set
            {
                SetProperty(ref _useEntityFilter, value);
                RaisePropertyChanged("EntityFilter_ComboBoxVisibility");

                // Automatically selects the entity to filter
                if (EntityToFilter == null)
                {
                    EntityToFilter = AppSS.I.Gh_Alg.GeometryDefs_PointLineListBundle_View.OfType<GhGeom_ParamDefBase>().First();
                }

                if (IsDisplayOnly)
                {
                    // Marks that an update in the data must be made
                    IsUpdatePending = true;

                    // Tells the interface that the display aggregator changed
                    AppSS.I.NlOptDetails_DisplayAggregator_Changed();
                }
            }
        }
        private GhGeom_ParamDefBase _entityToFilter;
        public GhGeom_ParamDefBase EntityToFilter
        {
            get => _entityToFilter;
            set
            {
                SetProperty(ref _entityToFilter, value);

                if (IsDisplayOnly)
                {
                    // Marks that an update in the data must be made
                    IsUpdatePending = true;

                    // Tells the interface that the display aggregator changed
                    AppSS.I.NlOptDetails_DisplayAggregator_Changed();
                }
            }
        }

        public Visibility EntityFilter_ComboBoxVisibility => UseEntityFilter ? Visibility.Visible : Visibility.Hidden;
        
        public DataTable GetFilteredTable(IProblemQuantitySource inSource, NlOpt_Point inOwnerPoint, bool inAlwaysReturnNewTable = false)
        {
            // If no filter is required, simply ignores
            if (UseEntityFilter == false)
            {
                // Both data and structure
                if (inAlwaysReturnNewTable) return inOwnerPoint?.GetRawOutputTable(inSource).Copy();

                // Returns a reference to the same table.
                return inOwnerPoint.GetRawOutputTable(inSource);
            }

            // Clones the table structure
            DataTable filteredTable = inOwnerPoint.GetRawOutputTable(inSource).Clone();

            IEnumerable<DataRow> filteredRowsEnumerable;

            switch (inSource)
            {
                case FeResultClassification feResultClassification:
                    {
                        // Gets the FeGroup
                        FeGroup group = inOwnerPoint.FeModel.Groups[EntityToFilter.FeGroupNameHelper];

                        // Results are saved by result FAMILY
                        switch (feResultClassification.ResultFamily)
                        {
                            case FeResultFamilyEnum.Nodal_Reaction:
                            case FeResultFamilyEnum.Nodal_Displacement:
                                filteredRowsEnumerable = EnumerableRowCollectionExtensions.Where(inOwnerPoint.GetRawOutputTable(inSource).AsEnumerable(), 
                                    inRow =>
                                    {
                                        FeMeshNode node = inOwnerPoint.FeModel.MeshNodes[inRow.Field<string>("Mesh Node Id")];

                                        // The node matches a joint in the group?
                                        if (node.MatchingJoint != null && group.Joints.Contains(node.MatchingJoint)) return true;

                                        // The node belongs to a Frame in the group?
                                        if (node.LinkedElements != null && group.Frames.Any(f => f.MeshBeamElements.Any(mbe => mbe.MeshNodes.Contains(node)))) return true;

                                        return false;
                                    });
                                break;

                            case FeResultFamilyEnum.ElementNodal_Stress:
                            case FeResultFamilyEnum.ElementNodal_Strain:
                            case FeResultFamilyEnum.ElementNodal_Force:
                            case FeResultFamilyEnum.ElementNodal_BendingStrain:
                                filteredRowsEnumerable = EnumerableRowCollectionExtensions.Where(inOwnerPoint.GetRawOutputTable(inSource).AsEnumerable(),
                                    inRow =>
                                    {
                                        FeMeshNode node = inOwnerPoint.FeModel.MeshNodes[inRow.Field<string>("Mesh Node Id")];
                                        FeMeshBeamElement beam = inOwnerPoint.FeModel.MeshBeamElements[inRow.Field<string>("Element Id")];

                                        // The node matches a joint in the group?
                                        if (node.MatchingJoint != null && group.Joints.Contains(node.MatchingJoint)) return true;

                                        // The node belongs to a Frame in the group?
                                        if (node.LinkedElements != null && group.Frames.Any(f => f.MeshBeamElements.Any(mbe => mbe.MeshNodes.Contains(node)))) return true;

                                        // The element belongs to a frame in the group
                                        if (group.Frames.Contains(beam.OwnerFrame)) return true;

                                        return false;
                                    });
                                break;


                            case FeResultFamilyEnum.SectionNode_Stress:
                            case FeResultFamilyEnum.SectionNode_Strain:
                                filteredRowsEnumerable = EnumerableRowCollectionExtensions.Where(inOwnerPoint.GetRawOutputTable(inSource).AsEnumerable(),
                                    inRow =>
                                    {
                                        FeMeshNode node = inOwnerPoint.FeModel.MeshNodes[inRow.Field<string>("Owner Mesh Node Id")];
                                        FeMeshBeamElement beam = inOwnerPoint.FeModel.MeshBeamElements[inRow.Field<string>("Element Id")];

                                        // The node matches a joint in the group?
                                        if (node.MatchingJoint != null && group.Joints.Contains(node.MatchingJoint)) return true;

                                        // The node belongs to a Frame in the group?
                                        if (node.LinkedElements != null && group.Frames.Any(f => f.MeshBeamElements.Any(mbe => mbe.MeshNodes.Contains(node)))) return true;

                                        // The element belongs to a frame in the group
                                        if (group.Frames.Contains(beam.OwnerFrame)) return true;

                                        return false;
                                    });
                                break;


                            case FeResultFamilyEnum.Others:
                                switch (feResultClassification.ResultType)
                                {
                                    case FeResultTypeEnum.ElementNodal_CodeCheck: // Same as for the Element Nodals as this is also an Element Nodal Result
                                        filteredRowsEnumerable = EnumerableRowCollectionExtensions.Where(inOwnerPoint.GetRawOutputTable(inSource).AsEnumerable(),
                                            inRow =>
                                            {
                                                FeMeshNode node = inOwnerPoint.FeModel.MeshNodes[inRow.Field<string>("Mesh Node Id")];
                                                FeMeshBeamElement beam = inOwnerPoint.FeModel.MeshBeamElements[inRow.Field<string>("Element Id")];

                                                // The node matches a joint in the group?
                                                if (node.MatchingJoint != null && group.Joints.Contains(node.MatchingJoint)) return true;

                                                // The node belongs to a Frame in the group?
                                                if (node.LinkedElements != null && group.Frames.Any(f => f.MeshBeamElements.Any(mbe => mbe.MeshNodes.Contains(node)))) return true;

                                                // The element belongs to a frame in the group
                                                if (group.Frames.Contains(beam.OwnerFrame)) return true;

                                                return false;
                                            });
                                        break;

                                    case FeResultTypeEnum.Element_StrainEnergy:
                                        filteredRowsEnumerable = EnumerableRowCollectionExtensions.Where(inOwnerPoint.GetRawOutputTable(inSource).AsEnumerable(),
                                            inRow =>
                                            {
                                                FeMeshBeamElement beam = inOwnerPoint.FeModel.MeshBeamElements[inRow.Field<string>("Element Id")];

                                                // The element belongs to a frame in the group
                                                if (group.Frames.Contains(beam.OwnerFrame)) return true;

                                                return false;
                                            });
                                        break;

                                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                                        // Filtering is not applicable
                                        return inOwnerPoint.GetRawOutputTable(inSource);

                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;

                case DoubleList_GhGeom_ParamDef doubleList_GhGeom_ParamDef:
                    return inOwnerPoint.GetRawOutputTable(inSource);

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Copies the filtered rows
            foreach (DataRow dataRow in filteredRowsEnumerable)
            {
                filteredTable.ImportRow(dataRow);
            }

            return filteredTable;
        }
        public void ApplyTransformationsToTableColumn(DataTable inFilteredTable, IProblemQuantitySource inSource)
        {
            if (inSource is FeResultClassification feRes)
            {
                if (feRes.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor)
                {
                    // Keeps only first row
                    for (int i = inFilteredTable.Rows.Count - 1; i >= 1; i--)
                    {
                        inFilteredTable.Rows.RemoveAt(i);
                    }

                    return;
                }
                else if (feRes.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor)
                {
                    // Keeps only second row

                    // Deletes all after second
                    for (int i = inFilteredTable.Rows.Count - 2; i >= 1; i--)
                    {
                        inFilteredTable.Rows.RemoveAt(i);
                    }

                    // Deletes first
                    inFilteredTable.Rows.RemoveAt(0);

                    return;
                }
                else if (feRes.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor)
                {
                    // Keeps only second row

                    // Deletes all after third
                    for (int i = inFilteredTable.Rows.Count - 3; i >= 1; i--)
                    {
                        inFilteredTable.Rows.RemoveAt(i);
                    }

                    // Deletes first twice
                    inFilteredTable.Rows.RemoveAt(0);
                    inFilteredTable.Rows.RemoveAt(0);

                    return;
                }
            }

            // Applies the changes into the concerning values
            if (UseTableAbsoluteValues && !HasScale)
            {
                // Only Absolute
                foreach (DataRow row in inFilteredTable.Rows)
                {
                    double absVal = Math.Abs(row.Field<double>(inSource.ConcernedResultColumnName));
                    row.SetField<double>(inSource.ConcernedResultColumnName, absVal);
                }
            }

            if (!UseTableAbsoluteValues && HasScale)
            {
                // Only Scale
                foreach (DataRow row in inFilteredTable.Rows)
                {
                    double scaleVal = row.Field<double>(inSource.ConcernedResultColumnName).Scale(ScaleRange.Range, new DoubleRange(0d, 1d));
                    row.SetField<double>(inSource.ConcernedResultColumnName, scaleVal);
                }
            }

            if (UseTableAbsoluteValues && HasScale)
            {
                // Both
                foreach (DataRow row in inFilteredTable.Rows)
                {
                    try
                    {
                        double absVal = Math.Abs(row.Field<double>(inSource.ConcernedResultColumnName));
                        double scaleVal = absVal.Scale(ScaleRange.Range, new DoubleRange(0d, 1d));
                        row.SetField<double>(inSource.ConcernedResultColumnName, scaleVal);
                    }
                    catch (Exception e )
                    {
                        int a = 0;
                        a++;
                        throw;
                    }
                }
            }
        }

        public List<double> GetValuesUsedInAggregate(DataTable inFilteredTable, IProblemQuantitySource inSource)
        {
            List<double> valsUsedInAggregate = null;

            // The eigenvalue buckling gets special treatment because it searches the inside of the output table
            if (inSource is FeResultClassification feRes && (
                feRes.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor ||
                feRes.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor ||
                feRes.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor))
            {
                // Gets a list of the non-negative values. The table should be already ordered by the mode.
                List<double> nonNegs = (from a in inFilteredTable.AsEnumerable()
                                        where a.Field<double>(inSource.ConcernedResultColumnName) > 0d
                                        select a.Field<double>(inSource.ConcernedResultColumnName)).ToList();

                switch (feRes.ResultType)
                {
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                        valsUsedInAggregate = new List<double>() { nonNegs[0] };
                        break;

                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                        valsUsedInAggregate = new List<double>() { nonNegs[1] };
                        break;

                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                        valsUsedInAggregate = new List<double>() { nonNegs[2] };
                        break;

                    default: throw new Exception("Unexpected value of the Result Type. Should be an Eigenvalue Buckling.");
                }
            }
            else
            { // Treatment of all others
                string columnName = inSource.ConcernedResultColumnName;
                if (string.IsNullOrWhiteSpace(columnName)) throw new Exception($"Could not find a concerned column name in the output table.");

                // Chains the transformation operations
                IEnumerable<double> concernedValues = from a in inFilteredTable.AsEnumerable() select a.Field<double>(columnName);
                IEnumerable<double> applyAbsolute_ConcernedValues = UseTableAbsoluteValues ? concernedValues.Select(Math.Abs) : concernedValues;
                IEnumerable<double> applyScale_ConcernedValues = HasScale ? applyAbsolute_ConcernedValues.Select(a => a.Scale(ScaleRange.Range, new DoubleRange(0d, 1d))) : applyAbsolute_ConcernedValues;

                valsUsedInAggregate = applyScale_ConcernedValues?.ToList();
            }

            return valsUsedInAggregate;
        }
        public double? GetAggregateValue(List<double> inListToAggregate, Quantity_AggregateTypeEnum? inAggregateType = null)
        {
            // Empty list returns null
            if (inListToAggregate == null || inListToAggregate.Count == 0) return null;

            // If the desired aggregate is not given, uses the one that is set
            Quantity_AggregateTypeEnum desiredAggregation = inAggregateType ?? AggregateType;

            // Really aggregates the values
            switch (desiredAggregation)
            {
                case Quantity_AggregateTypeEnum.Max:
                    return inListToAggregate.Max();

                case Quantity_AggregateTypeEnum.Min:
                    return inListToAggregate.Min();

                case Quantity_AggregateTypeEnum.Mean:
                    return inListToAggregate.Average();

                case Quantity_AggregateTypeEnum.StandardDeviation:
                    return inListToAggregate.StandardDeviation();

                case Quantity_AggregateTypeEnum.Sum:
                    return inListToAggregate.Sum();

                case Quantity_AggregateTypeEnum.Product:
                    return inListToAggregate.Aggregate(1d, (total, next) =>
                    {
                        total *= next;
                        return total;
                    });

                default:
                    return null;
            }
        }
        
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

        #region For when this instance is used as management of point output display in WPF
        private bool IsUpdatePending = false;

        private IProblemQuantitySource _wpfDisplayProblemQuantitySource = null;
        public IProblemQuantitySource WpfDisplayProblemQuantitySource
        {
            get => _wpfDisplayProblemQuantitySource;
            set
            {
                _wpfDisplayProblemQuantitySource = value;
                IsUpdatePending = true;
            }
        }

        private NlOpt_Point _wpfDisplayPoint = null;
        public NlOpt_Point WpfDisplayPoint
        {
            get => _wpfDisplayPoint;
            set
            {
                _wpfDisplayPoint = value;
                IsUpdatePending = true;
            }
        }

        private int _wpfPointDisplay_Count;
        public int WpfPointDisplay_Count
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_Count;
            }
            set => SetProperty(ref _wpfPointDisplay_Count, value);
        }

        private double? _wpfPointDisplay_Max;
        public double? WpfPointDisplay_Max
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_Max;
            }
            set => SetProperty(ref _wpfPointDisplay_Max, value);
        }

        private double? _wpfPointDisplay_Min;
        public double? WpfPointDisplay_Min
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_Min;
            }
            set => SetProperty(ref _wpfPointDisplay_Min, value);
        }

        private double? _wpfPointDisplay_Mean;
        public double? WpfPointDisplay_Mean
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_Mean;
            }
            set => SetProperty(ref _wpfPointDisplay_Mean, value);
        }

        private double? _wpfPointDisplay_StDev;
        public double? WpfPointDisplay_StDev
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_StDev;
            }
            set => SetProperty(ref _wpfPointDisplay_StDev, value);
        }

        private double? _wpfPointDisplay_Sum;
        public double? WpfPointDisplay_Sum
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_Sum;
            }
            set => SetProperty(ref _wpfPointDisplay_Sum, value);
        }

        private double? _wpfPointDisplay_Product;
        public double? WpfPointDisplay_Product
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_Product;
            }
            set => SetProperty(ref _wpfPointDisplay_Product, value);
        }

        private DataTable _wpfPointDisplay_FilteredTransformedTable;
        public DataTable WpfPointDisplay_FilteredTransformedTable
        {
            get
            {
                if (IsUpdatePending) WpfPointData_DataUpdate();
                return _wpfPointDisplay_FilteredTransformedTable;
            }
            set => SetProperty(ref _wpfPointDisplay_FilteredTransformedTable, value);
        }

        public void WpfPointDisplayDataGridAutoGeneratedColumns(object sender, EventArgs args)
        {
            if (sender is DataGrid dg)
            {
                foreach (DataGridColumn dataGridColumn in dg.Columns)
                {
                    if (dataGridColumn is DataGridTextColumn textCol)
                    {
                        try
                        {
                            // Tries to get the DataColumn that is used in this GridColumn
                            DataColumn dCol = WpfPointDisplay_FilteredTransformedTable.Columns[textCol.Header as string];

                            // Sets the double converter
                            if (dCol.DataType == typeof(double))
                            {
                                (textCol.Binding as Binding).Converter = new DefaultNumberConverter();
                                //textCol.FontFamily = new FontFamily("Lucida Console");
                            }

                            // Highlights the header if the column is the one mentioned
                            if ((textCol.Header as string) == AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay.ConcernedResultColumnName)
                            {
                                textCol.HeaderStyle = dg.FindResource("WpfPointDisplayDataGrid_SelectedDataHeaderStyle") as Style;
                            }
                        }
                        catch 
                        {
                            continue; // Could not find, simply ignores
                        }

                    }
                }
            }
        }
        
        public void WpfPointData_DataUpdate(IProblemQuantitySource inSource = null, NlOpt_Point inPoint = null)
        {
            IsUpdatePending = false;

            // Ensures that we have a selected type for output
            if (AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay == null)
            {
                if (inSource != null) AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay = inSource;
                else
                {
                    AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay = AppSS.I.ProbQuantMgn.WpfProblemQuantities_ObjectiveFunction.OfType<IProblemQuantitySource>().First();
                }
            }

            IProblemQuantitySource source = inSource ?? AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay;
            NlOpt_Point point = inPoint ?? AppSS.I.SolveMgr.Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint;

            // Applies the filters to the given table and updates the display
            _wpfPointDisplay_FilteredTransformedTable = GetFilteredTable(source, point, true);
            ApplyTransformationsToTableColumn(_wpfPointDisplay_FilteredTransformedTable, source);

            List<double> vals = (from a in _wpfPointDisplay_FilteredTransformedTable.AsEnumerable() select a.Field<double>(source.ConcernedResultColumnName)).ToList();
            
            // Updates the aggregate values
            _wpfPointDisplay_Max = GetAggregateValue(vals, Quantity_AggregateTypeEnum.Max);
            _wpfPointDisplay_Min = GetAggregateValue(vals, Quantity_AggregateTypeEnum.Min);
            _wpfPointDisplay_Mean = GetAggregateValue(vals, Quantity_AggregateTypeEnum.Mean);
            _wpfPointDisplay_StDev = GetAggregateValue(vals, Quantity_AggregateTypeEnum.StandardDeviation);
            _wpfPointDisplay_Sum = GetAggregateValue(vals, Quantity_AggregateTypeEnum.Sum);
            _wpfPointDisplay_Product = GetAggregateValue(vals, Quantity_AggregateTypeEnum.Product);
            _wpfPointDisplay_Count = vals?.Count ?? 0;

            // Updates the charts
            AppSS.I.ChartDisplayMgr.UpdateDistributionChart(AppSS.I.ChartDisplayMgr.NlOptPointDetails_CartesianChart, vals);
        }

        public void CopySettingsFrom(ProblemQuantityAggregator inOther)
        {
            _useTableAbsoluteValues = inOther.UseTableAbsoluteValues;
            _hasScale = inOther.HasScale;
            _scaleRange.Range = new DoubleRange(inOther.ScaleRange.Range.Min, inOther.ScaleRange.Range.Max);
            _useEntityFilter = inOther.UseEntityFilter;
            _entityToFilter = inOther.EntityToFilter;

            IsUpdatePending = true;
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
