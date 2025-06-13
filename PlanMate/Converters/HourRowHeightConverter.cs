using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class HourRowHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 1 && values[0] is double viewportHeight)
            {
                return viewportHeight / 24.0;
            }
            return 60.0; // fallback
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
