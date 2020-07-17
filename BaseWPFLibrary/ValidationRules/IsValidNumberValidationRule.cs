using System.Globalization;
using System.Windows.Controls;

namespace BaseWPFLibrary.ValidationRules
{
   public class IsValidNumberValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is string)) return new ValidationResult(false, "Value is not string.");

            string text = value as string;

            if (string.IsNullOrWhiteSpace(text)) return new ValidationResult(false, "The string is empty.");
            if (double.TryParse(text, out _) || int.TryParse(text, out _))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "The string is not numeric.");
            }

        }
    }
}
