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

        // Static event for member changes
        public static event Action? MemberChanged;

        // Enhanced notification method for club statistics refresh
        public static void NotifyClubMemberCountChanged(int clubId)
        {
            ClubMemberCountChanged?.Invoke(clubId);
        }

        // Static event for club member count changes
        public static event Action<int>? ClubMemberCountChanged;

        // Public method to trigger the leadership changed event
        public static void NotifyLeadershipChanged()
        {
            LeadershipChanged?.Invoke();
        }

        // Public method to trigger the member changed event
        public static void NotifyMemberChanged()
        {
            MemberChanged?.Invoke();
        }

        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<MemberListViewModel>? _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private ObservableCollection<User> _members = new();
        private ObservableCollection<User> _filteredMembers = new();
        private string _searchText = string.Empty;
        private SystemRole? _selectedRole;
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

            MemberChanged += async () => await LoadMembersAsync();
            LoadCurrentUserAsync();
            InitializeCommands();

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

        public SystemRole? SelectedRole
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

        // Current User
        private User? _currentUser;
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(CanCreateUsers));
                    OnPropertyChanged(nameof(CanEditUsers));
                    OnPropertyChanged(nameof(CanDeleteUsers));
                    OnPropertyChanged(nameof(CanExportUsers));
                    OnPropertyChanged(nameof(CanAccessUserManagement));
                    OnPropertyChanged(nameof(CanAddClubMembers));
                    OnPropertyChanged(nameof(CanEditClubMembers));
                    OnPropertyChanged(nameof(CanRemoveClubMembers));
                }
            }
        }

        // Current User's Club Role
        private ClubRole? _currentUserClubRole;
        public ClubRole? CurrentUserClubRole
        {
            get => _currentUserClubRole;
            set
            {
                if (SetProperty(ref _currentUserClubRole, value))
                {
                    OnPropertyChanged(nameof(CanAddClubMembers));
                    OnPropertyChanged(nameof(CanEditClubMembers));
                    OnPropertyChanged(nameof(CanRemoveClubMembers));
                }
            }
        }

        // Authorization Properties
        public bool CanCreateUsers => CurrentUser != null && _authorizationService.CanCreateUsers(CurrentUser.SystemRole);
        public bool CanEditUsers => CurrentUser != null && _authorizationService.CanEditUsers(CurrentUser.SystemRole);
        public bool CanDeleteUsers => CurrentUser != null && _authorizationService.CanDeleteUsers(CurrentUser.SystemRole);
        public bool CanExportUsers => CurrentUser != null && _authorizationService.CanExportReports(CurrentUser.SystemRole); // Using CanExportReports as equivalent
        public bool CanAccessUserManagement => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "MemberManagement");

        // Club Member Management Permissions
        public bool CanAddClubMembers => CurrentUser != null && _authorizationService.CanAddClubMembers(CurrentUser.SystemRole, CurrentUserClubRole);
        public bool CanEditClubMembers => CurrentUser != null && _authorizationService.CanEditClubMembers(CurrentUser.SystemRole, CurrentUserClubRole);
        public bool CanRemoveClubMembers => CurrentUser != null && _authorizationService.CanRemoveClubMembers(CurrentUser.SystemRole, CurrentUserClubRole);

        // Commands
        public ICommand AddMemberCommand { get; private set; } = null!;
        public ICommand EditMemberCommand { get; private set; } = null!;
        public ICommand DeleteMemberCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ExportMembersCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddMemberCommand = new RelayCommand(AddMember, _ => CanAddMember());
            EditMemberCommand = new RelayCommand<User>(EditMember, CanEditMember);
            DeleteMemberCommand = new RelayCommand<User>(DeleteMember, CanDeleteMember);
            RefreshCommand = new RelayCommand(async _ => await RefreshMembersAsync(), _ => CanAccessUserManagement && !IsLoading && !_disposed);
            ExportMembersCommand = new RelayCommand(ExportMembers, _ => CanExportMembers());
        }

        private bool CanExecuteCommand() => !IsLoading && !_disposed;

        private bool CanAddMember() => CanAddClubMembers && CanExecuteCommand();

        private bool CanEditMember(User? member) => member != null && CanEditClubMembers && CanExecuteCommand();

        private bool CanDeleteMember(User? member) => member != null && CanRemoveClubMembers && CanExecuteCommand() &&
            (CurrentUser?.SystemRole == SystemRole.Admin || member.UserID != CurrentUser?.UserID);

        private bool CanExportMembers() => FilteredMembers.Any() && CanExportUsers && CanExecuteCommand();

        // Load current user method
        public async void LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[MemberListViewModel] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.SystemRole})");

                // Load club role if there's an active club filter
                if (ClubFilter != null)
                {
                    await LoadCurrentUserClubRoleAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemberListViewModel] Error loading current user: {ex.Message}");
                CurrentUser = null;
            }
        }

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

                IEnumerable<User> members;

                // If a club filter is set, load only members of that club
                if (ClubFilter != null)
                {
                    Console.WriteLine($"[MemberListViewModel] Loading members for club: {ClubFilter.Name} (ID: {ClubFilter.ClubID})");
                    var clubMembers = await _clubService.GetClubMembersAsync(ClubFilter.ClubID);
                    members = clubMembers.Select(cm => cm.User);
                }
                else
                {
                    Console.WriteLine("[MemberListViewModel] Loading all users (no club filter)");
                    members = await _userService.GetAllUsersAsync();
                }

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
                filtered = filtered.Where(m => m.SystemRole == SelectedRole);
            }

            // Note: Don't apply club filter here if ClubFilter is set, because we already loaded only club members
            // The club filter is applied during LoadMembersAsync() when ClubFilter != null

            var result = filtered.ToList();

            FilteredMembers.Clear();
            foreach (var member in result)
            {
                FilteredMembers.Add(member);
            }

            Console.WriteLine($"[MemberListViewModel] Filtered {Members.Count} members to {FilteredMembers.Count} results");
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

                    // Notify other ViewModels about member change
                    MemberChanged?.Invoke();

                    // If a club filter is active, notify about member count change for that club
                    if (ClubFilter != null)
                    {
                        NotifyClubMemberCountChanged(ClubFilter.ClubID);
                    }
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
                _logger?.LogWarning("Edit member failed - no member selected");
                await ShowNotificationAsync("Please select a member to edit");
                return;
            }

            try
            {
                _logger?.LogInformation("Edit Member command executed for: {MemberName} (ID: {MemberId})", member.FullName, member.UserID);

                // Create and show the edit dialog
                var editDialog = new Views.EditUserDialog(member, _userService);
                _logger?.LogDebug("EditUserDialog created successfully for {MemberName}", member.FullName);

                var dialogResult = editDialog.ShowDialog();
                _logger?.LogDebug("Dialog result: {DialogResult}", dialogResult);

                if (dialogResult == true && editDialog.UpdatedUser != null)
                {
                    _logger?.LogInformation("Processing member update: {MemberName} (ID: {MemberId})", editDialog.UpdatedUser.FullName, editDialog.UpdatedUser.UserID);
                    IsLoading = true;

                    // Update the user
                    var updatedUser = await _userService.UpdateUserAsync(editDialog.UpdatedUser);

                    // Reload members to ensure we have the latest data
                    await LoadMembersAsync();

                    _logger?.LogInformation("Member {MemberName} updated successfully", editDialog.UpdatedUser.FullName);
                    await ShowNotificationAsync($"Member {editDialog.UpdatedUser.FullName} updated successfully");

                    // Notify other ViewModels about member change
                    MemberChanged?.Invoke();

                    // If a club filter is active or member was associated with a club, notify about member count changes
                    if (ClubFilter != null)
                    {
                        NotifyClubMemberCountChanged(ClubFilter.ClubID);
                    }
                    if (member.ClubID.HasValue && (ClubFilter == null || member.ClubID.Value != ClubFilter.ClubID))
                    {
                        NotifyClubMemberCountChanged(member.ClubID.Value);
                    }
                }
                else
                {
                    _logger?.LogInformation("Edit member operation cancelled by user");
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "Business logic error while updating member {MemberName}: {Error}", member.FullName, ex.Message);
                await ShowNotificationAsync($"Update failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error editing member {MemberName}: {Error}", member.FullName, ex.Message);
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
                    $"Are you sure you want to delete {member.FullName}?\n\nThis action cannot be undone and will remove them from any clubs and events.",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[MemberListViewModel] User confirmed deletion of member: {member.FullName}");
                    IsLoading = true;

                    // UserService now handles all cascade deletion including club memberships
                    await _userService.DeleteUserAsync(member.UserID);

                    // Reload members to ensure accurate list
                    await LoadMembersAsync();

                    Console.WriteLine($"[MemberListViewModel] Member {member.FullName} deleted successfully");
                    await ShowNotificationAsync($"Member {member.FullName} has been deleted successfully");
                    _logger?.LogInformation("Successfully deleted member {UserId}: {FullName}", member.UserID, member.FullName);

                    // Notify other ViewModels about member change
                    MemberChanged?.Invoke();

                    // Notify about club member count change if member was in a club
                    if (member.ClubID.HasValue)
                    {
                        NotifyClubMemberCountChanged(member.ClubID.Value);
                    }
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
                        csvContent.AppendLine($"{member.FullName},{member.Email},{member.SystemRole},{member.Club?.Name ?? "N/A"},{member.CreatedAt:yyyy-MM-dd},{(member.IsActive ? "Active" : "Inactive")}");
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

        // Method to load current user's club role
        private async Task LoadCurrentUserClubRoleAsync()
        {
            if (CurrentUser == null || ClubFilter == null)
            {
                CurrentUserClubRole = null;
                return;
            }

            try
            {
                var clubMembers = await _clubService.GetClubMembersAsync(ClubFilter.ClubID);
                var currentUserMembership = clubMembers.FirstOrDefault(cm => cm.UserID == CurrentUser.UserID);

                CurrentUserClubRole = currentUserMembership?.ClubRole;

                _logger?.LogInformation("Current user club role loaded: {Role} for club {ClubName}",
                    CurrentUserClubRole, ClubFilter.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load current user's club role for club {ClubId}", ClubFilter.ClubID);
                CurrentUserClubRole = null;
            }
        }

        public async void SetClubFilter(Club club)
        {
            ClubFilter = club;
            // Load current user's role in this club
            await LoadCurrentUserClubRoleAsync();
            // Reload members when club filter changes
            await LoadMembersAsync();
        }

        public async void ClearClubFilter()
        {
            ClubFilter = null;
            CurrentUserClubRole = null;
            // Reload all members when club filter is cleared
            await LoadMembersAsync();
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
