using ClubManagementApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Converters
{
    public class ViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DashboardTemplate { get; set; }
        public DataTemplate? UsersTemplate { get; set; }
        public DataTemplate? ClubsTemplate { get; set; }
        public DataTemplate? EventsTemplate { get; set; }
        public DataTemplate? ReportsTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is string viewName)
            {
                return viewName switch
                {
                    "Dashboard" => DashboardTemplate,
                    "Users" => UsersTemplate,
                    "Clubs" => ClubsTemplate,
                    "Events" => EventsTemplate,
                    "Reports" => ReportsTemplate,
                    _ => DashboardTemplate
                };
            }

            return DashboardTemplate;
        }
    }
}