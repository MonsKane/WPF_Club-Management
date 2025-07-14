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
        private readonly IAuthorizationService _authorizationService;

        private User? _currentUser;
        private string _currentView = "Dashboard";
        private ObservableCollection<User> _users = new();
        private ObservableCollection<Club> _clubs = new();
        private ObservableCollection<Event> _events = new();
        private ObservableCollection<Report> _reports = new();
        private bool _hasNotifications;
        private string _notificationMessage = string.Empty;

        // Child ViewModels
        private DashboardViewModel _dashboardViewModel = null!;
        public DashboardViewModel DashboardViewModel
        {
            get => _dashboardViewModel;
            private set
            {
                Console.WriteLine($"[MainViewModel] Setting DashboardViewModel: {value != null}");
                SetProperty(ref _dashboardViewModel, value!);
            }
        }
        public MemberListViewModel? MemberListViewModel { get; private set; }
        public UserManagementViewModel? UserManagementViewModel { get; private set; }
        public EventManagementViewModel? EventManagementViewModel { get; private set; }
        public ClubManagementViewModel? ClubManagementViewModel { get; private set; }
        public ReportsViewModel? ReportsViewModel { get; private set; }

        public MainViewModel(IUserService userService, IClubService clubService,
                           IEventService eventService, IReportService reportService,
                           INavigationService navigationService, INotificationService notificationService, IAuthorizationService authorizationService)
        {
            Console.WriteLine("[MainViewModel] Initializing MainViewModel with services");
            _userService = userService;
            _clubService = clubService;
            _eventService = eventService;
            _reportService = reportService;
            _navigationService = navigationService;
            _notificationService = notificationService;
            _authorizationService = authorizationService;

            InitializeCommands();
            InitializeChildViewModels();
            Console.WriteLine("[MainViewModel] MainViewModel initialization completed");
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    // Notify UI that access control properties have changed
                    OnPropertyChanged(nameof(CanAccessAdminFeatures));
                    OnPropertyChanged(nameof(CanAccessUserManagement));
                    OnPropertyChanged(nameof(CanAccessClubManagement));
                    OnPropertyChanged(nameof(CanAccessEventManagement));
                    OnPropertyChanged(nameof(CanAccessReports));
                    OnPropertyChanged(nameof(CanAccessMemberManagement));
                    OnPropertyChanged(nameof(CanCreateUsers));
                    OnPropertyChanged(nameof(CanEditUsers));
                    OnPropertyChanged(nameof(CanDeleteUsers));
                    OnPropertyChanged(nameof(CanAssignRoles));
                    OnPropertyChanged(nameof(CanCreateClubs));
                    OnPropertyChanged(nameof(CanEditClubs));
                    OnPropertyChanged(nameof(CanDeleteClubs));
                    OnPropertyChanged(nameof(CanCreateEvents));
                    OnPropertyChanged(nameof(CanEditEvents));
                    OnPropertyChanged(nameof(CanDeleteEvents));
                    OnPropertyChanged(nameof(CanRegisterForEvents));
                    OnPropertyChanged(nameof(CanGenerateReports));
                    OnPropertyChanged(nameof(CanExportReports));
                    OnPropertyChanged(nameof(CanAccessGlobalSettings));
                    OnPropertyChanged(nameof(CanAccessClubSettings));
                }
            }
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
        public ICommand OpenUserManagementCommand { get; private set; } = null!;
        public ICommand OpenClubManagementCommand { get; private set; } = null!;
        public ICommand OpenEventManagementCommand { get; private set; } = null!;
        public ICommand OpenReportsCommand { get; private set; } = null!;
        public ICommand RefreshDataCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand DismissNotificationCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NavigateToDashboardCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[NAVIGATION] Navigating to Dashboard");
                CurrentView = "Dashboard";
                // Load dashboard statistics when navigating to dashboard
                if (DashboardViewModel != null)
                {
                    await DashboardViewModel!.LoadAsync();
                }
                Console.WriteLine("[NAVIGATION] Dashboard statistics refreshed");
            });
            OpenMemberListCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[NAVIGATION] Navigating to Member List");
                CurrentView = "Members";
                // Load member data when navigating to member list
                if (MemberListViewModel != null)
                {
                    await MemberListViewModel.LoadAsync();
                }
                Console.WriteLine("[NAVIGATION] Member list data refreshed");
            });
            OpenUserManagementCommand = new RelayCommand(() =>
            {
                Console.WriteLine("[NAVIGATION] Opening User Management Window");
                CurrentView = "UserManagement";
            });
            OpenClubManagementCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[NAVIGATION] Navigating to Club Management");
                CurrentView = "Clubs";
                // Load club data when navigating to club management
                if (ClubManagementViewModel != null)
                {
                    await ClubManagementViewModel.LoadAsync();
                }
                Console.WriteLine("[NAVIGATION] Club management data refreshed");
            });
            OpenEventManagementCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[NAVIGATION] Navigating to Event Management");
                CurrentView = "Events";
                // Load event data when navigating to event management
                if (EventManagementViewModel != null)
                {
                    await EventManagementViewModel.LoadAsync();
                }
                Console.WriteLine("[NAVIGATION] Event management data refreshed");
            });
            OpenReportsCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[NAVIGATION] Navigating to Reports");
                CurrentView = "Reports";
                // Load report data when navigating to reports
                if (ReportsViewModel != null)
                {
                    await ReportsViewModel.LoadAsync();
                }
                Console.WriteLine("[NAVIGATION] Reports data refreshed");
            });
            RefreshDataCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[DATA] Refreshing all data...");
                await LoadDataAsync();
            });
            LogoutCommand = new RelayCommand(Logout);
            DismissNotificationCommand = new RelayCommand(DismissNotification);
        }

        private void InitializeChildViewModels()
        {
            Console.WriteLine("[MainViewModel] Initializing child ViewModels");
            DashboardViewModel = new DashboardViewModel(_userService, _clubService, _eventService, _reportService, _navigationService, _authorizationService);
            Console.WriteLine($"[MainViewModel] DashboardViewModel created: {DashboardViewModel != null}");
            Console.WriteLine($"[MainViewModel] DashboardViewModel.RefreshCommand created: {DashboardViewModel!.RefreshCommand != null}");
            MemberListViewModel = new MemberListViewModel(_userService, _clubService, _notificationService, _authorizationService);
            UserManagementViewModel = new UserManagementViewModel(_userService, _notificationService, _navigationService, _authorizationService);
            EventManagementViewModel = new EventManagementViewModel(_eventService, _clubService, _userService, _authorizationService);
            ClubManagementViewModel = new ClubManagementViewModel(_clubService, _userService, _eventService, _navigationService, _authorizationService);
            ReportsViewModel = new ReportsViewModel(_reportService, _userService, _eventService, _clubService, _authorizationService);
            Console.WriteLine("[MainViewModel] Child ViewModels initialized successfully");
        }

        private async Task LoadDataAsync()
        {
            Console.WriteLine("[DATA] Starting data load operation...");
            try
            {
                Console.WriteLine("[DATA] Loading users...");
                var users = await _userService.GetAllUsersAsync();
                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);
                Console.WriteLine($"[DATA] Loaded {users.Count()} users");

                Console.WriteLine("[DATA] Loading clubs...");
                var clubs = await _clubService.GetAllClubsAsync();
                Clubs.Clear();
                foreach (var club in clubs)
                    Clubs.Add(club);
                Console.WriteLine($"[DATA] Loaded {clubs.Count()} clubs");

                Console.WriteLine("[DATA] Loading events...");
                var events = await _eventService.GetAllEventsAsync();
                Events.Clear();
                foreach (var eventItem in events)
                    Events.Add(eventItem);
                Console.WriteLine($"[DATA] Loaded {events.Count()} events");

                Console.WriteLine("[DATA] Loading reports...");
                var reports = await _reportService.GetAllReportsAsync();
                Reports.Clear();
                foreach (var report in reports)
                    Reports.Add(report);
                Console.WriteLine($"[DATA] Loaded {reports.Count()} reports");

                // Load dashboard statistics
                Console.WriteLine("[DATA] Loading dashboard statistics...");
                if (DashboardViewModel != null)
                {
                    await DashboardViewModel.LoadAsync();
                }
                Console.WriteLine("[DATA] Dashboard statistics loaded");

                Console.WriteLine("[DATA] All data loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DATA] ERROR loading data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            Console.WriteLine($"[MAIN_LOGIN] Attempting login for: {email}");
            try
            {
                var isValid = await _userService.ValidateUserCredentialsAsync(email, password);
                if (isValid)
                {
                    Console.WriteLine("[MAIN_LOGIN] Credentials validated, getting user details...");
                    CurrentUser = await _userService.GetUserByEmailAsync(email);
                    Console.WriteLine($"[MAIN_LOGIN] User set: {CurrentUser?.FullName} ({CurrentUser?.SystemRole})");

                    Console.WriteLine("[MAIN_LOGIN] Loading initial data...");
                    await LoadDataAsync();
                    Console.WriteLine("[MAIN_LOGIN] Login process completed successfully");
                    return true;
                }
                Console.WriteLine("[MAIN_LOGIN] Invalid credentials");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN_LOGIN] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }

        private void Logout()
        {
            Console.WriteLine($"[LOGOUT] User {CurrentUser?.FullName} logging out...");

            // Clear user session and all cached data
            CurrentUser = null;
            CurrentView = "Dashboard";
            Users.Clear();
            Clubs.Clear();
            Events.Clear();
            Reports.Clear();

            // Clear notifications
            HasNotifications = false;
            NotificationMessage = string.Empty;

            Console.WriteLine("[LOGOUT] Logout completed, all data and session cleared");

            // Navigate back to login window
            _navigationService.NavigateToLogin();
        }

        // User Management Permissions
        public bool CanAccessUserManagement => CurrentUser?.SystemRole != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "UserManagement");
        public bool CanCreateUsers => CurrentUser?.SystemRole != null && _authorizationService.CanCreateUsers(CurrentUser.SystemRole, null);
        public bool CanEditUsers => CurrentUser?.SystemRole != null && _authorizationService.CanEditUsers(CurrentUser.SystemRole, null);
        public bool CanDeleteUsers => CurrentUser?.SystemRole != null && _authorizationService.CanDeleteUsers(CurrentUser.SystemRole);
        public bool CanAssignRoles => CurrentUser?.SystemRole != null && _authorizationService.CanAssignRoles(CurrentUser.SystemRole, null);

        // Club Management Permissions
        public bool CanAccessClubManagement => CurrentUser?.SystemRole != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "ClubManagement");
        public bool CanJoinEvents => CurrentUser?.SystemRole != null && _authorizationService.CanJoinEvents(CurrentUser.SystemRole);
        public bool CanCreateClubs => CurrentUser?.SystemRole != null && _authorizationService.CanCreateClubs(CurrentUser.SystemRole);
        public bool CanEditClubs => CurrentUser?.SystemRole != null && _authorizationService.CanEditClubs(CurrentUser.SystemRole, null);
        public bool CanDeleteClubs => CurrentUser?.SystemRole != null && _authorizationService.CanDeleteClubs(CurrentUser.SystemRole);

        // Event Management Permissions
        public bool CanAccessEventManagement => CurrentUser?.SystemRole != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "EventManagement");
        public bool CanCreateEvents => CurrentUser?.SystemRole != null && _authorizationService.CanCreateEvents(CurrentUser.SystemRole);
        public bool CanEditEvents => CurrentUser?.SystemRole != null && _authorizationService.CanEditEvents(CurrentUser.SystemRole, null);
        public bool CanDeleteEvents => CurrentUser?.SystemRole != null && _authorizationService.CanDeleteEvents(CurrentUser.SystemRole, null);
        public bool CanRegisterForEvents => CurrentUser?.SystemRole != null && _authorizationService.CanRegisterForEvents(CurrentUser.SystemRole);

        // System Settings Permissions
        public bool CanAccessGlobalSettings => CurrentUser?.SystemRole != null && _authorizationService.CanAccessGlobalSettings(CurrentUser.SystemRole);
        public bool CanAccessClubSettings => CurrentUser?.SystemRole != null && _authorizationService.CanAccessClubSettings(CurrentUser.SystemRole, null);

        // Reporting Permissions
        public bool CanAccessReports => CurrentUser?.SystemRole != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "ReportView");
        public bool CanGenerateReports => CurrentUser?.SystemRole != null && _authorizationService.CanGenerateReports(CurrentUser.SystemRole);
        public bool CanExportReports => CurrentUser?.SystemRole != null && _authorizationService.CanExportReports(CurrentUser.SystemRole);

        // Legacy properties for backward compatibility
        public bool CanAccessAdminFeatures => CanAccessUserManagement || CanAccessGlobalSettings;
        public bool CanAccessMemberManagement => CanAccessUserManagement;

        public void ShowNotification(string message)
        {
            Console.WriteLine($"[MainViewModel] Showing notification: {message}");
            NotificationMessage = message;
            HasNotifications = true;
        }

        private void DismissNotification()
        {
            Console.WriteLine("[MainViewModel] Dismissing notification");
            HasNotifications = false;
            NotificationMessage = string.Empty;
        }

        public override Task LoadAsync()
        {
            return LoadDataAsync();
        }
    }
}
