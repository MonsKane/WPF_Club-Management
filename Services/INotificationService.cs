using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public class NotificationRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Recipients { get; set; } = new List<string>();
        public NotificationType Type { get; set; }
        public int? EventId { get; set; }
        public int? ClubId { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }

    public interface INotificationService
    {
        // Core notification methods
        Task<string> CreateNotificationAsync(CreateNotificationRequest request);
        Task<Notification?> GetNotificationAsync(string notificationId);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, NotificationFilter? filter = null);
        Task<bool> MarkAsReadAsync(string notificationId, int? userId = null);
        Task<int> MarkMultipleAsReadAsync(IEnumerable<string> notificationIds, int? userId = null);
        Task<bool> DeleteNotificationAsync(string notificationId, int? userId = null);
        Task<int> DeleteMultipleNotificationsAsync(IEnumerable<string> notificationIds, int? userId = null);
        
        // Template management
        Task<string> CreateTemplateAsync(CreateTemplateRequest request);
        Task<NotificationTemplate?> GetTemplateAsync(string templateId);
        Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(bool? isActive = null);
        Task<string> CreateFromTemplateAsync(string templateId, Dictionary<string, object> parameters, CreateNotificationRequest baseRequest);
        
        // Bulk operations
        Task<IEnumerable<string>> CreateBulkNotificationsAsync(BulkNotificationRequest request);
        
        // Scheduling
        Task<string> ScheduleNotificationAsync(ScheduleNotificationRequest request);
        Task<IEnumerable<ScheduledNotification>> GetScheduledNotificationsAsync(bool? isActive = null);
        Task ProcessScheduledNotificationsAsync();
        
        // Statistics and reporting
        Task<NotificationStatistics> GetStatisticsAsync(int? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
        
        // Legacy email methods (for backward compatibility)
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailToMultipleAsync(List<string> recipients, string subject, string body);
        Task SendInAppNotificationAsync(int userId, string title, string message);
        Task SendEventNotificationAsync(int eventId, string subject, string message, NotificationType type = NotificationType.Both);
        Task SendClubNotificationAsync(int clubId, string subject, string message, NotificationType type = NotificationType.Both);
        Task SendRoleBasedNotificationAsync(UserRole role, string subject, string message, int? clubId = null, NotificationType type = NotificationType.Both);
        Task NotifyEventRegistrationAsync(int eventId, int userId);
        Task NotifyEventReminderAsync(int eventId);
        Task NotifyMembershipChangeAsync(int userId, int clubId, string changeType);
        Task SendNotificationAsync(NotificationRequest request);
    }
}