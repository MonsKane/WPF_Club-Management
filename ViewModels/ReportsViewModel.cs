using ClubManagementApp.Commands;
using ClubManagementApp.Helpers;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Controls;
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
        private ComboBoxItem? _selectedReportType;
        private DateTime? _selectedDate;
        private Report? _selectedReport;
        private int _todayReportsCount;
        private int _monthReportsCount;
        private bool _hasNoReports;

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
            _ = LoadCurrentUserAsync(); // Fire-and-forget call
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

        public ComboBoxItem? SelectedReportType
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

        // Current User
        private User? _currentUser;
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(CanAccessReports));
                    OnPropertyChanged(nameof(CanGenerateReports));
                    OnPropertyChanged(nameof(CanExportReports));
                }
            }
        }

        // Authorization Properties
        public bool CanAccessReports => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.SystemRole, "ReportView");
        public bool CanGenerateReports => CurrentUser != null && _authorizationService.CanGenerateReports(CurrentUser.SystemRole);
        public bool CanExportReports => CurrentUser != null && _authorizationService.CanExportReports(CurrentUser.SystemRole);

        // Commands
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand GenerateMembershipReportCommand { get; private set; } = null!;
        public ICommand GenerateEventReportCommand { get; private set; } = null!;
        public ICommand GenerateFinancialReportCommand { get; private set; } = null!;
        public ICommand GenerateActivityReportCommand { get; private set; } = null!;
        public ICommand ViewReportCommand { get; private set; } = null!;
        public ICommand UpdateReportCommand { get; private set; } = null!;
        public ICommand DownloadReportCommand { get; private set; } = null!;
        public ICommand EmailReportCommand { get; private set; } = null!;
        public ICommand DeleteReportCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            GenerateReportCommand = new RelayCommand(GenerateReport, CanGenerateReport);
            GenerateMembershipReportCommand = new RelayCommand(async () => await GenerateMembershipReportAsync(), CanGenerateReport);
            GenerateActivityReportCommand = new RelayCommand(async () => await GenerateActivityReportAsync(), CanGenerateReport);
            ViewReportCommand = new RelayCommand<Report>(ViewReport, CanViewReport);
            UpdateReportCommand = new RelayCommand<Report>(UpdateReport, CanUpdateReport);
            DownloadReportCommand = new RelayCommand<Report>(DownloadReport, CanExportReport);
            EmailReportCommand = new RelayCommand<Report>(EmailReport, CanExportReport);
            DeleteReportCommand = new RelayCommand<Report>(DeleteReport, CanDeleteReport);
            RefreshCommand = new RelayCommand(async () => await LoadReportsAsync(), CanAccessReport);
        }

        // CanExecute methods
        private bool CanAccessReport() => CanAccessReports;
        private bool CanGenerateReport() => CanGenerateReports;
        private bool CanViewReport(Report? report) => report != null && CanAccessReports;
        private bool CanUpdateReport(Report? report) => report != null && CanGenerateReports &&
            (CurrentUser?.SystemRole == SystemRole.Admin || report.GeneratedByUserID == CurrentUser?.UserID);
        private bool CanExportReport(Report? report) => report != null && CanExportReports;
        private bool CanDeleteReport(Report? report) => report != null && CanGenerateReports &&
            (CurrentUser?.SystemRole == SystemRole.Admin || report.GeneratedByUserID == CurrentUser?.UserID);

        // Load current user method
        public async Task LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[ReportsViewModel] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.SystemRole})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error loading current user: {ex.Message}");
                CurrentUser = null;
            }
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
            if (SelectedReportType?.Content?.ToString() != "All Types")
            {
                var reportType = SelectedReportType?.Content switch
                {
                    "Membership" => ReportType.MemberStatistics,
                    "Activity" => ReportType.ActivityTracking,
                    _ => (ReportType?)null
                };

                if (reportType.HasValue)
                {
                    filtered = filtered.Where(r => r.Type == reportType.Value);
                }
            }

            // Filter by date
            if (SelectedDate.HasValue)
            {
                var date = SelectedDate.Value.Date;
                var nextDate = date.AddDays(1);

                filtered = filtered.Where(r => r.GeneratedDate >= date && r.GeneratedDate < nextDate);
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

        private async void GenerateReport()
        {
            Console.WriteLine("[ReportsViewModel] GenerateReport called");

            try
            {
                var dialog = new Views.CustomReportDialog(_clubService);
                var result = dialog.ShowDialog();

                if (result == true && dialog.ReportParameters != null)
                {
                    Console.WriteLine($"[ReportsViewModel] Generating custom report: {dialog.ReportParameters.Title}");
                    await GenerateCustomReportAsync(dialog.ReportParameters);
                }
                else
                {
                    Console.WriteLine("[ReportsViewModel] Report generation cancelled");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error opening custom report dialog: {ex.Message}");
                System.Windows.MessageBox.Show($"Error opening report dialog: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task GenerateCustomReportAsync(Views.CustomReportDialog.CustomReportParameters parameters)
        {
            try
            {
                IsLoading = true;

                Console.WriteLine($"[ReportsViewModel] Generating custom report: {parameters.Title} ({parameters.Type})");

                // Create the report based on type
                switch (parameters.Type)
                {
                    case ReportType.MemberStatistics:
                        await GenerateCustomMembershipReportAsync(parameters);
                        break;
                    case ReportType.ActivityTracking:
                        await GenerateCustomActivityReportAsync(parameters);
                        break;
                    default:
                        break;
                }

                Console.WriteLine($"[ReportsViewModel] Custom report generated successfully: {parameters.Title}");
                System.Windows.MessageBox.Show($"Custom report '{parameters.Title}' has been generated successfully!", "Success",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating custom report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error generating custom report: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
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
                var events = await _eventService.GetAllEventsAsync();

                // For regular report, use beginning of month to end of month
                var now = DateTime.Now;
                var startDate = new DateTime(now.Year, now.Month, 1);
                var endDate = now;

                // Generate report content
                var reportContent = GenerateMembershipReportContent(users, clubs, events, startDate, endDate, null);

                var report = new Report
                {
                    Title = $"Membership Report - {DateTime.Now:yyyy-MM-dd}",
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

        private async Task GenerateCustomMembershipReportAsync(Views.CustomReportDialog.CustomReportParameters parameters)
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting custom membership report generation");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                var users = await _userService.GetAllUsersAsync();
                var clubs = await _clubService.GetAllClubsAsync();
                var events = await _eventService.GetAllEventsAsync();

                // Use custom date range
                var startDate = parameters.StartDate ?? DateTime.Now.AddMonths(-1);
                var endDate = parameters.EndDate ?? DateTime.Now;

                // For custom reports, allow club selection (null means all clubs)
                int? selectedClubId = parameters.SelectedClubId;
                // TODO: Add club selection UI to CustomReportDialog for membership reports

                // Generate report content
                var reportContent = GenerateMembershipReportContent(users, clubs, events, startDate, endDate, selectedClubId);

                var report = new Report
                {
                    Title = parameters.Title,
                    Type = ReportType.MemberStatistics,
                    Content = reportContent,
                    GeneratedDate = DateTime.Now,
                    Semester = currentSemester,
                    ClubID = selectedClubId ?? currentUser.ClubID,
                    GeneratedByUserID = currentUser.UserID
                };

                var createdReport = await _reportService.CreateReportAsync(report);
                Reports.Insert(0, createdReport);
                FilterReports();
                Console.WriteLine($"[ReportsViewModel] Custom membership report created successfully with ID: {createdReport.ReportID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating custom membership report: {ex.Message}");
                throw;
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
                    Title = $"Event Report - {DateTime.Now:yyyy-MM-dd}",
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
                    Title = $"Financial Report - {DateTime.Now:yyyy-MM-dd}",
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
                var clubs = await _clubService.GetAllClubsAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events and {users.Count()} users for activity report");

                // For regular report, use beginning of month to end of month
                var now = DateTime.Now;
                var startDate = new DateTime(now.Year, now.Month, 1);
                var endDate = now;

                // Generate activity report content
                var reportContent = GenerateActivityReportContent(events, users, clubs, startDate, endDate);

                var report = new Report
                {
                    Title = $"Activity Report - {DateTime.Now:yyyy-MM-dd}",
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

        private async Task GenerateCustomActivityReportAsync(Views.CustomReportDialog.CustomReportParameters parameters)
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting custom activity report generation");
                IsLoading = true;

                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("No current user found. Please log in again.");
                }

                var currentSemester = GetCurrentSemester();
                var events = await _eventService.GetAllEventsAsync();
                var users = await _userService.GetAllUsersAsync();
                var clubs = await _clubService.GetAllClubsAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events and {users.Count()} users for custom activity report");

                // Use custom date range
                var startDate = parameters.StartDate ?? DateTime.Now.AddMonths(-1);
                var endDate = parameters.EndDate ?? DateTime.Now;

                // Generate activity report content
                var reportContent = GenerateActivityReportContent(events, users, clubs, startDate, endDate);

                var report = new Report
                {
                    Title = parameters.Title,
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
                Console.WriteLine($"[ReportsViewModel] Custom activity report created successfully with ID: {createdReport.ReportID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error generating custom activity report: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GenerateMembershipReportContent(IEnumerable<User> users, IEnumerable<Club> clubs, IEnumerable<Event> events, DateTime startDate, DateTime endDate, int? selectedClubId)
        {
            var content = $"MEMBERSHIP ANALYSIS REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Analysis Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}\n\n";

            // Filter events within the date range
            var periodEvents = events.Where(e => e.EventDate >= startDate && e.EventDate <= endDate).ToList();

            // Filter clubs if specific club is selected
            var targetClubs = selectedClubId.HasValue ? clubs.Where(c => c.ClubID == selectedClubId.Value) : clubs;

            content += $"SUMMARY:\n";
            content += $"Total Users: {users.Count()}\n";
            content += $"Total Clubs Analyzed: {targetClubs.Count()}\n";
            content += $"Events in Period: {periodEvents.Count}\n\n";

            foreach (var club in targetClubs.OrderBy(c => c.Name))
            {
                var clubMembers = users.Where(u => u.ClubID == club.ClubID && u.IsActive).ToList();
                var clubEvents = periodEvents.Where(e => e.ClubID == club.ClubID).ToList();

                content += $"\n=== {club.Name.ToUpper()} CLUB ANALYSIS ===\n";
                content += $"Total Members: {clubMembers.Count}\n";
                content += $"Events in Period: {clubEvents.Count}\n\n";

                if (clubMembers.Any())
                {
                    content += "MEMBER ACTIVITY ANALYSIS:\n";
                    content += "Member Name\t\tTotal Events\tActivity Level\n";
                    content += "─────────────────────────────────────────────────\n";

                    foreach (var member in clubMembers.OrderBy(m => m.FullName))
                    {
                        // Count events this member participated in during the period
                        var memberEventCount = 0;
                        if (member.EventParticipations != null)
                        {
                            memberEventCount = member.EventParticipations
                                .Count(ep => ep.Status == AttendanceStatus.Attended &&
                                           clubEvents.Any(ce => ce.EventID == ep.EventID));
                        }

                        // Determine activity level based on event participation
                        string activityLevel;
                        if (memberEventCount >= 5)
                            activityLevel = "Active";
                        else if (memberEventCount >= 2)
                            activityLevel = "Normal";
                        else
                            activityLevel = "Inactive";

                        var memberName = member.FullName.Length > 20 ? member.FullName.Substring(0, 17) + "..." : member.FullName;
                        content += $"{memberName,-20}\t{memberEventCount,2}\t\t{activityLevel}\n";
                    }

                    // Activity level summary for this club
                    var activeCount = clubMembers.Count(m =>
                        (m.EventParticipations?.Count(ep => ep.Status == AttendanceStatus.Attended &&
                                                           clubEvents.Any(ce => ce.EventID == ep.EventID)) ?? 0) >= 5);
                    var normalCount = clubMembers.Count(m =>
                    {
                        var eventCount = m.EventParticipations?.Count(ep => ep.Status == AttendanceStatus.Attended &&
                                                                           clubEvents.Any(ce => ce.EventID == ep.EventID)) ?? 0;
                        return eventCount >= 2 && eventCount < 5;
                    });
                    var inactiveCount = clubMembers.Count(m =>
                        (m.EventParticipations?.Count(ep => ep.Status == AttendanceStatus.Attended &&
                                                           clubEvents.Any(ce => ce.EventID == ep.EventID)) ?? 0) < 2);

                    content += $"\nACTIVITY LEVEL SUMMARY:\n";
                    content += $"• Active (5+ events): {activeCount} members ({(clubMembers.Count > 0 ? (activeCount * 100.0 / clubMembers.Count) : 0):F1}%)\n";
                    content += $"• Normal (2-4 events): {normalCount} members ({(clubMembers.Count > 0 ? (normalCount * 100.0 / clubMembers.Count) : 0):F1}%)\n";
                    content += $"• Inactive (0-1 events): {inactiveCount} members ({(clubMembers.Count > 0 ? (inactiveCount * 100.0 / clubMembers.Count) : 0):F1}%)\n";
                }
                else
                {
                    content += "No active members found for this club.\n";
                }
            }

            // Overall statistics across all analyzed clubs
            if (targetClubs.Count() > 1)
            {
                var allMembers = users.Where(u => targetClubs.Any(c => c.ClubID == u.ClubID) && u.IsActive).ToList();
                var totalActiveMembers = allMembers.Count(m =>
                    (m.EventParticipations?.Count(ep => ep.Status == AttendanceStatus.Attended &&
                                                       periodEvents.Any(pe => pe.EventID == ep.EventID)) ?? 0) >= 5);
                var totalNormalMembers = allMembers.Count(m =>
                {
                    var eventCount = m.EventParticipations?.Count(ep => ep.Status == AttendanceStatus.Attended &&
                                                                       periodEvents.Any(pe => pe.EventID == ep.EventID)) ?? 0;
                    return eventCount >= 2 && eventCount < 5;
                });
                var totalInactiveMembers = allMembers.Count(m =>
                    (m.EventParticipations?.Count(ep => ep.Status == AttendanceStatus.Attended &&
                                                       periodEvents.Any(pe => pe.EventID == ep.EventID)) ?? 0) < 2);

                content += $"\n=== OVERALL STATISTICS ===\n";
                content += $"Total Active Members: {totalActiveMembers} ({(allMembers.Count > 0 ? (totalActiveMembers * 100.0 / allMembers.Count) : 0):F1}%)\n";
                content += $"Total Normal Members: {totalNormalMembers} ({(allMembers.Count > 0 ? (totalNormalMembers * 100.0 / allMembers.Count) : 0):F1}%)\n";
                content += $"Total Inactive Members: {totalInactiveMembers} ({(allMembers.Count > 0 ? (totalInactiveMembers * 100.0 / allMembers.Count) : 0):F1}%)\n";
            }

            return content;
        }

        private string GenerateEventReportContent(IEnumerable<Event> events)
        {
            var content = $"EVENT REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";
            content += $"Total Events: {events.Count()}\n\n";

            var now = DateTime.Now;
            var upcoming = events.Count(e => e.EventDate > now);
            var ongoing = events.Count(e => e.EventDate.Date == now.Date);
            var completed = events.Count(e => e.EventDate < now);

            content += "EVENTS BY STATUS:\n";
            content += $"- Upcoming: {upcoming}\n";
            content += $"- Ongoing: {ongoing}\n";
            content += $"- Completed: {completed}\n\n";

            content += "RECENT EVENTS:\n";
            foreach (var eventItem in events.OrderByDescending(e => e.EventDate).Take(10))
            {
                content += $"- {eventItem.Name} ({eventItem.EventDate:yyyy-MM-dd})\n";
            }

            return content;
        }

        private string GenerateActivityReportContent(IEnumerable<Event> events, IEnumerable<User> users, IEnumerable<Club> clubs, DateTime startDate, DateTime endDate)
        {
            var content = $"ACTIVITY TRACKING REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Analysis Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}\n\n";

            // Filter data within the specified date range
            var periodEvents = events.Where(e => e.CreatedDate >= startDate && e.CreatedDate <= endDate).ToList();
            var periodUsers = users.Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate).ToList();
            var periodClubs = clubs.Where(c => c.EstablishedDate >= startDate && c.EstablishedDate <= endDate).ToList();

            content += $"=== CREATION STATISTICS ===\n";
            content += $"New Accounts Created: {periodUsers.Count}\n";
            content += $"New Clubs Created: {periodClubs.Count}\n";
            content += $"New Events Created: {periodEvents.Count}\n\n";

            // Account creation breakdown by role
            if (periodUsers.Any())
            {
                content += $"NEW ACCOUNTS BY ROLE:\n";
                var usersByRole = periodUsers.GroupBy(u => u.SystemRole);
                foreach (var group in usersByRole.OrderBy(g => g.Key))
                {
                    content += $"• {group.Key}: {group.Count()} accounts\n";
                }
                content += $"\n";

                content += $"NEW ACCOUNT DETAILS:\n";
                content += $"Date\t\tName\t\t\tRole\t\tClub\n";
                content += $"─────────────────────────────────────────────────────────────\n";
                foreach (var user in periodUsers.OrderBy(u => u.CreatedAt))
                {
                    var userName = user.FullName.Length > 15 ? user.FullName.Substring(0, 12) + "..." : user.FullName;
                    var clubName = clubs.FirstOrDefault(c => c.ClubID == user.ClubID)?.Name ?? "N/A";
                    if (clubName.Length > 12) clubName = clubName.Substring(0, 9) + "...";
                    content += $"{user.CreatedAt:MM-dd}\t\t{userName,-15}\t{user.SystemRole,-10}\t{clubName}\n";
                }
                content += $"\n";
            }

            // Club creation details
            if (periodClubs.Any())
            {
                content += $"NEW CLUBS CREATED:\n";
                content += $"Date\t\tClub Name\t\tDescription\n";
                content += $"─────────────────────────────────────────────────────────────\n";
                foreach (var club in periodClubs.OrderBy(c => c.EstablishedDate))
                {
                    var clubName = club.Name.Length > 15 ? club.Name.Substring(0, 12) + "..." : club.Name;
                    var description = (club.Description?.Length > 20 ? club.Description.Substring(0, 17) + "..." : club.Description) ?? "No description";
                    content += $"{club.EstablishedDate:MM-dd}\t\t{clubName,-15}\t{description}\n";
                }
                content += $"\n";
            }

            // Event creation details
            if (periodEvents.Any())
            {
                content += $"NEW EVENTS CREATED:\n";
                content += $"Date\t\tEvent Name\t\tClub\t\tEvent Date\n";
                content += $"─────────────────────────────────────────────────────────────\n";
                foreach (var eventItem in periodEvents.OrderBy(e => e.CreatedDate))
                {
                    var eventName = eventItem.Name.Length > 15 ? eventItem.Name.Substring(0, 12) + "..." : eventItem.Name;
                    var clubName = clubs.FirstOrDefault(c => c.ClubID == eventItem.ClubID)?.Name ?? "N/A";
                    if (clubName.Length > 10) clubName = clubName.Substring(0, 7) + "...";
                    content += $"{eventItem.CreatedDate:MM-dd}\t\t{eventName,-15}\t{clubName,-10}\t{eventItem.EventDate:MM-dd}\n";
                }
                content += $"\n";
            }

            // Daily activity breakdown
            content += $"=== DAILY ACTIVITY BREAKDOWN ===\n";
            var dailyStats = new Dictionary<DateTime, (int Users, int Clubs, int Events)>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayUsers = periodUsers.Count(u => u.CreatedAt.Date == date);
                var dayClubs = periodClubs.Count(c => c.EstablishedDate?.Date == date);
                var dayEvents = periodEvents.Count(e => e.CreatedDate.Date == date);

                if (dayUsers > 0 || dayClubs > 0 || dayEvents > 0)
                {
                    dailyStats[date] = (dayUsers, dayClubs, dayEvents);
                }
            }

            if (dailyStats.Any())
            {
                content += $"Date\t\tAccounts\tClubs\tEvents\tTotal\n";
                content += $"─────────────────────────────────────────────────\n";
                foreach (var stat in dailyStats.OrderBy(s => s.Key))
                {
                    var total = stat.Value.Users + stat.Value.Clubs + stat.Value.Events;
                    content += $"{stat.Key:MM-dd}\t\t{stat.Value.Users,2}\t\t{stat.Value.Clubs,2}\t{stat.Value.Events,2}\t{total,2}\n";
                }
            }
            else
            {
                content += $"No creation activity found in the specified period.\n";
            }

            // Summary statistics
            content += $"\n=== SUMMARY STATISTICS ===\n";
            var totalDays = (endDate.Date - startDate.Date).Days + 1;
            var avgUsersPerDay = totalDays > 0 ? (double)periodUsers.Count / totalDays : 0;
            var avgClubsPerDay = totalDays > 0 ? (double)periodClubs.Count / totalDays : 0;
            var avgEventsPerDay = totalDays > 0 ? (double)periodEvents.Count / totalDays : 0;

            content += $"Analysis Period: {totalDays} days\n";
            content += $"Average Accounts/Day: {avgUsersPerDay:F2}\n";
            content += $"Average Clubs/Day: {avgClubsPerDay:F2}\n";
            content += $"Average Events/Day: {avgEventsPerDay:F2}\n";
            content += $"Total Activity Items: {periodUsers.Count + periodClubs.Count + periodEvents.Count}\n";

            return content;
        }

        private string GenerateFinancialReportContent(IEnumerable<Event> events, IEnumerable<Club> clubs)
        {
            var content = $"FINANCIAL REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            // Calculate financial metrics
            var totalEvents = events.Count();
            var totalClubs = clubs.Count();
            var estimatedEventCosts = totalEvents * 500; // Placeholder calculation
            var estimatedRevenue = totalEvents * 750; // Placeholder calculation

            content += "FINANCIAL SUMMARY:\n";
            content += $"Total Clubs: {totalClubs}\n";
            content += $"Total Events: {totalEvents}\n";
            content += $"Estimated Event Costs: ${estimatedEventCosts:N2}\n";
            content += $"Estimated Revenue: ${estimatedRevenue:N2}\n";
            content += $"Net Income: ${(estimatedRevenue - estimatedEventCosts):N2}\n\n";

            content += "CLUB BREAKDOWN:\n";
            foreach (var club in clubs.OrderBy(c => c.Name))
            {
                var clubEvents = events.Where(e => e.ClubID == club.ClubID).Count();
                var clubCosts = clubEvents * 500;
                var clubRevenue = clubEvents * 750;
                content += $"- {club.Name}: {clubEvents} events, ${clubRevenue:N2} revenue, ${clubCosts:N2} costs\n";
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
            if (report == null) return;

            Console.WriteLine($"[ReportsViewModel] Viewing report: {report.Title} (ID: {report.ReportID})");

            try
            {
                // Create a comprehensive report viewer window
                var viewerWindow = new System.Windows.Window
                {
                    Title = $"Report Viewer - {report.Title}",
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    ResizeMode = System.Windows.ResizeMode.CanResize
                };

                var mainGrid = new System.Windows.Controls.Grid();
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

                // Header section with report metadata
                var headerPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new System.Windows.Thickness(20, 20, 20, 10),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250))
                };

                var titleBlock = new System.Windows.Controls.TextBlock
                {
                    Text = report.Title,
                    FontSize = 20,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Margin = new System.Windows.Thickness(15, 15, 15, 5),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219))
                };

                var metadataGrid = new System.Windows.Controls.Grid
                {
                    Margin = new System.Windows.Thickness(15, 5, 15, 15)
                };
                metadataGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                metadataGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                metadataGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
                metadataGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
                metadataGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());

                // Report Type
                var typeLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "Report Type:",
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new System.Windows.Thickness(0, 2, 10, 2)
                };
                var typeValue = new System.Windows.Controls.TextBlock
                {
                    Text = report.Type.ToString(),
                    Margin = new System.Windows.Thickness(0, 2, 0, 2)
                };
                System.Windows.Controls.Grid.SetRow(typeLabel, 0);
                System.Windows.Controls.Grid.SetColumn(typeLabel, 0);
                System.Windows.Controls.Grid.SetRow(typeValue, 0);
                System.Windows.Controls.Grid.SetColumn(typeValue, 1);
                metadataGrid.Children.Add(typeLabel);
                metadataGrid.Children.Add(typeValue);

                // Generated Date
                var dateLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "Generated Date:",
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new System.Windows.Thickness(0, 2, 10, 2)
                };
                var dateValue = new System.Windows.Controls.TextBlock
                {
                    Text = report.GeneratedDate.ToString("MMMM dd, yyyy 'at' HH:mm"),
                    Margin = new System.Windows.Thickness(0, 2, 0, 2)
                };
                System.Windows.Controls.Grid.SetRow(dateLabel, 1);
                System.Windows.Controls.Grid.SetColumn(dateLabel, 0);
                System.Windows.Controls.Grid.SetRow(dateValue, 1);
                System.Windows.Controls.Grid.SetColumn(dateValue, 1);
                metadataGrid.Children.Add(dateLabel);
                metadataGrid.Children.Add(dateValue);

                // Semester
                var semesterLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "Semester:",
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new System.Windows.Thickness(0, 2, 10, 2)
                };
                var semesterValue = new System.Windows.Controls.TextBlock
                {
                    Text = report.Semester ?? "N/A",
                    Margin = new System.Windows.Thickness(0, 2, 0, 2)
                };
                System.Windows.Controls.Grid.SetRow(semesterLabel, 2);
                System.Windows.Controls.Grid.SetColumn(semesterLabel, 0);
                System.Windows.Controls.Grid.SetRow(semesterValue, 2);
                System.Windows.Controls.Grid.SetColumn(semesterValue, 1);
                metadataGrid.Children.Add(semesterLabel);
                metadataGrid.Children.Add(semesterValue);

                headerPanel.Children.Add(titleBlock);
                headerPanel.Children.Add(metadataGrid);
                System.Windows.Controls.Grid.SetRow(headerPanel, 0);
                mainGrid.Children.Add(headerPanel);

                // Content section
                var contentLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "Report Content:",
                    FontSize = 16,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new System.Windows.Thickness(20, 10, 20, 5)
                };

                var scrollViewer = new System.Windows.Controls.ScrollViewer
                {
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    Margin = new System.Windows.Thickness(20, 0, 20, 20),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 195, 199)),
                    BorderThickness = new System.Windows.Thickness(1),
                    MaxHeight = 400
                };

                var contentText = string.IsNullOrWhiteSpace(report.Content) ? "No content available for this report." : report.Content;
                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = contentText,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 13,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Padding = new System.Windows.Thickness(15),
                    LineHeight = 20,
                    Background = System.Windows.Media.Brushes.White
                };

                if (string.IsNullOrWhiteSpace(report.Content))
                {
                    textBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125));
                    textBlock.FontStyle = System.Windows.FontStyles.Italic;
                }

                scrollViewer.Content = textBlock;

                var contentPanel = new System.Windows.Controls.StackPanel();
                contentPanel.Children.Add(contentLabel);
                contentPanel.Children.Add(scrollViewer);
                System.Windows.Controls.Grid.SetRow(contentPanel, 1);
                mainGrid.Children.Add(contentPanel);

                // Footer with action buttons
                var footerPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Margin = new System.Windows.Thickness(20, 10, 20, 20)
                };

                var closeButton = new System.Windows.Controls.Button
                {
                    Content = "Close",
                    Width = 80,
                    Height = 30,
                    Margin = new System.Windows.Thickness(5, 0, 0, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new System.Windows.Thickness(0)
                };
                closeButton.Click += (s, e) => viewerWindow.Close();

                var editButton = new System.Windows.Controls.Button
                {
                    Content = "Edit",
                    Width = 80,
                    Height = 30,
                    Margin = new System.Windows.Thickness(5, 0, 0, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),
                    Foreground = System.Windows.Media.Brushes.Black,
                    BorderThickness = new System.Windows.Thickness(0)
                };
                editButton.Click += (s, e) =>
                {
                    try
                    {
                        var editDialog = new System.Windows.Window
                        {
                            Title = $"Edit Report - {report.Title}",
                            Width = 800,
                            Height = 600,
                            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                            ResizeMode = System.Windows.ResizeMode.CanResize
                        };

                        var editGrid = new System.Windows.Controls.Grid();
                        editGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                        editGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                        editGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

                        // Header
                        var headerPanel = new System.Windows.Controls.StackPanel
                        {
                            Margin = new System.Windows.Thickness(20, 20, 20, 10)
                        };
                        var titleLabel = new System.Windows.Controls.TextBlock
                        {
                            Text = "Report Title:",
                            FontWeight = System.Windows.FontWeights.Bold,
                            Margin = new System.Windows.Thickness(0, 0, 0, 5)
                        };
                        var titleTextBox = new System.Windows.Controls.TextBox
                        {
                            Text = report.Title,
                            Margin = new System.Windows.Thickness(0, 0, 0, 10)
                        };
                        var contentLabel = new System.Windows.Controls.TextBlock
                        {
                            Text = "Report Content:",
                            FontWeight = System.Windows.FontWeights.Bold
                        };
                        headerPanel.Children.Add(titleLabel);
                        headerPanel.Children.Add(titleTextBox);
                        headerPanel.Children.Add(contentLabel);
                        System.Windows.Controls.Grid.SetRow(headerPanel, 0);
                        editGrid.Children.Add(headerPanel);

                        // Content editor
                        var contentTextBox = new System.Windows.Controls.TextBox
                        {
                            Text = report.Content,
                            AcceptsReturn = true,
                            TextWrapping = System.Windows.TextWrapping.Wrap,
                            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                            Margin = new System.Windows.Thickness(20, 0, 20, 20),
                            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                            FontSize = 12
                        };
                        System.Windows.Controls.Grid.SetRow(contentTextBox, 1);
                        editGrid.Children.Add(contentTextBox);

                        // Buttons
                        var editButtonPanel = new System.Windows.Controls.StackPanel
                        {
                            Orientation = System.Windows.Controls.Orientation.Horizontal,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                            Margin = new System.Windows.Thickness(20, 10, 20, 20)
                        };
                        var saveButton = new System.Windows.Controls.Button
                        {
                            Content = "Save",
                            Width = 80,
                            Height = 30,
                            Margin = new System.Windows.Thickness(5, 0, 0, 0),
                            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),
                            Foreground = System.Windows.Media.Brushes.White,
                            BorderThickness = new System.Windows.Thickness(0)
                        };
                        var cancelEditButton = new System.Windows.Controls.Button
                        {
                            Content = "Cancel",
                            Width = 80,
                            Height = 30,
                            Margin = new System.Windows.Thickness(5, 0, 0, 0),
                            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125)),
                            Foreground = System.Windows.Media.Brushes.White,
                            BorderThickness = new System.Windows.Thickness(0)
                        };

                        saveButton.Click += async (saveS, saveE) =>
                        {
                            try
                            {
                                report.Title = titleTextBox.Text;
                                report.Content = contentTextBox.Text;
                                await _reportService.UpdateReportAsync(report);
                                System.Windows.MessageBox.Show("Report updated successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                                editDialog.Close();
                                // Refresh the content in the viewer
                                textBlock.Text = report.Content;
                                titleBlock.Text = report.Title;
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show($"Error saving report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            }
                        };
                        cancelEditButton.Click += (cancelS, cancelE) => editDialog.Close();

                        editButtonPanel.Children.Add(saveButton);
                        editButtonPanel.Children.Add(cancelEditButton);
                        System.Windows.Controls.Grid.SetRow(editButtonPanel, 2);
                        editGrid.Children.Add(editButtonPanel);

                        editDialog.Content = editGrid;
                        editDialog.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error opening edit dialog: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                };

                var printButton = new System.Windows.Controls.Button
                {
                    Content = "Print",
                    Width = 80,
                    Height = 30,
                    Margin = new System.Windows.Thickness(5, 0, 0, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new System.Windows.Thickness(0)
                };
                printButton.Click += (s, e) =>
                {
                    try
                    {
                        var printDialog = new System.Windows.Controls.PrintDialog();
                        if (printDialog.ShowDialog() == true)
                        {
                            var printContent = new System.Windows.Controls.TextBlock
                            {
                                Text = $"{report.Title}\n\nGenerated: {report.GeneratedDate:MMMM dd, yyyy 'at' HH:mm}\nType: {report.Type}\nSemester: {report.Semester}\n\n{contentText}",
                                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                                FontSize = 12,
                                TextWrapping = System.Windows.TextWrapping.Wrap,
                                Margin = new System.Windows.Thickness(50),
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                TextAlignment = System.Windows.TextAlignment.Center
                            };
                            printDialog.PrintVisual(printContent, $"Report - {report.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error printing report: {ex.Message}", "Print Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                };

                footerPanel.Children.Add(editButton);
                footerPanel.Children.Add(printButton);
                footerPanel.Children.Add(closeButton);
                System.Windows.Controls.Grid.SetRow(footerPanel, 2);
                mainGrid.Children.Add(footerPanel);

                viewerWindow.Content = mainGrid;
                viewerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportsViewModel] Error viewing report: {ex.Message}");
                System.Windows.MessageBox.Show($"Error viewing report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateReport(Report? report)
        {
            if (report == null) return;

            Console.WriteLine($"[ReportsViewModel] Updating report: {report.Title} (ID: {report.ReportID})");

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

                // Title
                var titleLabel = new System.Windows.Controls.Label { Content = "Report Title:", Margin = new System.Windows.Thickness(10, 10, 10, 5) };
                System.Windows.Controls.Grid.SetRow(titleLabel, 0);
                grid.Children.Add(titleLabel);

                var titleTextBox = new System.Windows.Controls.TextBox
                {
                    Text = report.Title,
                    Margin = new System.Windows.Thickness(10, 0, 10, 10)
                };
                System.Windows.Controls.Grid.SetRow(titleTextBox, 1);
                grid.Children.Add(titleTextBox);

                // Content
                var contentLabel = new System.Windows.Controls.Label { Content = "Report Content:", Margin = new System.Windows.Thickness(10, 0, 10, 5) };
                System.Windows.Controls.Grid.SetRow(contentLabel, 2);
                grid.Children.Add(contentLabel);

                var contentTextBox = new System.Windows.Controls.TextBox
                {
                    Text = report.Content,
                    Margin = new System.Windows.Thickness(10, 0, 10, 10),
                    AcceptsReturn = true,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
                };
                System.Windows.Controls.Grid.SetRow(contentTextBox, 2);
                grid.Children.Add(contentTextBox);

                // Buttons
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
                    try
                    {
                        updateButton.IsEnabled = false;
                        updateButton.Content = "Updating...";

                        // Update the report
                        report.Title = titleTextBox.Text.Trim();
                        report.Content = contentTextBox.Text.Trim();

                        await _reportService.UpdateReportAsync(report);

                        // Update the report in the collection
                        var existingReport = Reports.FirstOrDefault(r => r.ReportID == report.ReportID);
                        if (existingReport != null)
                        {
                            var index = Reports.IndexOf(existingReport);
                            if (index >= 0)
                            {
                                Reports[index] = report;
                            }
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
                    finally
                    {
                        updateButton.IsEnabled = true;
                        updateButton.Content = "Update";
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

        private async void DownloadReport(Report? report)
        {
            if (report == null) return;

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
            if (report == null) return;

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

                        // Try to use EmailService first, fallback to mailto if not available
                        try
                        {
                            var emailService = ServiceLocator.GetService<IEmailService>();
                            if (emailService != null)
                            {
                                await emailService.SendEmailAsync(email, subject, body, isHtml: true);
                                Console.WriteLine($"[ReportsViewModel] Email sent successfully via EmailService for report: {report.Title}");
                                System.Windows.MessageBox.Show("Email sent successfully!", "Email Sent", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            }
                            else
                            {
                                throw new InvalidOperationException("EmailService not available");
                            }
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"[ReportsViewModel] EmailService failed, falling back to mailto: {emailEx.Message}");

                            // Fallback to mailto
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
                        }
                        emailDialog.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ReportsViewModel] Error preparing email: {ex.Message}");
                        System.Windows.MessageBox.Show($"Error preparing email: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

        private async void DeleteReport(Report? report)
        {
            if (report == null) return;

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

        public override async Task LoadAsync()
        {
            await LoadCurrentUserAsync();
            await LoadReportsAsync();
        }
    }
}
