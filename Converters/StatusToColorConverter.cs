using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClubManagementApp.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public static readonly StatusToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "active" => new SolidColorBrush(Color.FromRgb(39, 174, 96)), // Green
                    "inactive" => new SolidColorBrush(Color.FromRgb(149, 165, 166)), // Gray
                    "pending" => new SolidColorBrush(Color.FromRgb(243, 156, 18)), // Orange
                    "suspended" => new SolidColorBrush(Color.FromRgb(231, 76, 60)), // Red
                    _ => new SolidColorBrush(Color.FromRgb(52, 73, 94)) // Default dark blue
                };
            }
            return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Default gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for this one-way converter
            return Binding.DoNothing;
        }
    }
}