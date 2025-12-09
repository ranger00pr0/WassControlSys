using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WassControlSys.Core
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();
            
            if (checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase))
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility && visibility == Visibility.Visible)
            {
                if (parameter != null)
                {
                    try
                    {
                        return Enum.Parse(targetType, parameter.ToString());
                    }
                    catch { }
                }
            }
            return Binding.DoNothing;
        }
    }
}
