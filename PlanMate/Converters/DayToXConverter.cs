using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class DayToXConverter : IValueConverter
    {
        // 한 열 너비(px)
        public double ColumnWidth { get; set; } = 50;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayOfWeek day)
            {
                // 일요일(0)부터 시작, 토요일(6)
                return (int)day * ColumnWidth;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
