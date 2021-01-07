using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt.ParamDefinitions;
using MathNet.Numerics.Statistics;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public class NlOpt_Point_ProblemQuantity_Output : BindableBase
    {
        private NlOpt_Point _ownerNlOptPoint;
        [NotNull] public ProblemQuantity Quantity { get; }

        public NlOpt_Point_ProblemQuantity_Output([NotNull] NlOpt_Point inOwnerNlOptPoint, [NotNull] ProblemQuantity inQuantity)
        {
            _ownerNlOptPoint = inOwnerNlOptPoint ?? throw new ArgumentNullException(nameof(inOwnerNlOptPoint));
            Quantity = inQuantity ?? throw new ArgumentNullException(nameof(inQuantity));

            // Gets and saves a reference to the Raw Data Table
            RawDataTable = inOwnerNlOptPoint.GetRawOutputTable(inQuantity.QuantitySource);
        }

        public DataTable RawDataTable { get; private set; }

        private DataTable _filteredDataTable = null;
        public DataTable FilteredOutputTable
        {
            get
            {
                // Checks if the filtered table already exists
                if (_filteredDataTable == null) _filteredDataTable = Quantity.QuantityAggregatorOptions.GetFilteredTable(Quantity.QuantitySource, _ownerNlOptPoint);

                return _filteredDataTable;
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
                        _valuesUsedInAggregate = Quantity.QuantityAggregatorOptions.GetValuesUsedInAggregate(FilteredOutputTable, Quantity.QuantitySource);
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
                if (_aggregatedValue == null) _aggregatedValue = Quantity.QuantityAggregatorOptions.GetAggregateValue(ValuesUsedInAggregate);

                return _aggregatedValue;
            }
        }

        public bool ConstraintIsRespected
        {
            get
            {
                if (!AggregatedValue.HasValue) return false;

                return NlOpt_Point_ConstraintData.IsRespectedStatic(Quantity, AggregatedValue.Value);
            }
        }

        public bool IsSuccess { get; set; } = true;
        public Exception FailureMessage { get; set; } = null;

        public void Wpf_UsesSelected_IProblemQuantitySource_Updated()
        {
            RaisePropertyChanged("Wpf_UsesSelected_IProblemQuantitySource");
        }
        public bool Wpf_UsesSelected_IProblemQuantitySource => Quantity.QuantitySource == AppSS.I.ProbQuantMgn.Wpf_SelectedProblemQuantityTypeForOutputDisplay;
    }
}
