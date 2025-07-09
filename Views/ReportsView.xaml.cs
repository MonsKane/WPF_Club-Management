using ClubManagementApp.ViewModels;
using System.Windows;

namespace ClubManagementApp.Views
{
    /// <summary>
    /// Interaction logic for ReportsView.xaml
    /// </summary>
    public partial class ReportsView : Window
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        public ReportsView(ReportsViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}