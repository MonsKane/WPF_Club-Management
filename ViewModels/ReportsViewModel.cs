using ClubManagementApp.Commands;
using ClubManagementApp.DTOs;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;
        private readonly IClubService _clubService;
        private ObservableCollection<Report> _reports = new();
        private ObservableCollection<Report> _filteredReports = new();
        private string _searchText = string.Empty;
        private string _selectedReportType = "All Types";
        private DateTime? _selectedDate;
        private bool _isLoading;
        private Report? _selectedReport;

        public ReportsViewModel(IReportService reportService, IUserService userService,
                              IEventService eventService, IClubService clubService)
        {
            Console.WriteLine("[ReportsViewModel] Initializing ReportsViewModel with services");
            _reportService = reportService;
            _userService = userService;
            _eventService = eventService;
            _clubService = clubService;
            InitializeCommands();
            Console.WriteLine("[ReportsViewModel] ReportsViewModel initialization completed");
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

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public Report? SelectedReport
        {
            get => _selectedReport;
            set => SetProperty(ref _selectedReport, value);
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
        public ICommand DeleteReportCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            GenerateReportCommand = new RelayCommand(GenerateReport);
            GenerateMembershipReportCommand = new RelayCommand(async () => await GenerateMembershipReportAsync());
            GenerateEventReportCommand = new RelayCommand(async () => await GenerateEventReportAsync());
            GenerateFinancialReportCommand = new RelayCommand(async () => await GenerateFinancialReportAsync());
            GenerateActivityReportCommand = new RelayCommand(async () => await GenerateActivityReportAsync());
            ViewReportCommand = new RelayCommand<Report>(ViewReport);
            DownloadReportCommand = new RelayCommand<Report>(DownloadReport);
            EmailReportCommand = new RelayCommand<Report>(EmailReport);
            DeleteReportCommand = new RelayCommand<Report>(DeleteReport);
            RefreshCommand = new RelayCommand(async () => await LoadReportsAsync());
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
        }

        private void GenerateReport(object? parameter)
        {
            Console.WriteLine("[ReportsViewModel] Generate Report command executed");
            // Logic to open report generation dialog
            System.Diagnostics.Debug.WriteLine("Generate Report clicked");
        }

        private async Task GenerateMembershipReportAsync()
        {
            try
            {
                Console.WriteLine("[ReportsViewModel] Starting membership report generation");
                IsLoading = true;

                var reportDto = new ReportDto
                {
                    Title = $"Membership Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.MemberStatistics,
                    GeneratedDate = DateTime.Now
                };

                var users = await _userService.GetAllUsersAsync();
                var clubs = await _clubService.GetAllClubsAsync();

                // Generate report content
                var reportContent = GenerateMembershipReportContent(users, clubs);
                reportDto.Content = reportContent;

                var report = new Report
                {
                    Title = reportDto.Title,
                    Type = reportDto.Type,
                    Content = reportDto.Content,
                    GeneratedDate = reportDto.GeneratedDate,
                    Semester = reportDto.Semester,
                    ClubID = reportDto.ClubID,
                    GeneratedByUserID = (await _userService.GetCurrentUserAsync())?.UserID ?? 1
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

                var reportDto = new ReportDto
                {
                    Title = $"Event Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.EventOutcomes,
                    GeneratedDate = DateTime.Now
                };

                var events = await _eventService.GetAllEventsAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events for event report");

                // Generate report content
                var reportContent = GenerateEventReportContent(events);
                reportDto.Content = reportContent;

                var report = new Report
                {
                    Title = reportDto.Title,
                    Type = reportDto.Type,
                    Content = reportDto.Content,
                    GeneratedDate = reportDto.GeneratedDate,
                    Semester = reportDto.Semester,
                    ClubID = reportDto.ClubID,
                    GeneratedByUserID = (await _userService.GetCurrentUserAsync())?.UserID ?? 1
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

                var reportDto = new ReportDto
                {
                    Title = $"Financial Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.SemesterSummary,
                    GeneratedDate = DateTime.Now
                };

                // Generate financial report content (placeholder)
                var reportContent = "Financial Report Content - To be implemented with actual financial data";
                reportDto.Content = reportContent;
                Console.WriteLine("[ReportsViewModel] Generated financial report content (placeholder)");

                var report = new Report
                {
                    Title = reportDto.Title,
                    Type = reportDto.Type,
                    Content = reportDto.Content,
                    GeneratedDate = reportDto.GeneratedDate,
                    Semester = reportDto.Semester,
                    ClubID = reportDto.ClubID,
                    GeneratedByUserID = (await _userService.GetCurrentUserAsync())?.UserID ?? 1
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

                var reportDto = new ReportDto
                {
                    Title = $"Activity Report - {DateTime.Now:yyyy-MM-dd}",
                    Type = ReportType.ActivityTracking,
                    GeneratedDate = DateTime.Now
                };

                var events = await _eventService.GetAllEventsAsync();
                var users = await _userService.GetAllUsersAsync();
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events and {users.Count()} users for activity report");

                // Generate activity report content
                var reportContent = GenerateActivityReportContent(events, users);
                reportDto.Content = reportContent;

                var report = new Report
                {
                    Title = reportDto.Title,
                    Type = reportDto.Type,
                    Content = reportDto.Content,
                    GeneratedDate = reportDto.GeneratedDate,
                    Semester = reportDto.Semester,
                    ClubID = reportDto.ClubID,
                    GeneratedByUserID = (await _userService.GetCurrentUserAsync())?.UserID ?? 1
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
            var content = $"MEMBERSHIP REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";
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

        private string GenerateActivityReportContent(IEnumerable<Event> events, IEnumerable<User> users)
        {
            var content = $"ACTIVITY REPORT\n";
            content += $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            var recentEvents = events.Where(e => e.EventDate >= DateTime.Now.AddDays(-30));
            content += $"Events in Last 30 Days: {recentEvents.Count()}\n";

            var recentUsers = users.Where(u => u.JoinDate >= DateTime.Now.AddDays(-30));
            content += $"New Users in Last 30 Days: {recentUsers.Count()}\n\n";

            content += "RECENT ACTIVITY:\n";
            foreach (var eventItem in recentEvents.OrderByDescending(e => e.EventDate).Take(5))
            {
                content += $"- Event: {eventItem.Name} ({eventItem.EventDate:yyyy-MM-dd})\n";
            }

            return content;
        }

        private void ViewReport(Report? report)
        {
            if (report == null) return;

            Console.WriteLine($"[ReportsViewModel] Viewing report: {report.Title} (ID: {report.ReportID})");
            // Logic to open report viewer window/dialog
            System.Diagnostics.Debug.WriteLine($"View Report: {report.Title}");
        }

        private void DownloadReport(Report? report)
        {
            if (report == null) return;

            Console.WriteLine($"[ReportsViewModel] Downloading report: {report.Title} (ID: {report.ReportID})");
            // Logic to download/export report
            System.Diagnostics.Debug.WriteLine($"Download Report: {report.Title}");
        }

        private void EmailReport(Report? report)
        {
            if (report == null) return;

            Console.WriteLine($"[ReportsViewModel] Emailing report: {report.Title} (ID: {report.ReportID})");
            // Logic to email report
            System.Diagnostics.Debug.WriteLine($"Email Report: {report.Title}");
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

        public override Task LoadAsync()
        {
            return LoadReportsAsync();
        }
    }
}