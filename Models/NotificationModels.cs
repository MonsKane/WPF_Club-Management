using System.ComponentModel.DataAnnotations;

namespace ClubManagementApp.Models
{
    // Request models
    public class CreateNotificationRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public NotificationCategory Category { get; set; }
        
        public int? UserId { get; set; }
        public int? ClubId { get; set; }
        public int? EventId { get; set; }
        
        public Dictionary<string, object>? Data { get; set; }
        public List<NotificationChannelType>? Channels { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        public bool SendImmediately { get; set; } = true;
    }
    
    public class CreateTemplateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string TitleTemplate { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string MessageTemplate { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public NotificationCategory Category { get; set; }
        
        public List<NotificationChannelType>? Channels { get; set; }
        public List<string>? Parameters { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
    
    public class BulkNotificationRequest
    {
        [Required]
        public List<int> UserIds { get; set; } = new List<int>();
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public NotificationCategory Category { get; set; }
        
        public int? ClubId { get; set; }
        public int? EventId { get; set; }
        
        public Dictionary<string, object>? Data { get; set; }
        public List<NotificationChannelType>? Channels { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        public bool SendImmediately { get; set; } = true;
    }
    
    public class ScheduleNotificationRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public CreateNotificationRequest NotificationRequest { get; set; } = new CreateNotificationRequest();
        
        public DateTime ScheduledTime { get; set; }
        public string? RecurrencePattern { get; set; }
    }
    
    // Filter and query models
    public class NotificationFilter
    {
        public bool? IsRead { get; set; }
        public NotificationType? Type { get; set; }
        public NotificationCategory? Category { get; set; }
        public NotificationPriority? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Limit { get; set; } = 50;
    }
    
    // Statistics and reporting models
    public class NotificationStatistics
    {
        public int TotalNotifications { get; set; }
        public int ReadNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int DeletedNotifications { get; set; }
        
        public Dictionary<NotificationType, int> NotificationsByType { get; set; } = new Dictionary<NotificationType, int>();
        public Dictionary<NotificationCategory, int> NotificationsByCategory { get; set; } = new Dictionary<NotificationCategory, int>();
    }
    
    public class NotificationDeliveryReport
    {
        public string NotificationId { get; set; } = string.Empty;
        public NotificationChannelType Channel { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime AttemptedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
    }
    
    // User preferences
    public class NotificationPreferences
    {
        public int UserId { get; set; }
        public bool EmailEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = true;
        public bool SMSEnabled { get; set; } = false;
        
        public Dictionary<NotificationCategory, bool> CategoryPreferences { get; set; } = new Dictionary<NotificationCategory, bool>();
        public Dictionary<NotificationPriority, bool> PriorityPreferences { get; set; } = new Dictionary<NotificationPriority, bool>();
        
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
        public List<DayOfWeek> QuietDays { get; set; } = new List<DayOfWeek>();
    }
    
    // Channel configuration
    public class NotificationChannelConfig
    {
        public NotificationChannelType Type { get; set; }
        public bool IsEnabled { get; set; }
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    }
    
    // Maintenance and cleanup
    public class NotificationMaintenanceInfo
    {
        public int ExpiredNotifications { get; set; }
        public int DeletedNotifications { get; set; }
        public int ArchivedNotifications { get; set; }
        public DateTime LastCleanupDate { get; set; }
        public TimeSpan CleanupDuration { get; set; }
    }
}