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
                var userService = _serviceProvider.GetRequiredService<IUserService>();
                var eventService = _serviceProvider.GetRequiredService<IEventService>();

                if (userService == null || eventService == null)
                    throw new InvalidOperationException("Unable to resolve required services from DI container.");

                var dialog = new ClubDetailsDialog(club, this, userService, eventService);
                dialog.Owner = Application.Current.MainWindow;
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
                var clubService = _serviceProvider.GetService(typeof(IClubService)) as IClubService;
                var userService = _serviceProvider.GetService(typeof(IUserService)) as IUserService;

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
        }

        public void ShowAddUserDialog()
        {
            try
            {
                var userService = _serviceProvider.GetService(typeof(IUserService)) as IUserService;

                if (userService == null)
                    throw new InvalidOperationException("Unable to resolve required services from DI container.");

                var dialog = new AddUserDialog(userService);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true && dialog.CreatedUser != null)
                {
                    ShowNotification("User created successfully!");
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Error opening add user dialog: {ex.Message}");
            }
        }

        public void ShowEditUserDialog(User user)
        {
            try
            {
                var userService = _serviceProvider.GetRequiredService<IUserService>();
                var clubService = _serviceProvider.GetService(typeof(IClubService)) as IClubService;

                if (userService == null || clubService == null)
                    throw new InvalidOperationException("Unable to resolve required services from DI container.");

                var dialog = new EditUserDialog(user, userService);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true && dialog.UpdatedUser != null)
                {
                    // Save the updated user
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ShowNotification("User updated successfully!");
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Error updating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Error opening edit user dialog: {ex.Message}");
            }
        }

        public void NavigateToLogin()
        {
            try
            {
                // Change shutdown mode to prevent application exit when MainWindow closes
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Create and show the login window first
                var loginViewModel = _serviceProvider.GetService(typeof(LoginViewModel)) as LoginViewModel;
                if (loginViewModel == null)
                    throw new InvalidOperationException("Unable to resolve LoginViewModel from DI container.");

                var loginWindow = new LoginWindow(loginViewModel);

                // Set up login success handler to properly manage application lifecycle
                loginViewModel.LoginSuccessful += async (sender, e) => await OnLoginSuccessfulFromLogout();

                // Set the login window as the new main window before showing it
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();

                // Close all other windows after the new main window is established
                var windowsToClose = Application.Current.Windows.Cast<Window>()
                    .Where(w => w != loginWindow)
                    .ToList();

                foreach (var window in windowsToClose)
                {
                    window.Close();
                }
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

        /// <summary>
        /// Handles login success when navigating from logout to ensure proper application lifecycle management
        /// </summary>
        private async Task OnLoginSuccessfulFromLogout()
        {
            try
            {
                // Get the user service to retrieve current user
                var userService = _serviceProvider.GetService(typeof(IUserService)) as IUserService;
                if (userService == null)
                    throw new InvalidOperationException("Unable to resolve IUserService from DI container.");

                var currentUser = await userService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    // Create new MainViewModel and set current user
                    var mainViewModel = _serviceProvider.GetService(typeof(MainViewModel)) as MainViewModel;
                    if (mainViewModel == null)
                        throw new InvalidOperationException("Unable to resolve MainViewModel from DI container.");

                    mainViewModel.CurrentUser = currentUser;
                    await mainViewModel.LoadAsync();

                    // Create and show main window
                    var mainWindow = new MainWindow(mainViewModel);

                    // Set shutdown mode back to normal behavior
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();

                    // Close login window
                    var loginWindows = Application.Current.Windows.Cast<Window>()
                        .Where(w => w is LoginWindow)
                        .ToList();

                    foreach (var loginWindow in loginWindows)
                    {
                        loginWindow.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Unable to retrieve current user information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during login transition: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
