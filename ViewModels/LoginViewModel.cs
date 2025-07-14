using ClubManagementApp.Commands;
using ClubManagementApp.Services;

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

        public new bool IsLoading
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
                ShowError("Vui lòng nhập đầy đủ email và mật khẩu.");
                System.Windows.MessageBox.Show("Vui lòng nhập đầy đủ email và mật khẩu.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
                        Console.WriteLine($"[LOGIN] SUCCESS - User authenticated: {user.FullName} ({user.SystemRole})");
                        _userService.SetCurrentUser(user);

                        // Show success dialog
                        System.Windows.MessageBox.Show($"Đăng nhập thành công!\nChào mừng {user.FullName}", "Đăng nhập thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        LoginSuccessful?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Console.WriteLine("[LOGIN] FAILED - User object not found after validation");
                        ShowError("Không tìm thấy thông tin người dùng.");
                        System.Windows.MessageBox.Show("Không tìm thấy thông tin người dùng.", "Lỗi đăng nhập", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                else
                {
                    Console.WriteLine("[LOGIN] FAILED - Invalid credentials");
                    ShowError("Email hoặc mật khẩu không đúng.");
                    System.Windows.MessageBox.Show("Email hoặc mật khẩu không đúng.", "Lỗi đăng nhập", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] FAILED - Exception: {ex.Message}");
                var errorMsg = $"Đăng nhập thất bại: {ex.Message}";
                ShowError(errorMsg);
                System.Windows.MessageBox.Show(errorMsg, "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

        public override Task LoadAsync()
        {
            throw new NotImplementedException();
        }
    }
}