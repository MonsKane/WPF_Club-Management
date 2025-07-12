using ClubManagementApp.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    public interface IEmailService
    {
        // Basic email operations
        Task<bool> SendEmailAsync(EmailMessage message);
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task<bool> SendBulkEmailAsync(List<EmailMessage> messages);

        // Template-based emails
        Task<bool> SendWelcomeEmailAsync(User user);
        Task<bool> SendEventNotificationAsync(User user, Event eventInfo);
        Task<bool> SendEventReminderAsync(User user, Event eventInfo);
        Task<bool> SendMembershipApprovalAsync(User user, Club club);
        Task<bool> SendPasswordResetAsync(User user, string resetToken);
        Task<bool> SendReportGeneratedAsync(User user, Report report);
        Task<bool> SendClubInvitationAsync(User user, Club club, User invitedBy);

        // Email queue management
        Task QueueEmailAsync(EmailMessage message, DateTime? scheduledTime = null);
        Task ProcessEmailQueueAsync();
        Task<List<QueuedEmail>> GetPendingEmailsAsync();
        Task<EmailStatistics> GetEmailStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        // Email templates
        Task<string> GetEmailTemplateAsync(EmailTemplateType templateType);
        Task SaveEmailTemplateAsync(EmailTemplateType templateType, string template);
        Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables);

        // Configuration and testing
        Task<bool> TestEmailConfigurationAsync();
        Task<EmailConfiguration> GetEmailConfigurationAsync();
        Task UpdateEmailConfigurationAsync(EmailConfiguration configuration);
    }

    public class EmailService : IEmailService
    {
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configurationService;
        private readonly ISettingsService _settingsService;
        private EmailConfiguration? _emailConfiguration;

        public EmailService(
            ILoggingService loggingService,
            IAuditService auditService,
            IConfiguration configurationService,
            ISettingsService settingsService)
        {
            _loggingService = loggingService;
            _auditService = auditService;
            _configurationService = configurationService;
            _settingsService = settingsService;
        }

        public async Task<bool> SendEmailAsync(EmailMessage message)
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEnabled)
                {
                    await _loggingService.LogWarningAsync("Email service is disabled");
                    return false;
                }

                using (var client = new SmtpClient(config.SmtpServer, config.SmtpPort))
                {
                    client.EnableSsl = config.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(config.Username, config.Password);

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(config.FromEmail, config.FromName);

                        // Add recipients
                        foreach (var to in message.ToAddresses)
                        {
                            mailMessage.To.Add(to);
                        }

                        foreach (var cc in message.CcAddresses ?? new List<string>())
                        {
                            mailMessage.CC.Add(cc);
                        }

                        foreach (var bcc in message.BccAddresses ?? new List<string>())
                        {
                            mailMessage.Bcc.Add(bcc);
                        }

                        mailMessage.Subject = message.Subject;
                        mailMessage.Body = message.Body;
                        mailMessage.IsBodyHtml = message.IsHtml;
                        mailMessage.Priority = message.Priority switch
                        {
                            EmailPriority.High => MailPriority.High,
                            EmailPriority.Low => MailPriority.Low,
                            _ => MailPriority.Normal
                        };

                        // Add attachments
                        if (message.Attachments?.Any() == true)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                if (File.Exists(attachment.FilePath))
                                {
                                    mailMessage.Attachments.Add(new Attachment(attachment.FilePath, attachment.ContentType));
                                }
                            }
                        }

                        await client.SendMailAsync(mailMessage);
                    }
                }

                await _auditService.LogSystemEventAsync("Email Sent",
                    $"Email sent to {string.Join(", ", message.ToAddresses)} - Subject: {message.Subject}");
                await _loggingService.LogInformationAsync($"Email sent successfully to {string.Join(", ", message.ToAddresses)}");

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send email to {string.Join(", ", message.ToAddresses)}", ex);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            var message = new EmailMessage
            {
                ToAddresses = new List<string> { to },
                Subject = subject,
                Body = body,
                IsHtml = isHtml
            };

            return await SendEmailAsync(message);
        }

        public async Task<bool> SendBulkEmailAsync(List<EmailMessage> messages)
        {
            var successCount = 0;
            var totalCount = messages.Count;

            foreach (var message in messages)
            {
                if (await SendEmailAsync(message))
                {
                    successCount++;
                }

                // Add small delay to avoid overwhelming the SMTP server
                await Task.Delay(100);
            }

            await _loggingService.LogInformationAsync($"Bulk email completed: {successCount}/{totalCount} emails sent successfully");
            return successCount == totalCount;
        }

        public async Task<bool> SendWelcomeEmailAsync(User user)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.Welcome);
                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "Email", user.Email },
                    { "StudentId", user.StudentID ?? "" },
                    { "JoinDate", user.JoinDate.ToString("MMMM dd, yyyy") },
                    { "ClubName", user.Club?.Name ?? "Club Management System" }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, "Welcome to Club Management System", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send welcome email to {user.Email}", ex);
                return false;
            }
        }

        public async Task<bool> SendEventNotificationAsync(User user, Event eventInfo)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.EventNotification);
                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "EventName", eventInfo.Name },
                    { "EventDate", eventInfo.EventDate.ToString("MMMM dd, yyyy") },
                    { "EventTime", eventInfo.EventDate.ToString("hh:mm tt") },
                    { "EventLocation", eventInfo.Location ?? "" },
                    { "EventDescription", eventInfo.Description ?? "" },
                    { "ClubName", eventInfo.Club?.Name ?? "" },
                    { "RegistrationDeadline", eventInfo.RegistrationDeadline?.ToString("MMMM dd, yyyy") ?? "Not specified" }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, $"New Event: {eventInfo.Name}", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send event notification to {user.Email}", ex);
                return false;
            }
        }

        public async Task<bool> SendEventReminderAsync(User user, Event eventInfo)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.EventReminder);
                var hoursUntilEvent = (eventInfo.EventDate - DateTime.Now).TotalHours;

                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "EventName", eventInfo.Name },
                    { "EventDate", eventInfo.EventDate.ToString("MMMM dd, yyyy") },
                    { "EventTime", eventInfo.EventDate.ToString("hh:mm tt") },
                    { "EventLocation", eventInfo.Location ?? "" },
                    { "HoursUntilEvent", Math.Round(hoursUntilEvent, 1) },
                    { "ClubName", eventInfo.Club?.Name ?? "" }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, $"Reminder: {eventInfo.Name}", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send event reminder to {user.Email}", ex);
                return false;
            }
        }

        public async Task<bool> SendMembershipApprovalAsync(User user, Club club)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.MembershipApproval);
                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "ClubName", club.Name },
                    { "ClubDescription", club.Description ?? "" },
                    { "ApprovalDate", DateTime.Now.ToString("MMMM dd, yyyy") }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, $"Membership Approved: {club.Name}", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send membership approval email to {user.Email}", ex);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(User user, string resetToken)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.PasswordReset);
                var resetLink = $"https://clubmanagement.app/reset-password?token={resetToken}";

                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "ResetLink", resetLink },
                    { "ExpirationTime", "24 hours" },
                    { "RequestTime", DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt") }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, "Password Reset Request", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send password reset email to {user.Email}", ex);
                return false;
            }
        }

        public async Task<bool> SendReportGeneratedAsync(User user, Report report)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.ReportGenerated);
                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "ReportTitle", report.Title },
                    { "ReportType", report.Type.ToString() },
                    { "GenerationDate", report.GeneratedDate.ToString("MMMM dd, yyyy") },
                    { "ClubName", report.Club?.Name ?? "System" }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, $"Report Generated: {report.Title}", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send report notification to {user.Email}", ex);
                return false;
            }
        }

        public async Task<bool> SendClubInvitationAsync(User user, Club club, User invitedBy)
        {
            try
            {
                var template = await GetEmailTemplateAsync(EmailTemplateType.ClubInvitation);
                var variables = new Dictionary<string, object>
                {
                    { "UserName", user.FullName },
                    { "ClubName", club.Name },
                    { "ClubDescription", club.Description ?? "" },
                    { "InvitedByName", invitedBy.FullName },
                    { "InvitationDate", DateTime.Now.ToString("MMMM dd, yyyy") }
                };

                var body = await ProcessTemplateAsync(template, variables);

                return await SendEmailAsync(user.Email, $"Invitation to Join {club.Name}", body, true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to send club invitation to {user.Email}", ex);
                return false;
            }
        }

        public async Task QueueEmailAsync(EmailMessage message, DateTime? scheduledTime = null)
        {
            try
            {
                var queuedEmail = new QueuedEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = message,
                    ScheduledTime = scheduledTime ?? DateTime.UtcNow,
                    Status = EmailStatus.Queued,
                    CreatedAt = DateTime.UtcNow,
                    Attempts = 0
                };

                await SaveQueuedEmailAsync(queuedEmail);
                await _loggingService.LogInformationAsync($"Email queued for {string.Join(", ", message.ToAddresses)}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to queue email", ex);
            }
        }

        public async Task ProcessEmailQueueAsync()
        {
            try
            {
                var pendingEmails = await GetPendingEmailsAsync();
                var processableEmails = pendingEmails
                    .Where(e => e.ScheduledTime <= DateTime.UtcNow && e.Attempts < 3)
                    .OrderBy(e => e.ScheduledTime)
                    .Take(10) // Process up to 10 emails at a time
                    .ToList();

                foreach (var queuedEmail in processableEmails)
                {
                    try
                    {
                        queuedEmail.Attempts++;
                        queuedEmail.LastAttemptAt = DateTime.UtcNow;

                        var success = await SendEmailAsync(queuedEmail.Message);

                        if (success)
                        {
                            queuedEmail.Status = EmailStatus.Sent;
                            queuedEmail.SentAt = DateTime.UtcNow;
                        }
                        else
                        {
                            queuedEmail.Status = queuedEmail.Attempts >= 3 ? EmailStatus.Failed : EmailStatus.Queued;
                            if (queuedEmail.Attempts >= 3)
                            {
                                queuedEmail.ErrorMessage = "Maximum retry attempts exceeded";
                            }
                        }

                        await UpdateQueuedEmailAsync(queuedEmail);
                    }
                    catch (Exception ex)
                    {
                        queuedEmail.Status = queuedEmail.Attempts >= 3 ? EmailStatus.Failed : EmailStatus.Queued;
                        queuedEmail.ErrorMessage = ex.Message;
                        await UpdateQueuedEmailAsync(queuedEmail);
                        await _loggingService.LogErrorAsync($"Failed to process queued email {queuedEmail.Id}", ex);
                    }

                    // Small delay between emails
                    await Task.Delay(200);
                }

                if (processableEmails.Any())
                {
                    await _loggingService.LogInformationAsync($"Processed {processableEmails.Count} queued emails");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to process email queue", ex);
            }
        }

        public async Task<List<QueuedEmail>> GetPendingEmailsAsync()
        {
            try
            {
                var queuePath = GetEmailQueuePath();
                if (!File.Exists(queuePath))
                    return new List<QueuedEmail>();

                var jsonData = await File.ReadAllTextAsync(queuePath);
                var allEmails = JsonSerializer.Deserialize<List<QueuedEmail>>(jsonData) ?? new List<QueuedEmail>();

                return allEmails.Where(e => e.Status == EmailStatus.Queued).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get pending emails", ex);
                return new List<QueuedEmail>();
            }
        }

        public async Task<EmailStatistics> GetEmailStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var queuePath = GetEmailQueuePath();
                if (!File.Exists(queuePath))
                    return new EmailStatistics();

                var jsonData = await File.ReadAllTextAsync(queuePath);
                var allEmails = JsonSerializer.Deserialize<List<QueuedEmail>>(jsonData) ?? new List<QueuedEmail>();

                var filteredEmails = allEmails.AsQueryable();

                if (fromDate.HasValue)
                    filteredEmails = filteredEmails.Where(e => e.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    filteredEmails = filteredEmails.Where(e => e.CreatedAt <= toDate.Value);

                var emailList = filteredEmails.ToList();

                return new EmailStatistics
                {
                    TotalEmails = emailList.Count,
                    SentEmails = emailList.Count(e => e.Status == EmailStatus.Sent),
                    FailedEmails = emailList.Count(e => e.Status == EmailStatus.Failed),
                    QueuedEmails = emailList.Count(e => e.Status == EmailStatus.Queued),
                    AverageDeliveryTime = emailList
                        .Where(e => e.Status == EmailStatus.Sent && e.SentAt.HasValue)
                        .Select(e => (e.SentAt!.Value - e.CreatedAt).TotalMinutes)
                        .DefaultIfEmpty(0)
                        .Average()
                };
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get email statistics", ex);
                return new EmailStatistics();
            }
        }

        public async Task<string> GetEmailTemplateAsync(EmailTemplateType templateType)
        {
            try
            {
                var templateKey = $"email.template.{templateType.ToString().ToLower()}";
                var template = await _settingsService.GetGlobalSettingAsync<string>(templateKey);

                if (string.IsNullOrEmpty(template))
                {
                    template = GetDefaultTemplate(templateType);
                    await _settingsService.SetGlobalSettingAsync(templateKey, template);
                }

                return template;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get email template {templateType}", ex);
                return GetDefaultTemplate(templateType);
            }
        }

        public async Task SaveEmailTemplateAsync(EmailTemplateType templateType, string template)
        {
            try
            {
                var templateKey = $"email.template.{templateType.ToString().ToLower()}";
                await _settingsService.SetGlobalSettingAsync(templateKey, template);
                await _auditService.LogSystemEventAsync("Email Template Updated", $"Template {templateType} updated");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to save email template {templateType}", ex);
                throw;
            }
        }

        public async Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables)
        {
            try
            {
                var result = template;

                foreach (var variable in variables)
                {
                    var placeholder = $"{{{{{variable.Key}}}}}";
                    result = result.Replace(placeholder, variable.Value?.ToString() ?? "");
                }

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to process email template", ex);
                return template;
            }
        }

        public async Task<bool> TestEmailConfigurationAsync()
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEnabled)
                    return false;

                var testMessage = new EmailMessage
                {
                    ToAddresses = new List<string> { config.FromEmail },
                    Subject = "Email Configuration Test",
                    Body = "This is a test email to verify the email configuration is working correctly.",
                    IsHtml = false
                };

                return await SendEmailAsync(testMessage);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Email configuration test failed", ex);
                return false;
            }
        }

        public Task<EmailConfiguration> GetEmailConfigurationAsync()
        {
            if (_emailConfiguration == null)
            {
                _emailConfiguration = new EmailConfiguration
                {
                    SmtpServer = _configurationService.GetValue<string>("EmailSettings:SmtpServer", "smtp.gmail.com"),
                    SmtpPort = _configurationService.GetValue<int>("EmailSettings:SmtpPort", 587),
                    EnableSsl = _configurationService.GetValue<bool>("EmailSettings:EnableSsl", true),
                    Username = _configurationService.GetValue<string>("EmailSettings:Username", ""),
                    Password = _configurationService.GetValue<string>("EmailSettings:Password", ""),
                    FromEmail = _configurationService.GetValue<string>("EmailSettings:FromEmail", ""),
                    FromName = _configurationService.GetValue<string>("EmailSettings:FromName", "Club Management System"),
                    IsEnabled = _configurationService.GetValue<bool>("EmailSettings:IsEnabled", false)
                };
            }

            return Task.FromResult(_emailConfiguration);
        }

        //public async Task UpdateEmailConfigurationAsync(EmailConfiguration configuration)
        //{
        //    try
        //    {
        //        await _configurationService.SetAsync("EmailSettings:SmtpServer", configuration.SmtpServer);
        //        await _configurationService.SetAsync("EmailSettings:SmtpPort", configuration.SmtpPort);
        //        await _configurationService.SetAsync("EmailSettings:EnableSsl", configuration.EnableSsl);
        //        await _configurationService.SetAsync("EmailSettings:Username", configuration.Username);
        //        await _configurationService.SetAsync("EmailSettings:Password", configuration.Password);
        //        await _configurationService.SetAsync("EmailSettings:FromEmail", configuration.FromEmail);
        //        await _configurationService.SetAsync("EmailSettings:FromName", configuration.FromName);
        //        await _configurationService.SetAsync("EmailSettings:IsEnabled", configuration.IsEnabled);

        //        await _configurationService.SaveAsync();
        //        _emailConfiguration = null; // Reset cache

        //        await _auditService.LogSystemEventAsync("Email Configuration Updated", "Email settings have been updated");
        //    }
        //    catch (Exception ex)
        //    {
        //        await _loggingService.LogErrorAsync("Failed to update email configuration", ex);
        //        throw;
        //    }
        //}

        private string GetDefaultTemplate(EmailTemplateType templateType)
        {
            return templateType switch
            {
                EmailTemplateType.Welcome => @"
                    <h2>Welcome to Club Management System!</h2>
                    <p>Dear {{UserName}},</p>
                    <p>Welcome to the Club Management System! Your account has been successfully created.</p>
                    <p><strong>Account Details:</strong></p>
                    <ul>
                        <li>Email: {{Email}}</li>
                        <li>Student ID: {{StudentId}}</li>
                        <li>Join Date: {{JoinDate}}</li>
                        <li>Club: {{ClubName}}</li>
                    </ul>
                    <p>You can now log in and start participating in club activities.</p>
                    <p>Best regards,<br>Club Management Team</p>",

                EmailTemplateType.EventNotification => @"
                    <h2>New Event: {{EventName}}</h2>
                    <p>Dear {{UserName}},</p>
                    <p>A new event has been scheduled by {{ClubName}}:</p>
                    <p><strong>Event Details:</strong></p>
                    <ul>
                        <li>Event: {{EventName}}</li>
                        <li>Date: {{EventDate}} at {{EventTime}}</li>
                        <li>Location: {{EventLocation}}</li>
                        <li>Registration Deadline: {{RegistrationDeadline}}</li>
                    </ul>
                    <p><strong>Description:</strong></p>
                    <p>{{EventDescription}}</p>
                    <p>Don't miss out on this exciting opportunity!</p>
                    <p>Best regards,<br>{{ClubName}}</p>",

                EmailTemplateType.EventReminder => @"
                    <h2>Event Reminder: {{EventName}}</h2>
                    <p>Dear {{UserName}},</p>
                    <p>This is a friendly reminder about the upcoming event:</p>
                    <p><strong>{{EventName}}</strong></p>
                    <p>Date: {{EventDate}} at {{EventTime}}</p>
                    <p>Location: {{EventLocation}}</p>
                    <p>The event is starting in approximately {{HoursUntilEvent}} hours.</p>
                    <p>We look forward to seeing you there!</p>
                    <p>Best regards,<br>{{ClubName}}</p>",

                EmailTemplateType.MembershipApproval => @"
                    <h2>Membership Approved!</h2>
                    <p>Dear {{UserName}},</p>
                    <p>Congratulations! Your membership application for <strong>{{ClubName}}</strong> has been approved.</p>
                    <p><strong>Club Information:</strong></p>
                    <p>{{ClubDescription}}</p>
                    <p>Approval Date: {{ApprovalDate}}</p>
                    <p>You can now participate in all club activities and events.</p>
                    <p>Welcome to the club!</p>
                    <p>Best regards,<br>{{ClubName}} Leadership</p>",

                EmailTemplateType.PasswordReset => @"
                    <h2>Password Reset Request</h2>
                    <p>Dear {{UserName}},</p>
                    <p>We received a request to reset your password on {{RequestTime}}.</p>
                    <p>Click the link below to reset your password:</p>
                    <p><a href='{{ResetLink}}'>Reset Password</a></p>
                    <p>This link will expire in {{ExpirationTime}}.</p>
                    <p>If you didn't request this password reset, please ignore this email.</p>
                    <p>Best regards,<br>Club Management Team</p>",

                EmailTemplateType.ReportGenerated => @"
                    <h2>Report Generated</h2>
                    <p>Dear {{UserName}},</p>
                    <p>A new report has been generated:</p>
                    <p><strong>Report Details:</strong></p>
                    <ul>
                        <li>Title: {{ReportTitle}}</li>
                        <li>Type: {{ReportType}}</li>
                        <li>Generated: {{GenerationDate}}</li>
                        <li>Club: {{ClubName}}</li>
                    </ul>
                    <p>The report is now available in the system.</p>
                    <p>Best regards,<br>Club Management Team</p>",

                EmailTemplateType.ClubInvitation => @"
                    <h2>Club Invitation</h2>
                    <p>Dear {{UserName}},</p>
                    <p>{{InvitedByName}} has invited you to join <strong>{{ClubName}}</strong>!</p>
                    <p><strong>About the Club:</strong></p>
                    <p>{{ClubDescription}}</p>
                    <p>Invitation Date: {{InvitationDate}}</p>
                    <p>If you're interested in joining, please contact the club leadership or log into the system to accept the invitation.</p>
                    <p>Best regards,<br>{{ClubName}}</p>",

                _ => "<p>Default email template</p>"
            };
        }

        private async Task SaveQueuedEmailAsync(QueuedEmail queuedEmail)
        {
            var queuePath = GetEmailQueuePath();
            var allEmails = new List<QueuedEmail>();

            if (File.Exists(queuePath))
            {
                var jsonData = await File.ReadAllTextAsync(queuePath);
                allEmails = JsonSerializer.Deserialize<List<QueuedEmail>>(jsonData) ?? new List<QueuedEmail>();
            }

            allEmails.Add(queuedEmail);

            var updatedJsonData = JsonSerializer.Serialize(allEmails, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(queuePath, updatedJsonData);
        }

        private async Task UpdateQueuedEmailAsync(QueuedEmail queuedEmail)
        {
            var queuePath = GetEmailQueuePath();
            if (!File.Exists(queuePath))
                return;

            var jsonData = await File.ReadAllTextAsync(queuePath);
            var allEmails = JsonSerializer.Deserialize<List<QueuedEmail>>(jsonData) ?? new List<QueuedEmail>();

            var existingEmail = allEmails.FirstOrDefault(e => e.Id == queuedEmail.Id);
            if (existingEmail != null)
            {
                var index = allEmails.IndexOf(existingEmail);
                allEmails[index] = queuedEmail;

                var updatedJsonData = JsonSerializer.Serialize(allEmails, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(queuePath, updatedJsonData);
            }
        }

        private string GetEmailQueuePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var clubManagementPath = Path.Combine(appDataPath, "ClubManagement");
            Directory.CreateDirectory(clubManagementPath);
            return Path.Combine(clubManagementPath, "email_queue.json");
        }

        public Task UpdateEmailConfigurationAsync(EmailConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }

    // Supporting classes and enums
    public class EmailMessage
    {
        public List<string> ToAddresses { get; set; } = new();
        public List<string>? CcAddresses { get; set; }
        public List<string>? BccAddresses { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
        public List<EmailAttachment>? Attachments { get; set; }
    }

    public class EmailAttachment
    {
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string? DisplayName { get; set; }
    }

    public class QueuedEmail
    {
        public string Id { get; set; } = string.Empty;
        public EmailMessage Message { get; set; } = new();
        public DateTime ScheduledTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public EmailStatus Status { get; set; }
        public int Attempts { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class EmailConfiguration
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = false;
    }

    public class EmailStatistics
    {
        public int TotalEmails { get; set; }
        public int SentEmails { get; set; }
        public int FailedEmails { get; set; }
        public int QueuedEmails { get; set; }
        public double AverageDeliveryTime { get; set; }
    }

    public enum EmailTemplateType
    {
        Welcome,
        EventNotification,
        EventReminder,
        MembershipApproval,
        PasswordReset,
        ReportGenerated,
        ClubInvitation
    }

    public enum EmailPriority
    {
        Low,
        Normal,
        High
    }

    public enum EmailStatus
    {
        Queued,
        Sent,
        Failed
    }
}