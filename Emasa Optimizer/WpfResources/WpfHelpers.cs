using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BaseWPFLibrary;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;

namespace Emasa_Optimizer.WpfResources
{
    public class DepObjs : DependencyObject
    {
        #region CancelErrorOnLostFocus - Dependency Property
        /// <summary>
        /// Adds a callback that will cancel errors on the TextBoxes on LostFocus
        /// </summary>
        public static readonly DependencyProperty CancelErrorOnLostFocus = DependencyProperty.RegisterAttached(
            "CancelErrorOnLostFocus", typeof(bool), typeof(DepObjs),
            new PropertyMetadata(false, PropertyChangedCallback_CancelErrorOnLostFocus));
        public static string GetCancelErrorOnLostFocus(DependencyObject d)
        {
            return (string)d.GetValue(CancelErrorOnLostFocus);
        }
        public static void SetCancelErrorOnLostFocus(DependencyObject d, string value)
        {
            
            d.SetValue(CancelErrorOnLostFocus, value);
        }
        private static void PropertyChangedCallback_CancelErrorOnLostFocus(DependencyObject inD, DependencyPropertyChangedEventArgs inE)
        {
            if (inD is TextBox tb)
            {
                if ((bool)inE.NewValue)
                {
                    tb.LostFocus += TextBoxOnLostFocus;
                }
                else
                {
                    tb.LostFocus -= TextBoxOnLostFocus;
                }
            }
        }
        private static void TextBoxOnLostFocus(object inSender, RoutedEventArgs inE)
        {
            if (inSender is TextBox tb)
            {
                BindingExpression bEx = tb.GetBindingExpression(TextBox.TextProperty);
                if (bEx.HasError)
                {
                    string toolTipContent = string.Empty;
                    for (int i = 0; i < bEx.ValidationErrors.Count; i++)
                    {
                        toolTipContent += bEx.ValidationErrors[i].ErrorContent;
                        toolTipContent += Environment.NewLine;
                    }

                    toolTipContent = toolTipContent.Substring(0, toolTipContent.Length - Environment.NewLine.Length);

                    if (!string.IsNullOrWhiteSpace(toolTipContent)) tb.ToolTip = toolTipContent;

                    // Attempts to bring back the original value
                    bEx.UpdateTarget();
                }
            }
        }
        #endregion

    }

    public class PercentageConverter : IValueConverter
    {
        //E.g. DB 0.042367 --> UI "4.24 %"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // From double to %
            if (!(value is double d)) throw new Exception("Value must be double.");
            return $"{d:P}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Accepts both formats
            if (!(value is string s)) throw new Exception("Value must be string.");

            s = s.Trim(' ');
            bool isPercent = s.EndsWith("%");

            s = s.Trim('%');

            if (!double.TryParse(s, out double d)) throw new Exception("Could not convert to a double.");


            return isPercent ? d / 100d : d;
        }
    }

    public class WpfImageCaptureViewDirectionEnumFlagsValueConverter : IValueConverter
    {
        private ImageCaptureViewDirectionEnum target;

        public WpfImageCaptureViewDirectionEnumFlagsValueConverter()
        {
        }

        // From value to WPF
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ImageCaptureViewDirectionEnum mask = (ImageCaptureViewDirectionEnum)parameter;
            target = (ImageCaptureViewDirectionEnum)value;
            return target.HasFlag(mask);
        }

        // From WPF to value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            this.target ^= (ImageCaptureViewDirectionEnum)parameter;
            return this.target;
        }
    }

    public class Wpf_EnumToString_Converter : IValueConverter
    {
        // From value to WPF
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Decides which list this is targeting
                FieldInfo[] fieldsThatTargetGivenEnum = typeof(ListDescSH).GetFields().Where(a =>
                    a.FieldType.IsGenericType &&
                    a.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                    a.FieldType.GenericTypeArguments.First() == value.GetType()).ToArray();

                if (fieldsThatTargetGivenEnum.Length == 0) return $"Could not find a dictionary that targets {value.GetType()}";
                if (fieldsThatTargetGivenEnum.Length != 1) return $"There are more dictionaries that target {value.GetType()}. This is currently not supported.";

                // Expected to be Dictionary<Enum,something>
                Type fieldType = fieldsThatTargetGivenEnum[0].FieldType;

                // Gets the value of the dictionary
                dynamic d = fieldsThatTargetGivenEnum[0].GetValue(ListDescSH.I); // the dictionary

                MethodInfo m_GetItem = fieldType.GetMethod("get_Item");
                object v = m_GetItem.Invoke(d, new[] {value});

                if (parameter == null) return $"{v}";
                if (v.GetType().GetProperty((string)parameter) != null) return $"{v.GetType().GetProperty((string)parameter).GetValue(v)}";
                if (v.GetType().GetField((string)parameter) != null) return $"{v.GetType().GetField((string)parameter).GetValue(v)}";

                throw new Exception($"Parameter/Field {parameter} does not exists in type {v.GetType()}.");
            }
            catch (Exception e)
            {
                throw new InvalidCastException($"Could not get the enum description of value {value} targeting parameter {parameter} on the static enum descriptions.", e);
            }
        }

        // From WPF to value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class Wpf_FeResultClassificationTreeViewCounter_Converter : IValueConverter
    {
        // From value to WPF
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionViewGroup c)
            {

                int recursive_count(CollectionViewGroup inCol)
                {
                    int innerCount = inCol.Items.OfType<CollectionViewGroup>().Sum(innerC => recursive_count(innerC));
                    innerCount += inCol.Items.OfType<FeResultClassification>().Count(a => a.OutputData_IsSelected);
                    return innerCount;
                }

                return $"{recursive_count(c)}";
            }

            return $"0";
        }

        // From WPF to value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "bla";
        }
    }

    public class DefaultNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetString(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string GetString(object value)
        {
            if (value == null) return "-";

            try
            {
                double d = (double)value;
                return $"{d:g3}";
                //return BaseWPFStaticMembers.DoubleToEngineering(d, "3");
            }
            catch
            {
                return value.ToString();
            }
        }
    }
}
