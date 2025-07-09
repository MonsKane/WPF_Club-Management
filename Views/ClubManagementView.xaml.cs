using ClubManagementApp.ViewModels;
using System.Windows;

namespace ClubManagementApp.Views
{
    /// <summary>
    /// Interaction logic for ClubManagementView.xaml
    /// </summary>
    public partial class ClubManagementView : Window
    {
        public ClubManagementView()
        {
            InitializeComponent();
        }

        public ClubManagementView(ClubManagementViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}