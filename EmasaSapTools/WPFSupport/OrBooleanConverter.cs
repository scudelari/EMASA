using System;
using System.Windows.Data;

namespace EmasaSapTools.WPFSupport
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class OrBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (values.LongLength > 0)
                foreach (object value in values)
                    if (value is bool && (bool) value)
                        return true;
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("Can only convert one way!");
        }
    }
}