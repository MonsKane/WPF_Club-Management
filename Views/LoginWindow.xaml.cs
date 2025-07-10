using ClubManagementApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeRestoreButton.Content = "🗖"; // Maximize icon
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeRestoreButton.Content = "🗗"; // Restore icon
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore
                MaximizeRestoreButton_Click(sender, e);
            }
            else
            {
                // Single-click to drag
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.LoginSuccessful -= OnLoginSuccessful;
            base.OnClosed(e);
        }
    }
}