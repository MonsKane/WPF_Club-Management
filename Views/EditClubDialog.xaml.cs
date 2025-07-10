using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class EditClubDialog : Window
    {
        public Club? UpdatedClub { get; private set; }
        public new bool DialogResult { get; private set; }
        private readonly Club _originalClub;

        public EditClubDialog(Club club)
        {
            InitializeComponent();
            _originalClub = club;
            LoadClubData();
        }

        private void LoadClubData()
        {
            ClubNameTextBox.Text = _originalClub.Name;
            DescriptionTextBox.Text = _originalClub.Description ?? string.Empty;
            FoundedDatePicker.SelectedDate = _originalClub.CreatedDate;
            
            // Set status based on IsActive property
            var status = _originalClub.IsActive ? "Active" : "Inactive";
            for (int i = 0; i < StatusComboBox.Items.Count; i++)
            {
                if (StatusComboBox.Items[i] is ComboBoxItem item && 
                    item.Content?.ToString() == status)
                {
                    StatusComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(ClubNameTextBox.Text))
            {
                MessageBox.Show("Club name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ClubNameTextBox.Focus();
                return;
            }

            if (ClubNameTextBox.Text.Length < 3)
            {
                MessageBox.Show("Club name must be at least 3 characters long.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ClubNameTextBox.Focus();
                return;
            }

            // Create updated club object
            UpdatedClub = new Club
            {
                ClubID = _originalClub.ClubID,
                Name = ClubNameTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                IsActive = GetSelectedStatus() == "Active",
                CreatedDate = _originalClub.CreatedDate,
                Members = _originalClub.Members,
                Events = _originalClub.Events
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string GetSelectedStatus()
        {
            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content?.ToString() ?? "Active";
            }
            return "Active";
        }
    }
}