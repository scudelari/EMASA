using System;
using System.Globalization;
using System.Windows.Controls;

namespace BaseWPFLibrary.ValidationRules
{
    public class IntValRule : ValidationRule
    {
        public IntValRule()
        {

        }
        public IntValRule(string[] vals)
        {
            if (vals.Length > 2) throw new InvalidOperationException($"{GetType().Name}: Too many parameters given to constructor.");

            if (int.TryParse(vals[0], out int min))
            {
                MinValue = min;
            }
            else throw new InvalidOperationException($"{GetType().Name}: Could not parse the minimum value from the given string.");

            if (vals.Length == 1) return;

            if (vals.Length == 2 && int.TryParse(vals[1], out int max))
            {
                MaxValue = max;
            }
            else throw new InvalidOperationException($"{GetType().Name}: Could not parse the maximum value from the given string.");
        }

        private int _minVal = int.MinValue;
        public int MinValue
        {
            get => _minVal;
            set 
            {
                if (_minVal > _maxVal) throw new InvalidOperationException($"{GetType().Name}: The minimum bound value of the must be lower than the maximum bound value.");
                _minVal = value; 
            }
        }
        private int _maxVal = int.MaxValue;
        public int MaxValue
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
            if (int.TryParse(text, out int intVal))
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
                return new ValidationResult(false, "The string is not an integer.");
            }
        }
    }
}
