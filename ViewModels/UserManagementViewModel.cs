using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace ClubManagementApp.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly INavigationService _navigationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<UserManagementViewModel>? _logger;

        private ObservableCollection<User> _users = new();
        private ObservableCollection<User> _filteredUsers = new();
        private User? _selectedUser;
        private string _searchText = string.Empty;
        private SystemRole? _selectedRole;
        private User? _currentUser;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterUsers();
                }
            }
        }

        public SystemRole? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    FilterUsers();
                }
            }
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(CanAccessUserManagement));
                    OnPropertyChanged(nameof(CanCreateUsers));
                    OnPropertyChanged(nameof(CanEditUsers));
                    OnPropertyChanged(nameof(CanDeleteUsers));
                }
            }
        }

        public bool CanAccessUserManagement => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "UserManagement");
        public bool CanCreateUsers => CurrentUser != null && _authorizationService.CanCreateUsers(CurrentUser.SystemRole);
        public bool CanEditUsers => CurrentUser != null && _authorizationService.CanEditUsers(CurrentUser.SystemRole);
        public bool CanDeleteUsers => CurrentUser != null && _authorizationService.CanDeleteUsers(CurrentUser.SystemRole);

        public ICommand RefreshCommand { get; }
        public ICommand CreateAccountCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public UserManagementViewModel(
            IUserService userService,
            INotificationService notificationService,
            INavigationService navigationService,
            IAuthorizationService authorizationService,
            ILogger<UserManagementViewModel>? logger = null)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _logger = logger;

            RefreshCommand = new RelayCommand(async () => await LoadUsersAsync(), () => CanAccessUserManagement);
            CreateAccountCommand = new RelayCommand(CreateAccount, () => CanCreateUsers);
            EditUserCommand = new RelayCommand(EditUser, () => SelectedUser != null && CanEditUsers);
            DeleteUserCommand = new RelayCommand(DeleteUser, () => SelectedUser != null && CanDeleteUsers && SelectedUser.UserID != CurrentUser?.UserID);

            _ = LoadCurrentUserAsync();
            _ = LoadUsersAsync();
        }

        private async Task LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                _logger?.LogInformation("Current user loaded successfully: {UserName}", CurrentUser?.FullName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading current user");
            }
        }

        public override async Task LoadAsync()
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                _logger?.LogDebug("Loading users from database");

                var users = await _userService.GetAllUsersAsync();

                _logger?.LogDebug("Loaded {UserCount} users from database", users.Count());

                // Update the Users collection
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                // Apply current filters to show updated data
                FilterUsers();

                _logger?.LogDebug("User list updated successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading users");
                MessageBox.Show("Failed to load users. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterUsers()
        {
            var filtered = Users.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(u =>
                    u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedRole.HasValue)
            {
                filtered = filtered.Where(u => u.SystemRole == SelectedRole.Value);
            }

            FilteredUsers = new ObservableCollection<User>(filtered);
        }

        private async void CreateAccount()
        {
            try
            {
                _logger?.LogInformation("Opening create account dialog");

                // Create and show the add user dialog directly to get proper dialog result
                var addDialog = new Views.AddUserDialog(_userService);
                addDialog.Owner = Application.Current.MainWindow;

                var dialogResult = addDialog.ShowDialog();
                _logger?.LogDebug("Add user dialog result: {DialogResult}", dialogResult);

                if (dialogResult == true && addDialog.CreatedUser != null)
                {
                    _logger?.LogInformation("User creation successful, refreshing user list");

                    // Refresh users list to show new user
                    await LoadUsersAsync();

                    // Try to select the newly created user
                    var newUser = Users.FirstOrDefault(u => u.UserID == addDialog.CreatedUser.UserID);
                    if (newUser != null)
                    {
                        SelectedUser = newUser;
                    }

                    _logger?.LogInformation("User {UserName} created successfully", addDialog.CreatedUser.FullName);
                }
                else
                {
                    _logger?.LogInformation("Create user operation cancelled by user");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening create account dialog");
                MessageBox.Show("Failed to open create account dialog.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditUser()
        {
            if (SelectedUser == null) return;

            try
            {
                _logger?.LogInformation("Opening edit dialog for user: {UserName} (ID: {UserId})", SelectedUser.FullName, SelectedUser.UserID);

                                // Create and show the edit dialog directly to get proper dialog result
                var editDialog = new Views.EditUserDialog(SelectedUser, _userService);
                editDialog.Owner = Application.Current.MainWindow;

                var dialogResult = editDialog.ShowDialog();
                _logger?.LogDebug("Edit dialog result: {DialogResult}", dialogResult);

                if (dialogResult == true && editDialog.UpdatedUser != null)
                {
                    _logger?.LogInformation("User edit successful, refreshing user list");

                    // Refresh users list to show updated data
                    await LoadUsersAsync();

                    // Try to reselect the updated user
                    var updatedUser = Users.FirstOrDefault(u => u.UserID == editDialog.UpdatedUser.UserID);
                    if (updatedUser != null)
                    {
                        SelectedUser = updatedUser;
                    }

                    _logger?.LogInformation("User {UserName} updated successfully", editDialog.UpdatedUser.FullName);
                }
                else
                {
                    _logger?.LogInformation("Edit user operation cancelled by user");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening edit user dialog");
                MessageBox.Show("Failed to open edit user dialog.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteUser()
        {
            if (SelectedUser == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete user '{SelectedUser.FullName}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                await _userService.DeleteUserAsync(SelectedUser.UserID);
                MessageBox.Show($"User '{SelectedUser.FullName}' has been deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user {UserId}", SelectedUser.UserID);
                MessageBox.Show("Failed to delete user. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
