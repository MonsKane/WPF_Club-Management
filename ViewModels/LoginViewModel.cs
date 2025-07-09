using ClubManagementApp.Commands;
using ClubManagementApp.Services;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClubManagementApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private bool _isLoading;

        public LoginViewModel(IUserService userService)
        {
            _userService = userService;
            LoginCommand = new RelayCommand(async () => await LoginAsync(), () => !IsLoading);
        }

        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                ClearError();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ClearError();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public RelayCommand LoginCommand { get; }

        public event EventHandler? LoginSuccessful;

        private async Task LoginAsync()
        {
            Console.WriteLine($"[LOGIN] Attempting login for email: {Email}");
            
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                Console.WriteLine("[LOGIN] FAILED - Missing email or password");
                ErrorMessage = "Please enter both email and password.";
                return;
            }

            IsLoading = true;
            HasError = false;
            Console.WriteLine("[LOGIN] Starting authentication process...");

            try
            {
                var isValid = await _userService.ValidateUserCredentialsAsync(Email, Password);
                Console.WriteLine($"[LOGIN] Credential validation result: {isValid}");
                
                if (isValid)
                {
                    var user = await _userService.GetUserByEmailAsync(Email);
                    if (user != null)
                    {
                        Console.WriteLine($"[LOGIN] SUCCESS - User authenticated: {user.FullName} ({user.Role})");
                        _userService.SetCurrentUser(user);
                        LoginSuccessful?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Console.WriteLine("[LOGIN] FAILED - User object not found after validation");
                        ErrorMessage = "User not found.";
                    }
                }
                else
                {
                    Console.WriteLine("[LOGIN] FAILED - Invalid credentials");
                    ErrorMessage = "Invalid email or password.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] FAILED - Exception: {ex.Message}");
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                Console.WriteLine("[LOGIN] Authentication process completed");
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
    }
}