using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class DayToXDynamicConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2
                && values[0] is DayOfWeek day
                && values[1] is double actualWidth)
            {
                int dayIndex = (int)day;
                return dayIndex * (actualWidth / 7);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
