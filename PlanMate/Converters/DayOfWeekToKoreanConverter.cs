using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class DayOfWeekToKoreanConverter : IValueConverter
    {
        private static readonly string[] KoreanDayNames = new[]
        {
            "일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayOfWeek dow)
            {
                return KoreanDayNames[(int)dow];
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                for (int i = 0; i < KoreanDayNames.Length; i++)
                {
                    if (KoreanDayNames[i].Equals(s, StringComparison.OrdinalIgnoreCase))
                        return (DayOfWeek)i;
                }
            }
            return Binding.DoNothing;
        }
    }
}
