using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#nullable enable

namespace WassControlSys.Core
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string? checkValue = value.ToString();
            string? targetValue = parameter.ToString();
            
            if (checkValue?.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase) == true)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility visibility && visibility == Visibility.Visible && parameter != null)
            {
                string? targetValue = parameter.ToString();
                if (targetValue != null)
                {
                    try
                    {
                        return Enum.Parse(targetType, targetValue);
                    }
                    catch 
                    { 
                        // Return DoNothing if parsing fails
                    }
                }
            }
            return Binding.DoNothing;
        }
    }
}
