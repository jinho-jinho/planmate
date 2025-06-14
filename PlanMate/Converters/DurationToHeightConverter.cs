using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanMate.Converters
{
    public class DurationToHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            // values[0] = (EndTime-StartTime).TotalMinutes, values[1] = Canvas.ActualHeight
            if (values[0] is double minutes && values[1] is double h && h > 0)
                return minutes * h / (24 * 60);
            return 0.0;
        }
        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c)
            => throw new NotSupportedException();
    }
}
