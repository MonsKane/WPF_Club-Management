using ClubManagementApp.ViewModels;
using System.Windows;

namespace ClubManagementApp
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}