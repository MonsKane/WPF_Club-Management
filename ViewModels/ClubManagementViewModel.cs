using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class ClubManagementViewModel : BaseViewModel
    {
        private readonly IClubService _clubService;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;
        private readonly INavigationService _navigationService;
        private readonly IAuthorizationService _authorizationService;
        private ObservableCollection<Club> _clubs = new();
        private ObservableCollection<Club> _filteredClubs = new();
        private string _searchText = string.Empty;
        private bool _isLoading;
        private Club? _selectedClub;
        private User? _currentUser;

        public ClubManagementViewModel(IClubService clubService, IUserService userService, IEventService eventService, INavigationService navigationService, IAuthorizationService authorizationService)
        {
            Console.WriteLine("[ClubManagementViewModel] Initializing ClubManagementViewModel with services");
            _clubService = clubService;
            _userService = userService;
            _eventService = eventService;
            _navigationService = navigationService;
            _authorizationService = authorizationService;
            InitializeCommands();
            LoadCurrentUserAsync();
            Console.WriteLine("[ClubManagementViewModel] ClubManagementViewModel initialization completed");
        }

        public ObservableCollection<Club> Clubs
        {
            get => _clubs;
            set => SetProperty(ref _clubs, value);
        }

        public ObservableCollection<Club> FilteredClubs
        {
            get => _filteredClubs;
            set => SetProperty(ref _filteredClubs, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    Console.WriteLine($"[ClubManagementViewModel] Search text changed to: '{value}'");
                    FilterClubs();
                }
            }
        }

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Club? SelectedClub
        {
            get => _selectedClub;
            set
            {
                if (SetProperty(ref _selectedClub, value))
                {
                    OnPropertyChanged(nameof(CanManageSelectedClub));
                }
            }
        }

        public bool HasNoClubs
        {
            get => !IsLoading && FilteredClubs.Count == 0;
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public bool CanManageClubs
        {
            get => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "ClubManagement");
        }

        public bool CanManageSelectedClub
        {
            get => SelectedClub != null && CurrentUser != null &&
                   _authorizationService.CanManageClub(CurrentUser.Role, CurrentUser.ClubID, SelectedClub.ClubID);
        }

        // Commands
        public ICommand AddClubCommand { get; private set; } = null!;
        public ICommand EditClubCommand { get; private set; } = null!;
        public ICommand DeleteClubCommand { get; private set; } = null!;
        public ICommand ViewClubDetailsCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ManageLeadershipCommand { get; private set; } = null!;
        public ICommand ViewMembersCommand { get; private set; } = null!;
        public ICommand ViewEventsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddClubCommand = new RelayCommand(AddClub, CanExecuteAddClub);
            EditClubCommand = new RelayCommand<Club>(EditClub, CanExecuteEditClub);
            DeleteClubCommand = new RelayCommand<Club>(DeleteClub, CanExecuteDeleteClub);
            ViewClubDetailsCommand = new RelayCommand<Club>(ViewClubDetails, CanExecuteViewClubDetails);
            RefreshCommand = new RelayCommand(async () => await LoadClubsAsync());
            ManageLeadershipCommand = new RelayCommand<Club>(ManageLeadership, CanExecuteManageLeadership);
            ViewMembersCommand = new RelayCommand<Club>(ViewMembers, CanExecuteViewMembers);
            ViewEventsCommand = new RelayCommand<Club>(ViewEvents, CanExecuteViewEvents);
        }

        private async void LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[ClubManagementViewModel] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.Role})");
                OnPropertyChanged(nameof(CanManageClubs));
                OnPropertyChanged(nameof(CanManageSelectedClub));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error loading current user: {ex.Message}");
            }
        }

        private bool CanExecuteAddClub(object? parameter)
        {
            return CanManageClubs;
        }

        private bool CanExecuteEditClub(Club? club)
        {
            return club != null && CurrentUser != null &&
                   _authorizationService.CanManageClub(CurrentUser.Role, CurrentUser.ClubID, club.ClubID);
        }

        private bool CanExecuteDeleteClub(Club? club)
        {
            return club != null && CurrentUser != null &&
                   _authorizationService.CanManageClub(CurrentUser.Role, CurrentUser.ClubID, club.ClubID);
        }

        private bool CanExecuteViewClubDetails(Club? club)
        {
            return club != null;
        }

        private bool CanExecuteManageLeadership(Club? club)
        {
            return club != null && CurrentUser != null &&
                   _authorizationService.CanManageClub(CurrentUser.Role, CurrentUser.ClubID, club.ClubID);
        }

        private bool CanExecuteViewMembers(Club? club)
        {
            return club != null && CurrentUser != null &&
                   _authorizationService.CanManageClub(CurrentUser.Role, CurrentUser.ClubID, club.ClubID);
        }

        private bool CanExecuteViewEvents(Club? club)
        {
            return club != null && CurrentUser != null &&
                   _authorizationService.CanManageEvent(CurrentUser.Role, CurrentUser.ClubID, club.ClubID);
        }

        private async Task LoadClubsAsync()
        {
            try
            {
                Console.WriteLine("[ClubManagementViewModel] Starting to load clubs");
                IsLoading = true;
                var clubs = await _clubService.GetAllClubsAsync();
                Console.WriteLine($"[ClubManagementViewModel] Retrieved {clubs.Count()} clubs from service");

                Clubs.Clear();
                foreach (var club in clubs)
                {
                    // Load additional data for each club
                    await LoadClubStatistics(club);
                    Clubs.Add(club);
                }

                FilterClubs();
                Console.WriteLine("[ClubManagementViewModel] Clubs loaded and filtered successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error loading clubs: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading clubs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoClubs));
            }
        }

        private async Task LoadClubStatistics(Club club)
        {
            try
            {
                Console.WriteLine($"[ClubManagementViewModel] Loading statistics for club: {club.Name} (ID: {club.ClubID})");
                // Load members count
                var members = await _userService.GetUsersByClubAsync(club.ClubID);
                // Don't assign to club.Members to avoid entity tracking issues
                // Just log the count for now
                Console.WriteLine($"[ClubManagementViewModel] Found {members.Count()} members for club: {club.Name}");

                // Load events count
                var events = await _eventService.GetEventsByClubAsync(club.ClubID);
                // Don't assign to club.Events to avoid entity tracking issues
                // Just log the count for now
                Console.WriteLine($"[ClubManagementViewModel] Found {events.Count()} events for club: {club.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error loading club statistics for {club.Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading club statistics for {club.Name}: {ex.Message}");
            }
        }

        private void FilterClubs()
        {
            Console.WriteLine($"[ClubManagementViewModel] Filtering clubs - Search: '{SearchText}'");
            var filtered = Clubs.AsEnumerable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(c =>
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (c.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            FilteredClubs.Clear();
            foreach (var club in filtered.OrderBy(c => c.Name))
            {
                FilteredClubs.Add(club);
            }
            Console.WriteLine($"[ClubManagementViewModel] Filtered to {FilteredClubs.Count} clubs");
            OnPropertyChanged(nameof(HasNoClubs));
        }

        private async void AddClub(object? parameter)
        {
            Console.WriteLine("[ClubManagementViewModel] Add Club command executed");
            try
            {
                var dialog = new Views.AddClubDialog();
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                dialog.ShowDialog();

                if (dialog.DialogResult && dialog.CreatedClub != null)
                {
                    Console.WriteLine($"[ClubManagementViewModel] Creating new club: {dialog.CreatedClub.Name}");
                    var createdClub = await _clubService.CreateClubAsync(dialog.CreatedClub);

                    // Load statistics for the new club
                    await LoadClubStatistics(createdClub);

                    Clubs.Add(createdClub);
                    FilterClubs();

                    System.Windows.MessageBox.Show(
                        $"Club '{createdClub.Name}' has been created successfully!",
                        "Club Created",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    Console.WriteLine($"[ClubManagementViewModel] Club created successfully: {createdClub.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error creating club: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error creating club: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async void EditClub(Club? club)
        {
            if (club == null) return;

            Console.WriteLine($"[ClubManagementViewModel] Edit Club command executed for: {club.Name} (ID: {club.ClubID})");
            try
            {
                var dialog = new Views.EditClubDialog(club);
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                dialog.ShowDialog();

                if (dialog.DialogResult && dialog.UpdatedClub != null)
                {
                    Console.WriteLine($"[ClubManagementViewModel] Updating club: {dialog.UpdatedClub.Name}");
                    var updatedClub = await _clubService.UpdateClubAsync(dialog.UpdatedClub);

                    // Find and replace the club in the collection
                    var index = Clubs.ToList().FindIndex(c => c.ClubID == updatedClub.ClubID);
                    if (index >= 0)
                    {
                        // Load statistics for the updated club
                        await LoadClubStatistics(updatedClub);

                        // Remove the old club and insert the updated one to trigger proper notifications
                        Clubs.RemoveAt(index);
                        Clubs.Insert(index, updatedClub);
                        FilterClubs();

                        System.Windows.MessageBox.Show(
                            $"Club '{updatedClub.Name}' has been updated successfully!",
                            "Club Updated",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        Console.WriteLine($"[ClubManagementViewModel] Club updated successfully: {updatedClub.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error updating club {club.Name}: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error updating club: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async void DeleteClub(Club? club)
        {
            if (club == null) return;

            Console.WriteLine($"[ClubManagementViewModel] Delete Club command executed for: {club.Name} (ID: {club.ClubID})");
            try
            {
                // Check if club has members or events by querying the services directly
                var members = await _userService.GetUsersByClubAsync(club.ClubID);
                var events = await _eventService.GetEventsByClubAsync(club.ClubID);

                if (members.Any() || events.Any())
                {
                    Console.WriteLine($"[ClubManagementViewModel] Cannot delete club {club.Name} - has {members.Count()} members and {events.Count()} events");
                    System.Windows.MessageBox.Show(
                        "Cannot delete club that has members or events. Please remove all members and events first.",
                        "Cannot Delete Club",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the club '{club.Name}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[ClubManagementViewModel] User confirmed deletion of club: {club.Name}");
                    await _clubService.DeleteClubAsync(club.ClubID);
                    Clubs.Remove(club);
                    FilterClubs();
                    Console.WriteLine($"[ClubManagementViewModel] Club deleted successfully: {club.Name}");
                }
                else
                {
                    Console.WriteLine($"[ClubManagementViewModel] User cancelled deletion of club: {club.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error deleting club {club.Name}: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error deleting club: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ViewClubDetails(Club? club)
        {
            if (club == null) return;

            Console.WriteLine($"[ClubManagementViewModel] View Club Details command executed for: {club.Name} (ID: {club.ClubID})");
            SelectedClub = club;
            try
            {
                _navigationService.ShowClubDetails(club);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error showing club details: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening club details: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ManageLeadership(Club? club)
        {
            if (club == null) return;

            Console.WriteLine($"[ClubManagementViewModel] Manage Leadership command executed for: {club.Name} (ID: {club.ClubID})");
            try
            {
                // Create and show the dialog
                var dialog = new Views.ManageLeadershipDialog(club, _clubService, _userService, _navigationService);
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                var result = dialog.ShowDialog();

                // If changes were saved, refresh the club data
                if (result == true)
                {
                    Console.WriteLine($"[ClubManagementViewModel] Leadership changes saved for club: {club.Name}");

                    // Refresh the club data to reflect leadership changes
                    await LoadClubsAsync();

                    // Show success notification
                    System.Windows.MessageBox.Show(
                        "Leadership roles have been updated successfully!",
                        "Leadership Updated",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    // Notify all MemberListViewModel instances to refresh
                    MemberListViewModel.NotifyLeadershipChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error opening leadership management: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening leadership management: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ViewMembers(Club? club)
        {
            if (club == null) return;

            Console.WriteLine($"[ClubManagementViewModel] View Members command executed for: {club.Name} (ID: {club.ClubID})");
            try
            {
                _navigationService.OpenMemberListWindow(club);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error opening member list: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening member list: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ViewEvents(Club? club)
        {
            if (club == null) return;

            Console.WriteLine($"[ClubManagementViewModel] View Events command executed for: {club.Name} (ID: {club.ClubID})");
            try
            {
                _navigationService.OpenEventManagementWindow(club);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClubManagementViewModel] Error opening event management: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening event management: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public string GetClubStatusText(Club club)
        {
            var memberCount = club.Members?.Count ?? 0;
            var eventCount = club.Events?.Count ?? 0;

            if (memberCount == 0)
                return "No Members";
            else if (eventCount == 0)
                return "No Events";
            else
                return "Active";
        }

        public string GetClubStatusColor(Club club)
        {
            var memberCount = club.Members?.Count ?? 0;
            var eventCount = club.Events?.Count ?? 0;

            if (memberCount == 0)
                return "#e74c3c"; // Red
            else if (eventCount == 0)
                return "#f39c12"; // Orange
            else
                return "#27ae60"; // Green
        }

        public override Task LoadAsync()
        {
            return LoadClubsAsync();
        }
    }
}
