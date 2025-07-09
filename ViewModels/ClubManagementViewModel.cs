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
        private ObservableCollection<Club> _clubs = new();
        private ObservableCollection<Club> _filteredClubs = new();
        private string _searchText = string.Empty;
        private bool _isLoading;
        private Club? _selectedClub;

        public ClubManagementViewModel(IClubService clubService, IUserService userService, IEventService eventService)
        {
            _clubService = clubService;
            _userService = userService;
            _eventService = eventService;
            InitializeCommands();
            _ = LoadClubsAsync();
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
                    FilterClubs();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Club? SelectedClub
        {
            get => _selectedClub;
            set => SetProperty(ref _selectedClub, value);
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
            AddClubCommand = new RelayCommand(AddClub);
            EditClubCommand = new RelayCommand<Club>(EditClub);
            DeleteClubCommand = new RelayCommand<Club>(DeleteClub);
            ViewClubDetailsCommand = new RelayCommand<Club>(ViewClubDetails);
            RefreshCommand = new RelayCommand(async () => await LoadClubsAsync());
            ManageLeadershipCommand = new RelayCommand<Club>(ManageLeadership);
            ViewMembersCommand = new RelayCommand<Club>(ViewMembers);
            ViewEventsCommand = new RelayCommand<Club>(ViewEvents);
        }

        private async Task LoadClubsAsync()
        {
            try
            {
                IsLoading = true;
                var clubs = await _clubService.GetAllClubsAsync();
                
                Clubs.Clear();
                foreach (var club in clubs)
                {
                    // Load additional data for each club
                    await LoadClubStatistics(club);
                    Clubs.Add(club);
                }
                
                FilterClubs();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading clubs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadClubStatistics(Club club)
        {
            try
            {
                // Load members count
                var members = await _userService.GetUsersByClubAsync(club.ClubID);
                club.Members = members.ToList();

                // Load events count
                var events = await _eventService.GetEventsByClubAsync(club.ClubID);
                club.Events = events.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading club statistics for {club.Name}: {ex.Message}");
            }
        }

        private void FilterClubs()
        {
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
        }

        private void AddClub(object? parameter)
        {
            // Logic to open add club window/dialog
            System.Diagnostics.Debug.WriteLine("Add Club clicked");
        }

        private void EditClub(Club? club)
        {
            if (club == null) return;
            
            // Logic to open edit club window/dialog
            System.Diagnostics.Debug.WriteLine($"Edit Club: {club.Name}");
        }

        private async void DeleteClub(Club? club)
        {
            if (club == null) return;

            try
            {
                // Check if club has members or events
                if (club.Members?.Any() == true || club.Events?.Any() == true)
                {
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
                    await _clubService.DeleteClubAsync(club.ClubID);
                    Clubs.Remove(club);
                    FilterClubs();
                }
            }
            catch (Exception ex)
            {
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
            
            SelectedClub = club;
            // Logic to open club details window/dialog
            System.Diagnostics.Debug.WriteLine($"View Club Details: {club.Name}");
        }

        private void ManageLeadership(Club? club)
        {
            if (club == null) return;
            
            // Logic to open leadership management window/dialog
            System.Diagnostics.Debug.WriteLine($"Manage Leadership for: {club.Name}");
        }

        private void ViewMembers(Club? club)
        {
            if (club == null) return;
            
            // Logic to navigate to members view filtered by this club
            System.Diagnostics.Debug.WriteLine($"View Members for: {club.Name}");
        }

        private void ViewEvents(Club? club)
        {
            if (club == null) return;
            
            // Logic to navigate to events view filtered by this club
            System.Diagnostics.Debug.WriteLine($"View Events for: {club.Name}");
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
    }
}