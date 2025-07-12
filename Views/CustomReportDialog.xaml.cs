using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class CustomReportDialog : Window
    {
        private List<Club>? _clubs;
        private IClubService _clubService;
        private bool _isInitialized = false;

        public class CustomReportParameters
        {
            public string Title { get; set; } = string.Empty;
            public ReportType Type { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            public int? SelectedClubId { get; set; } = default;
        }

        public CustomReportParameters? ReportParameters { get; private set; }

        public CustomReportDialog(IClubService clubService)
        {
            _clubService = clubService;
            InitializeComponent();
            InitializeDefaults();
            _isInitialized = true; // Set this last
        }

        private void InitializeDefaults()
        {
            // Set default title with current date
            ReportTitleTextBox.Text = $"Custom Report - {DateTime.Now:yyyy-MM-dd}";

            // Set default date range to current month
            var now = DateTime.Now;
            StartDatePicker.SelectedDate = new DateTime(now.Year, now.Month, 1);
            EndDatePicker.SelectedDate = now;

            // Set default report type
            ReportTypeComboBox.SelectedIndex = 0;

            Console.WriteLine("[CustomReportDialog] Initialized with default values");
        }

        private async void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent event from firing too early
            if (!_isInitialized || ReportTypeComboBox.SelectedItem == null)
                return;

            var selectedItem = ReportTypeComboBox.SelectedItem as ComboBoxItem;

            if (selectedItem != null && selectedItem.Content?.ToString() == "Membership Statistics")
            {
                ClubSelectionPanel.Visibility = Visibility.Visible;

                // Populate ClubComboBox
                ClubComboBox.Items.Clear();
                ClubComboBox.Items.Add(new ComboBoxItem
                {
                    Content = "All Clubs",
                    Tag = null,
                    IsSelected = true
                });

                if (_clubService != null)
                {
                    _clubs = (List<Club>?)await _clubService.GetAllClubsAsync();

                    if (_clubs is not null)
                        foreach (var club in _clubs)
                        {
                            ClubComboBox.Items.Add(new ComboBoxItem
                            {
                                Content = club.Name,
                                Tag = club.ClubID
                            });
                        }
                }
            }
            else
            {
                ClubSelectionPanel.Visibility = Visibility.Collapsed;
            }
        }



        private int? GetSelectedClubId()
        {
            if (ClubComboBox.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Tag?.ToString(), out int clubId))
            {
                return clubId;
            }

            return null;
        }


        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[CustomReportDialog] Generate button clicked");

            if (ValidateInput())
            {
                Console.WriteLine("[CustomReportDialog] Validation passed, creating report parameters");

                try
                {
                    var selectedReportType = GetSelectedReportType();

                    ReportParameters = new CustomReportParameters
                    {
                        Title = ReportTitleTextBox.Text.Trim(),
                        Type = selectedReportType,
                        StartDate = StartDatePicker.SelectedDate,
                        EndDate = EndDatePicker.SelectedDate,
                        SelectedClubId = selectedReportType == ReportType.MemberStatistics ? GetSelectedClubId() : null
                    };

                    Console.WriteLine($"[CustomReportDialog] Report parameters created: {ReportParameters.Title} ({ReportParameters.Type})");

                    this.DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CustomReportDialog] Error creating report parameters: {ex.Message}");
                    MessageBox.Show($"Error creating report parameters: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Console.WriteLine("[CustomReportDialog] Validation failed");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[CustomReportDialog] Cancel button clicked");
            this.DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            Console.WriteLine("[CustomReportDialog] Starting validation...");

            // Validate report title
            if (string.IsNullOrWhiteSpace(ReportTitleTextBox.Text))
            {
                Console.WriteLine("[CustomReportDialog] Validation failed: Report title is empty");
                MessageBox.Show("Please enter a report title.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReportTitleTextBox.Focus();
                return false;
            }

            if (ReportTitleTextBox.Text.Trim().Length < 3)
            {
                Console.WriteLine($"[CustomReportDialog] Validation failed: Report title too short ({ReportTitleTextBox.Text.Trim().Length} chars)");
                MessageBox.Show("Report title must be at least 3 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReportTitleTextBox.Focus();
                return false;
            }

            // Validate report type selection
            if (ReportTypeComboBox.SelectedItem == null)
            {
                Console.WriteLine("[CustomReportDialog] Validation failed: No report type selected");
                MessageBox.Show("Please select a report type.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReportTypeComboBox.Focus();
                return false;
            }

            // Validate date range
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
                {
                    Console.WriteLine("[CustomReportDialog] Validation failed: Start date is after end date");
                    MessageBox.Show("Start date cannot be after end date.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartDatePicker.Focus();
                    return false;
                }

                // Check if date range is too large (more than 2 years)
                var dateRange = EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value;
                if (dateRange.TotalDays > 730) // 2 years
                {
                    Console.WriteLine($"[CustomReportDialog] Validation failed: Date range too large ({dateRange.TotalDays} days)");
                    MessageBox.Show("Date range cannot exceed 2 years.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartDatePicker.Focus();
                    return false;
                }
            }

            Console.WriteLine("[CustomReportDialog] Validation passed");
            return true;
        }

        private ReportType GetSelectedReportType()
        {
            var selectedItem = ReportTypeComboBox.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString();

            return tag switch
            {
                "MemberStatistics" => ReportType.MemberStatistics,
                "ActivityTracking" => ReportType.ActivityTracking,
                _ => ReportType.MemberStatistics // Default fallback
            };
        }
    }
}