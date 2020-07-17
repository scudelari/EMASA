using System;
using System.Globalization;
using System.Windows.Data;

namespace BaseWPFLibrary.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool) return !(bool)value;
            else throw new InvalidCastException("Class InverseBooleanConverter only accepts bool as input.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool) return !(bool)value;
            else throw new InvalidCastException("Class InverseBooleanConverter only accepts bool as input.");
        }
    }
}
