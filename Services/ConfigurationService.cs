using System.IO;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    public interface IConfigurationService
    {
        T GetValue<T>(string key, T defaultValue = default!);
        void SetValue<T>(string key, T value);
        Task SetAsync<T>(string key, T value);
        bool HasKey(string key);
        void RemoveKey(string key);
        Task SaveAsync();
        Task LoadAsync();
        void ResetToDefaults();
        Task<Dictionary<string, object>> GetAllAsync();
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private readonly Dictionary<string, object> _settings;
        private readonly object _lockObject = new object();

        public ConfigurationService()
        {
            _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "ClubManagementApp", "config.json");
            _settings = new Dictionary<string, object>();
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Load default settings
            LoadDefaults();
        }

        public T GetValue<T>(string key, T defaultValue = default!)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    System.Diagnostics.Debug.WriteLine("Configuration key cannot be null or empty");
                    return defaultValue;
                }

                lock (_lockObject)
                {
                    if (_settings.TryGetValue(key, out var value))
                    {
                        try
                        {
                            if (value is JsonElement jsonElement)
                            {
                                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                            }
                            return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to convert configuration value for key '{key}': {ex.Message}");
                            return defaultValue;
                        }
                    }
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting configuration value for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    System.Diagnostics.Debug.WriteLine("Configuration key cannot be null or empty");
                    return;
                }

                lock (_lockObject)
                {
                    _settings[key] = value!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting configuration value for key '{key}': {ex.Message}");
            }
        }

        public async Task SetAsync<T>(string key, T value)
        {
            SetValue(key, value);
            await SaveAsync();
        }

        public bool HasKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return false;
                }

                lock (_lockObject)
                {
                    return _settings.ContainsKey(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if configuration key exists '{key}': {ex.Message}");
                return false;
            }
        }

        public void RemoveKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    System.Diagnostics.Debug.WriteLine("Configuration key cannot be null or empty");
                    return;
                }

                lock (_lockObject)
                {
                    _settings.Remove(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing configuration key '{key}': {ex.Message}");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                Dictionary<string, object> settingsCopy;
                lock (_lockObject)
                {
                    settingsCopy = new Dictionary<string, object>(_settings);
                }

                var json = JsonSerializer.Serialize(settingsCopy, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent application crashes
                System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    
                    if (loadedSettings != null)
                    {
                        lock (_lockObject)
                        {
                            _settings.Clear();
                            foreach (var kvp in loadedSettings)
                            {
                                _settings[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and continue with defaults
                System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
                LoadDefaults();
            }
        }

        public void ResetToDefaults()
        {
            lock (_lockObject)
            {
                _settings.Clear();
                LoadDefaults();
            }
        }

        private void LoadDefaults()
        {
            // Application Settings
            _settings["App.Theme"] = "Light";
            _settings["App.Language"] = "English";
            _settings["App.AutoSave"] = true;
            _settings["App.AutoSaveInterval"] = 300; // seconds
            
            // Database Settings
            _settings["Database.ConnectionTimeout"] = 30;
            _settings["Database.CommandTimeout"] = 60;
            _settings["Database.EnableRetry"] = true;
            _settings["Database.MaxRetryAttempts"] = 3;
            
            // Email Settings
            _settings["Email.SmtpServer"] = "smtp.gmail.com";
            _settings["Email.SmtpPort"] = 587;
            _settings["Email.EnableSsl"] = true;
            _settings["Email.SenderName"] = "Club Management System";
            _settings["Email.SenderEmail"] = "";
            _settings["Email.Username"] = "";
            _settings["Email.Password"] = "";
            
            // Notification Settings
            _settings["Notifications.EnableEmail"] = true;
            _settings["Notifications.EnableInApp"] = true;
            _settings["Notifications.EventReminder.DaysBefore"] = 3;
            _settings["Notifications.EventReminder.HoursBefore"] = 2;
            
            // Security Settings
            _settings["Security.PasswordMinLength"] = 8;
            _settings["Security.RequireSpecialCharacters"] = true;
            _settings["Security.RequireNumbers"] = true;
            _settings["Security.RequireUppercase"] = true;
            _settings["Security.RequireLowercase"] = true;
            _settings["Security.SessionTimeoutMinutes"] = 60;
            _settings["Security.MaxLoginAttempts"] = 5;
            _settings["Security.LockoutDurationMinutes"] = 15;
            
            // Report Settings
            _settings["Reports.DefaultFormat"] = "PDF";
            _settings["Reports.IncludeLogo"] = true;
            _settings["Reports.AutoGenerate"] = false;
            _settings["Reports.RetentionDays"] = 365;
            
            // Event Settings
            _settings["Events.DefaultDuration"] = 120; // minutes
            _settings["Events.AllowLateRegistration"] = true;
            _settings["Events.LateRegistrationHours"] = 2;
            _settings["Events.AutoMarkAbsent"] = true;
            _settings["Events.AbsentMarkingHours"] = 24;
            
            // Club Settings
            _settings["Club.MaxMembersPerClub"] = 500;
            _settings["Club.RequireApprovalForJoining"] = false;
            _settings["Club.AllowMultipleClubMembership"] = false;
            
            // UI Settings
            _settings["UI.ItemsPerPage"] = 20;
            _settings["UI.ShowWelcomeScreen"] = true;
            _settings["UI.EnableAnimations"] = true;
            _settings["UI.DefaultView"] = "Dashboard";
            
            // Logging Settings
            _settings["Logging.Level"] = "Information";
            _settings["Logging.EnableFileLogging"] = false;
            _settings["Logging.MaxLogFileSize"] = 10; // MB
            _settings["Logging.LogRetentionDays"] = 30;
        }

        public async Task<Dictionary<string, object>> GetAllAsync()
        {
            await Task.CompletedTask; // Make it async for consistency
            lock (_lockObject)
            {
                return new Dictionary<string, object>(_settings);
            }
        }
    }

    // Configuration keys as constants for type safety
    public static class ConfigurationKeys
    {
        public static class App
        {
            public const string Theme = "App.Theme";
            public const string Language = "App.Language";
            public const string AutoSave = "App.AutoSave";
            public const string AutoSaveInterval = "App.AutoSaveInterval";
        }

        public static class Database
        {
            public const string ConnectionTimeout = "Database.ConnectionTimeout";
            public const string CommandTimeout = "Database.CommandTimeout";
            public const string EnableRetry = "Database.EnableRetry";
            public const string MaxRetryAttempts = "Database.MaxRetryAttempts";
        }

        public static class Email
        {
            public const string SmtpServer = "Email.SmtpServer";
            public const string SmtpPort = "Email.SmtpPort";
            public const string EnableSsl = "Email.EnableSsl";
            public const string SenderName = "Email.SenderName";
            public const string SenderEmail = "Email.SenderEmail";
            public const string Username = "Email.Username";
            public const string Password = "Email.Password";
        }

        public static class Notifications
        {
            public const string EnableEmail = "Notifications.EnableEmail";
            public const string EnableInApp = "Notifications.EnableInApp";
            public const string EventReminderDaysBefore = "Notifications.EventReminder.DaysBefore";
            public const string EventReminderHoursBefore = "Notifications.EventReminder.HoursBefore";
        }

        public static class Security
        {
            public const string PasswordMinLength = "Security.PasswordMinLength";
            public const string RequireSpecialCharacters = "Security.RequireSpecialCharacters";
            public const string RequireNumbers = "Security.RequireNumbers";
            public const string RequireUppercase = "Security.RequireUppercase";
            public const string RequireLowercase = "Security.RequireLowercase";
            public const string SessionTimeoutMinutes = "Security.SessionTimeoutMinutes";
            public const string MaxLoginAttempts = "Security.MaxLoginAttempts";
            public const string LockoutDurationMinutes = "Security.LockoutDurationMinutes";
        }

        public static class Reports
        {
            public const string DefaultFormat = "Reports.DefaultFormat";
            public const string IncludeLogo = "Reports.IncludeLogo";
            public const string AutoGenerate = "Reports.AutoGenerate";
            public const string RetentionDays = "Reports.RetentionDays";
        }

        public static class Events
        {
            public const string DefaultDuration = "Events.DefaultDuration";
            public const string AllowLateRegistration = "Events.AllowLateRegistration";
            public const string LateRegistrationHours = "Events.LateRegistrationHours";
            public const string AutoMarkAbsent = "Events.AutoMarkAbsent";
            public const string AbsentMarkingHours = "Events.AbsentMarkingHours";
        }

        public static class Club
        {
            public const string MaxMembersPerClub = "Club.MaxMembersPerClub";
            public const string RequireApprovalForJoining = "Club.RequireApprovalForJoining";
            public const string AllowMultipleClubMembership = "Club.AllowMultipleClubMembership";
        }

        public static class UI
        {
            public const string ItemsPerPage = "UI.ItemsPerPage";
            public const string ShowWelcomeScreen = "UI.ShowWelcomeScreen";
            public const string EnableAnimations = "UI.EnableAnimations";
            public const string DefaultView = "UI.DefaultView";
        }

        public static class Logging
        {
            public const string Level = "Logging.Level";
            public const string EnableFileLogging = "Logging.EnableFileLogging";
            public const string MaxLogFileSize = "Logging.MaxLogFileSize";
            public const string LogRetentionDays = "Logging.LogRetentionDays";
        }
    }
}