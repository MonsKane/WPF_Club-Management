using ClubManagementApp.Data;
using ClubManagementApp.Models;
using ClubManagementApp.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClubManagementApp.Services
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly ISettingsService _settingsService;
        private SmtpClient? _smtpClient;

        public NotificationService(
            ClubManagementDbContext context, 
            ILogger<NotificationService> logger, 
            IConfiguration configuration,
            ILoggingService loggingService,
            IAuditService auditService,
            ISettingsService settingsService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _loggingService = loggingService;
            _auditService = auditService;
            _settingsService = settingsService;
        }

        private SmtpClient ConfigureSmtpClient()
        {
            if (_smtpClient == null)
            {
                _smtpClient = new SmtpClient
                {
                    Host = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com",
                    Port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
                    EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true"),
                    Credentials = new NetworkCredential(
                        _configuration["EmailSettings:Username"],
                        _configuration["EmailSettings:Password"]
                    )
                };
            }
            return _smtpClient;
        }

        // Core notification methods
        public async Task<string> CreateNotificationAsync(CreateNotificationRequest request)
        {
            try
            {
                // Input validation
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Notification request cannot be null");
                
                if (string.IsNullOrWhiteSpace(request.Title))
                    throw new ArgumentException("Notification title cannot be null or empty", nameof(request.Title));
                
                if (string.IsNullOrWhiteSpace(request.Message))
                    throw new ArgumentException("Notification message cannot be null or empty", nameof(request.Message));

                // Validate user exists if specified
                if (request.UserId.HasValue)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.UserID == request.UserId.Value && u.IsActive);
                    if (!userExists)
                        throw new ArgumentException($"User with ID {request.UserId.Value} not found or inactive", nameof(request.UserId));
                }

                // Validate club exists if specified
                if (request.ClubId.HasValue)
                {
                    var clubExists = await _context.Clubs.AnyAsync(c => c.ClubID == request.ClubId.Value);
                    if (!clubExists)
                        throw new ArgumentException($"Club with ID {request.ClubId.Value} not found", nameof(request.ClubId));
                }

                // Validate event exists if specified
                if (request.EventId.HasValue)
                {
                    var eventExists = await _context.Events.AnyAsync(e => e.EventID == request.EventId.Value);
                    if (!eventExists)
                        throw new ArgumentException($"Event with ID {request.EventId.Value} not found", nameof(request.EventId));
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    Priority = request.Priority,
                    Category = request.Category,
                    UserId = request.UserId,
                    ClubId = request.ClubId,
                    EventId = request.EventId,
                    Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                    Channels = request.Channels ?? new List<NotificationChannelType> { NotificationChannelType.InApp },
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send notification through specified channels
                await ProcessNotificationChannelsAsync(notification, request.SendImmediately);

                if (request.UserId.HasValue)
                {
                    await _auditService.LogUserActionAsync(request.UserId.Value, "Notification Create", $"Created notification {notification.Id}");
                }
                return notification.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                throw;
            }
        }

        public async Task<Notification?> GetNotificationAsync(string notificationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(notificationId))
                    throw new ArgumentException("Notification ID cannot be null or empty", nameof(notificationId));

                return await _context.Notifications
                    .Include(n => n.User)
                    .Include(n => n.Club)
                    .Include(n => n.Event)
                    .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notification {notificationId}");
                throw;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, NotificationFilter? filter = null)
        {
            try
            {
                if (userId <= 0)
                    throw new ArgumentException("User ID must be greater than 0", nameof(userId));

                // Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserID == userId && u.IsActive);
                if (!userExists)
                    throw new ArgumentException($"User with ID {userId} not found or inactive", nameof(userId));

                var query = _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsDeleted);

                if (filter != null)
                {
                    if (filter.IsRead.HasValue)
                        query = query.Where(n => n.IsRead == filter.IsRead.Value);
                    
                    if (filter.Type.HasValue)
                        query = query.Where(n => n.Type == filter.Type.Value);
                    
                    if (filter.Category.HasValue)
                        query = query.Where(n => n.Category == filter.Category.Value);
                    
                    if (filter.Priority.HasValue)
                        query = query.Where(n => n.Priority == filter.Priority.Value);
                    
                    if (filter.FromDate.HasValue)
                        query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);
                    
                    if (filter.ToDate.HasValue)
                        query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);
                }

                return await query
                    .Include(n => n.Club)
                    .Include(n => n.Event)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(filter?.Limit ?? 50)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notifications for user {userId}");
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(string notificationId, int? userId = null)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted);

            if (notification == null || (userId.HasValue && notification.UserId != userId))
                return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> MarkMultipleAsReadAsync(IEnumerable<string> notificationIds, int? userId = null)
        {
            var query = _context.Notifications
                .Where(n => notificationIds.Contains(n.Id) && !n.IsDeleted && !n.IsRead);

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            var notifications = await query.ToListAsync();
            var readTime = DateTime.UtcNow;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = readTime;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId, int? userId = null)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted);

            if (notification == null || (userId.HasValue && notification.UserId != userId))
                return false;

            notification.IsDeleted = true;
            notification.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // Template management
        public async Task<string> CreateTemplateAsync(CreateTemplateRequest request)
        {
            try
            {
                // Input validation
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Template request cannot be null");
                
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Template name cannot be null or empty", nameof(request.Name));
                
                if (string.IsNullOrWhiteSpace(request.TitleTemplate))
                    throw new ArgumentException("Title template cannot be null or empty", nameof(request.TitleTemplate));
                
                if (string.IsNullOrWhiteSpace(request.MessageTemplate))
                    throw new ArgumentException("Message template cannot be null or empty", nameof(request.MessageTemplate));

                // Check for duplicate template name
                var existingTemplate = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == request.Name);
                if (existingTemplate != null)
                    throw new InvalidOperationException($"A template with name '{request.Name}' already exists");

                var template = new NotificationTemplate
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    Description = request.Description,
                    TitleTemplate = request.TitleTemplate,
                    MessageTemplate = request.MessageTemplate,
                    Type = request.Type,
                    Priority = request.Priority,
                    Category = request.Category,
                    Channels = request.Channels ?? new List<NotificationChannelType> { NotificationChannelType.InApp },
                    Parameters = request.Parameters ?? new List<string>(),
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.NotificationTemplates.Add(template);
                await _context.SaveChangesAsync();

                return template.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification template");
                throw;
            }
        }

        public async Task<NotificationTemplate?> GetTemplateAsync(string templateId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templateId))
                    throw new ArgumentException("Template ID cannot be null or empty", nameof(templateId));

                return await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving template {templateId}");
                throw;
            }
        }

        public async Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(bool? isActive = null)
        {
            var query = _context.NotificationTemplates.AsQueryable();
            
            if (isActive.HasValue)
                query = query.Where(t => t.IsActive == isActive.Value);

            return await query.OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<string> CreateFromTemplateAsync(string templateId, Dictionary<string, object> parameters, CreateNotificationRequest baseRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templateId))
                    throw new ArgumentException("Template ID cannot be null or empty", nameof(templateId));
                
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters), "Parameters cannot be null");
                
                if (baseRequest == null)
                    throw new ArgumentNullException(nameof(baseRequest), "Base request cannot be null");

                var template = await GetTemplateAsync(templateId);
                if (template == null || !template.IsActive)
                    throw new ArgumentException("Template not found or inactive");

                var title = ProcessTemplate(template.TitleTemplate, parameters);
                var message = ProcessTemplate(template.MessageTemplate, parameters);

                var request = new CreateNotificationRequest
                {
                    Title = title,
                    Message = message,
                    Type = template.Type,
                    Priority = template.Priority,
                    Category = template.Category,
                    Channels = template.Channels,
                    UserId = baseRequest.UserId,
                    ClubId = baseRequest.ClubId,
                    EventId = baseRequest.EventId,
                    Data = baseRequest.Data,
                    ExpiresAt = baseRequest.ExpiresAt,
                    SendImmediately = baseRequest.SendImmediately
                };

                return await CreateNotificationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating notification from template {templateId}");
                throw;
            }
        }

        // Bulk operations
        public async Task<IEnumerable<string>> CreateBulkNotificationsAsync(BulkNotificationRequest request)
        {
            try
            {
                // Input validation
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Bulk notification request cannot be null");
                
                if (string.IsNullOrWhiteSpace(request.Title))
                    throw new ArgumentException("Notification title cannot be null or empty", nameof(request.Title));
                
                if (string.IsNullOrWhiteSpace(request.Message))
                    throw new ArgumentException("Notification message cannot be null or empty", nameof(request.Message));
                
                if (request.UserIds == null || !request.UserIds.Any())
                    throw new ArgumentException("User IDs list cannot be null or empty", nameof(request.UserIds));

                // Validate all user IDs exist
                var validUserIds = await _context.Users
                    .Where(u => request.UserIds.Contains(u.UserID) && u.IsActive)
                    .Select(u => u.UserID)
                    .ToListAsync();
                
                var invalidUserIds = request.UserIds.Except(validUserIds).ToList();
                if (invalidUserIds.Any())
                    throw new ArgumentException($"Invalid or inactive user IDs: {string.Join(", ", invalidUserIds)}");

                var notificationIds = new List<string>();

                foreach (var userId in request.UserIds)
                {
                    var notificationRequest = new CreateNotificationRequest
                    {
                        Title = request.Title,
                        Message = request.Message,
                        Type = request.Type,
                        Priority = request.Priority,
                        Category = request.Category,
                        Channels = request.Channels,
                        UserId = userId,
                        ClubId = request.ClubId,
                        EventId = request.EventId,
                        Data = request.Data,
                        ExpiresAt = request.ExpiresAt,
                        SendImmediately = request.SendImmediately
                    };

                    var id = await CreateNotificationAsync(notificationRequest);
                    notificationIds.Add(id);
                }

                return notificationIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk notifications");
                throw;
            }
        }

        public async Task<int> DeleteMultipleNotificationsAsync(IEnumerable<string> notificationIds, int? userId = null)
        {
            var query = _context.Notifications
                .Where(n => notificationIds.Contains(n.Id) && !n.IsDeleted);

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            var notifications = await query.ToListAsync();
            var deleteTime = DateTime.UtcNow;

            foreach (var notification in notifications)
            {
                notification.IsDeleted = true;
                notification.DeletedAt = deleteTime;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        // Scheduling
        public async Task<string> ScheduleNotificationAsync(ScheduleNotificationRequest request)
        {
            var scheduledNotification = new ScheduledNotification
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                NotificationRequest = JsonSerializer.Serialize(request.NotificationRequest),
                ScheduledTime = request.ScheduledTime,
                RecurrencePattern = request.RecurrencePattern,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ScheduledNotifications.Add(scheduledNotification);
            await _context.SaveChangesAsync();

            return scheduledNotification.Id;
        }

        public async Task<IEnumerable<ScheduledNotification>> GetScheduledNotificationsAsync(bool? isActive = null)
        {
            var query = _context.ScheduledNotifications.AsQueryable();
            
            if (isActive.HasValue)
                query = query.Where(s => s.IsActive == isActive.Value);

            return await query.OrderBy(s => s.ScheduledTime).ToListAsync();
        }

        public async Task ProcessScheduledNotificationsAsync()
        {
            var now = DateTime.UtcNow;
            var dueNotifications = await _context.ScheduledNotifications
                .Where(s => s.IsActive && s.ScheduledTime <= now)
                .ToListAsync();

            foreach (var scheduled in dueNotifications)
            {
                try
                {
                    var request = JsonSerializer.Deserialize<CreateNotificationRequest>(scheduled.NotificationRequest);
                    if (request != null)
                    {
                        await CreateNotificationAsync(request);
                        scheduled.LastProcessedAt = now;
                        scheduled.ProcessCount++;

                        // Handle recurrence
                        if (!string.IsNullOrEmpty(scheduled.RecurrencePattern))
                        {
                            scheduled.ScheduledTime = CalculateNextScheduledTime(scheduled.ScheduledTime, scheduled.RecurrencePattern);
                        }
                        else
                        {
                            scheduled.IsActive = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing scheduled notification {scheduled.Id}");
                }
            }

            await _context.SaveChangesAsync();
        }

        // Statistics and reporting
        public async Task<NotificationStatistics> GetStatisticsAsync(int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Notifications.AsQueryable();
            
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);
            
            if (fromDate.HasValue)
                query = query.Where(n => n.CreatedAt >= fromDate.Value);
            
            if (toDate.HasValue)
                query = query.Where(n => n.CreatedAt <= toDate.Value);

            var total = await query.CountAsync();
            var read = await query.CountAsync(n => n.IsRead);
            var unread = total - read;
            var deleted = await query.CountAsync(n => n.IsDeleted);

            var byType = await query
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            var byCategory = await query
                .GroupBy(n => n.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);

            return new NotificationStatistics
            {
                TotalNotifications = total,
                ReadNotifications = read,
                UnreadNotifications = unread,
                DeletedNotifications = deleted,
                NotificationsByType = byType,
                NotificationsByCategory = byCategory
            };
        }

        // Email sending methods
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(to))
                    throw new ArgumentException("Email address cannot be null or empty", nameof(to));
                
                if (string.IsNullOrWhiteSpace(subject))
                    throw new ArgumentException("Email subject cannot be null or empty", nameof(subject));
                
                if (string.IsNullOrWhiteSpace(body))
                    throw new ArgumentException("Email body cannot be null or empty", nameof(body));

                // Validate email format
                if (!ValidationHelper.UserValidation.IsValidEmail(to))
                    throw new ArgumentException($"Invalid email address format: {to}", nameof(to));

                var smtpClient = ConfigureSmtpClient();
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:FromAddress"] ?? "noreply@clubmanagement.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                
                mailMessage.To.Add(to);
                
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                throw;
            }
        }

        public async Task SendEmailToMultipleAsync(List<string> recipients, string subject, string body)
        {
            try
            {
                // Input validation
                if (recipients == null || !recipients.Any())
                    throw new ArgumentException("Recipients list cannot be null or empty", nameof(recipients));
                
                if (string.IsNullOrWhiteSpace(subject))
                    throw new ArgumentException("Email subject cannot be null or empty", nameof(subject));
                
                if (string.IsNullOrWhiteSpace(body))
                    throw new ArgumentException("Email body cannot be null or empty", nameof(body));

                // Validate all email addresses
                var invalidEmails = recipients.Where(email => string.IsNullOrWhiteSpace(email) || !ValidationHelper.UserValidation.IsValidEmail(email)).ToList();
                if (invalidEmails.Any())
                    throw new ArgumentException($"Invalid email addresses: {string.Join(", ", invalidEmails)}");

                var tasks = recipients.Select(email => SendEmailAsync(email, subject, body));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emails to multiple recipients");
                throw;
            }
        }

        public async Task SendInAppNotificationAsync(int userId, string title, string message)
        {
            // In a real application, this would store notifications in database
            // and use SignalR for real-time delivery
            _logger.LogInformation($"In-app notification sent to user {userId}: {title}");
            
            // For now, we'll just log it. In production, implement with SignalR
            await Task.CompletedTask;
        }

        public async Task SendEventNotificationAsync(int eventId, string subject, string message, NotificationType type = NotificationType.Both)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventEntity == null) return;

            var participants = eventEntity.Participants.Select(p => p.User).ToList();

            foreach (var participant in participants)
            {
                if (type == NotificationType.Email || type == NotificationType.Both)
                {
                    await SendEmailAsync(participant.Email, subject, message);
                }

                if (type == NotificationType.InApp || type == NotificationType.Both)
                {
                    await SendInAppNotificationAsync(participant.UserID, subject, message);
                }
            }
        }

        public async Task SendClubNotificationAsync(int clubId, string subject, string message, NotificationType type = NotificationType.Both)
        {
            var clubMembers = await _context.Users
                .Where(u => u.ClubID == clubId && u.IsActive)
                .ToListAsync();

            foreach (var member in clubMembers)
            {
                if (type == NotificationType.Email || type == NotificationType.Both)
                {
                    await SendEmailAsync(member.Email, subject, message);
                }

                if (type == NotificationType.InApp || type == NotificationType.Both)
                {
                    await SendInAppNotificationAsync(member.UserID, subject, message);
                }
            }
        }

        public async Task SendRoleBasedNotificationAsync(SystemRole role, string subject, string message, int? clubId = null, NotificationType type = NotificationType.Both)
        {
            var query = _context.Users.Where(u => u.SystemRole == role && u.IsActive);
            
            if (clubId.HasValue)
            {
                query = query.Where(u => u.ClubID == clubId.Value);
            }

            var users = await query.ToListAsync();

            foreach (var user in users)
            {
                if (type == NotificationType.Email || type == NotificationType.Both)
                {
                    await SendEmailAsync(user.Email, subject, message);
                }

                if (type == NotificationType.InApp || type == NotificationType.Both)
                {
                    await SendInAppNotificationAsync(user.UserID, subject, message);
                }
            }
        }

        public async Task NotifyEventRegistrationAsync(int eventId, int userId)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            var user = await _context.Users.FindAsync(userId);

            if (eventEntity == null || user == null) return;

            var subject = $"Event Registration Confirmation: {eventEntity.Name}";
            var message = $"Dear {user.FullName},\n\nYou have successfully registered for the event '{eventEntity.Name}' scheduled on {eventEntity.EventDate:yyyy-MM-dd HH:mm}.\n\nLocation: {eventEntity.Location}\n\nThank you!";

            await SendEmailAsync(user.Email, subject, message);
            await SendInAppNotificationAsync(userId, subject, message);
        }

        public async Task NotifyEventReminderAsync(int eventId)
        {
            // Default reminder time of 1 day before the event
            await NotifyEventReminderAsync(eventId, TimeSpan.FromDays(1));
        }

        public async Task NotifyEventReminderAsync(int eventId, TimeSpan reminderTime)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventEntity == null) return;

            var subject = $"Event Reminder: {eventEntity.Name}";
            var message = $"This is a reminder that the event '{eventEntity.Name}' is scheduled for {eventEntity.EventDate:yyyy-MM-dd HH:mm}.\n\nLocation: {eventEntity.Location}\n\nDon't forget to attend!";

            await SendEventNotificationAsync(eventId, subject, message, NotificationType.Both);
        }

        public async Task NotifyMembershipChangeAsync(int userId, int clubId, string changeType)
        {
            var user = await _context.Users.FindAsync(userId);
            var club = await _context.Clubs.FindAsync(clubId);

            if (user == null || club == null) return;

            var subject = $"Membership {changeType}: {club.Name}";
            var message = $"Dear {user.FullName},\n\nYour membership status in '{club.Name}' has been {changeType.ToLower()}.\n\nThank you!";

            await SendEmailAsync(user.Email, subject, message);
            await SendInAppNotificationAsync(userId, subject, message);
        }

        public async Task SendNotificationAsync(NotificationRequest request)
        {
            try
            {
                if (request.Type == NotificationType.Email || request.Type == NotificationType.Both)
                {
                    await SendEmailToMultipleAsync(request.Recipients, request.Subject, request.Message);
                }

                // For in-app notifications, we would need user IDs instead of email addresses
                // This is a simplified implementation
                if (request.Type == NotificationType.InApp || request.Type == NotificationType.Both)
                {
                    _logger.LogInformation($"In-app notification would be sent: {request.Subject}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification");
                throw;
            }
        }

        // Helper methods
        private async Task ProcessNotificationChannelsAsync(Notification notification, bool sendImmediately)
        {
            if (!sendImmediately)
                return;

            foreach (var channel in notification.Channels)
            {
                try
                {
                    await ProcessSingleChannelAsync(notification, channel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending notification through {channel} channel");
                }
            }
        }

        private async Task ProcessSingleChannelAsync(Notification notification, NotificationChannelType channel)
        {
            switch (channel)
            {
                case NotificationChannelType.Email:
                    await ProcessEmailChannelAsync(notification);
                    break;

                case NotificationChannelType.InApp:
                    await ProcessInAppChannelAsync(notification);
                    break;

                case NotificationChannelType.Push:
                    await ProcessPushChannelAsync(notification);
                    break;
            }
        }

        private async Task ProcessEmailChannelAsync(Notification notification)
        {
            if (!notification.UserId.HasValue)
                return;

            var user = await _context.Users.FindAsync(notification.UserId.Value);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await SendEmailAsync(user.Email, notification.Title, notification.Message);
            }
        }

        private async Task ProcessInAppChannelAsync(Notification notification)
        {
            if (notification.UserId.HasValue)
            {
                await SendInAppNotificationAsync(notification.UserId.Value, notification.Title, notification.Message);
            }
        }

        private async Task ProcessPushChannelAsync(Notification notification)
        {
            // Implement push notification logic here
            _logger.LogInformation($"Push notification would be sent: {notification.Title}");
            await Task.CompletedTask; // Placeholder for actual push notification implementation
        }

        private string ProcessTemplate(string template, Dictionary<string, object> parameters)
        {
            var result = template;
            foreach (var param in parameters)
            {
                var placeholder = $"{{{param.Key}}}";
                result = result.Replace(placeholder, param.Value?.ToString() ?? "");
            }
            return result;
        }

        private DateTime CalculateNextScheduledTime(DateTime currentTime, string recurrencePattern)
        {
            // Simple recurrence pattern implementation
            // In production, you might want to use a more sophisticated library like NCrontab
            return recurrencePattern.ToLower() switch
            {
                "daily" => currentTime.AddDays(1),
                "weekly" => currentTime.AddDays(7),
                "monthly" => currentTime.AddMonths(1),
                "yearly" => currentTime.AddYears(1),
                _ => currentTime.AddDays(1) // Default to daily
            };
        }

        // Cleanup and maintenance
         public async Task<NotificationMaintenanceInfo> CleanupExpiredNotificationsAsync()
         {
             var startTime = DateTime.UtcNow;
             var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep notifications for 30 days
             
             var expiredNotifications = await _context.Notifications
                 .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTime.UtcNow)
                 .ToListAsync();
             
             var oldDeletedNotifications = await _context.Notifications
                 .Where(n => n.IsDeleted && n.DeletedAt.HasValue && n.DeletedAt.Value < cutoffDate)
                 .ToListAsync();
             
             // Mark expired notifications as deleted
             foreach (var notification in expiredNotifications)
             {
                 notification.IsDeleted = true;
                 notification.DeletedAt = DateTime.UtcNow;
             }
             
             // Remove old deleted notifications
             _context.Notifications.RemoveRange(oldDeletedNotifications);
             
             await _context.SaveChangesAsync();
             
             var endTime = DateTime.UtcNow;
             
             return new NotificationMaintenanceInfo
             {
                 ExpiredNotifications = expiredNotifications.Count,
                 DeletedNotifications = oldDeletedNotifications.Count,
                 ArchivedNotifications = 0,
                 LastCleanupDate = endTime,
                 CleanupDuration = endTime - startTime
             };
         }
         
         public async Task<bool> CancelScheduledNotificationAsync(string scheduledNotificationId)
         {
             var scheduled = await _context.ScheduledNotifications
                 .FirstOrDefaultAsync(s => s.Id == scheduledNotificationId);
             
             if (scheduled == null)
                 return false;
             
             scheduled.IsActive = false;
             scheduled.CancelledAt = DateTime.UtcNow;
             await _context.SaveChangesAsync();
             
             return true;
         }
         
         // Event handlers for automatic notifications
         public async Task HandleUserRegistrationAsync(int userId)
         {
             var user = await _context.Users.FindAsync(userId);
             if (user == null) return;
             
             var request = new CreateNotificationRequest
             {
                 Title = "Welcome to Club Management!",
                 Message = $"Welcome {user.FullName}! Your account has been successfully created.",
                 Type = NotificationType.Welcome,
                 Category = NotificationCategory.Account,
                 Priority = NotificationPriority.Normal,
                 UserId = userId,
                 Channels = new List<NotificationChannelType> { NotificationChannelType.Email, NotificationChannelType.InApp }
             };
             
             await CreateNotificationAsync(request);
         }
         
         public async Task HandleEventReminderAsync(int eventId, TimeSpan reminderTime)
         {
             var eventEntity = await _context.Events
                 .Include(e => e.Participants)
                 .ThenInclude(p => p.User)
                 .FirstOrDefaultAsync(e => e.EventID == eventId);
             
             if (eventEntity == null) return;
             
             var reminderDate = eventEntity.EventDate - reminderTime;
             
             foreach (var participant in eventEntity.Participants.Where(p => p.Status == AttendanceStatus.Registered))
             {
                 var request = new CreateNotificationRequest
                 {
                     Title = $"Event Reminder: {eventEntity.Name}",
                     Message = $"Don't forget! {eventEntity.Name} is scheduled for {eventEntity.EventDate:MMM dd, yyyy} at {eventEntity.EventDate:HH:mm}.",
                     Type = NotificationType.EventReminder,
                     Category = NotificationCategory.Events,
                     Priority = NotificationPriority.High,
                     UserId = participant.UserID,
                     EventId = eventId,
                     Channels = new List<NotificationChannelType> { NotificationChannelType.Email, NotificationChannelType.InApp }
                 };
                 
                 var scheduleRequest = new ScheduleNotificationRequest
                 {
                     Name = $"Event Reminder - {eventEntity.Name} - User {participant.UserID}",
                     NotificationRequest = request,
                     ScheduledTime = reminderDate
                 };
                 
                 await ScheduleNotificationAsync(scheduleRequest);
             }
         }
         
         public async Task HandlePasswordResetAsync(int userId, string resetToken)
         {
             var user = await _context.Users.FindAsync(userId);
             if (user == null) return;
             
             var request = new CreateNotificationRequest
             {
                 Title = "Password Reset Request",
                 Message = $"A password reset has been requested for your account. If this wasn't you, please contact support immediately.",
                 Type = NotificationType.PasswordReset,
                 Category = NotificationCategory.Security,
                 Priority = NotificationPriority.High,
                 UserId = userId,
                 Data = new Dictionary<string, object> { { "resetToken", resetToken } },
                 Channels = new List<NotificationChannelType> { NotificationChannelType.Email }
             };
             
             await CreateNotificationAsync(request);
         }
         
         public async Task HandleSystemMaintenanceAsync(DateTime maintenanceStart, DateTime maintenanceEnd, string description)
         {
             var allUsers = await _context.Users.Where(u => u.IsActive).ToListAsync();
             
             var userIds = allUsers.Select(u => u.UserID).ToList();
             
             var request = new BulkNotificationRequest
             {
                 UserIds = userIds,
                 Title = "Scheduled System Maintenance",
                 Message = $"System maintenance is scheduled from {maintenanceStart:MMM dd, yyyy HH:mm} to {maintenanceEnd:MMM dd, yyyy HH:mm}. {description}",
                 Type = NotificationType.SystemMaintenance,
                 Category = NotificationCategory.System,
                 Priority = NotificationPriority.High,
                 Channels = new List<NotificationChannelType> { NotificationChannelType.Email, NotificationChannelType.InApp }
             };
             
             await CreateBulkNotificationsAsync(request);
         }

         public void Dispose()
         {
             _smtpClient?.Dispose();
         }
    }
}