using ClubManagementApp.ViewModels;
using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void ClubCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Club club && DataContext is ClubManagementViewModel viewModel)
            {
                // Clear previous selection
                foreach (var c in viewModel.Clubs)
                {
                    c.IsSelected = false;
                }
                
                // Set new selection
                club.IsSelected = true;
                viewModel.SelectedClub = club;
            }
        }
    }
}