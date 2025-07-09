using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClubManagementApp.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public static readonly BooleanToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for this one-way converter
            return Binding.DoNothing;
        }
    }
}