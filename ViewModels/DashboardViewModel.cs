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
        private readonly INavigationService _navigationService;
        private readonly IAuthorizationService _authorizationService;
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
                                IEventService eventService, IReportService reportService,
                                INavigationService navigationService, IAuthorizationService authorizationService)
        {
            Console.WriteLine("[DashboardViewModel] Initializing DashboardViewModel with services");
            _userService = userService;
            _clubService = clubService;
            _eventService = eventService;
            _reportService = reportService;
            _navigationService = navigationService;
            _authorizationService = authorizationService;
            InitializeCommands();
            LoadCurrentUserAsync();
            Console.WriteLine("[DashboardViewModel] DashboardViewModel initialization completed");
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set
            {
                Console.WriteLine($"[DashboardViewModel] Setting TotalUsers to: {value}");
                SetProperty(ref _totalUsers, value);
            }
        }

        public int TotalClubs
        {
            get => _totalClubs;
            set
            {
                Console.WriteLine($"[DashboardViewModel] Setting TotalClubs to: {value}");
                SetProperty(ref _totalClubs, value);
            }
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

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<Event> RecentEvents
        {
            get => _recentEvents;
            set => SetProperty(ref _recentEvents, value);
        }

        // Current User
        private User? _currentUser;
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(CanCreateUsers));
                    OnPropertyChanged(nameof(CanCreateClubs));
                    OnPropertyChanged(nameof(CanCreateEvents));
                    OnPropertyChanged(nameof(CanGenerateReports));
                    OnPropertyChanged(nameof(CanAccessUserManagement));
                    OnPropertyChanged(nameof(CanAccessClubManagement));
                    OnPropertyChanged(nameof(CanAccessEventManagement));
                    OnPropertyChanged(nameof(CanAccessReports));
                }
            }
        }

        // Authorization Properties
        public bool CanCreateUsers => CurrentUser != null && _authorizationService.CanCreateUsers(CurrentUser.Role);
        public bool CanCreateClubs => CurrentUser != null && _authorizationService.CanCreateClubs(CurrentUser.Role);
        public bool CanCreateEvents => CurrentUser != null && _authorizationService.CanCreateEvents(CurrentUser.Role);
        public bool CanGenerateReports => CurrentUser != null && _authorizationService.CanGenerateReports(CurrentUser.Role);
        public bool CanAccessUserManagement => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "UserManagement");
        public bool CanAccessClubManagement => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "ClubManagement");
        public bool CanAccessEventManagement => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "EventManagement");
        public bool CanAccessReports => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "Reports");

        // Commands
        public ICommand AddUserCommand { get; private set; } = null!;
        public ICommand CreateEventCommand { get; private set; } = null!;
        public ICommand AddClubCommand { get; private set; } = null!;
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand ViewAllUsersCommand { get; private set; } = null!;
        public ICommand ViewAllClubsCommand { get; private set; } = null!;
        public ICommand ViewAllEventsCommand { get; private set; } = null!;
        public ICommand ViewAllReportsCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        // Enhanced Quick Actions Commands
        public ICommand QuickSearchCommand { get; private set; } = null!;
        public ICommand ExportDataCommand { get; private set; } = null!;
        public ICommand BackupDataCommand { get; private set; } = null!;
        public ICommand ViewNotificationsCommand { get; private set; } = null!;
        public ICommand SettingsCommand { get; private set; } = null!;
        public ICommand ViewUpcomingEventsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddUserCommand = new RelayCommand(AddUser, _ => CanCreateUsers);
            CreateEventCommand = new RelayCommand(CreateEvent, _ => CanCreateEvents);
            AddClubCommand = new RelayCommand(AddClub, _ => CanCreateClubs);
            GenerateReportCommand = new RelayCommand(GenerateReport, _ => CanGenerateReports);
            ViewAllUsersCommand = new RelayCommand(ViewAllUsers, _ => CanAccessUserManagement);
            ViewAllClubsCommand = new RelayCommand(ViewAllClubs, _ => CanAccessClubManagement);
            ViewAllEventsCommand = new RelayCommand(ViewAllEvents, _ => CanAccessEventManagement);
            ViewAllReportsCommand = new RelayCommand(ViewAllReports, _ => CanAccessReports);
            RefreshCommand = new RelayCommand(async () =>
            {
                Console.WriteLine("[DashboardViewModel] RefreshCommand executed - starting refresh");
                await LoadDashboardDataAsync();
                Console.WriteLine("[DashboardViewModel] RefreshCommand completed");
            });

            // Enhanced Quick Actions Commands
            QuickSearchCommand = new RelayCommand(QuickSearch);
            ExportDataCommand = new RelayCommand(ExportData, _ => CanGenerateReports);
            BackupDataCommand = new RelayCommand(BackupData, _ => CurrentUser?.Role == UserRole.SystemAdmin);
            ViewNotificationsCommand = new RelayCommand(ViewNotifications);
            SettingsCommand = new RelayCommand(OpenSettings);
            ViewUpcomingEventsCommand = new RelayCommand(ViewUpcomingEvents, _ => CanAccessEventManagement);
        }

        // Load current user method
        public async Task LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[DashboardViewModel] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.Role})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error loading current user: {ex.Message}");
                CurrentUser = null;
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                Console.WriteLine("[DashboardViewModel] Starting to load dashboard data");
                IsLoading = true;

                // Load statistics efficiently using count methods
                var canViewUser = _authorizationService.CanViewUser(CurrentUser!.Role);
                var canViewClub = _authorizationService.CanViewClub(CurrentUser!.Role);
                var canViewEvent = _authorizationService.CanViewEvent(CurrentUser!.Role);
                var canViewReport = _authorizationService.CanViewReports(CurrentUser!.Role, default, default);

                TotalUsers = canViewUser ? await _userService.GetTotalUsersCountAsync() : 0;
                TotalClubs = canViewClub ? await _clubService.GetTotalClubsCountAsync() : 0;
                TotalEvents = canViewEvent ? await _eventService.GetTotalEventsCountAsync() : 0;
                TotalReports = canViewReport ? await _reportService.GetTotalReportsCountAsync() : 0;
                Console.WriteLine($"[DashboardViewModel] Retrieved counts - Users: {TotalUsers}, Clubs: {TotalClubs}, Events: {TotalEvents}, Reports: {TotalReports}");

                // Calculate event statistics efficiently
                ActiveEvents = await _eventService.GetActiveEventsCountAsync();
                UpcomingEvents = await _eventService.GetUpcomingEventsCountAsync();
                Console.WriteLine($"[DashboardViewModel] Event statistics - Active: {ActiveEvents}, Upcoming: {UpcomingEvents}");

                // Calculate new members this month efficiently
                NewMembersThisMonth = await _userService.GetNewMembersThisMonthCountAsync();
                Console.WriteLine($"[DashboardViewModel] New members this month: {NewMembersThisMonth}");

                // Load recent events (still need full data for display)
                var allEvents = await _eventService.GetAllEventsAsync();
                var recentEventsList = allEvents
                    .OrderByDescending(e => e.CreatedDate)
                    .Take(5)
                    .ToList();

                RecentEvents.Clear();
                foreach (var eventItem in recentEventsList)
                {
                    RecentEvents.Add(eventItem);
                }
                Console.WriteLine($"[DashboardViewModel] Loaded {RecentEvents.Count} recent events");
                Console.WriteLine("[DashboardViewModel] Dashboard data loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error loading dashboard data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddUser(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Add User command executed from Dashboard");
            try
            {
                var addUserDialog = new Views.AddUserDialog(_userService);
                var result = addUserDialog.ShowDialog();
                if (result == true)
                {
                    Console.WriteLine("[DashboardViewModel] User created successfully");
                    _navigationService.ShowNotification("Tạo tài khoản người dùng thành công!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error opening Add User dialog: {ex.Message}");
                _navigationService.ShowNotification("Không thể mở cửa sổ tạo tài khoản người dùng");
            }
        }

        private void CreateEvent(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Create Event command executed from Dashboard");
            try
            {
                _navigationService.OpenEventManagementWindow();
                Console.WriteLine("[DashboardViewModel] Successfully opened Event Management window");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error opening Event Management window: {ex.Message}");
                _navigationService.ShowNotification("Không thể mở cửa sổ quản lý sự kiện");
            }
        }

        private void AddClub(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Add Club command executed from Dashboard");
            try
            {
                _navigationService.OpenClubManagementWindow();
                Console.WriteLine("[DashboardViewModel] Successfully opened Club Management window");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error opening Club Management window: {ex.Message}");
                _navigationService.ShowNotification("Không thể mở cửa sổ quản lý câu lạc bộ");
            }
        }

        private void GenerateReport(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Generate Report command executed from Dashboard");
            try
            {
                _navigationService.OpenReportsWindow();
                Console.WriteLine("[DashboardViewModel] Successfully opened Reports window");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error opening Reports window: {ex.Message}");
                _navigationService.ShowNotification("Không thể mở cửa sổ báo cáo");
            }
        }

        private void ViewAllUsers(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] View All Users command executed from Dashboard");
            // Logic to navigate to users view
            System.Diagnostics.Debug.WriteLine("View All Users clicked from Dashboard");
        }

        private void ViewAllClubs(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] View All Clubs command executed from Dashboard");
            // Logic to navigate to clubs view
            System.Diagnostics.Debug.WriteLine("View All Clubs clicked from Dashboard");
        }

        private void ViewAllEvents(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] View All Events command executed from Dashboard");
            // Logic to navigate to events view
            System.Diagnostics.Debug.WriteLine("View All Events clicked from Dashboard");
        }

        private void ViewAllReports(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] View All Reports command executed from Dashboard");
            // Logic to navigate to reports view
            System.Diagnostics.Debug.WriteLine("View All Reports clicked from Dashboard");
        }

        // Enhanced Quick Actions Methods
        private void QuickSearch(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Quick Search command executed from Dashboard");
            try
            {
                _navigationService.ShowNotification("Tính năng tìm kiếm nhanh sẽ được triển khai sớm!");
                // TODO: Implement global search functionality
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error in Quick Search: {ex.Message}");
                _navigationService.ShowNotification("Lỗi khi thực hiện tìm kiếm");
            }
        }

        private void ExportData(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Export Data command executed from Dashboard");
            try
            {
                _navigationService.ShowNotification("Đang xuất dữ liệu... Tính năng sẽ được hoàn thiện sớm!");
                // TODO: Implement data export functionality
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error in Export Data: {ex.Message}");
                _navigationService.ShowNotification("Lỗi khi xuất dữ liệu");
            }
        }

        private void BackupData(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Backup Data command executed from Dashboard");
            try
            {
                _navigationService.ShowNotification("Đang sao lưu dữ liệu... Tính năng sẽ được hoàn thiện sớm!");
                // TODO: Implement backup functionality
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error in Backup Data: {ex.Message}");
                _navigationService.ShowNotification("Lỗi khi sao lưu dữ liệu");
            }
        }

        private void ViewNotifications(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] View Notifications command executed from Dashboard");
            try
            {
                _navigationService.ShowNotification("Hiển thị thông báo... Tính năng sẽ được hoàn thiện sớm!");
                // TODO: Implement notifications view
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error in View Notifications: {ex.Message}");
                _navigationService.ShowNotification("Lỗi khi hiển thị thông báo");
            }
        }

        private void OpenSettings(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] Open Settings command executed from Dashboard");
            try
            {
                _navigationService.ShowNotification("Mở cài đặt... Tính năng sẽ được hoàn thiện sớm!");
                // TODO: Implement settings window
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error in Open Settings: {ex.Message}");
                _navigationService.ShowNotification("Lỗi khi mở cài đặt");
            }
        }

        private void ViewUpcomingEvents(object? parameter)
        {
            Console.WriteLine("[DashboardViewModel] View Upcoming Events command executed from Dashboard");
            try
            {
                _navigationService.OpenEventManagementWindow();
                Console.WriteLine("[DashboardViewModel] Successfully opened Event Management window for upcoming events");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardViewModel] Error opening Event Management window: {ex.Message}");
                _navigationService.ShowNotification("Không thể mở cửa sổ sự kiện sắp tới");
            }
        }

        public override Task LoadAsync()
        {
            return LoadDashboardDataAsync();
        }
    }
}
