using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Text.RegularExpressions;
using System.Windows;

using User = ClubManagementApp.Models.User;

namespace ClubManagementApp.Views
{
    public partial class EditUserDialog : Window
    {
        public User? UpdatedUser { get; private set; }
        private readonly User _originalUser;

        private readonly IUserService _userService;

        public EditUserDialog(User user, IUserService userService)
        {
            _userService = userService;
            InitializeComponent();
            _originalUser = user ?? throw new ArgumentNullException(nameof(user));
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                FullNameTextBox.Text = _originalUser.FullName;
                EmailTextBox.Text = _originalUser.Email;
                IsActiveCheckBox.IsChecked = _originalUser.IsActive;

                // Set the selected role in ComboBox using SelectedValue
                RoleComboBox.SelectedValue = _originalUser.SystemRole;

                PhoneTextBox.Text = _originalUser.PhoneNumber ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable save button to prevent multiple submissions
                SaveButton.IsEnabled = false;

                if (!ValidateInput())
                {
                    SaveButton.IsEnabled = true;
                    return;
                }

                // Check if any changes were actually made
                bool hasChanges = HasUserDataChanged();
                if (!hasChanges)
                {
                    MessageBox.Show("No changes detected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    SaveButton.IsEnabled = true;
                    return;
                }

                // Create updated user object with proper data mapping
                UpdatedUser = CreateUpdatedUserObject();

                // Perform the update operation
                var updatedUser = await _userService.UpdateUserAsync(UpdatedUser);

                if (updatedUser != null)
                {
                    MessageBox.Show("User account updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    base.DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to update user account. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SaveButton.IsEnabled = true;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle business logic errors (like duplicate email)
                MessageBox.Show($"Update failed: {ex.Message}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                MessageBox.Show($"An unexpected error occurred while saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SaveButton.IsEnabled = true;
            }
        }

        private bool HasUserDataChanged()
        {
            // Check if any user data has actually changed
            return FullNameTextBox.Text.Trim() != _originalUser.FullName ||
                   EmailTextBox.Text.Trim() != _originalUser.Email ||
                   ((SystemRole)(RoleComboBox.SelectedValue ?? SystemRole.Member)) != _originalUser.SystemRole ||
                   (string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim()) != _originalUser.PhoneNumber ||
                   (IsActiveCheckBox.IsChecked ?? true) != _originalUser.IsActive ||
                   !string.IsNullOrWhiteSpace(NewPasswordBox.Password); // Password change
        }

        private User CreateUpdatedUserObject()
        {
            var updatedUser = new User
            {
                UserID = _originalUser.UserID,
                FullName = FullNameTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                SystemRole = (SystemRole)(RoleComboBox.SelectedValue ?? SystemRole.Member),
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                IsActive = IsActiveCheckBox.IsChecked ?? true,

                // Preserve original values that shouldn't change in this dialog
                ClubID = _originalUser.ClubID,
                ActivityLevel = _originalUser.ActivityLevel,
                StudentID = _originalUser.StudentID,
                TwoFactorEnabled = _originalUser.TwoFactorEnabled,
                CreatedAt = _originalUser.CreatedAt // Preserve creation date
            };

            // Handle password updates
            if (!string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                // New password provided - will be hashed by UserService
                updatedUser.Password = NewPasswordBox.Password;
            }
            else
            {
                // No password change - keep existing hashed password
                updatedUser.Password = _originalUser.Password;
            }

            return updatedUser;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = false;
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

            // Validate Role
            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a role for the user.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
                return false;
            }

            // Validate password change (if provided)
            if (!string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                if (NewPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("New password must be at least 6 characters long.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPasswordBox.Focus();
                    return false;
                }

                if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password)
                {
                    MessageBox.Show("New passwords do not match.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmNewPasswordBox.Focus();
                    return false;
                }
            }
            else if (!string.IsNullOrWhiteSpace(ConfirmNewPasswordBox.Password))
            {
                MessageBox.Show("Please enter the new password in both fields or leave both blank.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPasswordBox.Focus();
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

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            try
            {
                // Basic phone number validation - allows digits, spaces, hyphens, parentheses, and plus sign
                var phoneRegex = new Regex(@"^[\+]?[\d\s\-\(\)]+$");
                return phoneRegex.IsMatch(phoneNumber) && phoneNumber.Length >= 10;
            }
            catch
            {
                return false;
            }
        }
    }
}
