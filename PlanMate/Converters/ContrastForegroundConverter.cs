using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PlanMate.Converters
{
    public class ContrastForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color bg;

            if (value is SolidColorBrush scb)
                bg = scb.Color;
            else if (value is Color c)
                bg = c;
            else
                return Brushes.Black; // 기본

            // 표준 밝기 계산 (0~1)
            double brightness =
                (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255;

            return brightness > 0.5
                ? Brushes.Black   // 밝은 배경 → 검은 글자
                : Brushes.White;  // 어두운 배경 → 흰 글자
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
