using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord;
using Accord.Math;

namespace AccordHelper.Opt.ParamDefinitions
{
    [Serializable]
    public class IntegerValueRange : ValueRangeBase
    {
        public IntegerValueRange(int inMin, int inMax) : this(new IntRange(inMin, inMax))
        {
        }

        public IntegerValueRange(IntRange inRange)
        {
            _range = inRange;
        }

        private IntRange _range;
        public IntRange Range
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
                if (int.TryParse(value, out int val)) Range = new IntRange(val, _range.Max);
                else throw new Exception($"Min value {value} is not valid for {GetType()}.");
            }
        }
        public override string Max_DisplayString
        {
            get => $"{_range.Max}";
            set
            {
                if (int.TryParse(value, out int val)) Range = new IntRange(_range.Min, val);
                else throw new Exception($"Max value {value} is not valid for {GetType()}.");
            }
        }

        public bool IsInside(int inValue)
        {
            return Range.IsInside(inValue);
        }
        public int Scale(int inValue, IntegerValueRange inToRange)
        {
            return Scale(inValue, inToRange.Range);
        }
        public int Scale(int inValue, IntRange inToRange)
        {
            return inValue.Scale(Range, inToRange);
        }
        public int DistanceFrom(int inValue)
        {
            if (IsInside(inValue)) return 0;
            return Math.Max(inValue - Range.Max, Range.Min - inValue);
        }
    }
}
