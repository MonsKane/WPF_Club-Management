using ClubManagementApp.Commands;
using ClubManagementApp.DTOs;
using ClubManagementApp.Helpers;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Web;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;
        private readonly IClubService _clubService;
        private readonly IAuthorizationService _authorizationService;
        private ObservableCollection<Report> _reports = new();
        private ObservableCollection<Report> _filteredReports = new();
        private string _searchText = string.Empty;
        private string _selectedReportType = "All Types";
        private DateTime? _selectedDate;
        private Report? _selectedReport;
        private int _todayReportsCount;
        private int _monthReportsCount;
        private bool _hasNoReports;
        private User? _currentUser;

        public ReportsViewModel(IReportService reportService, IUserService userService,
                              IEventService eventService, IClubService clubService, IAuthorizationService authorizationService)
        {
            Console.WriteLine("[ReportsViewModel] Initializing ReportsViewModel with services");
            _reportService = reportService;
            _userService = userService;
            _eventService = eventService;
            _clubService = clubService;
            _authorizationService = authorizationService;
            InitializeCommands();
            LoadCurrentUserAsync();
            Console.WriteLine("[ReportsViewModel] ReportsViewModel initialization completed");
        }

        private void UpdateReportStatistics()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            
            TodayReportsCount = Reports.Count(r => r.GeneratedDate.Date == today);
            MonthReportsCount = Reports.Count(r => r.GeneratedDate >= startOfMonth);
            
            Console.WriteLine($"[ReportsViewModel] Statistics updated - Today: {TodayReportsCount}, This Month: {MonthReportsCount}");
        }

        public ObservableCollection<Report> Reports
        {
            get => _reports;
            set => SetProperty(ref _reports, value);
        }

        public ObservableCollection<Report> FilteredReports
        {
            get => _filteredReports;
            set => SetProperty(ref _filteredReports, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterReports();
                }
            }
        }

        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                if (SetProperty(ref _selectedReportType, value))
                {
                    Console.WriteLine($"[ReportsViewModel] Selected report type changed to: {value}");
                    FilterReports();
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
                    FilterReports();
                }
            }
        }

        // IsLoading property is inherited from BaseViewModel

        public Report? SelectedReport
        {
            get => _selectedReport;
            set => SetProperty(ref _selectedReport, value);
        }

        public int TodayReportsCount
        {
            get => _todayReportsCount;
            set => SetProperty(ref _todayReportsCount, value);
        }

        public int MonthReportsCount
        {
            get => _monthReportsCount;
            set => SetProperty(ref _monthReportsCount, value);
        }

        public bool HasNoReports
        {
            get => _hasNoReports;
            set => SetProperty(ref _hasNoReports, value);
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        // Reporting Permissions
        public bool CanAccessReports
        {
            get => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "ReportView");
        }

        public bool CanGenerateReports
        {
            get => CurrentUser != null && _authorizationService.CanGenerateReports(CurrentUser.Role);
        }

        public bool CanExportReports
        {
            get => CurrentUser != null && _authorizationService.CanExportReports(CurrentUser.Role);
        }

        // Commands
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand GenerateMembershipReportCommand { get; private set; } = null!;
        public ICommand GenerateEventReportCommand { get; private set; } = null!;
        public ICommand GenerateFinancialReportCommand { get; private set; } = null!;
        public ICommand GenerateActivityReportCommand { get; private set; } = null!;
        public ICommand ViewReportCommand { get; private set; } = null!;
        public ICommand DownloadReportCommand { get; private set; } = null!;
        public ICommand EmailReportCommand { get; private set; } = null!;
        public ICommand UpdateReportCommand { get; private set; } = null!;
        public ICommand DeleteReportCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            GenerateReportCommand = new RelayCommand(GenerateReport, CanExecuteGenerateReport);
            GenerateMembershipReportCommand = new RelayCommand(async _ => await GenerateMembershipReportAsync(), CanExecuteGenerateReport);
            GenerateEventReportCommand = new RelayCommand(async _ => await GenerateEventReportAsync(), CanExecuteGenerateReport);
            GenerateFinancialReportCommand = new RelayCommand(async _ => await GenerateFinancialReportAsync(), CanExecuteGenerateReport);
            GenerateActivityReportCommand = new RelayCommand(async _ => await GenerateActivityReportAsync(), CanExecuteGenerateReport);
            ViewReportCommand = new RelayCommand<Report>(ViewReport, CanExecuteViewReport);
            DownloadReportCommand = new RelayCommand<Report>(DownloadReport, CanExecuteDownloadReport);
            EmailReportCommand = new RelayCommand<Report>(EmailReport, CanExecuteEmailReport);
            UpdateReportCommand = new RelayCommand<Report>(UpdateReport, CanExecuteUpdateReport);
            DeleteReportCommand = new RelayCommand<Report>(DeleteReport, CanExecuteDeleteReport);
            RefreshCommand = new RelayCommand(async _ => await LoadReportsAsync());
        }

        private async void LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[ReportsViewModel] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.Role})");
                OnPropertyChanged(nameof(CanAccessReports));
                OnPropertyChanged(nameof(CanGenerateReports));
                OnPropertyChanged(nameof(CanExportReports));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error loading current user: {ex.Message}");
            }
        }

        private bool CanExecuteGenerateReport(object? parameter)
        {
            return CanGenerateReports;
        }

        private bool CanExecuteViewReport(Report? report)
        {
            var canExecute = report != null && CanAccessReports;
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] CanExecuteViewReport: {canExecute} (report: {report?.Title}, CanAccessReports: {CanAccessReports})\n");
            return canExecute;
        }

        private bool CanExecuteDownloadReport(Report? report)
        {
            var canExecute = report != null && CanExportReports;
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] CanExecuteDownloadReport: {canExecute} (report: {report?.Title}, CanExportReports: {CanExportReports})\n");
            return canExecute;
        }

        private bool CanExecuteEmailReport(Report? report)
        {
            var canExecute = report != null && CanExportReports;
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] CanExecuteEmailReport: {canExecute} (report: {report?.Title}, CanExportReports: {CanExportReports})\n");
            return canExecute;
        }

        private bool CanExecuteUpdateReport(Report? report)
        {
            var canExecute = report != null && CanGenerateReports;
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] CanExecuteUpdateReport: {canExecute} (report: {report?.Title}, CanGenerateReports: {CanGenerateReports})\n");
            return canExecute;
        }

        private bool CanExecuteDeleteReport(Report? report)
        {
            var canExecute = report != null && CanGenerateReports;
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] CanExecuteDeleteReport: {canExecute} (report: {report?.Title}, CanGenerateReports: {CanGenerateReports})\n");
            return canExecute;
        }

        private async Task LoadReportsAsync()
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting to load reports");
                IsLoading = true;
                var reports = await _reportService.GetAllReportsAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {reports.Count()} reports from service");

                Reports.Clear();
                foreach (var report in reports.OrderByDescending(r => r.GeneratedDate))
                {
                    Reports.Add(report);
                }

                FilterReports();
                Console.WriteLine("[ReportsViewModel] Reports loaded and filtered successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error loading reports: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading reports: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterReports()
        {
            Console.WriteLine($"[ReportsViewModel] Filtering reports - Search: '{SearchText}', Type: {SelectedReportType}, Date: {SelectedDate}");
            var filtered = Reports.AsEnumerable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(r =>
                    r.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    r.Type.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (r.GeneratedByUser?.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Filter by report type
            if (SelectedReportType != "All Types")
            {
                filtered = filtered.Where(r => r.Type.ToString().Equals(SelectedReportType, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by date
            if (SelectedDate.HasValue)
            {
                var selectedDate = SelectedDate.Value.Date;
                filtered = filtered.Where(r => r.GeneratedDate.Date == selectedDate);
            }

            FilteredReports.Clear();
            foreach (var report in filtered)
            {
                FilteredReports.Add(report);
            }
            Console.WriteLine($"[ReportsViewModel] Filtered to {FilteredReports.Count} reports");
            
            // Update statistics
            UpdateReportStatistics();
            
            // Update HasNoReports property
            HasNoReports = FilteredReports.Count == 0;
        }

        private async void GenerateReport(object? parameter)
        {
            Console.WriteLine("[ReportsViewModel] Generate Report command executed");
            
            try
            {
                var dialog = new Views.CustomReportDialog();
                var result = dialog.ShowDialog();
                
                if (result == true && dialog.ReportParameters != null)
                {
                    Console.WriteLine($"[ReportsViewModel] Custom report dialog completed with parameters: {dialog.ReportParameters.Title}");
                    await GenerateCustomReportAsync(dialog.ReportParameters);
                }
                else
                {
                    Console.WriteLine("[ReportsViewModel] Custom report dialog was cancelled");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error opening custom report dialog: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error opening report generation dialog: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task GenerateMembershipReportAsync()
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting membership report generation");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                var users = await _userService.GetAllUsersAsync();
                var clubs = await _clubService.GetAllClubsAsync();

                // Generate report content
                var reportContent = GenerateMembershipReportContent(users, clubs);

                var report = new Report
                {
                    Title = currentUser.ClubID.HasValue ? 
                        $"Membership Report - {DateTime.Now:yyyy-MM-dd}" : 
                        $"System-wide Membership Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.MemberStatistics,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = currentUser.ClubID, // Use current user's club or null for system-wide
                    GeneratedByUserID = currentUser.UserID
                };

                var createdReport = await _reportService.CreateReportAsync(report);
                Reports.Insert(0, createdReport);
                FilterReports();
                Console.WriteLine($"[ReportsViewModel] Membership report created successfully with ID: {createdReport.ReportID}");

                System.Windows.MessageBox.Show(
                    "Membership report generated successfully!",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating membership report: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error generating membership report: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateEventReportAsync()
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting event report generation");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                var events = await _eventService.GetAllEventsAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events for event report");

                // Generate report content
                var reportContent = GenerateEventReportContent(events);

                var report = new Report
                {
                    Title = currentUser.ClubID.HasValue ? 
                        $"Event Report - {DateTime.Now:yyyy-MM-dd}" : 
                        $"System-wide Event Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.EventOutcomes,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = currentUser.ClubID,
                    GeneratedByUserID = currentUser.UserID
                };

                var createdReport = await _reportService.CreateReportAsync(report);
                Reports.Insert(0, createdReport);
                FilterReports();
                Console.WriteLine($"[ReportsViewModel] Event report created successfully with ID: {createdReport.ReportID}");

                System.Windows.MessageBox.Show(
                    "Event report generated successfully!",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating event report: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error generating event report: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateFinancialReportAsync()
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting financial report generation");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                var events = await _eventService.GetAllEventsAsync();
                var clubs = await _clubService.GetAllClubsAsync();
                
                // Generate financial report content
                var reportContent = GenerateFinancialReportContent(events, clubs);
                Console.WriteLine("[ReportsViewModel] Generated financial report content");

                var report = new Report
                {
                    Title = currentUser.ClubID.HasValue ? 
                        $"Financial Report - {DateTime.Now:yyyy-MM-dd}" : 
                        $"System-wide Financial Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.SemesterSummary,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = currentUser.ClubID,
                    GeneratedByUserID = currentUser.UserID
                };

                var createdReport = await _reportService.CreateReportAsync(report);
                Reports.Insert(0, createdReport);
                FilterReports();
                Console.WriteLine($"[ReportsViewModel] Financial report created successfully with ID: {createdReport.ReportID}");

                System.Windows.MessageBox.Show(
                    "Financial report generated successfully!",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating financial report: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error generating financial report: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateActivityReportAsync()
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting activity report generation");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                var events = await _eventService.GetAllEventsAsync();
                var users = await _userService.GetAllUsersAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events and {users.Count()} users for activity report");

                // Generate activity report content
                var reportContent = GenerateActivityReportContent(events, users);

                var report = new Report
                {
                    Title = currentUser.ClubID.HasValue ? 
                        $"Activity Report - {DateTime.Now:yyyy-MM-dd}" : 
                        $"System-wide Activity Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.ActivityTracking,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = currentUser.ClubID,
                    GeneratedByUserID = currentUser.UserID
                };

                var createdReport = await _reportService.CreateReportAsync(report);
                Reports.Insert(0, createdReport);
                FilterReports();
                Console.WriteLine($"[ReportsViewModel] Activity report created successfully with ID: {createdReport.ReportID}");

                System.Windows.MessageBox.Show(
                    "Activity report generated successfully!",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating activity report: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error generating activity report: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GenerateMembershipReportContent(IEnumerable<User> users, IEnumerable<Club> clubs)
        {
            var currentUser = CurrentUser;
            var isSystemWide = !currentUser?.ClubID.HasValue ?? true;
            
            var content = isSystemWide ? "SYSTEM-WIDE MEMBERSHIP REPORT\n" : "MEMBERSHIP REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Generated by: {currentUser?.FullName ?? "System"}\n\n";
            
            if (isSystemWide)
            {
                content += $"Total Users: {users.Count()}\n";
                content += $"Total Clubs: {clubs.Count()}\n\n";

                content += "USERS BY ROLE:\n";
                var usersByRole = users.GroupBy(u => u.Role);
                foreach (var group in usersByRole)
                {
                    content += $"- {group.Key}: {group.Count()}\n";
                }

                content += "\nCLUBS SUMMARY:\n";
                foreach (var club in clubs)
                {
                    var memberCount = users.Count(u => u.ClubID == club.ClubID);
                    content += $"- {club.Name}: {memberCount} members\n";
                }
            }
            else
            {
                // Club-specific report
                var clubUsers = users.Where(u => u.ClubID == currentUser?.ClubID);
                var userClub = clubs.FirstOrDefault(c => c.ClubID == currentUser?.ClubID);
                
                content += $"Club: {userClub?.Name ?? "Unknown"}\n";
                content += $"Total Members: {clubUsers.Count()}\n\n";

                content += "MEMBERS BY ROLE:\n";
                var usersByRole = clubUsers.GroupBy(u => u.Role);
                foreach (var group in usersByRole)
                {
                    content += $"- {group.Key}: {group.Count()}\n";
                }

                content += "\nMEMBERS BY ACTIVITY LEVEL:\n";
                var usersByActivity = clubUsers.GroupBy(u => u.ActivityLevel);
                foreach (var group in usersByActivity)
                {
                    content += $"- {group.Key}: {group.Count()}\n";
                }
            }

            return content;
        }

        private string GenerateEventReportContent(IEnumerable<Event> events)
        {
            var currentUser = CurrentUser;
            var isSystemWide = !currentUser?.ClubID.HasValue ?? true;
            
            var content = isSystemWide ? "SYSTEM-WIDE EVENT REPORT\n" : "EVENT REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Generated by: {currentUser?.FullName ?? "System"}\n\n";
            
            // Filter events based on user's club if not system-wide
            var filteredEvents = isSystemWide ? events : events.Where(e => e.ClubID == currentUser?.ClubID);
            
            content += $"Total Events: {filteredEvents.Count()}\n\n";

            var now = DateTime.Now;
            var upcoming = filteredEvents.Count(e => e.EventDate > now);
            var ongoing = filteredEvents.Count(e => e.EventDate.Date == now.Date);
            var completed = filteredEvents.Count(e => e.EventDate < now);

            content += "EVENTS BY STATUS:\n";
            content += $"- Upcoming: {upcoming}\n";
            content += $"- Ongoing: {ongoing}\n";
            content += $"- Completed: {completed}\n\n";

            content += "RECENT EVENTS:\n";
            foreach (var eventItem in filteredEvents.OrderByDescending(e => e.EventDate).Take(10))
            {
                content += $"- {eventItem.Name} ({eventItem.EventDate:yyyy-MM-dd})\n";
            }

            return content;
        }

        private string GenerateActivityReportContent(IEnumerable<Event> events, IEnumerable<User> users)
        {
            var currentUser = CurrentUser;
            var isSystemWide = !currentUser?.ClubID.HasValue ?? true;
            
            var content = isSystemWide ? "SYSTEM-WIDE ACTIVITY REPORT\n" : "ACTIVITY REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Generated by: {currentUser?.FullName ?? "System"}\n\n";
            
            // Filter data based on user's club if not system-wide
            var filteredEvents = isSystemWide ? events : events.Where(e => e.ClubID == currentUser?.ClubID);
            var filteredUsers = isSystemWide ? users : users.Where(u => u.ClubID == currentUser?.ClubID);

            var recentEvents = filteredEvents.Where(e => e.EventDate >= DateTime.Now.AddDays(-30));
            content += $"Events in Last 30 Days: {recentEvents.Count()}\n";

            var recentUsers = filteredUsers.Where(u => u.JoinDate >= DateTime.Now.AddDays(-30));
            content += $"New Users in Last 30 Days: {recentUsers.Count()}\n\n";

            content += "RECENT ACTIVITY:\n";
            foreach (var eventItem in recentEvents.OrderByDescending(e => e.EventDate).Take(5))
            {
                content += $"- Event: {eventItem.Name} ({eventItem.EventDate:yyyy-MM-dd})\n";
            }
            
            if (recentUsers.Any())
            {
                content += "\nNEW MEMBERS:\n";
                foreach (var user in recentUsers.OrderByDescending(u => u.JoinDate).Take(5))
                {
                    content += $"- {user.FullName} joined on {user.JoinDate:yyyy-MM-dd}\n";
                }
            }

            return content;
        }

        private string GenerateFinancialReportContent(IEnumerable<Event> events, IEnumerable<Club> clubs)
        {
            var currentUser = CurrentUser;
            var isSystemWide = !currentUser?.ClubID.HasValue ?? true;
            
            var content = isSystemWide ? "SYSTEM-WIDE FINANCIAL REPORT\n" : "FINANCIAL REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Generated by: {currentUser?.FullName ?? "System"}\n\n";
            
            // Filter data based on user's club if not system-wide
            var filteredEvents = isSystemWide ? events : events.Where(e => e.ClubID == currentUser?.ClubID);
            var filteredClubs = isSystemWide ? clubs : clubs.Where(c => c.ClubID == currentUser?.ClubID);

            // Calculate financial metrics
            var totalEvents = filteredEvents.Count();
            var totalClubs = filteredClubs.Count();
            var estimatedEventCosts = totalEvents * 500; // Placeholder calculation
            var estimatedRevenue = totalEvents * 750; // Placeholder calculation

            content += "FINANCIAL SUMMARY:\n";
            if (isSystemWide)
            {
                content += $"Total Clubs: {totalClubs}\n";
            }
            else
            {
                var userClub = filteredClubs.FirstOrDefault();
                content += $"Club: {userClub?.Name ?? "Unknown"}\n";
            }
            content += $"Total Events: {totalEvents}\n";
            content += $"Estimated Event Costs: ${estimatedEventCosts:N2}\n";
            content += $"Estimated Revenue: ${estimatedRevenue:N2}\n";
            content += $"Net Income: ${(estimatedRevenue - estimatedEventCosts):N2}\n\n";

            if (isSystemWide)
            {
                content += "CLUB BREAKDOWN:\n";
                foreach (var club in filteredClubs.OrderBy(c => c.Name))
                {
                    var clubEvents = events.Where(e => e.ClubID == club.ClubID).Count();
                    var clubCosts = clubEvents * 500;
                    var clubRevenue = clubEvents * 750;
                    content += $"- {club.Name}: {clubEvents} events, ${clubRevenue:N2} revenue, ${clubCosts:N2} costs\n";
                }
            }
            else
            {
                content += "EVENT BREAKDOWN:\n";
                foreach (var eventItem in filteredEvents.OrderByDescending(e => e.EventDate).Take(10))
                {
                    var eventCost = 500;
                    var eventRevenue = 750;
                    content += $"- {eventItem.Name} ({eventItem.EventDate:yyyy-MM-dd}): ${eventRevenue:N2} revenue, ${eventCost:N2} cost\n";
                }
            }

            return content;
        }

        private string GetCurrentSemester()
        {
            var now = DateTime.Now;
            var year = now.Year;
            
            // Determine semester based on month
            if (now.Month >= 1 && now.Month <= 5)
            {
                return $"Spring {year}";
            }
            else if (now.Month >= 6 && now.Month <= 8)
            {
                return $"Summer {year}";
            }
            else
            {
                return $"Fall {year}";
            }
        }

        private void ViewReport(Report? report)
        {
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] ViewReport command executed with report: {report?.Title ?? "null"}\n");
            if (report == null) 
            {
                System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] ViewReport: Report is null, returning\n");
                return;
            }

            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] Viewing report: {report.Title} (ID: {report.ReportID})\n");
            
            try
            {
                // Create a simple report viewer window
                var viewerWindow = new System.Windows.Window
                {
                    Title = $"Report Viewer - {report.Title}",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };

                var scrollViewer = new System.Windows.Controls.ScrollViewer
                {
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    Padding = new System.Windows.Thickness(20)
                };

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = report.Content,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };

                scrollViewer.Content = textBlock;
                viewerWindow.Content = scrollViewer;
                viewerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error viewing report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error viewing report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void DownloadReport(Report? report)
        {
            System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] DownloadReport command executed with report: {report?.Title ?? "null"}\n");
            if (report == null) 
            {
                System.IO.File.AppendAllText("debug_log.txt", $"[ReportsViewModel] DownloadReport: Report is null, returning\n");
                return;
            }

            Console.WriteLine($"[ReportsViewModel] Downloading report: {report.Title} (ID: {report.ReportID})");
            
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Report",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"{report.Title}_{DateTime.Now:yyyyMMdd}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllTextAsync(saveFileDialog.FileName, report.Content);
                    Console.WriteLine($"[ReportsViewModel] Report saved to: {saveFileDialog.FileName}");
                    System.Windows.MessageBox.Show($"Report saved successfully to: {saveFileDialog.FileName}", "Download Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error downloading report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error downloading report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void EmailReport(Report? report)
        {
            Console.WriteLine($"[ReportsViewModel] EmailReport command executed with report: {report?.Title ?? "null"}");
            if (report == null) 
            {
                Console.WriteLine($"[ReportsViewModel] EmailReport: Report is null, returning");
                return;
            }

            Console.WriteLine($"[ReportsViewModel] Emailing report: {report.Title} (ID: {report.ReportID})");
            
            try
            {
                // Create a simple email dialog
                var emailDialog = new System.Windows.Window
                {
                    Title = "Email Report",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    ResizeMode = System.Windows.ResizeMode.NoResize
                };

                var grid = new System.Windows.Controls.Grid();
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

                var emailLabel = new System.Windows.Controls.Label { Content = "Email Address:", Margin = new System.Windows.Thickness(10) };
                System.Windows.Controls.Grid.SetRow(emailLabel, 0);
                grid.Children.Add(emailLabel);

                var emailTextBox = new System.Windows.Controls.TextBox { Margin = new System.Windows.Thickness(10, 0, 10, 10) };
                System.Windows.Controls.Grid.SetRow(emailTextBox, 1);
                grid.Children.Add(emailTextBox);

                var buttonPanel = new System.Windows.Controls.StackPanel 
                { 
                    Orientation = System.Windows.Controls.Orientation.Horizontal, 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Margin = new System.Windows.Thickness(10)
                };
                
                var sendButton = new System.Windows.Controls.Button 
                { 
                    Content = "Send", 
                    Width = 75, 
                    Height = 25, 
                    Margin = new System.Windows.Thickness(5, 0, 0, 0)
                };
                
                var cancelButton = new System.Windows.Controls.Button 
                { 
                    Content = "Cancel", 
                    Width = 75, 
                    Height = 25, 
                    Margin = new System.Windows.Thickness(5, 0, 0, 0)
                };

                sendButton.Click += async (s, e) => 
                {
                    var email = emailTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        System.Windows.MessageBox.Show("Please enter an email address.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    // Validate email format
                    if (!IsValidEmail(email))
                    {
                        System.Windows.MessageBox.Show("Please enter a valid email address.", "Invalid Email", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        sendButton.IsEnabled = false;
                        sendButton.Content = "Sending...";

                        // Create HTML email content
                        var subject = $"Report: {report.Title}";
                        var body = $@"
                            <html>
                            <body>
                                <h2>Club Management Report</h2>
                                <p><strong>Report Title:</strong> {report.Title}</p>
                                <p><strong>Report Type:</strong> {report.Type}</p>
                                <p><strong>Generated Date:</strong> {report.GeneratedDate:MMMM dd, yyyy}</p>
                                <p><strong>Semester:</strong> {report.Semester}</p>
                                <hr/>
                                <h3>Report Content:</h3>
                                <pre>{System.Web.HttpUtility.HtmlEncode(report.Content)}</pre>
                            </body>
                            </html>";

                        // Use EmailService to send the email automatically
                        Services.IEmailService? emailService = null;
                        try
                        {
                            emailService = ServiceLocator.GetService<Services.IEmailService>();
                        }
                        catch (Exception serviceEx)
                        {
                            Console.WriteLine($"[ReportsViewModel] ServiceLocator not configured: {serviceEx.Message}");
                        }
                        
                        if (emailService != null)
                        {
                            var success = await emailService.SendEmailAsync(email, subject, body, true);
                            
                            if (success)
                            {
                                Console.WriteLine($"[ReportsViewModel] Report sent successfully to {email}");
                                System.Windows.MessageBox.Show(
                                    $"Report sent successfully to {email}!",
                                    "Email Sent",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Information);
                                emailDialog.Close();
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    "Failed to send email. Please check your email configuration.",
                                    "Send Failed",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            // Fallback to mailto if EmailService is not available
                            var mailtoSubject = System.Uri.EscapeDataString(subject);
                            var mailtoBody = System.Uri.EscapeDataString($"Please find the report content below:\n\n{report.Content}");
                            var mailtoUrl = $"mailto:{email}?subject={mailtoSubject}&body={mailtoBody}";
                            
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = mailtoUrl,
                                UseShellExecute = true
                            });

                            Console.WriteLine($"[ReportsViewModel] Email client opened for report: {report.Title}");
                            System.Windows.MessageBox.Show("Email client opened with report content.", "Email Prepared", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            emailDialog.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ReportsViewModel] Error sending email: {ex.Message}");
                        System.Windows.MessageBox.Show($"Error sending email: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    finally
                    {
                        sendButton.IsEnabled = true;
                        sendButton.Content = "Send";
                    }
                };

                cancelButton.Click += (s, e) => emailDialog.Close();

                buttonPanel.Children.Add(sendButton);
                buttonPanel.Children.Add(cancelButton);
                System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
                grid.Children.Add(buttonPanel);

                emailDialog.Content = grid;
                emailDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error opening email dialog: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening email dialog: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateReport(Report? report)
        {
            Console.WriteLine($"[ReportsViewModel] UpdateReport command executed with report: {report?.Title ?? "null"}");
            if (report == null) 
            {
                Console.WriteLine($"[ReportsViewModel] UpdateReport: Report is null, returning");
                return;
            }

            Console.WriteLine($"[ReportsViewModel] Update report requested: {report.Title} (ID: {report.ReportID})");
            
            try
            {
                // Create a simple update dialog
                var updateDialog = new System.Windows.Window
                {
                    Title = "Update Report",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    ResizeMode = System.Windows.ResizeMode.NoResize
                };

                var grid = new System.Windows.Controls.Grid();
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

                var titleLabel = new System.Windows.Controls.Label { Content = "Report Title:", Margin = new System.Windows.Thickness(10) };
                System.Windows.Controls.Grid.SetRow(titleLabel, 0);
                grid.Children.Add(titleLabel);

                var titleTextBox = new System.Windows.Controls.TextBox { Text = report.Title, Margin = new System.Windows.Thickness(10, 0, 10, 10) };
                System.Windows.Controls.Grid.SetRow(titleTextBox, 1);
                grid.Children.Add(titleTextBox);

                var contentLabel = new System.Windows.Controls.Label { Content = "Report Content:", Margin = new System.Windows.Thickness(10, 0, 10, 5) };
                var contentTextBox = new System.Windows.Controls.TextBox 
                { 
                    Text = report.Content, 
                    Margin = new System.Windows.Thickness(10, 0, 10, 10),
                    AcceptsReturn = true,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
                };

                var contentPanel = new System.Windows.Controls.StackPanel();
                contentPanel.Children.Add(contentLabel);
                contentPanel.Children.Add(contentTextBox);
                System.Windows.Controls.Grid.SetRow(contentPanel, 2);
                grid.Children.Add(contentPanel);

                var buttonPanel = new System.Windows.Controls.StackPanel 
                { 
                    Orientation = System.Windows.Controls.Orientation.Horizontal, 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Margin = new System.Windows.Thickness(10)
                };
                
                var updateButton = new System.Windows.Controls.Button 
                { 
                    Content = "Update", 
                    Width = 75, 
                    Height = 25, 
                    Margin = new System.Windows.Thickness(5, 0, 0, 0)
                };
                
                var cancelButton = new System.Windows.Controls.Button 
                { 
                    Content = "Cancel", 
                    Width = 75, 
                    Height = 25, 
                    Margin = new System.Windows.Thickness(5, 0, 0, 0)
                };

                updateButton.Click += async (s, e) => 
                {
                    if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                    {
                        System.Windows.MessageBox.Show("Please enter a report title.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        report.Title = titleTextBox.Text.Trim();
                        report.Content = contentTextBox.Text;
                        
                        await _reportService.UpdateReportAsync(report);
                        
                        // Update the report in the collection
                        var index = Reports.IndexOf(report);
                        if (index >= 0)
                        {
                            Reports[index] = report;
                        }
                        
                        FilterReports();
                        Console.WriteLine($"[ReportsViewModel] Report updated successfully: {report.Title}");
                        System.Windows.MessageBox.Show("Report updated successfully.", "Update Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        updateDialog.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ReportsViewModel] Error updating report: {ex.Message}");
                        System.Windows.MessageBox.Show($"Error updating report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                };

                cancelButton.Click += (s, e) => updateDialog.Close();

                buttonPanel.Children.Add(updateButton);
                buttonPanel.Children.Add(cancelButton);
                System.Windows.Controls.Grid.SetRow(buttonPanel, 3);
                grid.Children.Add(buttonPanel);

                updateDialog.Content = grid;
                updateDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error opening update dialog: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening update dialog: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void DeleteReport(Report? report)
        {
            Console.WriteLine($"[ReportsViewModel] DeleteReport command executed with report: {report?.Title ?? "null"}");
            if (report == null) 
            {
                Console.WriteLine($"[ReportsViewModel] DeleteReport: Report is null, returning");
                return;
            }

            Console.WriteLine($"[ReportsViewModel] Delete report requested: {report.Title} (ID: {report.ReportID})");
            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the report '{report.Title}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Console.WriteLine($"[ReportsViewModel] Deleting report: {report.Title} (ID: {report.ReportID})");
                    await _reportService.DeleteReportAsync(report.ReportID);
                    Reports.Remove(report);
                    FilterReports();
                    Console.WriteLine($"[ReportsViewModel] Report deleted successfully: {report.Title}");
                }
                else
                {
                    Console.WriteLine($"[ReportsViewModel] Report deletion cancelled by user: {report.Title}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error deleting report {report.Title}: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error deleting report: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task GenerateCustomReportAsync(Views.CustomReportDialog.CustomReportParameters parameters)
        {
            try
            {
                Console.WriteLine($"[ReportsViewModel] Starting custom report generation: {parameters.Title} ({parameters.Type})");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                string reportContent;

                // Generate content based on report type
                switch (parameters.Type)
                {
                    case ReportType.MemberStatistics:
                        var users = await _userService.GetAllUsersAsync();
                        var clubs = await _clubService.GetAllClubsAsync();
                        reportContent = GenerateCustomMembershipReportContent(users, clubs, parameters);
                        break;
                    case ReportType.EventOutcomes:
                        var events = await _eventService.GetAllEventsAsync();
                        reportContent = GenerateCustomEventReportContent(events, parameters);
                        break;
                    case ReportType.SemesterSummary:
                        var allEvents = await _eventService.GetAllEventsAsync();
                        var allClubs = await _clubService.GetAllClubsAsync();
                        reportContent = GenerateCustomFinancialReportContent(allEvents, allClubs, parameters);
                        break;
                    case ReportType.ActivityTracking:
                        var activityEvents = await _eventService.GetAllEventsAsync();
                        reportContent = GenerateCustomActivityReportContent(activityEvents, parameters);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported report type: {parameters.Type}");
                }

                var report = new Report
                {
                    Title = parameters.Title,
                    Type = parameters.Type,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = currentUser.ClubID,
                    GeneratedByUserID = currentUser.UserID
                };

                var createdReport = await _reportService.CreateReportAsync(report);
                Reports.Insert(0, createdReport);
                FilterReports();
                Console.WriteLine($"[ReportsViewModel] Custom report created successfully with ID: {createdReport.ReportID}");

                System.Windows.MessageBox.Show(
                    "Custom report generated successfully!",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating custom report: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error generating custom report: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GenerateCustomMembershipReportContent(IEnumerable<User> users, IEnumerable<Club> clubs, Views.CustomReportDialog.CustomReportParameters parameters)
        {
            var filteredUsers = FilterUsersByDateRange(users, parameters.StartDate, parameters.EndDate);
            var content = new StringBuilder();

            if (parameters.IncludeSummary)
            {
                content.AppendLine("=== EXECUTIVE SUMMARY ===");
                content.AppendLine($"Report Period: {parameters.StartDate?.ToString("yyyy-MM-dd") ?? "All time"} to {parameters.EndDate?.ToString("yyyy-MM-dd") ?? "Present"}");
                content.AppendLine($"Total Users: {filteredUsers.Count()}");
                content.AppendLine($"Total Clubs: {clubs.Count()}");
                content.AppendLine();
            }

            if (parameters.IncludeDetails)
            {
                content.AppendLine("=== DETAILED MEMBERSHIP BREAKDOWN ===");
                foreach (var club in clubs)
                {
                    var clubMembers = filteredUsers.Where(u => u.ClubID == club.ClubID);
                    content.AppendLine($"\n{club.Name}:");
                    content.AppendLine($"  Total Members: {clubMembers.Count()}");
                    content.AppendLine($"  Active Members: {clubMembers.Count(u => u.IsActive)}");
                    content.AppendLine($"  Inactive Members: {clubMembers.Count(u => !u.IsActive)}");
                }
                content.AppendLine();
            }

            if (parameters.IncludeCharts)
            {
                content.AppendLine("=== MEMBERSHIP STATISTICS ===");
                content.AppendLine($"Active Users: {filteredUsers.Count(u => u.IsActive)}");
                content.AppendLine($"Inactive Users: {filteredUsers.Count(u => !u.IsActive)}");
                content.AppendLine($"System Admin Users: {filteredUsers.Count(u => u.Role == UserRole.SystemAdmin)}");
                content.AppendLine($"Club President Users: {filteredUsers.Count(u => u.Role == UserRole.ClubPresident)}");
                content.AppendLine($"Club Officer Users: {filteredUsers.Count(u => u.Role == UserRole.ClubOfficer)}");
                content.AppendLine($"Regular Members: {filteredUsers.Count(u => u.Role == UserRole.Member)}");
                content.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(parameters.Notes))
            {
                content.AppendLine("=== ADDITIONAL NOTES ===");
                content.AppendLine(parameters.Notes);
                content.AppendLine();
            }

            content.AppendLine($"Generated by: {CurrentUser?.FullName}");
            content.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return content.ToString();
        }

        private string GenerateCustomEventReportContent(IEnumerable<Event> events, Views.CustomReportDialog.CustomReportParameters parameters)
        {
            var filteredEvents = FilterEventsByDateRange(events, parameters.StartDate, parameters.EndDate);
            var content = new StringBuilder();

            if (parameters.IncludeSummary)
            {
                content.AppendLine("=== EXECUTIVE SUMMARY ===");
                content.AppendLine($"Report Period: {parameters.StartDate?.ToString("yyyy-MM-dd") ?? "All time"} to {parameters.EndDate?.ToString("yyyy-MM-dd") ?? "Present"}");
                content.AppendLine($"Total Events: {filteredEvents.Count()}");
                content.AppendLine($"Completed Events: {filteredEvents.Count(e => e.Status == EventStatus.Completed)}");
                content.AppendLine($"Upcoming Events: {filteredEvents.Count(e => e.Status == EventStatus.Scheduled)}");
                content.AppendLine();
            }

            if (parameters.IncludeDetails)
            {
                content.AppendLine("=== EVENT DETAILS ===");
                foreach (var evt in filteredEvents.OrderBy(e => e.EventDate))
                {
                    content.AppendLine($"\n{evt.Name}:");
                    content.AppendLine($"  Date: {evt.EventDate:yyyy-MM-dd HH:mm}");
                    content.AppendLine($"  Location: {evt.Location}");
                    content.AppendLine($"  Status: {evt.Status}");
                    content.AppendLine($"  Max Participants: {evt.MaxParticipants?.ToString() ?? "Unlimited"}");
                    if (!string.IsNullOrEmpty(evt.Description))
                    {
                        content.AppendLine($"  Description: {evt.Description}");
                    }
                }
                content.AppendLine();
            }

            if (parameters.IncludeCharts)
            {
                content.AppendLine("=== EVENT STATISTICS ===");
                content.AppendLine($"Events by Status:");
                foreach (EventStatus status in Enum.GetValues<EventStatus>())
                {
                    var count = filteredEvents.Count(e => e.Status == status);
                    content.AppendLine($"  {status}: {count}");
                }
                content.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(parameters.Notes))
            {
                content.AppendLine("=== ADDITIONAL NOTES ===");
                content.AppendLine(parameters.Notes);
                content.AppendLine();
            }

            content.AppendLine($"Generated by: {CurrentUser?.FullName}");
            content.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return content.ToString();
        }

        private string GenerateCustomFinancialReportContent(IEnumerable<Event> events, IEnumerable<Club> clubs, Views.CustomReportDialog.CustomReportParameters parameters)
        {
            var filteredEvents = FilterEventsByDateRange(events, parameters.StartDate, parameters.EndDate);
            var content = new StringBuilder();

            if (parameters.IncludeSummary)
            {
                content.AppendLine("=== EXECUTIVE SUMMARY ===");
                content.AppendLine($"Report Period: {parameters.StartDate?.ToString("yyyy-MM-dd") ?? "All time"} to {parameters.EndDate?.ToString("yyyy-MM-dd") ?? "Present"}");
                content.AppendLine($"Total Events Analyzed: {filteredEvents.Count()}");
                content.AppendLine($"Total Clubs: {clubs.Count()}");
                content.AppendLine();
            }

            if (parameters.IncludeDetails)
            {
                content.AppendLine("=== FINANCIAL BREAKDOWN BY CLUB ===");
                foreach (var club in clubs)
                {
                    var clubEvents = filteredEvents.Where(e => e.ClubID == club.ClubID);
                    content.AppendLine($"\n{club.Name}:");
                    content.AppendLine($"  Total Events: {clubEvents.Count()}");
                    content.AppendLine($"  Status: {club.Status}");
                    content.AppendLine($"  Events This Period: {clubEvents.Count()}");
                }
                content.AppendLine();
            }

            if (parameters.IncludeCharts)
            {
                content.AppendLine("=== FINANCIAL STATISTICS ===");
                content.AppendLine($"Total Active Clubs: {clubs.Count(c => c.IsActive)}");
                content.AppendLine($"Total Inactive Clubs: {clubs.Count(c => !c.IsActive)}");
                content.AppendLine($"Events per Club (Average): {(clubs.Any() ? filteredEvents.Count() / (double)clubs.Count() : 0):F1}");
                content.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(parameters.Notes))
            {
                content.AppendLine("=== ADDITIONAL NOTES ===");
                content.AppendLine(parameters.Notes);
                content.AppendLine();
            }

            content.AppendLine($"Generated by: {CurrentUser?.FullName}");
            content.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return content.ToString();
        }

        private string GenerateCustomActivityReportContent(IEnumerable<Event> events, Views.CustomReportDialog.CustomReportParameters parameters)
        {
            var filteredEvents = FilterEventsByDateRange(events, parameters.StartDate, parameters.EndDate);
            var content = new StringBuilder();

            if (parameters.IncludeSummary)
            {
                content.AppendLine("=== EXECUTIVE SUMMARY ===");
                content.AppendLine($"Report Period: {parameters.StartDate?.ToString("yyyy-MM-dd") ?? "All time"} to {parameters.EndDate?.ToString("yyyy-MM-dd") ?? "Present"}");
                content.AppendLine($"Total Activities: {filteredEvents.Count()}");
                content.AppendLine($"Active Period Events: {filteredEvents.Count(e => e.EventDate >= DateTime.Now.AddDays(-30))}");
                content.AppendLine();
            }

            if (parameters.IncludeDetails)
            {
                content.AppendLine("=== ACTIVITY TIMELINE ===");
                var eventsByMonth = filteredEvents.GroupBy(e => new { e.EventDate.Year, e.EventDate.Month })
                                                 .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);
                
                foreach (var monthGroup in eventsByMonth)
                {
                    content.AppendLine($"\n{monthGroup.Key.Year}-{monthGroup.Key.Month:D2}:");
                    content.AppendLine($"  Events: {monthGroup.Count()}");
                    foreach (var evt in monthGroup.OrderBy(e => e.EventDate))
                    {
                        content.AppendLine($"    - {evt.Name} ({evt.EventDate:dd/MM})");
                    }
                }
                content.AppendLine();
            }

            if (parameters.IncludeCharts)
            {
                content.AppendLine("=== ACTIVITY STATISTICS ===");
                content.AppendLine($"Most Active Month: {GetMostActiveMonth(filteredEvents)}");
                content.AppendLine($"Average Events per Month: {GetAverageEventsPerMonth(filteredEvents):F1}");
                content.AppendLine($"Peak Activity Day: {GetPeakActivityDay(filteredEvents)}");
                content.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(parameters.Notes))
            {
                content.AppendLine("=== ADDITIONAL NOTES ===");
                content.AppendLine(parameters.Notes);
                content.AppendLine();
            }

            content.AppendLine($"Generated by: {CurrentUser?.FullName}");
            content.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return content.ToString();
        }

        private IEnumerable<User> FilterUsersByDateRange(IEnumerable<User> users, DateTime? startDate, DateTime? endDate)
        {
            var filtered = users.AsEnumerable();
            
            if (startDate.HasValue)
            {
                filtered = filtered.Where(u => u.JoinDate >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                filtered = filtered.Where(u => u.JoinDate <= endDate.Value.AddDays(1));
            }
            
            return filtered;
        }

        private IEnumerable<Event> FilterEventsByDateRange(IEnumerable<Event> events, DateTime? startDate, DateTime? endDate)
        {
            var filtered = events.AsEnumerable();
            
            if (startDate.HasValue)
            {
                filtered = filtered.Where(e => e.EventDate >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                filtered = filtered.Where(e => e.EventDate <= endDate.Value.AddDays(1));
            }
            
            return filtered;
        }

        private string GetMostActiveMonth(IEnumerable<Event> events)
        {
            if (!events.Any()) return "N/A";
            
            var monthGroups = events.GroupBy(e => new { e.EventDate.Year, e.EventDate.Month });
            var mostActive = monthGroups.OrderByDescending(g => g.Count()).FirstOrDefault();
            
            return mostActive != null ? $"{mostActive.Key.Year}-{mostActive.Key.Month:D2} ({mostActive.Count()} events)" : "N/A";
        }

        private double GetAverageEventsPerMonth(IEnumerable<Event> events)
        {
            if (!events.Any()) return 0;
            
            var monthGroups = events.GroupBy(e => new { e.EventDate.Year, e.EventDate.Month });
            return monthGroups.Any() ? monthGroups.Average(g => g.Count()) : 0;
        }

        private string GetPeakActivityDay(IEnumerable<Event> events)
        {
            if (!events.Any()) return "N/A";
            
            var dayGroups = events.GroupBy(e => e.EventDate.DayOfWeek);
            var peakDay = dayGroups.OrderByDescending(g => g.Count()).FirstOrDefault();
            
            return peakDay != null ? $"{peakDay.Key} ({peakDay.Count()} events)" : "N/A";
        }

        public override Task LoadAsync()
        {
            return LoadReportsAsync();
        }
    }
}