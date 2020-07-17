using System;
using System.Globalization;
using System.Windows.Controls;

namespace BaseWPFLibrary.ValidationRules
{
    public class LongValRule : ValidationRule
    {
        public LongValRule()
        {

        }
        public LongValRule(string[] vals)
        {
            if (vals.Length > 2) throw new InvalidOperationException($"{GetType().Name}: Too many parameters given to constructor.");

            if (long.TryParse(vals[0], out long min))
            {
                MinValue = min;
            }
            else throw new InvalidOperationException($"{GetType().Name}: Could not parse the minimum value from the given string.");

            if (vals.Length == 1) return;

            if (vals.Length == 2 && long.TryParse(vals[1], out long max))
            {
                MaxValue = max;
            }
            else throw new InvalidOperationException($"{GetType().Name}: Could not parse the maximum value from the given string.");
        }

        private long _minVal = long.MinValue;
        public long MinValue
        {
            get => _minVal;
            set
            {
                if (_minVal > _maxVal) throw new InvalidOperationException($"{GetType().Name}: The minimum bound value of the must be lower than the maximum bound value.");
                _minVal = value;
            }
        }
        private long _maxVal = long.MaxValue;
        public long MaxValue
        {
            get => _maxVal;
            set
            {
                if (_minVal > _maxVal) throw new InvalidOperationException($"{GetType().Name}: The minimum bound value of the must be lower than the maximum bound value.");
                _maxVal = value;
            }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is string)) return new ValidationResult(false, "Value is not string.");

            string text = value as string;

            if (string.IsNullOrWhiteSpace(text)) return new ValidationResult(false, "The string is empty.");
            if (long.TryParse(text, out long intVal))
            {
                if (intVal > MaxValue)
                    return new ValidationResult(false, $"Value is larger than the maximum value of {MaxValue}.");
                else if (intVal < MinValue)
                    return new ValidationResult(false, $"Value is lower than the minimum value of {MinValue}.");
                else
                    return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "The string is not a long integer.");
            }
        }
    }
}
