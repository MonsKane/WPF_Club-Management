using ClubManagementApp.Services;

namespace ClubManagementApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public AuditLogType LogType { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string? AdditionalData { get; set; }
    }

    public enum AuditLogType
    {
        UserAction,
        SystemEvent,
        DataChange,
        SecurityEvent,
        Error
    }
}