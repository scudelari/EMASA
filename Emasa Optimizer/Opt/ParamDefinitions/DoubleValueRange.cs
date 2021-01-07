using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.Helpers.Accord;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    [Serializable]
    public class DoubleValueRange : ValueRangeBase, IEquatable<DoubleValueRange>
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

        public double Mid => (_range.Max - _range.Min) / 2d + _range.Min;

        public override string WpfMinString
        {
            get => $"{_range.Min}";
            set
            {
                if (double.TryParse(value, out double val)) Range = new DoubleRange(val, _range.Max);
                else throw new Exception($"Min value {value} is not valid for {GetType()}.");
            }
        }
        public override string WpfMaxString
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


        #region IEquality based on the Double Range, which is based on the min and max
        public bool Equals(DoubleValueRange other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _range.Equals(other._range);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DoubleValueRange)obj);
        }

        public override int GetHashCode()
        {
            return _range.GetHashCode();
        }

        public static bool operator ==(DoubleValueRange left, DoubleValueRange right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DoubleValueRange left, DoubleValueRange right)
        {
            return !Equals(left, right);
        } 
        #endregion
    }
}
