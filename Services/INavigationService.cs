using ClubManagementApp.ViewModels;

namespace ClubManagementApp.Services
{
    public interface INavigationService
    {
        void OpenMemberListWindow();
        void OpenEventManagementWindow();
        void OpenClubManagementWindow();
        void OpenReportsWindow();
        void ShowNotification(string message);
    }
}