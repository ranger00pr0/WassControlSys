using System;
using System.Globalization;
using System.Windows.Data;

namespace WassControlSys.Core
{
    public class SectionToIsActiveConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return false;
            
            // values[0] = CurrentSection (enum)
            // values[1] = CommandParameter (string)
            
            if (values[0] == null || values[1] == null) return false;
            
            string currentSection = values[0].ToString() ?? "";
            string buttonSection = values[1].ToString() ?? "";
            
            return string.Equals(currentSection, buttonSection, StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
