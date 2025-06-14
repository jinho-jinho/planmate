using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PlanMate.Converters
{
    public class IconToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconCode && !string.IsNullOrEmpty(iconCode))
            {
                var url = $"https://openweathermap.org/img/wn/{iconCode}@2x.png";
                try
                {
                    return new BitmapImage(new Uri(url));
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}