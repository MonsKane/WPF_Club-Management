using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class AddClubDialog : Window
    {
        public Club? CreatedClub { get; private set; }

        public AddClubDialog()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
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

            // Create club object
            CreatedClub = new Club
            {
                Name = ClubNameTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                IsActive = GetSelectedStatus() == "Active",
                FoundedDate = FoundedDatePicker.SelectedDate.GetValueOrDefault(DateTime.Now),
                CreatedDate = DateTime.Now
            };

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