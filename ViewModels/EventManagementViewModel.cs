using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static ClubManagementApp.Models.EventStatus;

namespace ClubManagementApp.ViewModels
{
    public class EventManagementViewModel : BaseViewModel
    {
        private readonly IEventService _eventService;
        private readonly IClubService _clubService;
        private readonly IUserService _userService;
        private readonly IAuthorizationService _authorizationService;
        private ObservableCollection<Event> _events = new();
        private ObservableCollection<Event> _filteredEvents = new();
        private ObservableCollection<Club> _clubs = new();
        private string _searchText = string.Empty;
        private string _selectedStatus = "All Events";
        private DateTime? _selectedDate;
        private bool _isLoading;
        private Event? _selectedEvent;
        private Club? _clubFilter;
        private User? _currentUser;

        public EventManagementViewModel(IEventService eventService, IClubService clubService, IUserService userService, IAuthorizationService authorizationService)
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Initializing EventManagementViewModel");
            _eventService = eventService;
            _clubService = clubService;
            _userService = userService;
            _authorizationService = authorizationService;
            InitializeCommands();
            LoadCurrentUserAsync();
            Console.WriteLine("[EVENT_MANAGEMENT_VM] EventManagementViewModel initialized successfully");
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
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Search text changed to: '{value}'");
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
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Status filter changed to: '{value}'");
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
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Date filter changed to: {value?.ToString("yyyy-MM-dd") ?? "None"}");
                    FilterEvents();
                }
            }
        }

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Event? SelectedEvent
        {
            get => _selectedEvent;
            set => SetProperty(ref _selectedEvent, value);
        }

        public Club? ClubFilter
        {
            get => _clubFilter;
            private set
            {
                if (SetProperty(ref _clubFilter, value))
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Club filter changed to: {value?.Name ?? "All Clubs"}");
                    FilterEvents();
                }
            }
        }

        public int UpcomingEventsCount
        {
            get
            {
                var now = DateTime.Now;
                return Events.Count(e => e.EventDate > now);
            }
        }

        public int OngoingEventsCount
        {
            get
            {
                var now = DateTime.Now;
                return Events.Count(e => e.EventDate.Date == now.Date);
            }
        }

        public int CompletedEventsCount
        {
            get
            {
                var now = DateTime.Now;
                return Events.Count(e => e.EventDate < now);
            }
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        // Event Management Permissions
        public bool CanAccessEventManagement
        {
            get => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "EventManagement");
        }

        public bool CanCreateEvents
        {
            get 
            {
                var canCreate = CurrentUser != null && _authorizationService.CanCreateEvents(CurrentUser.Role);
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] CanCreateEvents check: CurrentUser={CurrentUser?.FullName ?? "NULL"}, Role={CurrentUser?.Role}, CanCreate={canCreate}");
                return canCreate;
            }
        }

        public bool CanEditEvents
        {
            get => CurrentUser != null && _authorizationService.CanEditEvents(CurrentUser.Role, CurrentUser.ClubID);
        }

        public bool CanDeleteEvents
        {
            get => CurrentUser != null && _authorizationService.CanDeleteEvents(CurrentUser.Role, CurrentUser.ClubID);
        }

        public bool CanRegisterForEvents
        {
            get => CurrentUser != null && _authorizationService.CanRegisterForEvents(CurrentUser.Role);
        }

        public bool CanEditSelectedEvent
        {
            get => SelectedEvent != null && CurrentUser != null &&
                   _authorizationService.CanEditEvents(CurrentUser.Role, CurrentUser.ClubID, SelectedEvent.ClubID, IsOwnEvent(SelectedEvent));
        }

        public bool CanDeleteSelectedEvent
        {
            get => SelectedEvent != null && CurrentUser != null &&
                   _authorizationService.CanDeleteEvents(CurrentUser.Role, CurrentUser.ClubID, SelectedEvent.ClubID, IsOwnEvent(SelectedEvent));
        }

        private bool IsOwnEvent(Event eventItem)
        {
            // Check if the current user is the creator/owner of the event
            // This would need to be implemented based on your Event model
            // For now, assuming events belong to the user's club or created by team leaders
            return CurrentUser?.Role == UserRole.TeamLeader && eventItem.ClubID == CurrentUser.ClubID;
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
            CreateEventCommand = new RelayCommand(CreateEvent, CanExecuteCreateEvent);
            EditEventCommand = new RelayCommand<Event>(EditEvent, CanExecuteEditEvent);
            DeleteEventCommand = new RelayCommand<Event>(DeleteEvent, CanExecuteDeleteEvent);
            ViewEventCommand = new RelayCommand<Event>(ViewEvent, CanExecuteViewEvent);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ExportEventsCommand = new RelayCommand(ExportEvents, CanExecuteExportEvents);
            ManageParticipantsCommand = new RelayCommand<Event>(ManageParticipants, CanExecuteManageParticipants);
        }

        private async void LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.Role})");
                OnPropertyChanged(nameof(CanAccessEventManagement));
                OnPropertyChanged(nameof(CanCreateEvents));
                OnPropertyChanged(nameof(CanEditEvents));
                OnPropertyChanged(nameof(CanDeleteEvents));
                OnPropertyChanged(nameof(CanRegisterForEvents));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error loading current user: {ex.Message}");
            }
        }

        private bool CanExecuteCreateEvent(object? parameter)
        {
            return CanCreateEvents;
        }

        private bool CanExecuteEditEvent(Event? eventItem)
        {
            return eventItem != null && CurrentUser != null &&
                   _authorizationService.CanEditEvents(CurrentUser.Role, CurrentUser.ClubID, eventItem.ClubID, IsOwnEvent(eventItem));
        }

        private bool CanExecuteDeleteEvent(Event? eventItem)
        {
            return eventItem != null && CurrentUser != null &&
                   _authorizationService.CanDeleteEvents(CurrentUser.Role, CurrentUser.ClubID, eventItem.ClubID, IsOwnEvent(eventItem));
        }

        private bool CanExecuteViewEvent(Event? eventItem)
        {
            return eventItem != null;
        }

        private bool CanExecuteExportEvents(object? parameter)
        {
            return CurrentUser != null && _authorizationService.CanExportReports(CurrentUser.Role);
        }

        private bool CanExecuteManageParticipants(Event? eventItem)
        {
            return eventItem != null && CurrentUser != null &&
                   _authorizationService.CanEditEvents(CurrentUser.Role, CurrentUser.ClubID, eventItem.ClubID, IsOwnEvent(eventItem));
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Starting to load event management data...");
                IsLoading = true;

                // Load events
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Loading events...");
                var events = await _eventService.GetAllEventsAsync();
                Events.Clear();
                foreach (var eventItem in events)
                {
                    Events.Add(eventItem);
                }
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Loaded {Events.Count} events");

                // Load clubs
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Loading clubs...");
                var clubs = await _clubService.GetAllClubsAsync();
                Clubs.Clear();
                foreach (var club in clubs)
                {
                    Clubs.Add(club);
                }
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Loaded {Clubs.Count} clubs");

                FilterEvents();
                
                // Notify property changes for count properties after loading
                OnPropertyChanged(nameof(UpcomingEventsCount));
                OnPropertyChanged(nameof(OngoingEventsCount));
                OnPropertyChanged(nameof(CompletedEventsCount));
                
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Event management data loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error loading events: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading events: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterEvents()
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Applying event filters...");
            var filtered = Events.AsEnumerable();
            var originalCount = Events.Count;

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(e =>
                    e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.Location?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Applied search filter: '{SearchText}'");
            }

            // Filter by status
            if (SelectedStatus != "All Events")
            {
                var now = DateTime.Now;
                filtered = SelectedStatus switch
                {
                    "Upcoming" => filtered.Where(e => e.EventDate > now && (e.Status == EventStatus.Scheduled || e.Status == EventStatus.Postponed)),
                    "Ongoing" => filtered.Where(e => e.EventDate.Date == now.Date && e.Status == EventStatus.InProgress),
                    "Cancelled" => filtered.Where(e => e.Status == EventStatus.Cancelled),
                    "Completed" => filtered.Where(e => e.Status == EventStatus.Completed || (e.EventDate < now && e.Status != EventStatus.Cancelled)),
                    _ => filtered
                };
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Applied status filter: '{SelectedStatus}'");
            }

            // Filter by date
            if (SelectedDate.HasValue)
            {
                var selectedDate = SelectedDate.Value.Date;
                filtered = filtered.Where(e => e.EventDate.Date == selectedDate);
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Applied date filter: {SelectedDate.Value:yyyy-MM-dd}");
            }

            // Filter by club
            if (ClubFilter != null)
            {
                filtered = filtered.Where(e => e.ClubID == ClubFilter.ClubID);
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Applied club filter: '{ClubFilter.Name}'");
            }

            FilteredEvents.Clear();
            foreach (var eventItem in filtered.OrderBy(e => e.EventDate))
            {
                FilteredEvents.Add(eventItem);
            }
            
            // Notify property changes for count properties
            OnPropertyChanged(nameof(UpcomingEventsCount));
            OnPropertyChanged(nameof(OngoingEventsCount));
            OnPropertyChanged(nameof(CompletedEventsCount));
            
            Console.WriteLine($"[EVENT_MANAGEMENT_VM] Filtering complete: {originalCount} -> {FilteredEvents.Count} events");
        }

        private async void CreateEvent(object? parameter)
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Create Event command executed");
            try
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Available clubs count: {Clubs.Count}");
                var addEventDialog = new Views.AddEventDialog(Clubs);
                Console.WriteLine("[EVENT_MANAGEMENT_VM] AddEventDialog created, showing dialog...");
                
                var dialogResult = addEventDialog.ShowDialog();
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Dialog result: {dialogResult}, NewEvent is null: {addEventDialog.NewEvent == null}");
                
                if (dialogResult == true && addEventDialog.NewEvent != null)
                {
                    var newEvent = addEventDialog.NewEvent;
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Creating new event: {newEvent.Name}");
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event details - Date: {newEvent.EventDate}, Location: {newEvent.Location}, ClubID: {newEvent.ClubID}");
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Events collection count before: {Events.Count}");
                    
                    var createdEvent = await _eventService.CreateEventAsync(newEvent);
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event service returned: {createdEvent.Name} with ID: {createdEvent.EventID}");
                    
                    Events.Add(createdEvent);
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Events collection count after adding: {Events.Count}");
                    
                    FilterEvents();
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Filtered events count: {FilteredEvents.Count}");
                    
                    // Reload data to verify persistence
                    await LoadDataAsync();
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] After reload - Events count: {Events.Count}, Filtered count: {FilteredEvents.Count}");
                    
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event created successfully: {createdEvent.Name}");
                    System.Windows.MessageBox.Show($"Event '{createdEvent.Name}' created successfully!", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    Console.WriteLine("[EVENT_MANAGEMENT_VM] Dialog was cancelled or no event was created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error creating event: {ex.Message}");
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Stack trace: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"Error creating event: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void EditEvent(Event? eventItem)
        {
            if (eventItem == null)
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Edit Event command called with null event");
                return;
            }

            Console.WriteLine($"[EVENT_MANAGEMENT_VM] Edit Event command executed for: {eventItem.Name} (ID: {eventItem.EventID})");
            try
            {
                var editEventDialog = new Views.EditEventDialog(eventItem, Clubs);
                if (editEventDialog.ShowDialog() == true && editEventDialog.UpdatedEvent != null)
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Updating event: {editEventDialog.UpdatedEvent.Name}");
                    var updatedEvent = await _eventService.UpdateEventAsync(editEventDialog.UpdatedEvent);

                    // Update the event in the collection
                    var index = Events.IndexOf(eventItem);
                    if (index >= 0)
                    {
                        // Remove the old event and insert the updated one to trigger proper notifications
                        Events.RemoveAt(index);
                        Events.Insert(index, updatedEvent);
                    }
                    FilterEvents();
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event updated successfully: {updatedEvent.Name}");
                    System.Windows.MessageBox.Show($"Event '{updatedEvent.Name}' updated successfully!", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error updating event {eventItem.Name}: {ex.Message}");
                System.Windows.MessageBox.Show($"Error updating event: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void DeleteEvent(Event? eventItem)
        {
            if (eventItem == null)
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Delete Event command called with null event");
                return;
            }

            Console.WriteLine($"[EVENT_MANAGEMENT_VM] Delete Event command executed for: {eventItem.Name} (ID: {eventItem.EventID})");

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the event '{eventItem.Name}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] User confirmed deletion of event: {eventItem.Name}");
                    await _eventService.DeleteEventAsync(eventItem.EventID);
                    Events.Remove(eventItem);
                    FilterEvents();
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event deleted successfully: {eventItem.Name}");
                }
                else
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] User cancelled deletion of event: {eventItem.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error deleting event {eventItem.Name}: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error deleting event: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ViewEvent(Event? eventItem)
        {
            if (eventItem == null)
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] View Event command called with null event");
                return;
            }

            Console.WriteLine($"[EVENT_MANAGEMENT_VM] View Event command executed for: {eventItem.Name} (ID: {eventItem.EventID})");
            try
            {
                var eventDetailsDialog = new Views.EventDetailsDialog(eventItem);
                eventDetailsDialog.ShowDialog();
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event details dialog opened for: {eventItem.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error opening event details: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening event details: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ManageParticipants(Event? eventItem)
        {
            if (eventItem == null)
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Manage Participants command called with null event");
                return;
            }

            Console.WriteLine($"[EVENT_MANAGEMENT_VM] Manage Participants command executed for: {eventItem.Name} (ID: {eventItem.EventID})");
            try
            {
                var participantManagementDialog = new Views.ParticipantManagementDialog(eventItem, _eventService);
                participantManagementDialog.ShowDialog();
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Participant management dialog opened for: {eventItem.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error opening participant management: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening participant management: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExportEvents(object? parameter)
        {
            Console.WriteLine($"[EVENT_MANAGEMENT_VM] Export Events command executed - exporting {FilteredEvents.Count} events");
            try
            {
                if (!FilteredEvents.Any())
                {
                    System.Windows.MessageBox.Show("No events to export.", "Information",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt",
                    DefaultExt = "csv",
                    FileName = $"Events_Export_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Name,Description,Date,Location,Club,Status,Max Participants,Registration Deadline,Created Date");

                    foreach (var eventItem in FilteredEvents)
                    {
                        csv.AppendLine($"\"{eventItem.Name}\"," +
                                     $"\"{eventItem.Description?.Replace("\"", "\"\"") ?? "N/A"}\"," +
                                     $"\"{eventItem.EventDate:yyyy-MM-dd HH:mm}\"," +
                                     $"\"{eventItem.Location ?? "N/A"}\"," +
                                     $"\"{eventItem.Club?.Name ?? "N/A"}\"," +
                                     $"\"{eventItem.Status}\"," +
                                     $"{eventItem.MaxParticipants ?? 0}," +
                                     $"\"{(eventItem.RegistrationDeadline?.ToString("yyyy-MM-dd") ?? "N/A")}\"," +
                                     $"\"{eventItem.CreatedDate:yyyy-MM-dd}\"");
                    }

                    System.IO.File.WriteAllText(saveFileDialog.FileName, csv.ToString(), System.Text.Encoding.UTF8);
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Events exported successfully to: {saveFileDialog.FileName}");
                    System.Windows.MessageBox.Show($"Events exported successfully to:\n{saveFileDialog.FileName}", "Export Complete",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error exporting events: {ex.Message}");
                System.Windows.MessageBox.Show($"Error exporting events: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public string GetEventStatus(Event eventItem)
        {
            return eventItem.StatusDisplay;
        }

        public void SetClubFilter(Club club)
        {
            ClubFilter = club;
        }

        public void ClearClubFilter()
        {
            ClubFilter = null;
        }

        public override Task LoadAsync()
        {
            return LoadDataAsync();
        }
    }
}
