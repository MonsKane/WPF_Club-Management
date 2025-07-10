using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class EventManagementViewModel : BaseViewModel
    {
        private readonly IEventService _eventService;
        private readonly IClubService _clubService;
        private readonly IUserService _userService;
        private ObservableCollection<Event> _events = new();
        private ObservableCollection<Event> _filteredEvents = new();
        private ObservableCollection<Club> _clubs = new();
        private string _searchText = string.Empty;
        private string _selectedStatus = "All Events";
        private DateTime? _selectedDate;
        private bool _isLoading;
        private Event? _selectedEvent;

        public EventManagementViewModel(IEventService eventService, IClubService clubService, IUserService userService)
        {
            _eventService = eventService;
            _clubService = clubService;
            _userService = userService;
            InitializeCommands();
        }

        public ObservableCollection<Event> Events
        {
            get => _events;
            set => SetProperty(ref _events, value);
        }

        public ObservableCollection<Event> FilteredEvents
        {
            get => _filteredEvents;
            set => SetProperty(ref _filteredEvents, value);
        }

        public ObservableCollection<Club> Clubs
        {
            get => _clubs;
            set => SetProperty(ref _clubs, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterEvents();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    FilterEvents();
                }
            }
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    FilterEvents();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Event? SelectedEvent
        {
            get => _selectedEvent;
            set => SetProperty(ref _selectedEvent, value);
        }

        // Commands
        public ICommand CreateEventCommand { get; private set; } = null!;
        public ICommand EditEventCommand { get; private set; } = null!;
        public ICommand DeleteEventCommand { get; private set; } = null!;
        public ICommand ViewEventCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ExportEventsCommand { get; private set; } = null!;
        public ICommand ManageParticipantsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            CreateEventCommand = new RelayCommand(CreateEvent);
            EditEventCommand = new RelayCommand<Event>(EditEvent);
            DeleteEventCommand = new RelayCommand<Event>(DeleteEvent);
            ViewEventCommand = new RelayCommand<Event>(ViewEvent);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ExportEventsCommand = new RelayCommand(ExportEvents);
            ManageParticipantsCommand = new RelayCommand<Event>(ManageParticipants);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Load events
                var events = await _eventService.GetAllEventsAsync();
                Events.Clear();
                foreach (var eventItem in events)
                {
                    Events.Add(eventItem);
                }

                // Load clubs
                var clubs = await _clubService.GetAllClubsAsync();
                Clubs.Clear();
                foreach (var club in clubs)
                {
                    Clubs.Add(club);
                }

                FilterEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading events: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterEvents()
        {
            var filtered = Events.AsEnumerable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(e =>
                    e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.Location?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Filter by status
            if (SelectedStatus != "All Events")
            {
                var now = DateTime.Now;
                filtered = SelectedStatus switch
                {
                    "Upcoming" => filtered.Where(e => e.EventDate > now),
                    "Ongoing" => filtered.Where(e => e.EventDate <= now && e.EventDate >= now),
                    "Completed" => filtered.Where(e => e.EventDate < now),
                    _ => filtered
                };
            }

            // Filter by date
            if (SelectedDate.HasValue)
            {
                var selectedDate = SelectedDate.Value.Date;
                filtered = filtered.Where(e =>
                    e.EventDate.Date <= selectedDate && e.EventDate.Date >= selectedDate);
            }

            FilteredEvents.Clear();
            foreach (var eventItem in filtered.OrderBy(e => e.EventDate))
            {
                FilteredEvents.Add(eventItem);
            }
        }

        private void CreateEvent(object? parameter)
        {
            // Logic to open create event window/dialog
            System.Diagnostics.Debug.WriteLine("Create Event clicked");
        }

        private void EditEvent(Event? eventItem)
        {
            if (eventItem == null) return;

            // Logic to open edit event window/dialog
            System.Diagnostics.Debug.WriteLine($"Edit Event: {eventItem.Name}");
        }

        private async void DeleteEvent(Event? eventItem)
        {
            if (eventItem == null) return;

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the event '{eventItem.Name}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await _eventService.DeleteEventAsync(eventItem.EventID);
                    Events.Remove(eventItem);
                    FilterEvents();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error deleting event: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ViewEvent(Event? eventItem)
        {
            if (eventItem == null) return;

            // Logic to open event details window/dialog
            System.Diagnostics.Debug.WriteLine($"View Event: {eventItem.Name}");
        }

        private void ManageParticipants(Event? eventItem)
        {
            if (eventItem == null) return;

            // Logic to open participants management window/dialog
            System.Diagnostics.Debug.WriteLine($"Manage Participants for: {eventItem.Name}");
        }

        private void ExportEvents(object? parameter)
        {
            // Logic to export events to CSV/Excel
            System.Diagnostics.Debug.WriteLine("Export Events clicked");
        }

        public string GetEventStatus(Event eventItem)
        {
            var now = DateTime.Now;
            if (eventItem.EventDate > now)
                return "Upcoming";
            else if (eventItem.EventDate <= now && eventItem.EventDate >= now)
                return "Ongoing";
            else
                return "Completed";
        }

        public override Task LoadAsync()
        {
            return LoadDataAsync();
        }
    }
}