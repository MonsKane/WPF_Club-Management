using ClubManagementApp.Services;
using ClubManagementApp.Views;
using ClubManagementApp.ViewModels;
using System.Windows;

namespace ClubManagementApp.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly IEventService _eventService;
        private readonly IReportService _reportService;
        
        // Event for notification instead of direct dependency on MainViewModel
        public event Action<string>? NotificationRequested;

        public NavigationService(IUserService userService, IClubService clubService, 
                               IEventService eventService, IReportService reportService)
        {
            _userService = userService;
            _clubService = clubService;
            _eventService = eventService;
            _reportService = reportService;
        }

        public void OpenMemberListWindow()
        {
            var memberListWindow = new MemberListView();
            memberListWindow.Show();
        }

        public void OpenEventManagementWindow()
        {
            var eventManagementWindow = new EventManagementView();
            eventManagementWindow.Show();
        }

        public void OpenClubManagementWindow()
        {
            var clubManagementWindow = new ClubManagementView();
            clubManagementWindow.Show();
        }

        public void OpenReportsWindow()
        {
            var reportsWindow = new ReportsView();
            reportsWindow.Show();
        }

        public void ShowNotification(string message)
        {
            NotificationRequested?.Invoke(message);
        }
    }
}