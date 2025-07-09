using ClubManagementApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginViewModel ViewModel { get; }

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
            
            // Subscribe to login success event
            ViewModel.LoginSuccessful += OnLoginSuccessful;
        }

        private void OnLoginSuccessful(object? sender, EventArgs e)
        {
            // DialogResult should not be set when window is shown non-modally
            // App.xaml.cs handles the window transition
            Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                ViewModel.Password = passwordBox.Password;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.LoginSuccessful -= OnLoginSuccessful;
            base.OnClosed(e);
        }
    }
}