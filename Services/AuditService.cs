using ClubManagementApp.Data;
using ClubManagementApp.Models;
using ClubManagementApp.DTOs;
using ClubManagementApp.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    public interface IAuditService
    {
        Task LogUserActionAsync(int userId, string action, string details, string? ipAddress = null);
        Task LogSystemEventAsync(string eventType, string description, object? data = null);
        Task LogDataChangeAsync(string tableName, int recordId, string operation, object? oldValues = null, object? newValues = null, int? userId = null);
        Task LogSecurityEventAsync(string eventType, string description, int? userId = null, string? ipAddress = null);
        Task LogErrorAsync(string errorType, string message, string? stackTrace = null, int? userId = null);
        
        Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter);
        Task<IEnumerable<AuditLogDto>> GetUserAuditLogsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<AuditLogDto>> GetSystemAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<AuditLogDto>> GetSecurityAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        
        Task<int> GetAuditLogCountAsync(AuditLogFilter filter);
        Task CleanupOldLogsAsync(int retentionDays = 365);
        Task<byte[]> ExportAuditLogsAsync(AuditLogFilter filter, string format = "CSV");
    }

    public class AuditService : IAuditService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private readonly IConfigurationService _configurationService;

        public AuditService(
            ClubManagementDbContext context,
            ILoggingService loggingService,
            IConfigurationService configurationService)
        {
            _context = context;
            _loggingService = loggingService;
            _configurationService = configurationService;
        }

        public async Task LogUserActionAsync(int userId, string action, string details, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    LogType = AuditLogType.UserAction
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to log user action for user {userId}", ex);
                // Don't throw to avoid breaking the main operation
            }
        }

        public async Task LogSystemEventAsync(string eventType, string description, object? data = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = eventType,
                    Details = description,
                    AdditionalData = data != null ? JsonSerializer.Serialize(data) : null,
                    Timestamp = DateTime.UtcNow,
                    LogType = AuditLogType.SystemEvent
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to log system event: {eventType}", ex);
            }
        }

        public async Task LogDataChangeAsync(string tableName, int recordId, string operation, object? oldValues = null, object? newValues = null, int? userId = null)
        {
            try
            {
                var changeData = new
                {
                    TableName = tableName,
                    RecordId = recordId,
                    Operation = operation,
                    OldValues = oldValues,
                    NewValues = newValues
                };

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = $"Data {operation}",
                    Details = $"{operation} operation on {tableName} (ID: {recordId})",
                    AdditionalData = JsonSerializer.Serialize(changeData),
                    Timestamp = DateTime.UtcNow,
                    LogType = AuditLogType.DataChange
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to log data change for {tableName}", ex);
            }
        }

        public async Task LogSecurityEventAsync(string eventType, string description, int? userId = null, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = eventType,
                    Details = description,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    LogType = AuditLogType.SecurityEvent
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to log security event: {eventType}", ex);
            }
        }

        public async Task LogErrorAsync(string errorType, string message, string? stackTrace = null, int? userId = null)
        {
            try
            {
                var errorData = new
                {
                    ErrorType = errorType,
                    Message = message,
                    StackTrace = stackTrace
                };

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "Error",
                    Details = $"{errorType}: {message}",
                    AdditionalData = JsonSerializer.Serialize(errorData),
                    Timestamp = DateTime.UtcNow,
                    LogType = AuditLogType.Error
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to log error: {errorType}", ex);
            }
        }

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter)
        {
            try
            {
                var query = _context.AuditLogs
                    .Include(al => al.User)
                    .AsQueryable();

                // Apply filters
                if (filter.UserId.HasValue)
                    query = query.Where(al => al.UserId == filter.UserId.Value);

                if (filter.LogType.HasValue)
                    query = query.Where(al => al.LogType == filter.LogType.Value);

                if (filter.FromDate.HasValue)
                    query = query.Where(al => al.Timestamp >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(al => al.Timestamp <= filter.ToDate.Value);

                if (!string.IsNullOrEmpty(filter.Action))
                    query = query.Where(al => al.Action.Contains(filter.Action));

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    query = query.Where(al => al.Details.Contains(filter.SearchTerm) || 
                                            al.Action.Contains(filter.SearchTerm));

                var logs = await query
                    .OrderByDescending(al => al.Timestamp)
                    .Skip(filter.Skip)
                    .Take(filter.Take)
                    .Select(al => new AuditLogDto
                    {
                        Id = al.Id,
                        UserId = al.UserId,
                        UserName = al.User != null ? al.User.FullName : "System",
                        Action = al.Action,
                        Details = al.Details,
                        LogType = al.LogType,
                        IpAddress = al.IpAddress,
                        Timestamp = al.Timestamp,
                        AdditionalData = al.AdditionalData
                    })
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to retrieve audit logs", ex);
                throw new DatabaseConnectionException("Failed to retrieve audit logs", ex);
            }
        }

        public async Task<IEnumerable<AuditLogDto>> GetUserAuditLogsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filter = new AuditLogFilter
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                LogType = AuditLogType.UserAction,
                Take = 100
            };

            return await GetAuditLogsAsync(filter);
        }

        public async Task<IEnumerable<AuditLogDto>> GetSystemAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filter = new AuditLogFilter
            {
                FromDate = fromDate,
                ToDate = toDate,
                LogType = AuditLogType.SystemEvent,
                Take = 100
            };

            return await GetAuditLogsAsync(filter);
        }

        public async Task<IEnumerable<AuditLogDto>> GetSecurityAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filter = new AuditLogFilter
            {
                FromDate = fromDate,
                ToDate = toDate,
                LogType = AuditLogType.SecurityEvent,
                Take = 100
            };

            return await GetAuditLogsAsync(filter);
        }

        public async Task<int> GetAuditLogCountAsync(AuditLogFilter filter)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                // Apply same filters as GetAuditLogsAsync
                if (filter.UserId.HasValue)
                    query = query.Where(al => al.UserId == filter.UserId.Value);

                if (filter.LogType.HasValue)
                    query = query.Where(al => al.LogType == filter.LogType.Value);

                if (filter.FromDate.HasValue)
                    query = query.Where(al => al.Timestamp >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(al => al.Timestamp <= filter.ToDate.Value);

                if (!string.IsNullOrEmpty(filter.Action))
                    query = query.Where(al => al.Action.Contains(filter.Action));

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    query = query.Where(al => al.Details.Contains(filter.SearchTerm) || 
                                            al.Action.Contains(filter.SearchTerm));

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get audit log count", ex);
                throw new DatabaseConnectionException("Failed to get audit log count", ex);
            }
        }

        public async Task CleanupOldLogsAsync(int retentionDays = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var oldLogs = await _context.AuditLogs
                    .Where(al => al.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.AuditLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();

                    await LogSystemEventAsync("Audit Cleanup", 
                        $"Removed {oldLogs.Count} audit logs older than {retentionDays} days");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to cleanup old audit logs", ex);
                throw new DatabaseConnectionException("Failed to cleanup old audit logs", ex);
            }
        }

        public async Task<byte[]> ExportAuditLogsAsync(AuditLogFilter filter, string format = "CSV")
        {
            try
            {
                var logs = await GetAuditLogsAsync(filter);

                if (format.ToUpper() == "CSV")
                {
                    return ExportToCsv(logs);
                }
                else if (format.ToUpper() == "JSON")
                {
                    return ExportToJson(logs);
                }
                else
                {
                    throw new ArgumentException($"Unsupported export format: {format}");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to export audit logs in {format} format", ex);
                throw new DatabaseConnectionException($"Failed to export audit logs", ex);
            }
        }

        private static byte[] ExportToCsv(IEnumerable<AuditLogDto> logs)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,UserId,UserName,Action,Details,LogType,IpAddress,Timestamp");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.UserId},{EscapeCsvField(log.UserName)},{EscapeCsvField(log.Action)},{EscapeCsvField(log.Details)},{log.LogType},{log.IpAddress},{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private static byte[] ExportToJson(IEnumerable<AuditLogDto> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\""; // Escape quotes by doubling them
            }

            return field;
        }
    }

    // DTOs and Enums for Audit Service
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public AuditLogType LogType { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string? AdditionalData { get; set; }
    }

    public class AuditLogFilter
    {
        public int? UserId { get; set; }
        public AuditLogType? LogType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Action { get; set; }
        public string? SearchTerm { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }

}