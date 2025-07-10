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
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MemberListViewModel>? _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private ObservableCollection<User> _members = new();
        private ObservableCollection<User> _filteredMembers = new();
        private string _searchText = string.Empty;
        private UserRole? _selectedRole;
        private bool _isLoading;
        private User? _selectedMember;
        private bool _disposed;

        public MemberListViewModel(
            IUserService userService,
            IClubService clubService,
            INotificationService notificationService,
            ILogger<MemberListViewModel>? logger = null)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _clubService = clubService ?? throw new ArgumentNullException(nameof(clubService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger;

            InitializeCommands();
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
                    FilterMembers();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public User? SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value);
        }

        // Commands
        public ICommand AddMemberCommand { get; private set; } = null!;
        public ICommand EditMemberCommand { get; private set; } = null!;
        public ICommand DeleteMemberCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ExportMembersCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddMemberCommand = new RelayCommand(AddMember, _ => CanExecuteCommand());
            EditMemberCommand = new RelayCommand<User>(EditMember, CanEditMember);
            DeleteMemberCommand = new RelayCommand<User>(DeleteMember, CanDeleteMember);
            RefreshCommand = new RelayCommand(async _ => await RefreshMembersAsync(), _ => CanExecuteCommand());
            ExportMembersCommand = new RelayCommand(ExportMembers, _ => CanExportMembers());
        }

        private bool CanExecuteCommand() => !IsLoading && !_disposed;

        private bool CanEditMember(User? member) => member != null && CanExecuteCommand();

        private bool CanDeleteMember(User? member) => member != null && CanExecuteCommand();

        private bool CanExportMembers() => FilteredMembers.Any() && CanExecuteCommand();

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
                IsLoading = true;

                var members = await _userService.GetAllUsersAsync();

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

                _logger?.LogInformation("Successfully loaded {Count} members", members.Count());
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Member loading was cancelled");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Failed to load members", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshMembersAsync()
        {
            await LoadMembersAsync();
            await ShowNotificationAsync("Member list refreshed successfully");
        }

        private void FilterMembers()
        {
            if (_disposed)
                return;

            try
            {
                var filtered = Members.AsEnumerable();

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerm = SearchText.Trim();
                    filtered = filtered.Where(m =>
                        (m.FullName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (m.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (m.Club?.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                // Filter by role
                if (SelectedRole.HasValue)
                {
                    filtered = filtered.Where(m => m.Role == SelectedRole.Value);
                }

                var filteredList = filtered.ToList();

                FilteredMembers.Clear();
                foreach (var member in filteredList)
                {
                    FilteredMembers.Add(member);
                }

                _logger?.LogDebug("Filtered members: {Count} of {Total}", filteredList.Count, Members.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while filtering members");
            }
        }

        private async void AddMember(object? parameter)
        {
            try
            {
                // Navigate to add member functionality
                // For now, show a simple input dialog for demonstration
                var newMemberName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter new member's full name:", "Add New Member", "");

                if (!string.IsNullOrWhiteSpace(newMemberName))
                {
                    var newMemberEmail = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter new member's email:", "Add New Member", "");

                    if (!string.IsNullOrWhiteSpace(newMemberEmail))
                    {
                        var newUser = new User
                        {
                            FullName = newMemberName.Trim(),
                            Email = newMemberEmail.Trim(),
                            Role = UserRole.Member,
                            JoinDate = DateTime.Now,
                            IsActive = true,
                            Password = "DefaultPassword123" // Should be changed on first login
                        };

                        var createdUser = await _userService.CreateUserAsync(newUser);
                        Members.Add(createdUser);
                        FilterMembers();
                        await ShowNotificationAsync($"Member {newMemberName} added successfully");
                    }
                }
                _logger?.LogInformation("Add Member action triggered");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Failed to open add member dialog", ex);
            }
        }

        private async void EditMember(User? member)
        {
            if (member == null)
            {
                await ShowNotificationAsync("Please select a member to edit");
                return;
            }

            try
            {
                // Simple edit functionality using input dialogs
                var newName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Edit member's full name:", "Edit Member", member.FullName);

                if (!string.IsNullOrWhiteSpace(newName) && newName != member.FullName)
                {
                    member.FullName = newName.Trim();
                    await _userService.UpdateUserAsync(member);
                    FilterMembers();
                    await ShowNotificationAsync($"Member {member.FullName} updated successfully");
                }
                _logger?.LogInformation("Edit Member action triggered for user {UserId}", member.UserID);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync($"Failed to open edit dialog for {member.FullName}", ex);
            }
        }

        private async void DeleteMember(User? member)
        {
            if (member == null)
            {
                await ShowNotificationAsync("Please select a member to delete");
                return;
            }

            try
            {
                // Using MessageBox for confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete {member.FullName}?\n\nThis action cannot be undone.",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
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

                    await ShowNotificationAsync($"Member {member.FullName} has been deleted successfully");
                    _logger?.LogInformation("Successfully deleted member {UserId}: {FullName}", member.UserID, member.FullName);
                }
            }
            catch (Exception ex)
            {
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
                if (!FilteredMembers.Any())
                {
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
                    var csvContent = new StringBuilder();
                    csvContent.AppendLine("Full Name,Email,Role,Club,Join Date,Status");

                    foreach (var member in FilteredMembers)
                    {
                        csvContent.AppendLine($"{member.FullName},{member.Email},{member.Role},{member.Club?.Name ?? "N/A"},{member.JoinDate:yyyy-MM-dd},{(member.IsActive ? "Active" : "Inactive")}");
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, csvContent.ToString());
                    await ShowNotificationAsync($"Successfully exported {FilteredMembers.Count} members to {Path.GetFileName(saveFileDialog.FileName)}");
                }
                _logger?.LogInformation("Export Members action triggered for {Count} members", FilteredMembers.Count);
            }
            catch (Exception ex)
            {
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

        public void Dispose()
        {
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