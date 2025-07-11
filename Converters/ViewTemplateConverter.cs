using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClubManagementApp.Converters
{
    public class ViewTemplateConverter : IValueConverter
    {
        public DataTemplate? DashboardTemplate { get; set; }
        public DataTemplate? UsersTemplate { get; set; }
        public DataTemplate? UserManagementTemplate { get; set; }
        public DataTemplate? ClubsTemplate { get; set; }
        public DataTemplate? EventsTemplate { get; set; }
        public DataTemplate? ReportsTemplate { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string viewName)
            {
                return viewName switch
                {
                    "Dashboard" => DashboardTemplate,
                    "Users" => UsersTemplate,
                    "UserManagement" => UserManagementTemplate,
                    "Clubs" => ClubsTemplate,
                    "Events" => EventsTemplate,
                    "Reports" => ReportsTemplate,
                    _ => DashboardTemplate
                };
            }

            return DashboardTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
