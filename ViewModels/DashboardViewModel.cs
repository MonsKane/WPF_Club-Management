using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly IEventService _eventService;
        private readonly IReportService _reportService;
        private int _totalUsers;
        private int _totalClubs;
        private int _totalEvents;
        private int _totalReports;
        private int _activeEvents;
        private int _upcomingEvents;
        private int _newMembersThisMonth;
        private bool _isLoading;
        private ObservableCollection<Event> _recentEvents = new();

        public DashboardViewModel(IUserService userService, IClubService clubService,
                                IEventService eventService, IReportService reportService)
        {
            _userService = userService;
            _clubService = clubService;
            _eventService = eventService;
            _reportService = reportService;
            InitializeCommands();
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set => SetProperty(ref _totalUsers, value);
        }

        public int TotalClubs
        {
            get => _totalClubs;
            set => SetProperty(ref _totalClubs, value);
        }

        public int TotalEvents
        {
            get => _totalEvents;
            set => SetProperty(ref _totalEvents, value);
        }

        public int TotalReports
        {
            get => _totalReports;
            set => SetProperty(ref _totalReports, value);
        }

        public int ActiveEvents
        {
            get => _activeEvents;
            set => SetProperty(ref _activeEvents, value);
        }

        public int UpcomingEvents
        {
            get => _upcomingEvents;
            set => SetProperty(ref _upcomingEvents, value);
        }

        public int NewMembersThisMonth
        {
            get => _newMembersThisMonth;
            set => SetProperty(ref _newMembersThisMonth, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<Event> RecentEvents
        {
            get => _recentEvents;
            set => SetProperty(ref _recentEvents, value);
        }

        // Commands
        public ICommand AddMemberCommand { get; private set; } = null!;
        public ICommand CreateEventCommand { get; private set; } = null!;
        public ICommand AddClubCommand { get; private set; } = null!;
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand ViewAllUsersCommand { get; private set; } = null!;
        public ICommand ViewAllClubsCommand { get; private set; } = null!;
        public ICommand ViewAllEventsCommand { get; private set; } = null!;
        public ICommand ViewAllReportsCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddMemberCommand = new RelayCommand(AddMember);
            CreateEventCommand = new RelayCommand(CreateEvent);
            AddClubCommand = new RelayCommand(AddClub);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            ViewAllUsersCommand = new RelayCommand(ViewAllUsers);
            ViewAllClubsCommand = new RelayCommand(ViewAllClubs);
            ViewAllEventsCommand = new RelayCommand(ViewAllEvents);
            ViewAllReportsCommand = new RelayCommand(ViewAllReports);
            RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;

                // Load statistics
                var users = await _userService.GetAllUsersAsync();
                var clubs = await _clubService.GetAllClubsAsync();
                var events = await _eventService.GetAllEventsAsync();
                var reports = await _reportService.GetAllReportsAsync();

                TotalUsers = users.Count();
                TotalClubs = clubs.Count();
                TotalEvents = events.Count();
                TotalReports = reports.Count();

                // Calculate event statistics
                var now = DateTime.Now;
                ActiveEvents = events.Count(e => e.EventDate <= now && e.EventDate >= now);
                UpcomingEvents = events.Count(e => e.EventDate > now);

                // Calculate new members this month
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                NewMembersThisMonth = users.Count(u => u.JoinDate >= startOfMonth);

                // Load recent events
                var recentEventsList = events
                    .OrderByDescending(e => e.CreatedDate)
                    .Take(5)
                    .ToList();

                RecentEvents.Clear();
                foreach (var eventItem in recentEventsList)
                {
                    RecentEvents.Add(eventItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddMember(object? parameter)
        {
            // Logic to navigate to add member view or open dialog
            System.Diagnostics.Debug.WriteLine("Add Member clicked from Dashboard");
        }

        private void CreateEvent(object? parameter)
        {
            // Logic to navigate to create event view or open dialog
            System.Diagnostics.Debug.WriteLine("Create Event clicked from Dashboard");
        }

        private void AddClub(object? parameter)
        {
            // Logic to navigate to add club view or open dialog
            System.Diagnostics.Debug.WriteLine("Add Club clicked from Dashboard");
        }

        private void GenerateReport(object? parameter)
        {
            // Logic to navigate to reports view or open report generation dialog
            System.Diagnostics.Debug.WriteLine("Generate Report clicked from Dashboard");
        }

        private void ViewAllUsers(object? parameter)
        {
            // Logic to navigate to users view
            System.Diagnostics.Debug.WriteLine("View All Users clicked from Dashboard");
        }

        private void ViewAllClubs(object? parameter)
        {
            // Logic to navigate to clubs view
            System.Diagnostics.Debug.WriteLine("View All Clubs clicked from Dashboard");
        }

        private void ViewAllEvents(object? parameter)
        {
            // Logic to navigate to events view
            System.Diagnostics.Debug.WriteLine("View All Events clicked from Dashboard");
        }

        private void ViewAllReports(object? parameter)
        {
            // Logic to navigate to reports view
            System.Diagnostics.Debug.WriteLine("View All Reports clicked from Dashboard");
        }

        public override Task LoadAsync()
        {
            return LoadDashboardDataAsync();
        }
    }
}