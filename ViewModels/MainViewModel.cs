using ClubManagementApp.Commands;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly IEventService _eventService;
        private readonly IReportService _reportService;
        private readonly INavigationService _navigationService;
        private readonly INotificationService _notificationService;

        private User? _currentUser;
        private string _currentView = "Dashboard";
        private ObservableCollection<User> _users = new();
        private ObservableCollection<Club> _clubs = new();
        private ObservableCollection<Event> _events = new();
        private ObservableCollection<Report> _reports = new();
        private bool _hasNotifications;
        private string _notificationMessage = string.Empty;

        // Child ViewModels
        public DashboardViewModel? DashboardViewModel { get; private set; }
        public MemberListViewModel? MemberListViewModel { get; private set; }
        public EventManagementViewModel? EventManagementViewModel { get; private set; }
        public ClubManagementViewModel? ClubManagementViewModel { get; private set; }
        public ReportsViewModel? ReportsViewModel { get; private set; }

        public MainViewModel(IUserService userService, IClubService clubService,
                           IEventService eventService, IReportService reportService,
                           INavigationService navigationService, INotificationService notificationService)
        {
            _userService = userService;
            _clubService = clubService;
            _eventService = eventService;
            _reportService = reportService;
            _navigationService = navigationService;
            _notificationService = notificationService;

            InitializeCommands();
            InitializeChildViewModels();
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public ObservableCollection<Club> Clubs
        {
            get => _clubs;
            set => SetProperty(ref _clubs, value);
        }

        public ObservableCollection<Event> Events
        {
            get => _events;
            set => SetProperty(ref _events, value);
        }

        public ObservableCollection<Report> Reports
        {
            get => _reports;
            set => SetProperty(ref _reports, value);
        }

        public bool HasNotifications
        {
            get => _hasNotifications;
            set => SetProperty(ref _hasNotifications, value);
        }

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }

        // Commands
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand OpenMemberListCommand { get; private set; } = null!;
        public ICommand OpenClubManagementCommand { get; private set; } = null!;
        public ICommand OpenEventManagementCommand { get; private set; } = null!;
        public ICommand OpenReportsCommand { get; private set; } = null!;
        public ICommand RefreshDataCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand DismissNotificationCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NavigateToDashboardCommand = new RelayCommand(() => CurrentView = "Dashboard");
            OpenMemberListCommand = new RelayCommand(() => _navigationService.OpenMemberListWindow());
            OpenClubManagementCommand = new RelayCommand(() => _navigationService.OpenClubManagementWindow());
            OpenEventManagementCommand = new RelayCommand(() => _navigationService.OpenEventManagementWindow());
            OpenReportsCommand = new RelayCommand(() => _navigationService.OpenReportsWindow());
            RefreshDataCommand = new RelayCommand(async () => await LoadDataAsync());
            LogoutCommand = new RelayCommand(Logout);
            DismissNotificationCommand = new RelayCommand(DismissNotification);
        }

        private void InitializeChildViewModels()
        {
            DashboardViewModel = new DashboardViewModel(_userService, _clubService, _eventService, _reportService);
            MemberListViewModel = new MemberListViewModel(_userService, _clubService, _notificationService, null);
            EventManagementViewModel = new EventManagementViewModel(_eventService, _clubService, _userService);
            ClubManagementViewModel = new ClubManagementViewModel(_clubService, _userService, _eventService);
            ReportsViewModel = new ReportsViewModel(_reportService, _userService, _eventService, _clubService);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);

                var clubs = await _clubService.GetAllClubsAsync();
                Clubs.Clear();
                foreach (var club in clubs)
                    Clubs.Add(club);

                var events = await _eventService.GetAllEventsAsync();
                Events.Clear();
                foreach (var eventItem in events)
                    Events.Add(eventItem);

                var reports = await _reportService.GetAllReportsAsync();
                Reports.Clear();
                foreach (var report in reports)
                    Reports.Add(report);
            }
            catch (Exception ex)
            {
                // Handle error - in a real app, you'd show a message to the user
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var isValid = await _userService.ValidateUserCredentialsAsync(email, password);
                if (isValid)
                {
                    CurrentUser = await _userService.GetUserByEmailAsync(email);
                    await LoadDataAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }

        private void Logout()
        {
            CurrentUser = null;
            CurrentView = "Dashboard";
            Users.Clear();
            Clubs.Clear();
            Events.Clear();
            Reports.Clear();
        }

        public bool CanAccessAdminFeatures => CurrentUser?.Role == UserRole.Admin;
        public bool CanAccessClubManagement => CurrentUser?.Role is UserRole.Admin or UserRole.Chairman or UserRole.ViceChairman;
        public bool CanAccessEventManagement => CurrentUser?.Role is UserRole.Admin or UserRole.Chairman or UserRole.ViceChairman or UserRole.TeamLeader;

        public void ShowNotification(string message)
        {
            NotificationMessage = message;
            HasNotifications = true;
        }

        private void DismissNotification()
        {
            HasNotifications = false;
            NotificationMessage = string.Empty;
        }

        public override Task LoadAsync()
        {
            return LoadDataAsync();
        }
    }
}