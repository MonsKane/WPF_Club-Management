using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    public interface ILoggingService
    {
        void LogInfo(string message, object? data = null);
        void LogWarning(string message, object? data = null);
        void LogError(string message, Exception? exception = null, object? data = null);
        void LogUserAction(int userId, string action, object? data = null);
        void LogSystemEvent(string eventType, object? data = null);
        Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, LogLevel? level = null);
        
        // Async versions
        Task LogInformationAsync(string message, object? data = null);
        Task LogWarningAsync(string message, object? data = null);
        Task LogErrorAsync(string message, Exception? exception = null, object? data = null);
        Task LogUserActionAsync(int userId, string action, object? data = null);
        Task LogSystemEventAsync(string eventType, object? data = null);
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Data { get; set; }
        public string? Exception { get; set; }
        public int? UserId { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        private readonly List<LogEntry> _logEntries; // In-memory storage for demo purposes
        private readonly object _lockObject = new object();

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
            _logEntries = new List<LogEntry>();
        }

        public void LogInfo(string message, object? data = null)
        {
            var logEntry = CreateLogEntry(LogLevel.Information, message, data);
            _logger.LogInformation(message);
            AddLogEntry(logEntry);
        }

        public void LogWarning(string message, object? data = null)
        {
            var logEntry = CreateLogEntry(LogLevel.Warning, message, data);
            _logger.LogWarning(message);
            AddLogEntry(logEntry);
        }

        public void LogError(string message, Exception? exception = null, object? data = null)
        {
            var logEntry = CreateLogEntry(LogLevel.Error, message, data, exception);
            _logger.LogError(exception, message);
            AddLogEntry(logEntry);
        }

        public void LogUserAction(int userId, string action, object? data = null)
        {
            var message = $"User {userId} performed action: {action}";
            var logEntry = CreateLogEntry(LogLevel.Information, message, data);
            logEntry.UserId = userId;
            logEntry.Category = "UserAction";
            
            _logger.LogInformation(message);
            AddLogEntry(logEntry);
        }

        public void LogSystemEvent(string eventType, object? data = null)
        {
            var message = $"System event: {eventType}";
            var logEntry = CreateLogEntry(LogLevel.Information, message, data);
            logEntry.Category = "SystemEvent";
            
            _logger.LogInformation(message);
            AddLogEntry(logEntry);
        }

        public async Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, LogLevel? level = null)
        {
            await Task.CompletedTask; // Simulate async operation
            
            lock (_lockObject)
            {
                var query = _logEntries.AsEnumerable();

                if (fromDate.HasValue)
                    query = query.Where(log => log.Timestamp >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(log => log.Timestamp <= toDate.Value);

                if (level.HasValue)
                    query = query.Where(log => log.Level == level.Value);

                return query.OrderByDescending(log => log.Timestamp).ToList();
            }
        }

        // Async method implementations
        public async Task LogInformationAsync(string message, object? data = null)
        {
            await Task.Run(() => LogInfo(message, data));
        }

        public async Task LogWarningAsync(string message, object? data = null)
        {
            await Task.Run(() => LogWarning(message, data));
        }

        public async Task LogErrorAsync(string message, Exception? exception = null, object? data = null)
        {
            await Task.Run(() => LogError(message, exception, data));
        }

        public async Task LogUserActionAsync(int userId, string action, object? data = null)
        {
            await Task.Run(() => LogUserAction(userId, action, data));
        }

        public async Task LogSystemEventAsync(string eventType, object? data = null)
        {
            await Task.Run(() => LogSystemEvent(eventType, data));
        }

        private LogEntry CreateLogEntry(LogLevel level, string message, object? data = null, Exception? exception = null)
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Data = data != null ? JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }) : null,
                Exception = exception?.ToString(),
                Category = "General"
            };
        }

        private void AddLogEntry(LogEntry logEntry)
        {
            lock (_lockObject)
            {
                _logEntries.Add(logEntry);
                
                // Keep only the last 1000 log entries to prevent memory issues
                if (_logEntries.Count > 1000)
                {
                    _logEntries.RemoveRange(0, _logEntries.Count - 1000);
                }
            }
        }
    }

    // Extension methods for easier logging
    public static class LoggingExtensions
    {
        public static void LogUserLogin(this ILoggingService logger, int userId, bool successful)
        {
            var action = successful ? "Login Successful" : "Login Failed";
            logger.LogUserAction(userId, action, new { Successful = successful, Timestamp = DateTime.Now });
        }

        public static void LogUserLogout(this ILoggingService logger, int userId)
        {
            logger.LogUserAction(userId, "Logout", new { Timestamp = DateTime.Now });
        }

        public static void LogEventCreation(this ILoggingService logger, int userId, int eventId, string eventName)
        {
            logger.LogUserAction(userId, "Event Created", new { EventId = eventId, EventName = eventName });
        }

        public static void LogEventRegistration(this ILoggingService logger, int userId, int eventId)
        {
            logger.LogUserAction(userId, "Event Registration", new { EventId = eventId });
        }

        public static void LogReportGeneration(this ILoggingService logger, int userId, int reportId, string reportType)
        {
            logger.LogUserAction(userId, "Report Generated", new { ReportId = reportId, ReportType = reportType });
        }

        public static void LogMembershipChange(this ILoggingService logger, int userId, int targetUserId, string changeType)
        {
            logger.LogUserAction(userId, "Membership Change", new { TargetUserId = targetUserId, ChangeType = changeType });
        }

        public static void LogClubCreation(this ILoggingService logger, int userId, int clubId, string clubName)
        {
            logger.LogUserAction(userId, "Club Created", new { ClubId = clubId, ClubName = clubName });
        }

        public static void LogDatabaseOperation(this ILoggingService logger, string operation, bool successful, Exception? exception = null)
        {
            if (successful)
            {
                logger.LogInfo($"Database operation successful: {operation}");
            }
            else
            {
                logger.LogError($"Database operation failed: {operation}", exception);
            }
        }

        public static void LogNotificationSent(this ILoggingService logger, string notificationType, string recipient, bool successful)
        {
            var message = $"Notification sent: {notificationType} to {recipient}";
            if (successful)
            {
                logger.LogInfo(message, new { NotificationType = notificationType, Recipient = recipient });
            }
            else
            {
                logger.LogWarning($"Failed to send notification: {notificationType} to {recipient}");
            }
        }

        public static void LogSecurityEvent(this ILoggingService logger, string eventType, int? userId = null, object? data = null)
        {
            var logData = new { EventType = eventType, UserId = userId, Data = data, Timestamp = DateTime.Now };
            logger.LogWarning($"Security event: {eventType}", logData);
        }

        public static void LogPerformanceMetric(this ILoggingService logger, string operation, TimeSpan duration, object? additionalData = null)
        {
            var data = new { Operation = operation, DurationMs = duration.TotalMilliseconds, AdditionalData = additionalData };
            logger.LogInfo($"Performance metric: {operation} took {duration.TotalMilliseconds}ms", data);
        }
    }
}