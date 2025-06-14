﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PlanMate.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString() switch
            {
                "User" => Brushes.CornflowerBlue,
                "Bot" => Brushes.Gray,
                _ => Brushes.DarkGray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
