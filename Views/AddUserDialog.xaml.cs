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

            // Set default role to Member
            RoleComboBox.SelectedValue = UserRole.Member;
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var user = new User
                {
                    FullName = FullNameTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    Password = PasswordBox.Password,
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                    Role = (UserRole)(RoleComboBox.SelectedValue ?? UserRole.Member),
                    IsActive = IsActiveCheckBox.IsChecked ?? true,
                    JoinDate = DateTime.Now
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            // Validate Full Name
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Please enter the user's full name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return false;
            }

            if (FullNameTextBox.Text.Trim().Length < 2)
            {
                MessageBox.Show("Full name must be at least 2 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
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

            // Validate Password
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please enter a password.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            if (PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            // Validate Confirm Password
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return false;
            }

            // Validate Role
            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a role for the user.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
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