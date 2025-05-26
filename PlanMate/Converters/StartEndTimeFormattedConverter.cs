using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class StartEndTimeFormattedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4) return "";

            if (values[0] is DateTime startDate &&
                values[1] is string startTime &&
                values[2] is DateTime endDate &&
                values[3] is string endTime)
            {
                var today = DateTime.Today;
                string FormatTime(string t) => $"{t[..2]}:{t[2..]}"; // "0930" -> "09:30"

                bool isTodayStart = startDate.Date == today;
                bool isTodayEnd = endDate.Date == today;

                if (startDate.Date == endDate.Date && isTodayStart)
                {
                    if (startTime == "0000" && endTime == "2359")
                        return "오늘 하루종일";
                    else
                        return $"오늘 {FormatTime(startTime)} ~ {FormatTime(endTime)}";
                }

                return $"{startDate:MM/dd} {FormatTime(startTime)} ~ {endDate:MM/dd} {FormatTime(endTime)}";
            }

            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
