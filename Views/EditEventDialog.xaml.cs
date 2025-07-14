using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class EditEventDialog : Window
    {
        public Event? UpdatedEvent { get; private set; }
        private readonly Event _originalEvent;
        private readonly User _user;

        public EditEventDialog(Event eventToEdit, IEnumerable<Club> clubs, User user)
        {
            InitializeComponent();
            _originalEvent = eventToEdit;
            ClubComboBox.ItemsSource = clubs;
            if (user?.Role is UserRole.Chairman)
            {
                ClubComboBox.ItemsSource = clubs.Where(c => c.ClubID == user.ClubID).ToList();
            }
            else
                ClubComboBox.ItemsSource = clubs;
            LoadEventData();
        }

        private void LoadEventData()
        {
            EventNameTextBox.Text = _originalEvent.Name;
            DescriptionTextBox.Text = _originalEvent.Description ?? string.Empty;
            EventDatePicker.SelectedDate = _originalEvent.EventDate.Date;

            // Set time
            HourComboBox.SelectedIndex = _originalEvent.EventDate.Hour;
            var minuteIndex = _originalEvent.EventDate.Minute switch
            {
                0 => 0,
                15 => 1,
                30 => 2,
                45 => 3,
                _ => 0
            };
            MinuteComboBox.SelectedIndex = minuteIndex;

            LocationTextBox.Text = _originalEvent.Location ?? string.Empty;

            // Set club selection
            ClubComboBox.SelectedValue = _originalEvent.ClubID;

            MaxParticipantsTextBox.Text = _originalEvent.MaxParticipants?.ToString() ?? string.Empty;
            RegistrationDeadlinePicker.SelectedDate = _originalEvent.RegistrationDeadline;

            // Set status
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if ((EventStatus)item.Tag == _originalEvent.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                try
                {
                    var selectedClub = ClubComboBox.SelectedItem as Club;
                    var selectedStatus = (EventStatus)((ComboBoxItem)StatusComboBox.SelectedItem).Tag;

                    if (selectedClub == null)
                    {
                        MessageBox.Show("Please select a club for this event.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Combine date and time
                    var eventDate = EventDatePicker.SelectedDate!.Value.Date;
                    var hour = int.Parse(((ComboBoxItem)HourComboBox.SelectedItem).Content.ToString()!);
                    var minute = int.Parse(((ComboBoxItem)MinuteComboBox.SelectedItem).Content.ToString()!);
                    var eventDateTime = eventDate.AddHours(hour).AddMinutes(minute);

                    UpdatedEvent = new Event
                    {
                        EventID = _originalEvent.EventID,
                        Name = EventNameTextBox.Text.Trim(),
                        Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
                        EventDate = eventDateTime,
                        Location = string.IsNullOrWhiteSpace(LocationTextBox.Text) ? string.Empty : LocationTextBox.Text.Trim(),
                        ClubID = selectedClub.ClubID,
                        // Don't set Club navigation property to avoid EF tracking conflicts
                        Status = selectedStatus,
                        CreatedDate = _originalEvent.CreatedDate,
                        IsActive = _originalEvent.IsActive
                    };

                    // Set max participants if provided
                    if (!string.IsNullOrWhiteSpace(MaxParticipantsTextBox.Text) &&
                        int.TryParse(MaxParticipantsTextBox.Text, out int maxParticipants) &&
                        maxParticipants > 0)
                    {
                        UpdatedEvent.MaxParticipants = maxParticipants;
                    }
                    else
                    {
                        UpdatedEvent.MaxParticipants = null;
                    }

                    // Set registration deadline if provided
                    if (RegistrationDeadlinePicker.SelectedDate.HasValue)
                    {
                        UpdatedEvent.RegistrationDeadline = RegistrationDeadlinePicker.SelectedDate.Value;
                    }
                    else
                    {
                        UpdatedEvent.RegistrationDeadline = null;
                    }

                    // Copy participants from original event
                    UpdatedEvent.Participants = _originalEvent.Participants;

                    this.DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating event: {ex.Message}", "Error",
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
            if (ClubComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a club for this event.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ClubComboBox.Focus();
                return false;
            }

            // For past events, only allow status and description changes
            var selectedDate = EventDatePicker.SelectedDate.Value.Date;
            var hour = int.Parse(((ComboBoxItem)HourComboBox.SelectedItem).Content.ToString()!);
            var minute = int.Parse(((ComboBoxItem)MinuteComboBox.SelectedItem).Content.ToString()!);
            var eventDateTime = selectedDate.AddHours(hour).AddMinutes(minute);

            if (eventDateTime <= DateTime.Now && _originalEvent.EventDate > DateTime.Now)
            {
                MessageBox.Show("Cannot change event date to the past for future events.", "Validation Error",
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
