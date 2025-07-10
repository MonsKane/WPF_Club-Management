using ClubManagementApp.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class EditUserDialog : Window
    {
        public User? UpdatedUser { get; private set; }
        private readonly User _originalUser;

        public EditUserDialog(User user)
        {
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

                // Set the selected role in ComboBox using string comparison
                string roleString = _originalUser.Role.ToString();
                Console.WriteLine($"[EditUserDialog] Looking for role: '{roleString}'");
                
                bool roleFound = false;
                foreach (ComboBoxItem item in RoleComboBox.Items)
                {
                    Console.WriteLine($"[EditUserDialog] Checking item with Tag: '{item.Tag}', Content: '{item.Content}'");
                    if (item.Tag != null && item.Tag.ToString() == roleString)
                    {
                        RoleComboBox.SelectedItem = item;
                        roleFound = true;
                        Console.WriteLine($"[EditUserDialog] Role '{roleString}' found and selected");
                        break;
                    }
                }
                
                // If role not found, show debug info and select first item as fallback
                if (!roleFound)
                {
                    Console.WriteLine($"[EditUserDialog] Warning: Role '{_originalUser.Role}' not found in ComboBox items");
                    Console.WriteLine($"[EditUserDialog] Available items count: {RoleComboBox.Items.Count}");
                    if (RoleComboBox.Items.Count > 0)
                    {
                        RoleComboBox.SelectedIndex = 0;
                        Console.WriteLine($"[EditUserDialog] Selected fallback item at index 0");
                    }
                }
                
                Console.WriteLine($"[EditUserDialog] Final selected item: {RoleComboBox.SelectedItem}");
                Console.WriteLine($"[EditUserDialog] Final selected value: {RoleComboBox.SelectedValue}");
                Console.WriteLine($"[EditUserDialog] Final selected index: {RoleComboBox.SelectedIndex}");
                
                PhoneTextBox.Text = _originalUser.PhoneNumber ?? string.Empty;
                Console.WriteLine($"[EditUserDialog] Phone number loaded: '{_originalUser.PhoneNumber}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditUserDialog] Error loading user data: {ex.Message}");
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("[EditUserDialog] SaveButton_Click called");
                Console.WriteLine($"[EditUserDialog] Current selected item: {RoleComboBox.SelectedItem}");
                Console.WriteLine($"[EditUserDialog] Current selected value: {RoleComboBox.SelectedValue}");
                
                if (ValidateInput())
                {
                    Console.WriteLine("[EditUserDialog] Validation passed, creating updated user");
                    
                    UpdatedUser = new User
                    {
                        UserID = _originalUser.UserID,
                        FullName = FullNameTextBox.Text.Trim(),
                        Email = EmailTextBox.Text.Trim(),
                        Role = Enum.Parse<UserRole>(((ComboBoxItem)RoleComboBox.SelectedItem).Tag?.ToString() ?? "Member"),
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                        IsActive = IsActiveCheckBox.IsChecked ?? true,
                        JoinDate = _originalUser.JoinDate,
                        ClubID = _originalUser.ClubID,
                        Club = _originalUser.Club,
                        ActivityLevel = _originalUser.ActivityLevel,
                        StudentID = _originalUser.StudentID,
                        TwoFactorEnabled = _originalUser.TwoFactorEnabled
                    };

                    // Update password if provided
                    if (!string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                    {
                        UpdatedUser.Password = NewPasswordBox.Password;
                    }
                    else
                    {
                        UpdatedUser.Password = _originalUser.Password;
                    }

                    Console.WriteLine("[EditUserDialog] Setting DialogResult = true");
                    base.DialogResult = true;
                    Close();
                }
                else
                {
                    Console.WriteLine("[EditUserDialog] Validation failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditUserDialog] Error in SaveButton_Click: {ex.Message}");
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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