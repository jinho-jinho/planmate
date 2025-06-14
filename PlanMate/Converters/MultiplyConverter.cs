using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d
                && parameter != null
                && double.TryParse(parameter.ToString(), out var factor))
            {
                return d * factor;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d
                && parameter != null
                && double.TryParse(parameter.ToString(), out var factor)
                && factor != 0)
            {
                return d / factor;
            }
            return 0.0;
        }
    }
}
