using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClubManagementApp.Converters
{
    public class BooleanToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(0, 123, 255)); // Blue border for selected
            }
            
            return new SolidColorBrush(Color.FromRgb(220, 220, 220)); // Light gray for unselected
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}