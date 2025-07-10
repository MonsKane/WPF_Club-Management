using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClubManagementApp.Converters
{
    public class BooleanToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(230, 244, 255)); // Light blue for selected
            }
            
            return new SolidColorBrush(Colors.White); // Default white
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}