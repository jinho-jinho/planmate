using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class DivideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d
                && parameter != null
                && double.TryParse(parameter.ToString(), out double divisor)
                && divisor != 0)
            {
                return d / divisor;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d
                && parameter != null
                && double.TryParse(parameter.ToString(), out double divisor))
            {
                return d * divisor;
            }
            return 0.0;
        }
    }
}
