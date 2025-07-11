using ClubManagementApp.ViewModels;
using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface INavigationService
    {
        void OpenMemberListWindow();
        void OpenMemberListWindow(Club club);
        void OpenEventManagementWindow();
        void OpenEventManagementWindow(Club club);
        void OpenClubManagementWindow();
        void OpenReportsWindow();
        void ShowNotification(string message);
        void ShowClubDetails(Club club);
        void ShowManageLeadership(Club club);
        void ShowAddUserDialog();
        void ShowEditUserDialog(User user);
        void NavigateToLogin();

        // Event for notification instead of direct dependency on MainViewModel
        event Action<string>? NotificationRequested;
    }
}
