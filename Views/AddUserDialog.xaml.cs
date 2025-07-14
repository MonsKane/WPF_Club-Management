using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class AddUserDialog : Window
    {
        private readonly IUserService _userService;

        public User? CreatedUser { get; private set; }

        public AddUserDialog(IUserService userService)
        {
            InitializeComponent();
            _userService = userService;

            // Populate the SystemRole ComboBox
            SystemRoleComboBox.ItemsSource = Enum.GetValues(typeof(SystemRole));
            SystemRoleComboBox.SelectedValue = SystemRole.Member;
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var user = new User
                {
                    FullName = UsernameTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    Password = "defaultpassword", // Default password since no password field in XAML
                    SystemRole = (SystemRole)(SystemRoleComboBox.SelectedValue ?? SystemRole.Member),
                    IsActive = true
                };

                // Create the user (no club assignment in user creation)
                var createdUser = await _userService.CreateUserAsync(user);
                CreatedUser = createdUser;

                MessageBox.Show("User account created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            CreateButton_Click(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            // Validate Username
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Please enter a username.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
                return false;
            }

            if (UsernameTextBox.Text.Trim().Length < 2)
            {
                MessageBox.Show("Username must be at least 2 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
                return false;
            }

            // Validate Email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Please enter an email address.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(EmailTextBox.Text.Trim()))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            // Password validation skipped since no password fields in XAML

            // Validate Role
            if (SystemRoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a role for the user.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SystemRoleComboBox.Focus();
                return false;
            }

            return true;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}