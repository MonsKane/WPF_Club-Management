using ClubManagementApp.Models;
using ClubManagementApp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ClubManagementApp.Views
{
    public partial class ManageLeadershipDialog : Window
    {
        private readonly IClubService _clubService;
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private Club _club;
        private ObservableCollection<User> _availableMembers;
        private ObservableCollection<User> _teamLeaders;
        private User? _chairman;
        private User? _viceChairman;
        private bool _hasChanges = false;

        public ManageLeadershipDialog(Club club, IClubService clubService, IUserService userService, INavigationService navigationService)
        {
            InitializeComponent();
            _club = club;
            _clubService = clubService;
            _userService = userService;
            _navigationService = navigationService;
            _availableMembers = new ObservableCollection<User>();
            _teamLeaders = new ObservableCollection<User>();
            
            InitializeDialog();
        }

        private async void InitializeDialog()
        {
            try
            {
                ClubNameText.Text = $"Club: {_club.Name}";
                
                // Load club members
                var members = await _userService.GetUsersByClubAsync(_club.ClubID);
                
                // Separate members by roles
                _chairman = members.FirstOrDefault(m => m.Role == UserRole.Chairman && m.ClubID == _club.ClubID);
                _viceChairman = members.FirstOrDefault(m => m.Role == UserRole.ViceChairman && m.ClubID == _club.ClubID);
                
                var teamLeaders = members.Where(m => m.Role == UserRole.TeamLeader && m.ClubID == _club.ClubID);
                _teamLeaders.Clear();
                foreach (var leader in teamLeaders)
                {
                    _teamLeaders.Add(leader);
                }
                
                // Available members (excluding current leadership)
                var availableMembers = members.Where(m => 
                    m.Role == UserRole.Member || 
                    (m.Role == UserRole.TeamLeader && m.ClubID == _club.ClubID) ||
                    (m.Role == UserRole.ViceChairman && m.ClubID == _club.ClubID) ||
                    (m.Role == UserRole.Chairman && m.ClubID == _club.ClubID));
                
                _availableMembers.Clear();
                foreach (var member in availableMembers)
                {
                    _availableMembers.Add(member);
                }
                
                UpdateUI();
                
                MembersList.ItemsSource = _availableMembers;
                TeamLeadersList.ItemsSource = _teamLeaders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading leadership data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            ChairmanText.Text = _chairman?.FullName ?? "Not Assigned";
            ViceChairmanText.Text = _viceChairman?.FullName ?? "Not Assigned";
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
                // Reset all members to Member role first
                var allMembers = await _userService.GetUsersByClubAsync(_club.ClubID);
                foreach (var member in allMembers)
                {
                    if (member.Role != UserRole.Admin && member.ClubID == _club.ClubID)
                    {
                        await _clubService.AssignClubLeadershipAsync(_club.ClubID, member.UserID, UserRole.Member);
                    }
                }

                // Assign new leadership roles
                if (_chairman != null)
                {
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, _chairman.UserID, UserRole.Chairman);
                }

                if (_viceChairman != null)
                {
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, _viceChairman.UserID, UserRole.ViceChairman);
                }

                foreach (var teamLeader in _teamLeaders)
                {
                    await _clubService.AssignClubLeadershipAsync(_club.ClubID, teamLeader.UserID, UserRole.TeamLeader);
                }

                _navigationService.ShowNotification("Leadership roles updated successfully!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving leadership changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}