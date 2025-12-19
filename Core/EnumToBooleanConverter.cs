using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

#nullable enable

namespace WassControlSys.Core
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string? checkValue = value.ToString();
            string? targetValue = parameter.ToString();

            return checkValue?.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool useValue || !useValue || parameter == null)
                return Binding.DoNothing;

            string? targetValue = parameter.ToString();
            if (targetValue == null)
                return Binding.DoNothing;
            
            try
            {
                return Enum.Parse(targetType, targetValue);
            }
            catch
            {
                return Binding.DoNothing;
            }
        }
    }
}
