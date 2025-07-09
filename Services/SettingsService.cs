using ClubManagementApp.Data;
using ClubManagementApp.Models;
using ClubManagementApp.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    public interface ISettingsService
    {
        // User-specific settings
        Task<T?> GetUserSettingAsync<T>(int userId, string key, T? defaultValue = default);
        Task SetUserSettingAsync<T>(int userId, string key, T value);
        Task RemoveUserSettingAsync(int userId, string key);
        Task<Dictionary<string, object>> GetAllUserSettingsAsync(int userId);
        Task ClearUserSettingsAsync(int userId);
        
        // Global application settings
        Task<T?> GetGlobalSettingAsync<T>(string key, T? defaultValue = default);
        Task SetGlobalSettingAsync<T>(string key, T value);
        Task RemoveGlobalSettingAsync(string key);
        Task<Dictionary<string, object>> GetAllGlobalSettingsAsync();
        
        // Club-specific settings
        Task<T?> GetClubSettingAsync<T>(int clubId, string key, T? defaultValue = default);
        Task SetClubSettingAsync<T>(int clubId, string key, T value);
        Task RemoveClubSettingAsync(int clubId, string key);
        Task<Dictionary<string, object>> GetAllClubSettingsAsync(int clubId);
        
        // Settings management
        Task ImportSettingsAsync(string jsonData, SettingsScope scope, int? entityId = null);
        Task<string> ExportSettingsAsync(SettingsScope scope, int? entityId = null);
        Task ResetSettingsAsync(SettingsScope scope, int? entityId = null);
        Task<bool> SettingExistsAsync(string key, SettingsScope scope, int? entityId = null);
    }

    public class SettingsService : ISettingsService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;

        public SettingsService(
            ClubManagementDbContext context,
            ILoggingService loggingService,
            IAuditService auditService)
        {
            _context = context;
            _loggingService = loggingService;
            _auditService = auditService;
        }

        public async Task<T?> GetUserSettingAsync<T>(int userId, string key, T? defaultValue = default)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key && s.Scope == SettingsScope.User);

                if (setting == null)
                    return defaultValue;

                return DeserializeValue<T>(setting.Value) ?? defaultValue;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get user setting {key} for user {userId}", ex);
                return defaultValue;
            }
        }

        public async Task SetUserSettingAsync<T>(int userId, string key, T value)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key && s.Scope == SettingsScope.User);

                var serializedValue = SerializeValue(value);

                if (setting == null)
                {
                    setting = new Setting
                    {
                        UserId = userId,
                        Key = key,
                        Value = serializedValue,
                        Scope = SettingsScope.User,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Settings.Add(setting);
                }
                else
                {
                    setting.Value = serializedValue;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogUserActionAsync(userId, "Setting Updated", $"Updated setting: {key}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to set user setting {key} for user {userId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task RemoveUserSettingAsync(int userId, string key)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key && s.Scope == SettingsScope.User);

                if (setting != null)
                {
                    _context.Settings.Remove(setting);
                    await _context.SaveChangesAsync();
                    await _auditService.LogUserActionAsync(userId, "Setting Removed", $"Removed setting: {key}");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to remove user setting {key} for user {userId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<Dictionary<string, object>> GetAllUserSettingsAsync(int userId)
        {
            try
            {
                var settings = await _context.Settings
                    .Where(s => s.UserId == userId && s.Scope == SettingsScope.User)
                    .ToDictionaryAsync(s => s.Key, s => DeserializeValue<object>(s.Value) ?? new object());

                return settings;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get all user settings for user {userId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task ClearUserSettingsAsync(int userId)
        {
            try
            {
                var settings = await _context.Settings
                    .Where(s => s.UserId == userId && s.Scope == SettingsScope.User)
                    .ToListAsync();

                if (settings.Any())
                {
                    _context.Settings.RemoveRange(settings);
                    await _context.SaveChangesAsync();
                    await _auditService.LogUserActionAsync(userId, "Settings Cleared", $"Cleared {settings.Count} user settings");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to clear user settings for user {userId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<T?> GetGlobalSettingAsync<T>(string key, T? defaultValue = default)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Key == key && s.Scope == SettingsScope.Global);

                if (setting == null)
                    return defaultValue;

                return DeserializeValue<T>(setting.Value) ?? defaultValue;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get global setting {key}", ex);
                return defaultValue;
            }
        }

        public async Task SetGlobalSettingAsync<T>(string key, T value)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Key == key && s.Scope == SettingsScope.Global);

                var serializedValue = SerializeValue(value);

                if (setting == null)
                {
                    setting = new Setting
                    {
                        Key = key,
                        Value = serializedValue,
                        Scope = SettingsScope.Global,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Settings.Add(setting);
                }
                else
                {
                    setting.Value = serializedValue;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogSystemEventAsync("Global Setting Updated", $"Updated global setting: {key}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to set global setting {key}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task RemoveGlobalSettingAsync(string key)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Key == key && s.Scope == SettingsScope.Global);

                if (setting != null)
                {
                    _context.Settings.Remove(setting);
                    await _context.SaveChangesAsync();
                    await _auditService.LogSystemEventAsync("Global Setting Removed", $"Removed global setting: {key}");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to remove global setting {key}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<Dictionary<string, object>> GetAllGlobalSettingsAsync()
        {
            try
            {
                var settings = await _context.Settings
                    .Where(s => s.Scope == SettingsScope.Global)
                    .ToDictionaryAsync(s => s.Key, s => DeserializeValue<object>(s.Value) ?? new object());

                return settings;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get all global settings", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<T?> GetClubSettingAsync<T>(int clubId, string key, T? defaultValue = default)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.ClubId == clubId && s.Key == key && s.Scope == SettingsScope.Club);

                if (setting == null)
                    return defaultValue;

                return DeserializeValue<T>(setting.Value) ?? defaultValue;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get club setting {key} for club {clubId}", ex);
                return defaultValue;
            }
        }

        public async Task SetClubSettingAsync<T>(int clubId, string key, T value)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.ClubId == clubId && s.Key == key && s.Scope == SettingsScope.Club);

                var serializedValue = SerializeValue(value);

                if (setting == null)
                {
                    setting = new Setting
                    {
                        ClubId = clubId,
                        Key = key,
                        Value = serializedValue,
                        Scope = SettingsScope.Club,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Settings.Add(setting);
                }
                else
                {
                    setting.Value = serializedValue;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogSystemEventAsync("Club Setting Updated", $"Updated club setting: {key} for club {clubId}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to set club setting {key} for club {clubId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task RemoveClubSettingAsync(int clubId, string key)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.ClubId == clubId && s.Key == key && s.Scope == SettingsScope.Club);

                if (setting != null)
                {
                    _context.Settings.Remove(setting);
                    await _context.SaveChangesAsync();
                    await _auditService.LogSystemEventAsync("Club Setting Removed", $"Removed club setting: {key} for club {clubId}");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to remove club setting {key} for club {clubId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<Dictionary<string, object>> GetAllClubSettingsAsync(int clubId)
        {
            try
            {
                var settings = await _context.Settings
                    .Where(s => s.ClubId == clubId && s.Scope == SettingsScope.Club)
                    .ToDictionaryAsync(s => s.Key, s => DeserializeValue<object>(s.Value) ?? new object());

                return settings;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get all club settings for club {clubId}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task ImportSettingsAsync(string jsonData, SettingsScope scope, int? entityId = null)
        {
            try
            {
                var importedSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                if (importedSettings == null)
                    throw new ArgumentException("Invalid JSON data");

                foreach (var kvp in importedSettings)
                {
                    switch (scope)
                    {
                        case SettingsScope.User:
                            if (entityId.HasValue)
                                await SetUserSettingAsync(entityId.Value, kvp.Key, kvp.Value);
                            break;
                        case SettingsScope.Club:
                            if (entityId.HasValue)
                                await SetClubSettingAsync(entityId.Value, kvp.Key, kvp.Value);
                            break;
                        case SettingsScope.Global:
                            await SetGlobalSettingAsync(kvp.Key, kvp.Value);
                            break;
                    }
                }

                await _auditService.LogSystemEventAsync("Settings Imported", 
                    $"Imported {importedSettings.Count} settings for scope {scope}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to import settings for scope {scope}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<string> ExportSettingsAsync(SettingsScope scope, int? entityId = null)
        {
            try
            {
                Dictionary<string, object> settings;

                switch (scope)
                {
                    case SettingsScope.User:
                        if (!entityId.HasValue)
                            throw new ArgumentException("Entity ID required for user settings");
                        settings = await GetAllUserSettingsAsync(entityId.Value);
                        break;
                    case SettingsScope.Club:
                        if (!entityId.HasValue)
                            throw new ArgumentException("Entity ID required for club settings");
                        settings = await GetAllClubSettingsAsync(entityId.Value);
                        break;
                    case SettingsScope.Global:
                        settings = await GetAllGlobalSettingsAsync();
                        break;
                    default:
                        throw new ArgumentException($"Invalid settings scope: {scope}");
                }

                return JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to export settings for scope {scope}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task ResetSettingsAsync(SettingsScope scope, int? entityId = null)
        {
            try
            {
                IQueryable<Setting> query = _context.Settings.Where(s => s.Scope == scope);

                switch (scope)
                {
                    case SettingsScope.User:
                        if (!entityId.HasValue)
                            throw new ArgumentException("Entity ID required for user settings");
                        query = query.Where(s => s.UserId == entityId.Value);
                        break;
                    case SettingsScope.Club:
                        if (!entityId.HasValue)
                            throw new ArgumentException("Entity ID required for club settings");
                        query = query.Where(s => s.ClubId == entityId.Value);
                        break;
                }

                var settings = await query.ToListAsync();
                if (settings.Any())
                {
                    _context.Settings.RemoveRange(settings);
                    await _context.SaveChangesAsync();

                    await _auditService.LogSystemEventAsync("Settings Reset", 
                        $"Reset {settings.Count} settings for scope {scope}");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to reset settings for scope {scope}", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<bool> SettingExistsAsync(string key, SettingsScope scope, int? entityId = null)
        {
            try
            {
                IQueryable<Setting> query = _context.Settings.Where(s => s.Key == key && s.Scope == scope);

                switch (scope)
                {
                    case SettingsScope.User:
                        if (!entityId.HasValue)
                            return false;
                        query = query.Where(s => s.UserId == entityId.Value);
                        break;
                    case SettingsScope.Club:
                        if (!entityId.HasValue)
                            return false;
                        query = query.Where(s => s.ClubId == entityId.Value);
                        break;
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check if setting {key} exists for scope {scope}", ex);
                return false;
            }
        }

        private static string SerializeValue<T>(T value)
        {
            return JsonSerializer.Serialize(value);
        }

        private static T? DeserializeValue<T>(string value)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            catch
            {
                return default;
            }
        }
    }



    // Settings constants for type safety
    public static class SettingsKeys
    {
        public static class User
        {
            public const string Theme = "user.theme";
            public const string Language = "user.language";
            public const string NotificationsEnabled = "user.notifications.enabled";
            public const string EmailNotifications = "user.notifications.email";
            public const string DefaultView = "user.ui.defaultView";
            public const string ItemsPerPage = "user.ui.itemsPerPage";
            public const string AutoSave = "user.editor.autoSave";
            public const string TimeZone = "user.timezone";
        }

        public static class Club
        {
            public const string AllowPublicEvents = "club.events.allowPublic";
            public const string RequireEventApproval = "club.events.requireApproval";
            public const string MaxMembersPerEvent = "club.events.maxMembers";
            public const string DefaultEventDuration = "club.events.defaultDuration";
            public const string AutoGenerateReports = "club.reports.autoGenerate";
            public const string ReportTemplate = "club.reports.template";
            public const string MembershipApprovalRequired = "club.membership.approvalRequired";
        }

        public static class Global
        {
            public const string MaintenanceMode = "system.maintenanceMode";
            public const string MaxFileUploadSize = "system.maxFileUploadSize";
            public const string SessionTimeout = "system.sessionTimeout";
            public const string BackupFrequency = "system.backupFrequency";
            public const string LogRetentionDays = "system.logRetentionDays";
            public const string EmailServerSettings = "system.email.server";
            public const string DefaultUserRole = "system.users.defaultRole";
        }
    }
}