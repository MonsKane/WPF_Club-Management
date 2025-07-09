using System;

namespace ClubManagementApp.Configuration
{
    /// <summary>
    /// Centralized configuration for service layer operations
    /// </summary>
    public static class ServiceConfiguration
    {
        public static class Security
        {
            public const int MaxFailedLoginAttempts = 5;
            public const int MaxUniqueIpAddresses = 3;
            public const int DefaultLockoutDurationMinutes = 30;
            public const int TwoFactorCodeExpirationMinutes = 5;
            public const int SessionTimeoutMinutes = 60;
            public const int TokenExpirationHours = 24;
            public const int PasswordHistoryCount = 5;
            public const int MinPasswordLength = 8;
            public const int MaxPasswordLength = 128;
            public const string DefaultPassword = "TempPassword123!";
        }

        public static class Notification
        {
            public const int MaxTitleLength = 200;
            public const int MaxMessageLength = 2000;
            public const int RetentionDays = 30;
            public const int MaxBulkNotifications = 100;
            public const int ReminderAdvanceDays = 1;
            public const int MaxTemplateParameters = 20;
            public const int CleanupBatchSize = 1000;
        }

        public static class DataImportExport
        {
            public const int MaxBatchSize = 1000;
            public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
            public const int MaxConcurrentImports = 3;
            public const int ImportTimeoutMinutes = 30;
            public const string DefaultDateFormat = "yyyy-MM-dd";
            public const string DefaultCsvDelimiter = ",";
            public const int MaxValidationErrors = 100;
        }

        public static class Events
        {
            public const int MaxEventNameLength = 100;
            public const int MaxEventDescriptionLength = 1000;
            public const int MaxLocationLength = 200;
            public const int DefaultReminderDays = 1;
            public const int MaxParticipants = 500;
            public const int EventHistoryDays = 365;
        }

        public static class Users
        {
            public const int MaxFullNameLength = 100;
            public const int MaxEmailLength = 255;
            public const int StudentIdLength = 10;
            public const int MaxStudentIdLength = 10;
            public const int MaxSearchResults = 100;
            public const int ActivityThresholdDays = 30;
            public const int InactiveUserDays = 90;
            public const int MaxRecentEventsCount = 5;
            public const int MaxNameLength = 100;
        }

        public static class Clubs
        {
            public const int MaxClubNameLength = 100;
            public const int MaxClubDescriptionLength = 500;
            public const int MaxMembersPerClub = 200;
            public const int MinMembersForActiveClub = 5;
            public const int MaxLeadershipRoles = 10;
        }

        public static class Reports
        {
            public const int MaxReportNameLength = 200;
            public const int ReportRetentionDays = 180;
            public const int MaxReportSize = 10 * 1024 * 1024; // 10MB
            public const int DefaultPageSize = 50;
            public const int MaxExportRecords = 10000;
        }

        public static class Logging
        {
            public const int MaxLogMessageLength = 4000;
            public const int LogRetentionDays = 90;
            public const int MaxLogBatchSize = 500;
            public const string DefaultLogLevel = "Information";
        }

        public static class Performance
        {
            public const int DefaultCacheExpirationMinutes = 15;
            public const int MaxConcurrentOperations = 10;
            public const int DatabaseTimeoutSeconds = 30;
            public const int MaxRetryAttempts = 3;
            public const int RetryDelayMilliseconds = 1000;
        }

        public static class Validation
        {
            public const int MaxStringLength = 4000;
            public const int MinSearchTermLength = 2;
            public const int MaxSearchTermLength = 100;
            public const int MaxPageSize = 100;
            public const int DefaultPageSize = 20;
        }

        public static class Email
        {
            public const int MaxSubjectLength = 200;
            public const int MaxBodyLength = 10000;
            public const int MaxRecipients = 100;
            public const int EmailRetryAttempts = 3;
            public const int EmailTimeoutSeconds = 30;
        }

        public static class Backup
        {
            public const int BackupRetentionDays = 30;
            public const int MaxBackupSizeGB = 5;
            public const int BackupTimeoutMinutes = 60;
            public const string DefaultBackupPath = "Backups";
            public const int MaxConcurrentBackups = 2;
        }

        public static class Audit
        {
            public const int AuditLogRetentionDays = 365;
            public const int MaxAuditBatchSize = 1000;
            public const int AuditCleanupIntervalHours = 24;
            public const int MaxAuditDetailsLength = 2000;
        }
    }

    /// <summary>
    /// Runtime configuration that can be modified
    /// </summary>
    public class RuntimeConfiguration
    {
        public bool EnableDetailedLogging { get; set; } = false;
        public bool EnablePerformanceMetrics { get; set; } = false;
        public bool EnableCaching { get; set; } = true;
        public bool EnableBackgroundTasks { get; set; } = true;
        public bool EnableEmailNotifications { get; set; } = true;
        public bool EnableAuditLogging { get; set; } = true;
        public bool EnableSecurityChecks { get; set; } = true;
        public bool EnableDataValidation { get; set; } = true;
        
        public int MaxConcurrentUsers { get; set; } = 100;
        public int SessionTimeoutMinutes { get; set; } = ServiceConfiguration.Security.SessionTimeoutMinutes;
        public int CacheExpirationMinutes { get; set; } = ServiceConfiguration.Performance.DefaultCacheExpirationMinutes;
        
        public string DefaultTimeZone { get; set; } = "UTC";
        public string DefaultCulture { get; set; } = "en-US";
        public string DefaultDateFormat { get; set; } = ServiceConfiguration.DataImportExport.DefaultDateFormat;
    }
}