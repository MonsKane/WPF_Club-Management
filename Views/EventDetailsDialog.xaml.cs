using ClubManagementApp.Models;
using System;
using System.Windows;
using System.Windows.Media;

namespace ClubManagementApp.Views
{
    /// <summary>
    /// Interaction logic for EventDetailsDialog.xaml
    /// </summary>
    public partial class EventDetailsDialog : Window
    {
        public Event Event { get; private set; }

        public EventDetailsDialog(Event eventItem)
        {
            InitializeComponent();
            Event = eventItem ?? throw new ArgumentNullException(nameof(eventItem));
            LoadEventData();
        }

        private void LoadEventData()
        {
            try
            {
                // Basic event information
                EventNameText.Text = Event.Name ?? "N/A";
                DescriptionText.Text = string.IsNullOrWhiteSpace(Event.Description) ? "No description available" : Event.Description;
                EventDateText.Text = Event.EventDate.ToString("dddd, MMMM dd, yyyy 'at' HH:mm");
                LocationText.Text = string.IsNullOrWhiteSpace(Event.Location) ? "Location TBD" : Event.Location;
                ClubText.Text = Event.Club?.Name ?? "No club assigned";
                
                // Participant information
                MaxParticipantsText.Text = Event.MaxParticipants?.ToString() ?? "Unlimited";
                CurrentParticipantsText.Text = Event.Participants?.Count.ToString() ?? "0";
                
                // Registration deadline
                RegistrationDeadlineText.Text = Event.RegistrationDeadline?.ToString("dddd, MMMM dd, yyyy") ?? "No deadline set";
                
                // Created date
                CreatedDateText.Text = Event.CreatedDate.ToString("dddd, MMMM dd, yyyy 'at' HH:mm");
                
                // Status with color coding
                SetStatusDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading event data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetStatusDisplay()
        {
            StatusText.Text = Event.Status.ToString();
            
            // Set status color based on event status
            switch (Event.Status)
            {
                case EventStatus.Scheduled:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7b, 0xff)); // Blue
                    StatusText.Foreground = Brushes.White;
                    break;
                case EventStatus.InProgress:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(0xff, 0x8c, 0x00)); // Orange
                    StatusText.Foreground = Brushes.White;
                    break;
                case EventStatus.Completed:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(0x28, 0xa7, 0x45)); // Green
                    StatusText.Foreground = Brushes.White;
                    break;
                case EventStatus.Cancelled:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(0xdc, 0x35, 0x45)); // Red
                    StatusText.Foreground = Brushes.White;
                    break;
                case EventStatus.Postponed:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(0x6c, 0x75, 0x7d)); // Gray
                    StatusText.Foreground = Brushes.White;
                    break;
                default:
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(0x6c, 0x75, 0x7d)); // Default gray
                    StatusText.Foreground = Brushes.White;
                    break;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}