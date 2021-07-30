using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaseWPFLibrary.Converters
{
    public class StringDirectoryPathValidAndAccessible_ToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string path)) return false;
            if (value == null || string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists) return true;
            }
            catch { }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"{this.GetType()} does not convert back.");
        }
    }
}
