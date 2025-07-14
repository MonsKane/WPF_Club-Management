using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class AddEventDialog : Window
    {
        public Event Event { get; private set; }
        public Event NewEvent { get; private set; }
        private Club _preSelectedClub; // Store the pre-selected club as fallback

        public AddEventDialog()
        {
            InitializeComponent();
            InitializeDefaults();
        }

        public AddEventDialog(IEnumerable<Club> clubs, User user) : this()
        {
            if (user?.Role is UserRole.Chairman)
            {
                ClubComboBox.ItemsSource = clubs.Where(c => c.ClubID == user.ClubID).ToList();
            }
            else
                ClubComboBox.ItemsSource = clubs;
        }

        public AddEventDialog(IEnumerable<Club> clubs, Club? preSelectedClub, User user) : this(clubs, user)
        {
            _preSelectedClub = preSelectedClub; // Store for fallback use

            if (preSelectedClub != null)
            {
                // Set the selected club after the window is loaded to ensure ItemsSource is ready
                this.Loaded += (s, e) =>
                {
                    // Use Dispatcher to ensure UI is fully rendered before setting selection
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Try both SelectedValue and SelectedItem to ensure proper selection
                        ClubComboBox.SelectedValue = preSelectedClub.ClubID;
                        ClubComboBox.SelectedItem = preSelectedClub;
                        ClubComboBox.IsEnabled = false; // Disable club selection when pre-selected
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                };
            }
        }

        private void InitializeDefaults()
        {
            // Set default date to tomorrow
            EventDatePicker.SelectedDate = DateTime.Today.AddDays(1);

            // Set default time to 10:00
            HourComboBox.SelectedIndex = 10; // 10 AM
            MinuteComboBox.SelectedIndex = 0; // 00 minutes

            // Set default registration deadline to event date
            RegistrationDeadlinePicker.SelectedDate = DateTime.Today.AddDays(1);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                try
                {
                    var selectedClub = ClubComboBox.SelectedItem as Club;
                    var selectedStatus = (EventStatus)((ComboBoxItem)StatusComboBox.SelectedItem).Tag;

                    // Combine date and time
                    var eventDate = EventDatePicker.SelectedDate!.Value.Date;
                    var hour = int.Parse(((ComboBoxItem)HourComboBox.SelectedItem).Content.ToString()!);
                    var minute = int.Parse(((ComboBoxItem)MinuteComboBox.SelectedItem).Content.ToString()!);
                    var eventDateTime = eventDate.AddHours(hour).AddMinutes(minute);

                    // Use the selected club (either from ComboBox or pre-selected fallback)
                    var finalSelectedClub = selectedClub ?? _preSelectedClub;

                    NewEvent = new Event
                    {
                        Name = EventNameTextBox.Text.Trim(),
                        Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
                        EventDate = eventDateTime,
                        Location = LocationTextBox.Text.Trim(),
                        ClubID = finalSelectedClub!.ClubID,
                        // Don't set Club navigation property to avoid EF tracking conflicts
                        Status = selectedStatus,
                        CreatedDate = DateTime.Now
                    };

                    // Set max participants if provided
                    if (!string.IsNullOrWhiteSpace(MaxParticipantsTextBox.Text) &&
                        int.TryParse(MaxParticipantsTextBox.Text, out int maxParticipants) &&
                        maxParticipants > 0)
                    {
                        NewEvent.MaxParticipants = maxParticipants;
                    }

                    // Set registration deadline if provided
                    if (RegistrationDeadlinePicker.SelectedDate.HasValue)
                    {
                        NewEvent.RegistrationDeadline = RegistrationDeadlinePicker.SelectedDate.Value;
                    }

                    this.DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating event: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            // Validate event name
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text))
            {
                MessageBox.Show("Please enter an event name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventNameTextBox.Focus();
                return false;
            }

            if (EventNameTextBox.Text.Trim().Length < 3)
            {
                MessageBox.Show("Event name must be at least 3 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventNameTextBox.Focus();
                return false;
            }

            // Validate event date
            if (!EventDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select an event date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventDatePicker.Focus();
                return false;
            }

            // Validate event time
            if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an event time.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HourComboBox.Focus();
                return false;
            }

            // Validate club selection
            if (ClubComboBox.SelectedItem == null && _preSelectedClub == null)
            {
                MessageBox.Show("Please select a club for this event.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ClubComboBox.Focus();
                return false;
            }

            // Validate event date is not in the past
            var selectedDate = EventDatePicker.SelectedDate.Value.Date;
            var hour = int.Parse(((ComboBoxItem)HourComboBox.SelectedItem).Content.ToString()!);
            var minute = int.Parse(((ComboBoxItem)MinuteComboBox.SelectedItem).Content.ToString()!);
            var eventDateTime = selectedDate.AddHours(hour).AddMinutes(minute);

            if (eventDateTime <= DateTime.Now)
            {
                MessageBox.Show("Event date and time must be in the future.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventDatePicker.Focus();
                return false;
            }

            // Validate max participants if provided
            if (!string.IsNullOrWhiteSpace(MaxParticipantsTextBox.Text))
            {
                if (!int.TryParse(MaxParticipantsTextBox.Text, out int maxParticipants) || maxParticipants <= 0)
                {
                    MessageBox.Show("Maximum participants must be a positive number.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    MaxParticipantsTextBox.Focus();
                    return false;
                }
            }

            // Validate registration deadline if provided
            if (RegistrationDeadlinePicker.SelectedDate.HasValue)
            {
                if (RegistrationDeadlinePicker.SelectedDate.Value > eventDateTime)
                {
                    MessageBox.Show("Registration deadline cannot be after the event date.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    RegistrationDeadlinePicker.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}