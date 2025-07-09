using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ClubManagementApp.Models
{
    public class Notification
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public NotificationCategory Category { get; set; }
        
        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        [ForeignKey("Club")]
        public int? ClubId { get; set; }
        public Club? Club { get; set; }
        
        [ForeignKey("Event")]
        public int? EventId { get; set; }
        public Event? Event { get; set; }
        
        public string? Data { get; set; } // JSON data
        
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        public bool IsRead { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        
        // Notification channels as JSON
        public string ChannelsJson { get; set; } = "[]";
        
        [NotMapped]
        public List<NotificationChannelType> Channels
        {
            get => string.IsNullOrEmpty(ChannelsJson) ? new List<NotificationChannelType>() : 
                   JsonSerializer.Deserialize<List<NotificationChannelType>>(ChannelsJson) ?? new List<NotificationChannelType>();
            set => ChannelsJson = JsonSerializer.Serialize(value);
        }
    }
    
    public class NotificationTemplate
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
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
        
        // Channels as JSON
        public string ChannelsJson { get; set; } = "[]";
        
        [NotMapped]
        public List<NotificationChannelType> Channels
        {
            get => string.IsNullOrEmpty(ChannelsJson) ? new List<NotificationChannelType>() : 
                   JsonSerializer.Deserialize<List<NotificationChannelType>>(ChannelsJson) ?? new List<NotificationChannelType>();
            set => ChannelsJson = JsonSerializer.Serialize(value);
        }
        
        // Parameters as JSON
        public string ParametersJson { get; set; } = "[]";
        
        [NotMapped]
        public List<string> Parameters
        {
            get => string.IsNullOrEmpty(ParametersJson) ? new List<string>() : 
                   JsonSerializer.Deserialize<List<string>>(ParametersJson) ?? new List<string>();
            set => ParametersJson = JsonSerializer.Serialize(value);
        }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class ScheduledNotification
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string NotificationRequest { get; set; } = string.Empty; // JSON serialized CreateNotificationRequest
        
        public DateTime ScheduledTime { get; set; }
        public string? RecurrencePattern { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastProcessedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int ProcessCount { get; set; } = 0;
    }
    
    // Enums
    public enum NotificationType
    {
        Welcome,
        EventRegistration,
        EventReminder,
        EventCancellation,
        ClubInvitation,
        ClubUpdate,
        PasswordReset,
        SecurityAlert,
        SystemMaintenance,
        ReportGenerated,
        General,
        Email,
        InApp,
        Both
    }
    
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
    
    public enum NotificationCategory
    {
        Account,
        Events,
        Clubs,
        Security,
        System,
        Reports,
        General
    }
    
    public enum NotificationChannelType
    {
        InApp,
        Email,
        SMS,
        Push
    }
    
    public enum DeliveryStatus
    {
        Pending,
        Delivered,
        Read,
        Deleted,
        Failed,
        NotFound,
        Error
    }
}