using ClubManagementApp.ViewModels;
using System.Windows;

namespace ClubManagementApp.Views
{
    /// <summary>
    /// Interaction logic for EventManagementView.xaml
    /// </summary>
    public partial class EventManagementView : Window
    {
        public EventManagementView()
        {
            InitializeComponent();
        }

        public EventManagementView(EventManagementViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}