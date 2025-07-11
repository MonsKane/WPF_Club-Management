using ClubManagementApp.Models;
using ClubManagementApp.ViewModels;
using ClubManagementApp.Views;
using Microsoft.Extensions.DependencyInjection;
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

        public async void OpenMemberListWindow(Club club)
        {
            await ShowWindowAsync<MemberListView, MemberListViewModel>(vm =>
            {
                if (vm is MemberListViewModel memberVM)
                {
                    memberVM.SetClubFilter(club);
                }
            });
        }

        public async void OpenEventManagementWindow()
        {
            await ShowWindowAsync<EventManagementView, EventManagementViewModel>();
        }

        public async void OpenEventManagementWindow(Club club)
        {
            await ShowWindowAsync<EventManagementView, EventManagementViewModel>(vm =>
            {
                if (vm is EventManagementViewModel eventVM)
                {
                    eventVM.SetClubFilter(club);
                }
            });
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

        public void ShowClubDetails(Club club)
        {
            try
            {
                var userService = _serviceProvider.GetService(typeof(UserService)) as UserService;
                var eventService = _serviceProvider.GetService(typeof(EventService)) as EventService;

                if (userService == null || eventService == null)
                    throw new InvalidOperationException("Unable to resolve required services from DI container.");

                var dialog = new ClubDetailsDialog(club, this, userService, eventService);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening club details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowManageLeadership(Club club)
        {
            try
            {
                var clubService = _serviceProvider.GetService(typeof(ClubService)) as ClubService;
                var userService = _serviceProvider.GetService(typeof(UserService)) as UserService;

                if (clubService == null || userService == null)
                    throw new InvalidOperationException("Unable to resolve required services from DI container.");

                var dialog = new ManageLeadershipDialog(club, clubService, userService, this);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowNotification($"Error opening leadership management: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public void NavigateToLogin()
        {
            try
            {
                // Close the current main window
                Application.Current.MainWindow?.Close();

                // Create and show the login window
                var loginViewModel = _serviceProvider.GetService(typeof(LoginViewModel)) as LoginViewModel;
                if (loginViewModel == null)
                    throw new InvalidOperationException("Unable to resolve LoginViewModel from DI container.");

                var loginWindow = new LoginWindow(loginViewModel);
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating to login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowWindowAsync<TWindow, TViewModel>(Action<TViewModel>? configureViewModel = null)
            where TWindow : Window
            where TViewModel : class
        {
            var view = _serviceProvider.GetService(typeof(TWindow)) as TWindow;
            var viewModel = _serviceProvider.GetService(typeof(TViewModel)) as TViewModel;

            if (view == null || viewModel == null)
                throw new InvalidOperationException("Unable to resolve window or view model from DI container.");

            view.DataContext = viewModel;

            // Configure the view model if a configuration action is provided
            configureViewModel?.Invoke(viewModel);

            if (viewModel is BaseViewModel loadable)
                await loadable.LoadAsync();

            view.Show();
        }
    }
}
