using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PlanMate.Converters
{
    public class ImportanceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                "상" => Brushes.IndianRed,
                "중" => Brushes.Orange,
                "하" => Brushes.LightGreen,
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
