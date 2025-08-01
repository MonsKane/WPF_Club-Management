using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class MemberListViewModel : BaseViewModel, IDisposable
    {
        // Static event for notifying all instances when leadership changes
        private static event Action? LeadershipChanged;
        
        // Public method to trigger the leadership changed event
        public static void NotifyLeadershipChanged()
        {
            LeadershipChanged?.Invoke();
        }
        
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<MemberListViewModel>? _logger;
        private User? _currentUser;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private ObservableCollection<User> _members = new();
        private ObservableCollection<User> _filteredMembers = new();
        private string _searchText = string.Empty;
        private UserRole? _selectedRole;
        private bool _isLoading;
        private User? _selectedMember;
        private bool _disposed;
        private Club? _clubFilter;

        public MemberListViewModel(
            IUserService userService,
            IClubService clubService,
            INotificationService notificationService,
            IAuthorizationService authorizationService,
            ILogger<MemberListViewModel>? logger = null)
        {
            Console.WriteLine("[MemberListViewModel] Initializing MemberListViewModel with services");
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _clubService = clubService ?? throw new ArgumentNullException(nameof(clubService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _logger = logger;

            InitializeCommands();
            _ = LoadCurrentUserAsync();
            
            // Subscribe to leadership change notifications
            LeadershipChanged += OnLeadershipChanged;
            
            Console.WriteLine("[MemberListViewModel] MemberListViewModel initialization completed");
        }

        public ObservableCollection<User> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        public ObservableCollection<User> FilteredMembers
        {
            get => _filteredMembers;
            set => SetProperty(ref _filteredMembers, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    Console.WriteLine($"[MemberListViewModel] Search text changed to: '{value}'");
                    FilterMembers();
                }
            }
        }

        public UserRole? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    Console.WriteLine($"[MemberListViewModel] Selected role filter changed to: {value}");
                    FilterMembers();
                }
            }
        }

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public User? SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value);
        }

        public Club? ClubFilter
        {
            get => _clubFilter;
            private set
            {
                if (SetProperty(ref _clubFilter, value))
                {
                    Console.WriteLine($"[MemberListViewModel] Club filter changed to: {value?.Name ?? "All Clubs"}");
                    FilterMembers();
                }
            }
        }

        // Commands
        public ICommand AddMemberCommand { get; private set; } = null!;
        public ICommand EditMemberCommand { get; private set; } = null!;
        public ICommand DeleteMemberCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ExportMembersCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddMemberCommand = new RelayCommand(AddMember, _ => CanExecuteAddMember());
            EditMemberCommand = new RelayCommand<User>(EditMember, CanExecuteEditMember);
            DeleteMemberCommand = new RelayCommand<User>(DeleteMember, CanExecuteDeleteMember);
            RefreshCommand = new RelayCommand(async _ => await RefreshMembersAsync(), _ => CanExecuteCommand());
            ExportMembersCommand = new RelayCommand(ExportMembers, _ => CanExecuteExportMembers());
        }

        private bool CanExecuteCommand() => !IsLoading && !_disposed;

        private bool CanExecuteAddMember() => CanCreateUsers && CanExecuteCommand();

        private bool CanExecuteEditMember(User? member) => member != null && CanEditUser(member) && CanExecuteCommand();

        private bool CanExecuteDeleteMember(User? member) => member != null && CanDeleteUser(member) && CanExecuteCommand();

        private bool CanExecuteExportMembers() => CanExportUsers && FilteredMembers.Any() && CanExecuteCommand();

        private async Task InitializeAsync()
        {
            try
            {
                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Failed to initialize member list", ex);
            }
        }

        private async Task LoadMembersAsync()
        {
            if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
                return;

            try
            {
                Console.WriteLine("[MemberListViewModel] Starting to load members");
                IsLoading = true;

                var members = await _userService.GetAllUsersAsync();
                Console.WriteLine($"[MemberListViewModel] Retrieved {members.Count()} members from service");

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                // Update UI on main thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Members.Clear();
                    foreach (var member in members)
                    {
                        Members.Add(member);
                    }
                    FilterMembers();
                });

                Console.WriteLine("[MemberListViewModel] Members loaded and UI updated successfully");
                _logger?.LogInformation("Successfully loaded {Count} members", members.Count());
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[MemberListViewModel] Member loading was cancelled");
                _logger?.LogInformation("Member loading was cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error loading members: {ex.Message}");
                await HandleErrorAsync("Failed to load members", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshMembersAsync()
        {
            Console.WriteLine("[MemberListViewModel] Refresh members command executed");
            await LoadMembersAsync();
            await ShowNotificationAsync("Member list refreshed successfully");
        }
        
        private async void OnLeadershipChanged()
        {
            try
            {
                Console.WriteLine("[MemberListViewModel] Leadership changed notification received, refreshing member list");
                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error refreshing members after leadership change: {ex.Message}");
            }
        }

        private void FilterMembers()
        {
            var filtered = Members.AsEnumerable();

            // Apply search text filter
            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(m =>
                    m.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    m.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    m.StudentID?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Apply role filter
            if (SelectedRole.HasValue)
            {
                filtered = filtered.Where(m => m.Role == SelectedRole);
            }

            // Apply club filter
            if (ClubFilter is not null)
            {
                filtered = filtered.Where(m => m.ClubID == ClubFilter.ClubID);
            }

            var result = filtered.ToList();

            FilteredMembers.Clear();
            foreach (var member in result)
            {
                FilteredMembers.Add(member);
            }
        }

        private async void AddMember(object? parameter)
        {
            try
            {
                Console.WriteLine("[MemberListViewModel] Add Member command executed");

                var addDialog = new Views.AddMemberDialog(_userService, _clubService, _clubFilter);
                if (addDialog.ShowDialog() == true)
                {
                    Console.WriteLine("[MemberListViewModel] Member added successfully, refreshing member list");
                    
                    // Refresh the member list to show the new/added member
                    await LoadMembersAsync();
                    
                    // Also refresh the filtered members to update the UI immediately
                    FilterMembers();
                }
                else
                {
                    Console.WriteLine("[MemberListViewModel] Add member cancelled by user");
                }
                _logger?.LogInformation("Add Member action triggered");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error adding member: {ex.Message}");
                await HandleErrorAsync("Failed to add member", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void EditMember(User? member)
        {
            if (member == null)
            {
                Console.WriteLine("[MemberListViewModel] Edit member failed - no member selected");
                await ShowNotificationAsync("Please select a member to edit");
                return;
            }

            try
            {
                Console.WriteLine($"[MemberListViewModel] Edit Member command executed for: {member.FullName} (ID: {member.UserID})");

                // Create and show the edit dialog
                var editDialog = new Views.EditUserDialog(member);
                Console.WriteLine($"[MemberListViewModel] EditUserDialog created successfully for {member.FullName}");

                var dialogResult = editDialog.ShowDialog();
                Console.WriteLine($"[MemberListViewModel] Dialog result: {dialogResult}");

                if (dialogResult == true && editDialog.UpdatedUser != null)
                {
                    Console.WriteLine($"[MemberListViewModel] Updating member: {editDialog.UpdatedUser.FullName}");
                    IsLoading = true;

                    var updatedUser = await _userService.UpdateUserAsync(editDialog.UpdatedUser);

                    // Refresh the member from database to ensure we have the latest data
                    var refreshedUser = await _userService.GetUserByIdAsync(editDialog.UpdatedUser.UserID);

                    if (refreshedUser != null)
                    {
                        // Find the member in the collection and update it
                        var index = Members.ToList().FindIndex(m => m.UserID == editDialog.UpdatedUser.UserID);
                        if (index >= 0)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                // Direct assignment works correctly with tracking enabled
                                Members[index] = refreshedUser;
                                FilterMembers();
                            });
                        }
                    }

                    Console.WriteLine($"[MemberListViewModel] Member {editDialog.UpdatedUser.FullName} updated successfully");
                    await ShowNotificationAsync($"Member {editDialog.UpdatedUser.FullName} updated successfully");
                }
                else
                {
                    Console.WriteLine("[MemberListViewModel] Edit member cancelled by user");
                }
                _logger?.LogInformation("Edit Member action triggered for user {UserId}", member.UserID);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error editing member {member.FullName}: {ex.Message}");
                await HandleErrorAsync($"Failed to edit member {member.FullName}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void DeleteMember(User? member)
        {
            if (member == null)
            {
                Console.WriteLine("[MemberListViewModel] Delete member failed - no member selected");
                await ShowNotificationAsync("Please select a member to delete");
                return;
            }

            try
            {
                Console.WriteLine($"[MemberListViewModel] Delete Member command executed for: {member.FullName} (ID: {member.UserID})");
                // Using MessageBox for confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete {member.FullName}?\n\nThis action cannot be undone.",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[MemberListViewModel] User confirmed deletion of member: {member.FullName}");
                    IsLoading = true;

                    await _userService.DeleteUserAsync(member.UserID);

                    // Update UI on main thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Members.Remove(member);
                        if (SelectedMember == member)
                        {
                            SelectedMember = null;
                        }
                        FilterMembers();
                    });

                    Console.WriteLine($"[MemberListViewModel] Member {member.FullName} deleted successfully");
                    await ShowNotificationAsync($"Member {member.FullName} has been deleted successfully");
                    _logger?.LogInformation("Successfully deleted member {UserId}: {FullName}", member.UserID, member.FullName);
                }
                else
                {
                    Console.WriteLine($"[MemberListViewModel] User cancelled deletion of member: {member.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error deleting member {member.FullName}: {ex.Message}");
                await HandleErrorAsync($"Failed to delete member {member.FullName}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExportMembers(object? parameter)
        {
            try
            {
                Console.WriteLine($"[MemberListViewModel] Export Members command executed for {FilteredMembers.Count} members");
                if (!FilteredMembers.Any())
                {
                    Console.WriteLine("[MemberListViewModel] Export cancelled - no members to export");
                    await ShowNotificationAsync("No members to export");
                    return;
                }

                // Export members to CSV format
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"Members_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    Console.WriteLine($"[MemberListViewModel] Exporting {FilteredMembers.Count} members to: {saveFileDialog.FileName}");
                    var csvContent = new StringBuilder();
                    csvContent.AppendLine("Full Name,Email,Role,Club,Join Date,Status");

                    foreach (var member in FilteredMembers)
                    {
                        csvContent.AppendLine($"{member.FullName},{member.Email},{member.Role},{member.Club?.Name ?? "N/A"},{member.JoinDate:yyyy-MM-dd},{(member.IsActive ? "Active" : "Inactive")}");
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, csvContent.ToString());
                    Console.WriteLine($"[MemberListViewModel] Successfully exported {FilteredMembers.Count} members to {Path.GetFileName(saveFileDialog.FileName)}");
                    await ShowNotificationAsync($"Successfully exported {FilteredMembers.Count} members to {Path.GetFileName(saveFileDialog.FileName)}");
                }
                else
                {
                    Console.WriteLine("[MemberListViewModel] Export cancelled by user");
                }
                _logger?.LogInformation("Export Members action triggered for {Count} members", FilteredMembers.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error exporting members: {ex.Message}");
                await HandleErrorAsync("Failed to export members", ex);
            }
        }

        private async Task HandleErrorAsync(string message, Exception ex)
        {
            _logger?.LogError(ex, "{Message}: {ErrorMessage}", message, ex.Message);

            var errorMessage = $"{message}: {ex.Message}";
            await ShowNotificationAsync(errorMessage);
        }

        private async Task ShowNotificationAsync(string message)
        {
            try
            {
                // Use notification service for better user experience
                await _notificationService.SendInAppNotificationAsync(0, "Member Management", message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to show notification: {Message}", message);
                // Fallback to MessageBox if notification service fails
                System.Windows.MessageBox.Show(message, "Member Management",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        // Permission Properties
        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        // User Management Permissions
        public bool CanAccessUserManagement => CurrentUser?.Role != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "UserManagement");
        public bool CanCreateUsers => CurrentUser?.Role != null && _authorizationService.CanCreateUsers(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanEditUsers => CurrentUser?.Role != null && _authorizationService.CanEditUsers(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanDeleteUsers => CurrentUser?.Role != null && _authorizationService.CanDeleteUsers(CurrentUser.Role);
        public bool CanAssignRoles => CurrentUser?.Role != null && _authorizationService.CanAssignRoles(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanExportUsers => CurrentUser?.Role != null && _authorizationService.CanExportReports(CurrentUser.Role);
        
        // User-specific permissions
        public bool CanEditUser(User user) => CurrentUser?.Role != null && _authorizationService.CanEditUsers(CurrentUser.Role, CurrentUser.ClubID) && 
                                              (CurrentUser.Role == UserRole.SystemAdmin || CurrentUser.ClubID == user.ClubID);
        public bool CanDeleteUser(User user) => CurrentUser?.Role != null && _authorizationService.CanDeleteUsers(CurrentUser.Role) && 
                                                 user.UserID != CurrentUser.UserID; // Cannot delete self

        private async Task LoadCurrentUserAsync()
        {
            try
            {
                // Get current user from UserService session
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[MemberListViewModel] Current user loaded: {CurrentUser?.FullName ?? "None"} (Role: {CurrentUser?.Role})");
                
                // Notify property changes for permission-dependent UI elements
                OnPropertyChanged(nameof(CanAccessUserManagement));
                OnPropertyChanged(nameof(CanCreateUsers));
                OnPropertyChanged(nameof(CanEditUsers));
                OnPropertyChanged(nameof(CanDeleteUsers));
                OnPropertyChanged(nameof(CanAssignRoles));
                OnPropertyChanged(nameof(CanExportUsers));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error loading current user: {ex.Message}");
            }
        }

        public void SetClubFilter(Club club)
        {
            ClubFilter = club;
        }

        public void ClearClubFilter()
        {
            ClubFilter = null;
        }

        public void Dispose()
        {
            // Unsubscribe from leadership change notifications
            LeadershipChanged -= OnLeadershipChanged;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }

        public override Task LoadAsync()
        {
            return InitializeAsync();
        }
    }
}