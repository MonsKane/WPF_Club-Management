using ClubManagementApp.Models;
using ClubManagementApp.Services;
using ClubManagementApp.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class AddMemberDialog : Window, INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly Club? _targetClub;
        private readonly ObservableCollection<User> _searchResults = new();
        private User? _selectedUser;
        private string _searchText = string.Empty;
        private bool _isSearchTabActive = true;
        private bool _isCreateTabActive = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddMemberDialog(IUserService userService, IClubService clubService, Club? targetClub)
        {
            InitializeComponent();
            _userService = userService;
            _clubService = clubService;
            _targetClub = targetClub;

            DataContext = this;
            SearchResultsListBox.ItemsSource = _searchResults;

            // Set initial tab state
            UpdateTabVisibility();
        }

        public ObservableCollection<User> SearchResults => _searchResults;

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                AddMemberButton.IsEnabled = value != null;
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearchTabActive
        {
            get => _isSearchTabActive;
            set
            {
                _isSearchTabActive = value;
                OnPropertyChanged();
            }
        }

        public bool IsCreateTabActive
        {
            get => _isCreateTabActive;
            set
            {
                _isCreateTabActive = value;
                OnPropertyChanged();
            }
        }

        private void SearchTabButton_Click(object sender, RoutedEventArgs e)
        {
            IsSearchTabActive = true;
            IsCreateTabActive = false;
            UpdateTabVisibility();
            AddMemberButton.Content = "👥 Add Member";
            AddMemberButton.IsEnabled = SelectedUser != null;
        }

        private void CreateTabButton_Click(object sender, RoutedEventArgs e)
        {
            IsSearchTabActive = false;
            IsCreateTabActive = true;
            UpdateTabVisibility();
            AddMemberButton.Content = "👤 Create & Add Member";
            AddMemberButton.IsEnabled = true;
        }

        private void UpdateTabVisibility()
        {
            SearchTabContent.Visibility = IsSearchTabActive ? Visibility.Visible : Visibility.Collapsed;
            CreateTabContent.Visibility = IsCreateTabActive ? Visibility.Visible : Visibility.Collapsed;
        }

        // Search button removed - auto-search functionality handles search

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
            {
                await PerformSearch();
            }
            else
            {
                _searchResults.Clear();
            }
        }

        private async Task PerformSearch()
        {
            try
            {
                _searchResults.Clear();

                if (string.IsNullOrWhiteSpace(SearchText))
                    return;

                var allUsers = await _userService.GetAllUsersAsync();

                HashSet<int> memberIds = new();
                if (_targetClub != null)
                {
                    var clubMembers = await _clubService.GetClubMembersAsync(_targetClub.ClubID);
                    memberIds = clubMembers.Select(m => m.UserID).ToHashSet();
                }

                var filteredUsers = allUsers.Where(u =>
                    !memberIds.Contains(u.UserID) && // Exclude existing members
                    (u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                     u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                    .Take(10); // Limit results

                foreach (var user in filteredUsers)
                {
                    _searchResults.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching users: {ex.Message}", "Search Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUser = SearchResultsListBox.SelectedItem as User;
        }

        private async void AddMemberButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsSearchTabActive)
                {
                    await AddExistingUser();
                }
                else
                {
                    await CreateAndAddNewUser();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding member: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddExistingUser()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Please select a user to add.", "No User Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_targetClub == null)
            {
                MessageBox.Show("No target club specified.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //var selectedRole = ExistingUserRoleComboBox.SelectedValue as UserRole?;
            //if (selectedRole == null)
            //{
            //    MessageBox.Show("Please select a role for the user.", "No Role Selected",
            //        MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            // Add user to club
            //var roleEnum = selectedRole.Value;
            await _clubService.AddUserToClubAsync(SelectedUser.UserID, _targetClub.ClubID, default);

            MessageBox.Show($"{SelectedUser.FullName} has been successfully added to {_targetClub.Name}.",
                "Member Added", MessageBoxButton.OK, MessageBoxImage.Information);

            // Trigger member change notification to refresh views
            MemberListViewModel.NotifyMemberChanged();

            DialogResult = true;
            Close();
        }

        private async Task CreateAndAddNewUser()
        {
            if (!ValidateNewUserInput())
                return;

            if (_targetClub == null)
            {
                MessageBox.Show("No target club specified.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedRole = NewUserRoleComboBox.SelectedValue as UserRole?;
            if (selectedRole == null)
            {
                MessageBox.Show("Please select a role for the user.", "No Role Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create new user
            var newUser = new User
            {
                FullName = FullNameTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                PhoneNumber = PhoneTextBox.Text.Trim(),
                IsActive = IsActiveCheckBox.IsChecked ?? true,
                ClubID = _targetClub?.ClubID
            };

            // Validate passwords match
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // Set password
            newUser.Password = PasswordBox.Password;

            // Create user and add to club
            var createdUser = await _userService.CreateUserAsync(newUser);
            if (createdUser == null)
            {
                MessageBox.Show("Failed to create user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var roleEnum = selectedRole.Value;
            await _clubService.AddUserToClubAsync(createdUser.UserID, _targetClub!.ClubID, roleEnum);

            MessageBox.Show($"{newUser.FullName} has been created and added to {_targetClub.Name}.",
                "Member Created and Added", MessageBoxButton.OK, MessageBoxImage.Information);

            // Trigger member change notification to refresh views
            MemberListViewModel.NotifyMemberChanged();

            DialogResult = true;
            Close();
        }

        private bool ValidateNewUserInput()
        {
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Please enter a full name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !IsValidEmail(EmailTextBox.Text))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password) || PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return false;
            }

            return true;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}