using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class ManageLeadershipDialog : Window, INotifyPropertyChanged
    {
        private readonly IClubService _clubService;
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private Club _club;
        private readonly ObservableCollection<User> _teamLeaders;
        private readonly ObservableCollection<User> _availableMembers;
        private readonly ObservableCollection<User> _allAvailableMembers = new();
        private User? _chairman;
        private User? _viceChairman;
        private bool _hasChanges = false;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private string _memberFilter = "Current Members"; // "All", "Current Members", "Not in Club"

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(value.ToString());
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(value);
                FilterMembers();
            }
        }

        public string MemberFilter
        {
            get => _memberFilter;
            set
            {
                _memberFilter = value;
                OnPropertyChanged(value);
                FilterMembers();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ManageLeadershipDialog(Club club, IClubService clubService, IUserService userService, INavigationService navigationService)
        {
            _club = club;
            _clubService = clubService;
            _userService = userService;
            _navigationService = navigationService;

            _availableMembers = new ObservableCollection<User>();
            _teamLeaders = new ObservableCollection<User>();

            InitializeComponent();

            DataContext = this;
            Loaded += async (s, e) => await InitializeDialogAsync();
        }

        private async System.Threading.Tasks.Task InitializeDialogAsync()
        {
            try
            {
                IsLoading = true;

                ClubNameText.Text = $"Club: {_club.Name}";

                // Load club members
                var members = await _userService.GetUsersByClubAsync(_club.ClubID);

                // Load users without club membership (removed members who can be reassigned)
                var unassignedUsers = await _userService.GetUsersWithoutClubAsync();

                // Separate current club members by roles
                _chairman = members.FirstOrDefault(m => m.Role == UserRole.Chairman && m.ClubID == _club.ClubID);
                _viceChairman = members.FirstOrDefault(m => m.Role == UserRole.ViceChairman && m.ClubID == _club.ClubID);

                var teamLeaders = members.Where(m => m.Role == UserRole.TeamLeader && m.ClubID == _club.ClubID);
                _teamLeaders.Clear();
                foreach (var leader in teamLeaders)
                {
                    _teamLeaders.Add(leader);
                }

                // Available members: current club members + unassigned users (removed members)
                var availableMembers = members.Where(m =>
                    m.Role == UserRole.Member ||
                    (m.Role == UserRole.TeamLeader && m.ClubID == _club.ClubID) ||
                    (m.Role == UserRole.ViceChairman && m.ClubID == _club.ClubID) ||
                    (m.Role == UserRole.Chairman && m.ClubID == _club.ClubID))
                    .Concat(unassignedUsers); // Include removed members for reassignment

                _allAvailableMembers.Clear();
                foreach (var member in availableMembers.OrderBy(m => m.FullName))
                {
                    _allAvailableMembers.Add(member);
                }

                // Apply initial filtering
                FilterMembers();

                UpdateUI();

                // Set ItemsSource for UI controls
                MembersList.ItemsSource = _availableMembers;
                TeamLeadersList.ItemsSource = _teamLeaders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leadership data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateUI()
        {
            ChairmanText.Text = _chairman?.FullName ?? "Not Assigned";
            ViceChairmanText.Text = _viceChairman?.FullName ?? "Not Assigned";
        }

        private void FilterMembers()
        {
            if (_allAvailableMembers == null) return;

            var filteredMembers = _allAvailableMembers.AsEnumerable();

            // Apply member filter
            switch (_memberFilter)
            {
                case "Current Members":
                    filteredMembers = filteredMembers.Where(m => m.ClubID == _club.ClubID);
                    break;
                case "Not in Club":
                    filteredMembers = filteredMembers.Where(m => m.ClubID == null);
                    break;
                    // "All" - no additional filtering
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filteredMembers = filteredMembers.Where(m =>
                    m.FullName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    m.Email.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(m.StudentID) && m.StudentID.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
            }

            _availableMembers.Clear();
            foreach (var member in filteredMembers.OrderBy(m => m.FullName))
            {
                _availableMembers.Add(member);
            }
        }

        private void ChangeChairman_Click(object sender, RoutedEventArgs e)
        {
            var selectedMember = MembersList.SelectedItem as User;
            if (selectedMember == null)
            {
                MessageBox.Show("Please select a member to assign as Chairman.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedMember == _chairman)
            {
                MessageBox.Show("This member is already the Chairman.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Assign {selectedMember.FullName} as Chairman?\n\nThis will remove the current Chairman role from {_chairman?.FullName ?? "no one"}.",
                "Confirm Assignment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _chairman = selectedMember;
                _hasChanges = true;
                UpdateUI();
            }
        }

        private void ChangeViceChairman_Click(object sender, RoutedEventArgs e)
        {
            var selectedMember = MembersList.SelectedItem as User;
            if (selectedMember == null)
            {
                MessageBox.Show("Please select a member to assign as Vice Chairman.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedMember == _viceChairman)
            {
                MessageBox.Show("This member is already the Vice Chairman.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedMember == _chairman)
            {
                MessageBox.Show("The Chairman cannot also be the Vice Chairman.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Assign {selectedMember.FullName} as Vice Chairman?\n\nThis will remove the current Vice Chairman role from {_viceChairman?.FullName ?? "no one"}.",
                "Confirm Assignment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viceChairman = selectedMember;
                _hasChanges = true;
                UpdateUI();
            }
        }

        private void AddTeamLeader_Click(object sender, RoutedEventArgs e)
        {
            var selectedMember = MembersList.SelectedItem as User;
            if (selectedMember == null)
            {
                MessageBox.Show("Please select a member to assign as Team Leader.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_teamLeaders.Contains(selectedMember))
            {
                MessageBox.Show("This member is already a Team Leader.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedMember == _chairman || selectedMember == _viceChairman)
            {
                MessageBox.Show("Chairman and Vice Chairman cannot also be Team Leaders.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Assign {selectedMember.FullName} as Team Leader?",
                "Confirm Assignment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _teamLeaders.Add(selectedMember);
                _hasChanges = true;
            }
        }

        private void RemoveTeamLeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User teamLeader)
            {
                var result = MessageBox.Show($"Remove {teamLeader.FullName} from Team Leader role?",
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _teamLeaders.Remove(teamLeader);
                    _hasChanges = true;
                }
            }
        }

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasChanges)
            {
                DialogResult = true;
                Close();
                return;
            }

            try
            {
                IsLoading = true;

                // Get current leadership to compare changes
                var allMembers = await _userService.GetUsersByClubAsync(_club.ClubID);
                var currentChairman = allMembers.FirstOrDefault(m => m.Role == UserRole.Chairman && m.ClubID == _club.ClubID);
                var currentViceChairman = allMembers.FirstOrDefault(m => m.Role == UserRole.ViceChairman && m.ClubID == _club.ClubID);
                var currentTeamLeaders = allMembers.Where(m => m.Role == UserRole.TeamLeader && m.ClubID == _club.ClubID).ToList();

                // Reset previous leadership roles to Member (only if they're being changed)
                if (currentChairman != null && currentChairman != _chairman)
                {
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, currentChairman.UserID, UserRole.Member);
                }

                if (currentViceChairman != null && currentViceChairman != _viceChairman)
                {
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, currentViceChairman.UserID, UserRole.Member);
                }

                // Reset team leaders who are no longer in the list
                foreach (var currentTL in currentTeamLeaders)
                {
                    if (!_teamLeaders.Contains(currentTL))
                    {
                        await _clubService.AssignClubLeadershipAsync(_club.ClubID, currentTL.UserID, UserRole.Member);
                    }
                }

                // Assign new leadership roles
                if (_chairman != null)
                {
                    // If user is not in club (ClubID is null), assign them to club first
                    if (_chairman.ClubID == null)
                    {
                        await _userService.AssignUserToClubAsync(_chairman.UserID, _club.ClubID);
                    }
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, _chairman.UserID, UserRole.Chairman);
                }

                if (_viceChairman != null)
                {
                    // If user is not in club (ClubID is null), assign them to club first
                    if (_viceChairman.ClubID == null)
                    {
                        await _userService.AssignUserToClubAsync(_viceChairman.UserID, _club.ClubID);
                    }
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, _viceChairman.UserID, UserRole.ViceChairman);
                }

                foreach (var teamLeader in _teamLeaders)
                {
                    // If user is not in club (ClubID is null), assign them to club first
                    if (teamLeader.ClubID == null)
                    {
                        await _userService.AssignUserToClubAsync(teamLeader.UserID, _club.ClubID);
                    }
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, teamLeader.UserID, UserRole.TeamLeader);
                }

                _navigationService.ShowNotification("Leadership roles updated successfully!");

                // Reset the changes flag
                _hasChanges = false;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving leadership changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to cancel?",
                    "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // The SearchText property will automatically trigger FilterMembers()
            // This event handler is here for any additional logic if needed
        }
    }
}