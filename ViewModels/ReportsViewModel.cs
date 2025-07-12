using ClubManagementApp.Commands;
using ClubManagementApp.Helpers;
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
        public bool CanAccessReports => CurrentUser != null && _authorizationService.CanAccessFeature(CurrentUser.Role, "ReportView");
        public bool CanGenerateReports => CurrentUser != null && _authorizationService.CanGenerateReports(CurrentUser.Role);
        public bool CanExportReports => CurrentUser != null && _authorizationService.CanExportReports(CurrentUser.Role);

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
            GenerateEventReportCommand = new RelayCommand(async () => await GenerateEventReportAsync(), CanGenerateReport);
            GenerateFinancialReportCommand = new RelayCommand(async () => await GenerateFinancialReportAsync(), CanGenerateReport);
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
            (CurrentUser?.Role == UserRole.SystemAdmin || report.GeneratedByUserID == CurrentUser?.UserID);
        private bool CanExportReport(Report? report) => report != null && CanExportReports;
        private bool CanDeleteReport(Report? report) => report != null && CanGenerateReports &&
            (CurrentUser?.Role == UserRole.SystemAdmin || report.GeneratedByUserID == CurrentUser?.UserID);

        // Load current user method
        public async Task LoadCurrentUserAsync()
        {
            try
            {
                CurrentUser = await _userService.GetCurrentUserAsync();
                Console.WriteLine($"[ReportsViewModel] Current user loaded: {CurrentUser?.FullName} (Role: {CurrentUser?.Role})");
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
            if (SelectedReportType != "All Types")
            {
                var reportType = SelectedReportType switch
                {
                    "Membership" => ReportType.MemberStatistics,
                    "Events" => ReportType.EventOutcomes,
                    "Financial" => ReportType.SemesterSummary,
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

        private async void GenerateReport()
        {
            Console.WriteLine("[ReportsViewModel] GenerateReport called");

            try
            {
                var dialog = new Views.CustomReportDialog();
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
                        await GenerateMembershipReportAsync();
                        break;
                    case ReportType.EventOutcomes:
                        await GenerateEventReportAsync();
                        break;
                    case ReportType.SemesterSummary:
                        await GenerateFinancialReportAsync();
                        break;
                    case ReportType.ActivityTracking:
                        await GenerateActivityReportAsync();
                        break;
                    default:
                        await GenerateMembershipReportAsync();
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

                // Generate report content
                var reportContent = GenerateMembershipReportContent(users, clubs);

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
                Console.WriteLine($"[ReportsViewModel] Retrieved {events.Count()} events and {users.Count()} users for activity report");

                // Generate activity report content
                var reportContent = GenerateActivityReportContent(events, users);

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
                    BorderThickness = new System.Windows.Thickness(1)
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
                        var index = Reports.IndexOf(Reports.FirstOrDefault(r => r.ReportID == report.ReportID));
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