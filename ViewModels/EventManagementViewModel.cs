using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

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
        private ComboBoxItem? _selectedStatus;
        private DateTime? _selectedDate;
        private bool _isLoading;
        private Event? _selectedEvent;
        private Club? _clubFilter;
        private User? _currentUser;

        // Static event for event changes
        public static event Action? EventChanged;

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

        private bool IsOwner(User user, int clubId)
        {
            if (_authorizationService.IsAdmin(user) || user.Role is UserRole.Member) return true;

            return user.ClubID == clubId;
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

        public ComboBoxItem? SelectedStatus
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
        public ICommand AddEventCommand { get; private set; } = null!;
        public ICommand EditEventCommand { get; private set; } = null!;
        public ICommand DeleteEventCommand { get; private set; } = null!;

        public ICommand JoinEventCommand { get; private set; } = null!;
        public ICommand ViewEventCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ExportEventsCommand { get; private set; } = null!;
        public ICommand ManageParticipantsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            //CreateEventCommand = new RelayCommand(CreateEvent, CanExecuteCreateEvent);
            //AddEventCommand = new RelayCommand(CreateEvent, CanExecuteCreateEvent);
            //EditEventCommand = new RelayCommand<Event>(EditEvent, CanExecuteEditEvent);
            //DeleteEventCommand = new RelayCommand<Event>(DeleteEvent, CanExecuteDeleteEvent);
            //JoinEventCommand = new RelayCommand<Event>(JoinEvent, CanExecuteJoinEvent);
            //ViewEventCommand = new RelayCommand<Event>(ViewEvent, CanExecuteViewEvent);
            //RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            //ExportEventsCommand = new RelayCommand(ExportEvents, CanExecuteExportEvents);
            //ManageParticipantsCommand = new RelayCommand<Event>(ManageParticipants, CanExecuteManageParticipants);

            CreateEventCommand = new RelayCommand(CreateEvent, (_) => CurrentUser!.Role is not UserRole.Member);
            AddEventCommand = new RelayCommand(CreateEvent, (_) => CurrentUser!.Role is not UserRole.Member);
            EditEventCommand = new RelayCommand<Event>(EditEvent, (_) => CurrentUser!.Role is not UserRole.Member);
            DeleteEventCommand = new RelayCommand<Event>(DeleteEvent, (_) => CurrentUser!.Role is not UserRole.Member);
            JoinEventCommand = new RelayCommand<Event>(JoinEvent, (_) => true);
            ViewEventCommand = new RelayCommand<Event>(ViewEvent, (_) => true);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ExportEventsCommand = new RelayCommand(ExportEvents, (_) => CurrentUser!.Role is not UserRole.Member);
            ManageParticipantsCommand = new RelayCommand<Event>(ManageParticipants, (_) => CurrentUser!.Role is not UserRole.Member);
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

        private bool CanExecuteJoinEvent(Event? eventItem)
        {
            return eventItem != null && CurrentUser != null && _authorizationService.CanJoinEvents(CurrentUser.Role);
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
                events = events.Where(e => IsOwner(CurrentUser!, e.ClubID)).ToList();
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
            if (SelectedStatus?.Content != "All Events")
            {
                filtered = SelectedStatus?.Content switch
                {
                    "Upcoming" => filtered.Where(e => e.Status == EventStatus.Scheduled),
                    "Ongoing" => filtered.Where(e => e.Status == EventStatus.InProgress),
                    "Cancelled" => filtered.Where(e => e.Status == EventStatus.Cancelled),
                    "Completed" => filtered.Where(e => e.Status == EventStatus.Completed),
                    _ => filtered
                };
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Applied status filter: '{SelectedStatus}'");
            }

            // Filter by date
            if (SelectedDate.HasValue)
            {
                var date = SelectedDate.Value.Date;
                var nextDate = date.AddDays(1);

                filtered = filtered.Where(e => e.EventDate >= date && e.EventDate < nextDate);
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
                if (_clubs.IsNullOrEmpty())
                {
                    var clubs = await _clubService.GetAllClubsAsync();
                    Clubs.Clear();
                    foreach (var club in clubs)
                    {
                        Clubs.Add(club);
                    }
                }

                // Pass the ClubFilter if we're in club-specific context
                var addEventDialog = ClubFilter != null
                    ? new Views.AddEventDialog(Clubs, ClubFilter, CurrentUser)
                    : new Views.AddEventDialog(Clubs, CurrentUser);

                if (addEventDialog.ShowDialog() == true && addEventDialog.NewEvent != null)
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Creating new event: {addEventDialog.NewEvent.Name}");
                    var createdEvent = await _eventService.CreateEventAsync(addEventDialog.NewEvent);
                    Events.Add(createdEvent);
                    FilterEvents();
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Event created successfully: {createdEvent.Name}");
                    System.Windows.MessageBox.Show($"Event '{createdEvent.Name}' created successfully!", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    // Notify other ViewModels about event change
                    EventChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error creating event: {ex.Message}");
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
                var editEventDialog = new Views.EditEventDialog(eventItem, Clubs, CurrentUser);
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

                    // Notify other ViewModels about event change
                    EventChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error updating event {eventItem.Name}: {ex.Message}");
                System.Windows.MessageBox.Show($"Error updating event: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void JoinEvent(Event? eventItem)
        {
            if (eventItem == null)
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Join Event command called with null event");
                return;
            }

            if (CurrentUser == null)
            {
                Console.WriteLine("[EVENT_MANAGEMENT_VM] Join Event command called with null current user");
                return;
            }

            Console.WriteLine($"[EVENT_MANAGEMENT_VM] Join Event command executed for: {eventItem.Name} (ID: {eventItem.EventID}) by user: {CurrentUser.FullName} (ID: {CurrentUser.UserID})");

            try
            {
                // Check if registration deadline has passed
                if (eventItem.RegistrationDeadline.HasValue && DateTime.Now > eventItem.RegistrationDeadline.Value)
                {
                    System.Windows.MessageBox.Show(
                        "Registration deadline has passed for this event.",
                        "Registration Closed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Check if event has already occurred
                if (eventItem.EventDate < DateTime.Now)
                {
                    System.Windows.MessageBox.Show(
                        "Cannot register for past events.",
                        "Event Already Occurred",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Check if event has reached maximum participants
                if (eventItem.MaxParticipants.HasValue)
                {
                    var participants = await _eventService.GetEventParticipantsAsync(eventItem.EventID);
                    if (participants.Count() >= eventItem.MaxParticipants.Value)
                    {
                        System.Windows.MessageBox.Show(
                            "This event has reached its maximum number of participants.",
                            "Event Full",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                }

                // Confirm registration
                var result = System.Windows.MessageBox.Show(
                    $"Do you want to register for the event '{eventItem.Name}'?",
                    "Confirm Registration",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] User confirmed registration for event: {eventItem.Name}");

                    bool success = await _eventService.RegisterUserForEventAsync(eventItem.EventID, CurrentUser.UserID);

                    if (success)
                    {
                        Console.WriteLine($"[EVENT_MANAGEMENT_VM] User successfully registered for event: {eventItem.Name}");
                        System.Windows.MessageBox.Show(
                            $"You have successfully registered for '{eventItem.Name}'.",
                            "Registration Successful",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        // Notify other ViewModels about event change
                        EventChanged?.Invoke();
                    }
                    else
                    {
                        Console.WriteLine($"[EVENT_MANAGEMENT_VM] Failed to register user for event: {eventItem.Name}");
                        System.Windows.MessageBox.Show(
                            "Registration failed. You may already be registered for this event.",
                            "Registration Failed",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }
                }
                else
                {
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] User cancelled registration for event: {eventItem.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error registering for event {eventItem.Name}: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error registering for event: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
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

                    // Notify other ViewModels about event change
                    EventChanged?.Invoke();
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
            var now = DateTime.Now;
            if (eventItem.EventDate > now)
                return "Upcoming";
            else if (eventItem.EventDate <= now && eventItem.EventDate >= now)
                return "Ongoing";
            else
                return "Completed";
        }

        public void SetClubFilter(Club club)
        {
            ClubFilter = club;
        }

        public void ClearClubFilter()
        {
            ClubFilter = null;
        }

        private bool CanExecuteGenerateReports(object? parameter)
        {
            return CurrentUser != null && _authorizationService.CanExportReports(CurrentUser.Role);
        }

        private async void GenerateEventStatisticsReport(object? parameter)
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Generate Event Statistics Report command executed");
            try
            {
                IsLoading = true;
                var currentSemester = GetCurrentSemester();
                var reportContent = GenerateEventStatisticsReportContent(FilteredEvents);

                var report = new Models.Report
                {
                    Title = $"Event Statistics Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = Models.ReportType.EventOutcomes,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = CurrentUser?.ClubID,
                    GeneratedByUserID = CurrentUser?.UserID ?? 0
                };

                // Save report to file
                await SaveReportToFileAsync(report, "Event_Statistics");

                System.Windows.MessageBox.Show("Event Statistics Report generated and saved successfully!", "Report Generated",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error generating event statistics report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void GenerateEventAttendanceReport(object? parameter)
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Generate Event Attendance Report command executed");
            try
            {
                IsLoading = true;
                var currentSemester = GetCurrentSemester();
                var reportContent = GenerateEventAttendanceReportContent(FilteredEvents);

                var report = new Models.Report
                {
                    Title = $"Event Attendance Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = Models.ReportType.EventOutcomes,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = CurrentUser?.ClubID,
                    GeneratedByUserID = CurrentUser?.UserID ?? 0
                };

                await SaveReportToFileAsync(report, "Event_Attendance");

                System.Windows.MessageBox.Show("Event Attendance Report generated and saved successfully!", "Report Generated",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error generating event attendance report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void GenerateEventPerformanceReport(object? parameter)
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Generate Event Performance Report command executed");
            try
            {
                IsLoading = true;
                var currentSemester = GetCurrentSemester();
                var reportContent = GenerateEventPerformanceReportContent(FilteredEvents);

                var report = new Models.Report
                {
                    Title = $"Event Performance Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = Models.ReportType.EventOutcomes,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = CurrentUser?.ClubID,
                    GeneratedByUserID = CurrentUser?.UserID ?? 0
                };

                await SaveReportToFileAsync(report, "Event_Performance");

                System.Windows.MessageBox.Show("Event Performance Report generated and saved successfully!", "Report Generated",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error generating event performance report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void GenerateEventSummaryReport(object? parameter)
        {
            Console.WriteLine("[EVENT_MANAGEMENT_VM] Generate Event Summary Report command executed");
            try
            {
                IsLoading = true;
                var currentSemester = GetCurrentSemester();
                var reportContent = GenerateEventSummaryReportContent(FilteredEvents);

                var report = new Models.Report
                {
                    Title = $"Event Summary Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = Models.ReportType.EventOutcomes,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = CurrentUser?.ClubID,
                    GeneratedByUserID = CurrentUser?.UserID ?? 0
                };

                await SaveReportToFileAsync(report, "Event_Summary");

                System.Windows.MessageBox.Show("Event Summary Report generated and saved successfully!", "Report Generated",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error generating event summary report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetCurrentSemester()
        {
            var now = DateTime.Now;
            var year = now.Year;
            var semester = now.Month >= 8 ? "Fall" : now.Month >= 1 && now.Month <= 5 ? "Spring" : "Summer";
            return $"{semester} {year}";
        }

        private string GenerateEventStatisticsReportContent(IEnumerable<Event> events)
        {
            var content = new System.Text.StringBuilder();
            content.AppendLine("EVENT STATISTICS REPORT");
            content.AppendLine("======================\n");
            content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n");

            var eventsList = events.ToList();
            content.AppendLine($"Total Events: {eventsList.Count}");
            content.AppendLine($"Upcoming Events: {eventsList.Count(e => e.EventDate > DateTime.Now)}");
            content.AppendLine($"Completed Events: {eventsList.Count(e => e.EventDate < DateTime.Now)}");
            content.AppendLine($"Events This Month: {eventsList.Count(e => e.EventDate.Month == DateTime.Now.Month && e.EventDate.Year == DateTime.Now.Year)}\n");

            // Events by Club
            var eventsByClub = eventsList.GroupBy(e => e.Club?.Name ?? "Unknown").OrderByDescending(g => g.Count());
            content.AppendLine("EVENTS BY CLUB:");
            foreach (var group in eventsByClub)
            {
                content.AppendLine($"  {group.Key}: {group.Count()} events");
            }

            return content.ToString();
        }

        private string GenerateEventAttendanceReportContent(IEnumerable<Event> events)
        {
            var content = new System.Text.StringBuilder();
            content.AppendLine("EVENT ATTENDANCE REPORT");
            content.AppendLine("=======================\n");
            content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n");

            var eventsList = events.ToList();
            content.AppendLine("EVENT ATTENDANCE DETAILS:");
            foreach (var eventItem in eventsList.OrderBy(e => e.EventDate))
            {
                content.AppendLine($"\nEvent: {eventItem.Name}");
                content.AppendLine($"Date: {eventItem.EventDate:yyyy-MM-dd HH:mm}");
                content.AppendLine($"Location: {eventItem.Location ?? "TBD"}");
                content.AppendLine($"Club: {eventItem.Club?.Name ?? "Unknown"}");
                content.AppendLine($"Participants: {eventItem.ParticipantCount}");
                content.AppendLine($"Max Capacity: {eventItem.MaxParticipants ?? 0}");
                var attendanceRate = eventItem.MaxParticipants > 0 ? (double)eventItem.ParticipantCount / eventItem.MaxParticipants.Value * 100 : 0;
                content.AppendLine($"Attendance Rate: {attendanceRate:F1}%");
            }

            return content.ToString();
        }

        private string GenerateEventPerformanceReportContent(IEnumerable<Event> events)
        {
            var content = new System.Text.StringBuilder();
            content.AppendLine("EVENT PERFORMANCE REPORT");
            content.AppendLine("========================\n");
            content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n");

            var eventsList = events.ToList();
            var completedEvents = eventsList.Where(e => e.EventDate < DateTime.Now).ToList();

            content.AppendLine("PERFORMANCE METRICS:");
            content.AppendLine($"Total Events Analyzed: {completedEvents.Count}");

            if (completedEvents.Any())
            {
                var avgParticipants = completedEvents.Average(e => e.ParticipantCount);
                var totalParticipants = completedEvents.Sum(e => e.ParticipantCount);
                content.AppendLine($"Average Participants per Event: {avgParticipants:F1}");
                content.AppendLine($"Total Participants: {totalParticipants}");

                var topEvent = completedEvents.OrderByDescending(e => e.ParticipantCount).FirstOrDefault();
                if (topEvent != null)
                {
                    content.AppendLine($"\nMost Attended Event: {topEvent.Name} ({topEvent.ParticipantCount} participants)");
                }
            }

            return content.ToString();
        }

        private string GenerateEventSummaryReportContent(IEnumerable<Event> events)
        {
            var content = new System.Text.StringBuilder();
            content.AppendLine("EVENT SUMMARY REPORT");
            content.AppendLine("===================\n");
            content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n");

            var eventsList = events.ToList();
            content.AppendLine("EXECUTIVE SUMMARY:");
            content.AppendLine($"Total Events: {eventsList.Count}");
            content.AppendLine($"Active Clubs: {eventsList.Select(e => e.ClubID).Distinct().Count()}");
            content.AppendLine($"Total Participants: {eventsList.Sum(e => e.ParticipantCount)}");

            var upcomingEvents = eventsList.Where(e => e.EventDate > DateTime.Now).ToList();
            var completedEvents = eventsList.Where(e => e.EventDate < DateTime.Now).ToList();

            content.AppendLine($"\nSTATUS BREAKDOWN:");
            content.AppendLine($"Upcoming Events: {upcomingEvents.Count}");
            content.AppendLine($"Completed Events: {completedEvents.Count}");

            content.AppendLine($"\nUPCOMING EVENTS:");
            foreach (var eventItem in upcomingEvents.Take(5).OrderBy(e => e.EventDate))
            {
                content.AppendLine($"  • {eventItem.Name} - {eventItem.EventDate:MMM dd, yyyy}");
            }

            return content.ToString();
        }

        private async Task SaveReportToFileAsync(Models.Report report, string reportType)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|PDF files (*.pdf)|*.pdf|CSV files (*.csv)|*.csv",
                    DefaultExt = "txt",
                    FileName = $"{reportType}_Report_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllTextAsync(saveFileDialog.FileName, report.Content, System.Text.Encoding.UTF8);
                    Console.WriteLine($"[EVENT_MANAGEMENT_VM] Report saved to: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_MANAGEMENT_VM] Error saving report: {ex.Message}");
                throw;
            }
        }

        public override Task LoadAsync()
        {
            return LoadDataAsync();
        }
    }
}
