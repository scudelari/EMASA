using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt.ParamDefinitions;
using MathNet.Numerics.Statistics;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public class SolutionPoint_ProblemQuantity_Output : BindableBase
    {
        private SolutionPoint _ownerSolutionPoint;
        [NotNull] public ProblemQuantity Quantity { get; }

        public SolutionPoint_ProblemQuantity_Output([NotNull] SolutionPoint inOwnerSolutionPoint, [NotNull] ProblemQuantity inQuantity)
        {
            _ownerSolutionPoint = inOwnerSolutionPoint ?? throw new ArgumentNullException(nameof(inOwnerSolutionPoint));
            Quantity = inQuantity ?? throw new ArgumentNullException(nameof(inQuantity));

            // Gets and saves a reference to the Raw Data Table
            RawDataTable = inOwnerSolutionPoint.GetRawOutputTable(inQuantity.QuantitySource);
        }

        public DataTable RawDataTable { get; private set; }

        private DataTable _filteredDataTable = null;
        public DataTable FilteredOutputTable
        {
            get
            {
                // If no filter is required, simply ignores
                if (Quantity.QuantityAggregatorOptions.UseEntityFilter == false) return RawDataTable;

                // Checks if the filtered table already exists
                if (_filteredDataTable != null) return _filteredDataTable;

                // Clones the table structure
                DataTable filteredTable = RawDataTable.Clone();

                IEnumerable<DataRow> filteredRowsEnumerable;

                switch (Quantity.QuantitySource)
                {
                    case FeResultClassification feResultClassification:
                        {
                            // Gets the FeGroup
                            FeGroup group = _ownerSolutionPoint.FeModel.Groups[Quantity.QuantityAggregatorOptions.EntityToFilter.Name];

                            // Results are saved by result FAMILY
                            switch (feResultClassification.ResultFamily)
                            {
                                case FeResultFamilyEnum.Nodal_Reaction:
                                case FeResultFamilyEnum.Nodal_Displacement:
                                    filteredRowsEnumerable = RawDataTable.AsEnumerable().Where(inRow =>
                                    {
                                        FeMeshNode node = _ownerSolutionPoint.FeModel.MeshNodes[inRow.Field<int>("Mesh Node Id")];

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
                                    filteredRowsEnumerable = RawDataTable.AsEnumerable().Where(inRow =>
                                    {
                                        FeMeshNode node = _ownerSolutionPoint.FeModel.MeshNodes[inRow.Field<int>("Mesh Node Id")];
                                        FeMeshBeamElement beam = _ownerSolutionPoint.FeModel.MeshBeamElements[inRow.Field<int>("Element Id")];

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
                                    filteredRowsEnumerable = RawDataTable.AsEnumerable().Where(inRow =>
                                    {
                                        FeMeshNode node = _ownerSolutionPoint.FeModel.MeshNodes[inRow.Field<int>("Owner Mesh Node Id")];
                                        FeMeshBeamElement beam = _ownerSolutionPoint.FeModel.MeshBeamElements[inRow.Field<int>("Element Id")];

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
                                            filteredRowsEnumerable = RawDataTable.AsEnumerable().Where(inRow =>
                                            {
                                                FeMeshNode node = _ownerSolutionPoint.FeModel.MeshNodes[inRow.Field<int>("Mesh Node Id")];
                                                FeMeshBeamElement beam = _ownerSolutionPoint.FeModel.MeshBeamElements[inRow.Field<int>("Element Id")];

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
                                            filteredRowsEnumerable = RawDataTable.AsEnumerable().Where(inRow =>
                                            {
                                                FeMeshBeamElement beam = _ownerSolutionPoint.FeModel.MeshBeamElements[inRow.Field<int>("Element Id")];

                                                // The element belongs to a frame in the group
                                                if (group.Frames.Contains(beam.OwnerFrame)) return true;

                                                return false;
                                            });
                                            break;

                                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                                            // Filtering is not applicable
                                            return RawDataTable;

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
                        return RawDataTable;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Copies the filtered rows
                foreach (DataRow dataRow in filteredRowsEnumerable)
                {
                    filteredTable.ImportRow(dataRow);
                }

                // Saves a reference to the new table
                _filteredDataTable = filteredTable;

                return filteredTable;
            }
        }

        public List<double> _valuesUsedInAggregate = null;
        public List<double> ValuesUsedInAggregate
        {
            get
            {
                // We still didn't acquire and treat the value list to aggregate
                if (_valuesUsedInAggregate == null)
                {
                    try
                    {
                        // The eigenvalue buckling gets special treatment because it searches the inside of the output table
                        if (Quantity.IsFeResult && (
                            Quantity.QuantitySource_AsFeResult.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor ||
                            Quantity.QuantitySource_AsFeResult.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor ||
                            Quantity.QuantitySource_AsFeResult.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor))
                        {
                            // Gets a list of the non-negative values. The table should be already ordered by the mode.
                            List<double> nonNegs = (from a in FilteredOutputTable.AsEnumerable()
                                where a.Field<double>(Quantity.ConcernedResult_ColumnName) > 0d
                                select a.Field<double>(Quantity.ConcernedResult_ColumnName)).ToList();

                            switch (Quantity.QuantitySource_AsFeResult.ResultType)
                            {
                                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                                    _valuesUsedInAggregate = new List<double>() { nonNegs[0] };
                                    break;

                                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                                    _valuesUsedInAggregate = new List<double>() { nonNegs[1] };
                                    break;

                                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                                    _valuesUsedInAggregate = new List<double>() { nonNegs[2] };
                                    break;

                                default: throw new Exception("Unexpected value of the Result Type. Should be an Eigenvalue Buckling.");
                            }
                        }
                        else 
                        { // Treatment of all others
                            string columnName = Quantity.ConcernedResult_ColumnName;
                            if (string.IsNullOrWhiteSpace(columnName)) throw new Exception($"Could not find a concerned column name in the output table.");

                            // Chains the transformation operations
                            IEnumerable<double> _concernedValues = from a in FilteredOutputTable.AsEnumerable() select a.Field<double>(columnName);
                            IEnumerable<double> _applyAbsolute_ConcernedValues = Quantity.QuantityAggregatorOptions.UseTableAbsoluteValues ? _concernedValues.Select(Math.Abs) : _concernedValues;
                            IEnumerable<double> _applyScale_ConcernedValues = Quantity.QuantityAggregatorOptions.HasScale ? _applyAbsolute_ConcernedValues.Select(a => a.Scale(Quantity.QuantityAggregatorOptions.ScaleRange.Range, new DoubleRange(0d, 1d))) : _applyAbsolute_ConcernedValues;

                            _valuesUsedInAggregate = _applyScale_ConcernedValues?.ToList();
                        }
                    }
                    catch (Exception e)
                    {
                        IsSuccess = false;
                        FailureMessage = e; 

                        _valuesUsedInAggregate = null;
                    }
                }

                return _valuesUsedInAggregate;
            }
        }

        private double? _aggregatedValue = null;
        public double? AggregatedValue
        {
            get
            {
                if (_aggregatedValue == null)
                {
                    try
                    {
                        if (ValuesUsedInAggregate != null && ValuesUsedInAggregate.Count > 0)
                        {
                            // Really aggregates the values
                            switch (Quantity.QuantityAggregatorOptions.AggregateType)
                            {
                                case Quantity_AggregateTypeEnum.Max:
                                    _aggregatedValue = ValuesUsedInAggregate.Max();
                                    break;

                                case Quantity_AggregateTypeEnum.Min:
                                    _aggregatedValue = ValuesUsedInAggregate.Min();
                                    break;

                                case Quantity_AggregateTypeEnum.Mean:
                                    _aggregatedValue = ValuesUsedInAggregate.Average();
                                    break;

                                case Quantity_AggregateTypeEnum.StandardDeviation:
                                    _aggregatedValue = ValuesUsedInAggregate.StandardDeviation();
                                    break;

                                case Quantity_AggregateTypeEnum.Sum:
                                    _aggregatedValue = ValuesUsedInAggregate.Sum();
                                    break;

                                case Quantity_AggregateTypeEnum.Product:
                                    _aggregatedValue = ValuesUsedInAggregate.Aggregate(1d, (total, next) =>
                                    {
                                        total *= next;
                                        return total;
                                    });
                                    break;

                                default:
                                    _aggregatedValue = null;
                                    break;

                            }
                        }
                    }
                    catch
                    {
                        _aggregatedValue = null;
                    }
                }

                return _aggregatedValue;
            }
        }

        public bool IsSuccess { get; set; } = true;
        public Exception FailureMessage { get; set; } = null;
    }
}
