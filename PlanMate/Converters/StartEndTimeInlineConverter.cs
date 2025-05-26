using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;
using System.Collections.Generic;
using PlanMate.Models;

namespace PlanMate.Converters
{
    public class StartEndTimeInlineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskItem task)
            {
                var today = DateTime.Today;
                var inlines = new List<Inline>();

                string formatTime(string t) => $"{t[..2]}:{t[2..]}";

                bool isTodayStart = task.StartDate.Date == today;
                bool isTodayEnd = task.EndDate.Date == today;

                if (task.StartDate.Date == task.EndDate.Date && isTodayStart)
                {
                    if (task.StartTime == "0000" && task.EndTime == "2359")
                    {
                        inlines.Add(new Bold(new Run("오늘 하루종일")));
                    }
                    else
                    {
                        inlines.Add(new Bold(new Run($"오늘 {formatTime(task.StartTime)} ~ {formatTime(task.EndTime)}")));
                    }
                }
                else
                {
                    inlines.Add(new Run($"{task.StartDate:MM/dd} {formatTime(task.StartTime)} ~ "));

                    if (isTodayEnd)
                        inlines.Add(new Bold(new Run($"{task.EndDate:MM/dd} {formatTime(task.EndTime)}")));
                    else
                        inlines.Add(new Run($"{task.EndDate:MM/dd} {formatTime(task.EndTime)}"));
                }

                return inlines;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
