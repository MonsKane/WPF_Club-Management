using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class EditClubDialog : Window
    {
        public Club? UpdatedClub { get; private set; }
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

            // Update the original club object instead of creating a new one
            _originalClub.Name = ClubNameTextBox.Text.Trim();
            _originalClub.Description = DescriptionTextBox.Text.Trim();
            _originalClub.IsActive = GetSelectedStatus() == "Active";
            _originalClub.FoundedDate = FoundedDatePicker.SelectedDate.GetValueOrDefault(DateTime.Now);

            UpdatedClub = _originalClub;

            this.DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
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