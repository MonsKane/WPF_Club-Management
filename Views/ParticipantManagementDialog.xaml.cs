using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClubManagementApp.Models;
using ClubManagementApp.Services;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace ClubManagementApp.Views
{
    public partial class ParticipantManagementDialog : Window
    {
        private readonly IEventService _eventService;
        private readonly Event _event;
        private List<EventParticipant> _participants = new List<EventParticipant>();

        public ParticipantManagementDialog(Event eventItem, IEventService eventService)
        {
            InitializeComponent();
            _event = eventItem ?? throw new ArgumentNullException(nameof(eventItem));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            
            LoadEventData();
            _ = LoadParticipantsAsync();
        }

        private void LoadEventData()
        {
            EventTitleText.Text = $"Event: {_event.Name} | Date: {_event.EventDate:MMM dd, yyyy}";
        }

        private async Task LoadParticipantsAsync()
        {
            try
            {
                _participants = (await _eventService.GetEventParticipantsAsync(_event.EventID)).ToList();
                ParticipantsDataGrid.ItemsSource = _participants;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading participants: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_participants == null) return;

            var total = _participants.Count;
            var registered = _participants.Count(p => p.Status == AttendanceStatus.Registered);
            var attended = _participants.Count(p => p.Status == AttendanceStatus.Attended);
            var absent = _participants.Count(p => p.Status == AttendanceStatus.Absent);

            TotalParticipantsText.Text = total.ToString();
            RegisteredParticipantsText.Text = registered.ToString();
            AttendedParticipantsText.Text = attended.ToString();
            AbsentParticipantsText.Text = absent.ToString();
        }

        private async void MarkAttended_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EventParticipant participant)
            {
                try
                {
                    if (participant.Status == AttendanceStatus.Attended)
                    {
                        MessageBox.Show("This participant is already marked as attended.", "Information", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    if (participant.Status == AttendanceStatus.Absent)
                    {
                        var result = MessageBox.Show("This participant is marked as absent. Do you want to mark them as attended anyway?", 
                            "Confirm Action", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    await _eventService.UpdateParticipantStatusAsync(_event.EventID, participant.UserID, AttendanceStatus.Attended);
                    
                    // Update local data
                    participant.Status = AttendanceStatus.Attended;
                    participant.AttendanceDate = DateTime.Now;
                    
                    // Refresh the DataGrid
                    ParticipantsDataGrid.Items.Refresh();
                    UpdateStatistics();
                    
                    MessageBox.Show("Participant marked as attended successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating participant status: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CancelParticipation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EventParticipant participant)
            {
                try
                {
                    if (participant.Status == AttendanceStatus.Absent)
                    {
                        MessageBox.Show("This participant is already marked as absent.", "Information", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var result = MessageBox.Show($"Are you sure you want to mark {participant.User?.FullName ?? "this participant"} as absent?", 
                        "Confirm Action", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        await _eventService.UpdateParticipantStatusAsync(_event.EventID, participant.UserID, AttendanceStatus.Absent);
                        
                        // Update local data
                        participant.Status = AttendanceStatus.Absent;
                        participant.AttendanceDate = null;
                        
                        // Refresh the DataGrid
                        ParticipantsDataGrid.Items.Refresh();
                        UpdateStatistics();
                        
                        MessageBox.Show("Participant marked as absent successfully!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating participant status: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_participants == null || !_participants.Any())
                {
                    MessageBox.Show("No participants to export.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt",
                    DefaultExt = "csv",
                    FileName = $"Event_{_event.Name}_Participants_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Name,Email,Registration Date,Status,Attendance Date");

                    foreach (var participant in _participants)
                    {
                        csv.AppendLine($"\"{participant.User?.FullName ?? "N/A"}\"," +
                                     $"\"{participant.User?.Email ?? "N/A"}\"," +
                                     $"\"{participant.RegistrationDate:yyyy-MM-dd}\"," +
                                     $"\"{participant.Status}\"," +
                                     $"\"{(participant.AttendanceDate?.ToString("yyyy-MM-dd") ?? "N/A")}\"");
                    }

                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Participants list exported successfully to:\n{saveFileDialog.FileName}", "Export Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting participants list: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadParticipantsAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
