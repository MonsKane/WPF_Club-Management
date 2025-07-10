using ClubManagementApp.ViewModels;
using ClubManagementApp.Views;
using System.Windows;

namespace ClubManagementApp.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public event Action<string>? NotificationRequested;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async void OpenMemberListWindow()
        {
            await ShowWindowAsync<MemberListView, MemberListViewModel>();
        }

        public async void OpenEventManagementWindow()
        {
            await ShowWindowAsync<EventManagementView, EventManagementViewModel>();
        }

        public async void OpenClubManagementWindow()
        {
            await ShowWindowAsync<ClubManagementView, ClubManagementViewModel>();
        }

        public async void OpenReportsWindow()
        {
            await ShowWindowAsync<ReportsView, ReportsViewModel>();
        }

        public void ShowNotification(string message)
        {
            NotificationRequested?.Invoke(message);
        }

        private async Task ShowWindowAsync<TWindow, TViewModel>()
            where TWindow : Window
            where TViewModel : class
        {
            var view = _serviceProvider.GetService(typeof(TWindow)) as TWindow;
            var viewModel = _serviceProvider.GetService(typeof(TViewModel)) as TViewModel;

            if (view == null || viewModel == null)
                throw new InvalidOperationException("Unable to resolve window or view model from DI container.");

            view.DataContext = viewModel;

            if (viewModel is BaseViewModel loadable)
                await loadable.LoadAsync();

            view.Show();
        }
    }
}
