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
        private ObservableCollection<Event> _reportFilteredEvents = new();
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

        public ObservableCollection<Event> FilteredEvents
        {
            get => _reportFilteredEvents;
            set => SetProperty(ref _reportFilteredEvents, value);
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

        // Report Generation Commands
        public ICommand GenerateEventStatisticsReportCommand { get; private set; } = null!;
        public ICommand GenerateEventAttendanceReportCommand { get; private set; } = null!;
        public ICommand GenerateEventPerformanceReportCommand { get; private set; } = null!;
        public ICommand GenerateEventSummaryReportCommand { get; private set; } = null!;

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

            // Initialize Report Generation Commands
            GenerateEventStatisticsReportCommand = new RelayCommand(GenerateEventStatisticsReport, (_) => CanExportReports);
            GenerateEventAttendanceReportCommand = new RelayCommand(GenerateEventAttendanceReport, (_) => CanExportReports);
            GenerateEventPerformanceReportCommand = new RelayCommand(GenerateEventPerformanceReport, (_) => CanExportReports);
            GenerateEventSummaryReportCommand = new RelayCommand(GenerateEventSummaryReport, (_) => CanExportReports);
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
                    Console.WriteLine($"[MAIN_LOGIN] User set: {CurrentUser?.FullName} ({CurrentUser?.Role})");

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
        public bool CanAccessUserManagement => CurrentUser?.Role != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "UserManagement");
        public bool CanCreateUsers => CurrentUser?.Role != null && _authorizationService.CanCreateUsers(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanEditUsers => CurrentUser?.Role != null && _authorizationService.CanEditUsers(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanDeleteUsers => CurrentUser?.Role != null && _authorizationService.CanDeleteUsers(CurrentUser.Role);
        public bool CanAssignRoles => CurrentUser?.Role != null && _authorizationService.CanAssignRoles(CurrentUser.Role, CurrentUser.ClubID);

        // Club Management Permissions
        public bool CanAccessClubManagement => CurrentUser?.Role != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "ClubManagement");
        public bool CanCreateClubs => CurrentUser?.Role != null && _authorizationService.CanCreateClubs(CurrentUser.Role);
        public bool CanEditClubs => CurrentUser?.Role != null && _authorizationService.CanEditClubs(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanDeleteClubs => CurrentUser?.Role != null && _authorizationService.CanDeleteClubs(CurrentUser.Role);

        // Event Management Permissions
        public bool CanAccessEventManagement => CurrentUser?.Role != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "EventManagement");
        public bool CanCreateEvents => CurrentUser?.Role != null && _authorizationService.CanCreateEvents(CurrentUser.Role);
        public bool CanEditEvents => CurrentUser?.Role != null && _authorizationService.CanEditEvents(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanDeleteEvents => CurrentUser?.Role != null && _authorizationService.CanDeleteEvents(CurrentUser.Role, CurrentUser.ClubID);
        public bool CanRegisterForEvents => CurrentUser?.Role != null && _authorizationService.CanRegisterForEvents(CurrentUser.Role);

        // Reporting Permissions
        public bool CanAccessReports => CurrentUser?.Role != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "ReportView");
        public bool CanGenerateReports => CurrentUser?.Role != null && _authorizationService.CanGenerateReports(CurrentUser.Role);
        public bool CanExportReports => CurrentUser?.Role != null && _authorizationService.CanExportReports(CurrentUser.Role);

        // System Settings Permissions
        public bool CanAccessGlobalSettings => CurrentUser?.Role != null && _authorizationService.CanAccessGlobalSettings(CurrentUser.Role);
        public bool CanAccessClubSettings => CurrentUser?.Role != null && _authorizationService.CanAccessClubSettings(CurrentUser.Role, CurrentUser.ClubID);

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
