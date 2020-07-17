using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord;
using Accord.Math;
using Prism.Mvvm;

namespace AccordHelper.Opt.ParamDefinitions
{
    [Serializable]
    public class DoubleValueRange : ValueRangeBase
    {
        public DoubleValueRange(double inMin, double inMax) : this(new DoubleRange(inMin, inMax))
        {
        }

        public DoubleValueRange(DoubleRange inRange)
        {
            _range = inRange;
        }

        private DoubleRange _range;
        public DoubleRange Range
        {
            get => _range;
            set
            {
                SetProperty(ref _range, value);
                DisplayVariablesChanged();
            }
        }

        public override string Min_DisplayString
        {
            get => $"{_range.Min}";
            set
            {
                if (double.TryParse(value, out double val)) Range = new DoubleRange(val, _range.Max);
                else throw new Exception($"Min value {value} is not valid for {GetType()}.");
            }
        }
        public override string Max_DisplayString
        {
            get => $"{_range.Max}";
            set
            {
                if (double.TryParse(value, out double val)) Range = new DoubleRange(_range.Min, val);
                else throw new Exception($"Max value {value} is not valid for {GetType()}.");
            }
        }

        public bool IsInside(double inValue)
        {
            return Range.IsInside(inValue);
        }
        public double Scale(double inValue, DoubleValueRange inToRange)
        {
            return Scale(inValue, inToRange.Range);
        }
        public double Scale(double inValue, DoubleRange inToRange)
        {
            return inValue.Scale(Range, inToRange);
        }
        public double DistanceFrom(double inValue)
        {
            if (IsInside(inValue)) return 0d;
            return Math.Max(inValue - Range.Max, Range.Min - inValue);
        }
    }
}
