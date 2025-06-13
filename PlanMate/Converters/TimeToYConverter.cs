using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class TimeToYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2
                && values[0] is double minutes
                && values[1] is double actualHeight
                && actualHeight > 0)
            {
                return minutes * actualHeight / (24 * 60);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
