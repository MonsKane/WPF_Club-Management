using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class AddEventDialog : Window
    {
        public Event? NewEvent { get; private set; }

        public AddEventDialog()
        {
            InitializeComponent();
            InitializeDefaults();
        }

        public AddEventDialog(IEnumerable<Club> clubs) : this()
        {
            ClubComboBox.ItemsSource = clubs;
            
            // Set default club selection to the first club if available
            var clubsList = clubs.ToList();
            if (clubsList.Count > 0)
            {
                ClubComboBox.SelectedIndex = 0;
                Console.WriteLine($"[ADD_EVENT_DIALOG] Default club selected: {clubsList[0].Name}");
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
            Console.WriteLine("[ADD_EVENT_DIALOG] CreateButton_Click called");
            if (ValidateInput())
            {
                Console.WriteLine("[ADD_EVENT_DIALOG] Validation passed, creating event...");
                try
                {
                    var selectedClub = ClubComboBox.SelectedItem as Club;
                    Console.WriteLine($"[ADD_EVENT_DIALOG] Selected club: {selectedClub?.Name ?? "NULL"}");
                    var selectedStatus = (EventStatus)((ComboBoxItem)StatusComboBox.SelectedItem).Tag;
                    Console.WriteLine($"[ADD_EVENT_DIALOG] Selected status: {selectedStatus}");
                    
                    // Combine date and time
                    var eventDate = EventDatePicker.SelectedDate!.Value.Date;
                    var hour = int.Parse(((ComboBoxItem)HourComboBox.SelectedItem).Content.ToString()!);
                    var minute = int.Parse(((ComboBoxItem)MinuteComboBox.SelectedItem).Content.ToString()!);
                    var eventDateTime = eventDate.AddHours(hour).AddMinutes(minute);

                    NewEvent = new Event
                    {
                        Name = EventNameTextBox.Text.Trim(),
                        Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
                        EventDate = eventDateTime,
                        Location = LocationTextBox.Text.Trim(),
                        ClubID = selectedClub!.ClubID,
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

                    Console.WriteLine($"[ADD_EVENT_DIALOG] Event created successfully: {NewEvent.Name}");
                    Console.WriteLine("[ADD_EVENT_DIALOG] Setting DialogResult to true and closing");
                    this.DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ADD_EVENT_DIALOG] Exception occurred: {ex.Message}");
                    Console.WriteLine($"[ADD_EVENT_DIALOG] Exception stack trace: {ex.StackTrace}");
                    MessageBox.Show($"Error creating event: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Console.WriteLine("[ADD_EVENT_DIALOG] Validation failed");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            Console.WriteLine("[ADD_EVENT_DIALOG] Starting validation...");
            
            // Validate event name
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text))
            {
                Console.WriteLine("[ADD_EVENT_DIALOG] Validation failed: Event name is empty");
                MessageBox.Show("Please enter an event name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventNameTextBox.Focus();
                return false;
            }

            if (EventNameTextBox.Text.Trim().Length < 3)
            {
                Console.WriteLine($"[ADD_EVENT_DIALOG] Validation failed: Event name too short ({EventNameTextBox.Text.Trim().Length} chars)");
                MessageBox.Show("Event name must be at least 3 characters long.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventNameTextBox.Focus();
                return false;
            }

            // Validate event date
            if (!EventDatePicker.SelectedDate.HasValue)
            {
                Console.WriteLine("[ADD_EVENT_DIALOG] Validation failed: No event date selected");
                MessageBox.Show("Please select an event date.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventDatePicker.Focus();
                return false;
            }

            // Validate event time
            if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
            {
                Console.WriteLine($"[ADD_EVENT_DIALOG] Validation failed: Time not selected (Hour: {HourComboBox.SelectedItem}, Minute: {MinuteComboBox.SelectedItem})");
                MessageBox.Show("Please select an event time.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HourComboBox.Focus();
                return false;
            }

            // Validate club selection
            if (ClubComboBox.SelectedItem == null)
            {
                Console.WriteLine($"[ADD_EVENT_DIALOG] Validation failed: No club selected (Available clubs: {ClubComboBox.Items.Count})");
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

            Console.WriteLine("[ADD_EVENT_DIALOG] All validations passed!");
            return true;
        }
    }
}