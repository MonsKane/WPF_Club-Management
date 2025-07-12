using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Windows;
using System.Windows.Media;

namespace ClubManagementApp.Views
{
    public partial class ClubDetailsDialog : Window
    {
        private readonly Club _club;
        private readonly NavigationService _navigationService;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;

        public ClubDetailsDialog(Club club, NavigationService navigationService, IUserService userService, IEventService eventService)
        {
            InitializeComponent();
            _club = club ?? throw new ArgumentNullException(nameof(club));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

            LoadClubDetails();
        }

        private async void LoadClubDetails()
        {
            try
            {
                // Basic Information
                ClubNameTextBlock.Text = _club.Name;
                DescriptionTextBlock.Text = _club.Description ?? "No description available";
                FoundedDateTextBlock.Text = _club.FoundedDate?.ToString("MMMM dd, yyyy") ?? "Not specified";
                MeetingScheduleTextBlock.Text = _club.MeetingSchedule ?? "Not specified";
                ContactEmailTextBlock.Text = _club.ContactEmail ?? "Not specified";
                ContactPhoneTextBlock.Text = _club.ContactPhone ?? "Not specified";
                WebsiteTextBlock.Text = _club.Website ?? "Not specified";
                CreatedDateTextBlock.Text = _club.CreatedAt.ToString("MMM yyyy");

                // Status
                SetStatusDisplay(_club.Status);

                // Load Statistics
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading club details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetStatusDisplay(string status)
        {
            StatusTextBlock.Text = status;

            switch (status.ToLower())
            {
                case "active":
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                    break;
                case "inactive":
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
                    break;
                case "suspended":
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(241, 196, 15)); // Yellow
                    break;
                default:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Gray
                    break;
            }
        }

        private async System.Threading.Tasks.Task LoadStatistics()
        {
            try
            {
                // Get member count
                var members = await _userService.GetAllUsersAsync();
                var clubMembers = members.Where(m => m.ClubID == _club.ClubID).ToList();
                MemberCountTextBlock.Text = clubMembers.Count.ToString();

                // Get event statistics
                var events = await _eventService.GetAllEventsAsync();
                var clubEvents = events.Where(e => e.ClubID == _club.ClubID).ToList();
                EventCountTextBlock.Text = clubEvents.Count.ToString();

                // Count active events (events that haven't ended yet)
                var activeEvents = clubEvents.Where(e => e.Status == EventStatus.InProgress).ToList();
                ActiveEventCountTextBlock.Text = activeEvents.Count.ToString();
            }
            catch (Exception ex)
            {
                // Set default values if statistics loading fails
                MemberCountTextBlock.Text = "N/A";
                EventCountTextBlock.Text = "N/A";
                ActiveEventCountTextBlock.Text = "N/A";

                System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
            }
        }

        private void ViewMembersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _navigationService.OpenMemberListWindow(_club);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening member list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewEventsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _navigationService.OpenEventManagementWindow(_club);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening event management: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageLeadershipButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // For now, show a message that this feature is coming soon
                // In a full implementation, this would open a leadership management dialog
                MessageBox.Show($"Leadership management for {_club.Name} will be available in a future update.",
                               "Feature Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening leadership management: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}