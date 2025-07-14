using ClubManagementApp.DTOs;
using ClubManagementApp.Helpers;
using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IBusinessLogicManager
    {
        // User Management Workflows
        Task<UserDto?> RegisterNewMemberAsync(CreateUserDto createUserDto, int currentUserId);
        Task<bool> TransferMembershipAsync(int userId, int newClubId, int currentUserId);
        Task<bool> PromoteMemberAsync(int userId, SystemRole newRole, int currentUserId);
        Task<bool> DeactivateMemberAsync(int userId, int currentUserId);

        // Event Management Workflows
        Task<EventDto?> CreateEventWithNotificationAsync(CreateEventDto createEventDto, int currentUserId);
        Task<bool> CancelEventAsync(int eventId, string reason, int currentUserId);
        Task<bool> ProcessEventAttendanceAsync(int eventId, List<EventParticipantDto> attendanceData, int currentUserId);

        // Club Management Workflows
        Task<ClubDto?> EstablishNewClubAsync(CreateClubDto createClubDto, int chairmanUserId, int currentUserId);
        Task<bool> RestructureClubLeadershipAsync(int clubId, ClubLeadershipDto newLeadership, int currentUserId);
        Task<ClubStatisticsDto> GetComprehensiveClubStatisticsAsync(int clubId, int currentUserId);

        // Report Generation Workflows
        Task<ReportDto?> GenerateComprehensiveReportAsync(ReportType reportType, int clubId, string semester, int currentUserId);
        Task<bool> SchedulePeriodicReportsAsync(int clubId, List<ReportType> reportTypes, string semester, int currentUserId);

        // Notification Workflows
        Task<bool> SendEventReminderNotificationsAsync(int eventId, int currentUserId);
        Task<bool> SendMembershipChangeNotificationAsync(int userId, string changeType, int currentUserId);
        Task<bool> SendClubAnnouncementAsync(int clubId, string title, string message, int currentUserId);
    }

    public class BusinessLogicManager : IBusinessLogicManager
    {
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly IEventService _eventService;
        private readonly IReportService _reportService;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;

        public BusinessLogicManager(
            IUserService userService,
            IClubService clubService,
            IEventService eventService,
            IReportService reportService,
            INotificationService notificationService,
            IAuthorizationService authorizationService)
        {
            _userService = userService;
            _clubService = clubService;
            _eventService = eventService;
            _reportService = reportService;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
        }

        public async Task<UserDto?> RegisterNewMemberAsync(CreateUserDto createUserDto, int currentUserId)
        {
            if (!await ValidateUserRegistrationAsync(createUserDto, currentUserId))
                return null;

            var newUser = CreateUserFromDto(createUserDto);
            var createdUser = await _userService.CreateUserAsync(newUser);

            if (createdUser == null)
                return null;

            await SendWelcomeNotificationAsync(createdUser.UserID);
            return await ConvertToUserDtoAsync(createdUser, createUserDto.ClubID);
        }

        private async Task<bool> ValidateUserRegistrationAsync(CreateUserDto createUserDto, int currentUserId)
        {
            // Validate input data
            if (!IsValidUserInput(createUserDto))
                return false;

            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !await _authorizationService.IsAuthorizedAsync(currentUserId, "ManageUsers"))
                return false;

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(createUserDto.Email);
            return existingUser == null;
        }

        private static bool IsValidUserInput(CreateUserDto createUserDto)
        {
            return ValidationHelper.UserValidation.IsValidEmail(createUserDto.Email) &&
                   ValidationHelper.UserValidation.IsValidFullName(createUserDto.FullName) &&
                   ValidationHelper.UserValidation.IsValidStudentID(createUserDto.StudentID) &&
                   ValidationHelper.UserValidation.IsValidPassword(createUserDto.Password);
        }

        private static User CreateUserFromDto(CreateUserDto createUserDto)
        {
            return new User
            {
                FullName = createUserDto.FullName,
                Email = createUserDto.Email,
                StudentID = createUserDto.StudentID,
                Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                SystemRole = createUserDto.SystemRole,
                ClubID = createUserDto.ClubID,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private async Task SendWelcomeNotificationAsync(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user?.ClubID.HasValue == true)
            {
                await _notificationService.NotifyMembershipChangeAsync(userId, user.ClubID.Value, "Welcome");
            }
        }

        private async Task<UserDto> ConvertToUserDtoAsync(User user, int? clubId)
        {
            var club = clubId.HasValue ? await _clubService.GetClubByIdAsync(clubId.Value) : null;
            return new UserDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                StudentID = user.StudentID ?? "",
                SystemRole = user.SystemRole,
                JoinDate = user.CreatedAt,
                IsActive = user.IsActive,
                ClubID = user.ClubID,
                ClubName = club?.Name ?? ""
            };
        }

        public async Task<bool> TransferMembershipAsync(int userId, int newClubId, int currentUserId)
        {
            // Check authorization
            if (!await _authorizationService.IsAuthorizedAsync(currentUserId, "ManageUsers"))
                return false;

            // Get user and validate
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            // Get new club and validate
            var newClub = await _clubService.GetClubByIdAsync(newClubId);
            if (newClub == null || !newClub.IsActive)
                return false;

            // Transfer membership
            var success = await _userService.AssignUserToClubAsync(userId, newClubId);
            if (success)
            {
                // Send notification
                await _notificationService.NotifyMembershipChangeAsync(userId, newClubId, "Transfer");
            }

            return success;
        }

        public async Task<bool> PromoteMemberAsync(int userId, SystemRole newRole, int currentUserId)
        {
            // Get current user and target user
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            var targetUser = await _userService.GetUserByIdAsync(userId);

            if (currentUser == null || targetUser == null)
                return false;

            // Check if current user can assign this role
            if (!ValidationHelper.UserValidation.CanAssignRole(currentUser.SystemRole, newRole))
                return false;

            // Update user role
            var success = await _userService.UpdateUserRoleAsync(userId, newRole);
            if (success)
            {
                // Send notification
                var user = await _userService.GetUserByIdAsync(userId);
                if (user?.ClubID.HasValue == true)
                {
                    await _notificationService.NotifyMembershipChangeAsync(userId, user.ClubID.Value, "Promotion");
                }
            }

            return success;
        }

        public async Task<bool> DeactivateMemberAsync(int userId, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            var targetUser = await _userService.GetUserByIdAsync(userId);

            if (currentUser == null || targetUser == null)
                return false;

            if (!ValidationHelper.BusinessRules.CanDeleteUser(targetUser, currentUser))
                return false;

            // Deactivate user
            targetUser.IsActive = false;
            var updatedUser = await _userService.UpdateUserAsync(targetUser);
            if (updatedUser != null)
            {
                // Send notification
                if (targetUser.ClubID.HasValue)
                {
                    await _notificationService.NotifyMembershipChangeAsync(userId, targetUser.ClubID.Value, "Deactivation");
                }
            }

            return updatedUser != null;
        }

        public async Task<EventDto?> CreateEventWithNotificationAsync(CreateEventDto createEventDto, int currentUserId)
        {
            // Validate input
            if (!ValidationHelper.EventValidation.IsValidEventName(createEventDto.Name) ||
                !ValidationHelper.EventValidation.IsValidEventDate(createEventDto.EventDate) ||
                !ValidationHelper.EventValidation.IsValidLocation(createEventDto.Location))
            {
                return null;
            }

            // Check authorization
            if (!await _authorizationService.IsAuthorizedAsync(currentUserId, "ManageEvents"))
                return null;

            // Create event
            var newEvent = new Event
            {
                Name = createEventDto.Name,
                Description = createEventDto.Description,
                EventDate = createEventDto.EventDate,
                Location = createEventDto.Location,
                ClubID = createEventDto.ClubID
            };

            var createdEvent = await _eventService.CreateEventAsync(newEvent);
            if (createdEvent == null)
                return null;

            // Send notifications to club members
            await _notificationService.SendEventNotificationAsync(
                createdEvent.EventID, "New Event", $"New event '{createdEvent.Name}' has been created");

            // Convert to DTO
            var club = await _clubService.GetClubByIdAsync(createdEvent.ClubID);
            return new EventDto
            {
                EventID = createdEvent.EventID,
                Name = createdEvent.Name,
                Description = createdEvent.Description ?? "",
                EventDate = createdEvent.EventDate,
                Location = createdEvent.Location ?? "",
                ClubID = createdEvent.ClubID,
                ClubName = club?.Name ?? "",
                IsUpcoming = createdEvent.EventDate > DateTime.Now
            };
        }

        public async Task<bool> CancelEventAsync(int eventId, string reason, int currentUserId)
        {
            // Get event
            var eventItem = await _eventService.GetEventByIdAsync(eventId);
            if (eventItem == null)
                return false;

            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !ValidationHelper.BusinessRules.CanDeleteEvent(eventItem, currentUser))
                return false;

            // Delete event
            var success = await _eventService.DeleteEventAsync(eventId);
            if (success)
            {
                // Send cancellation notifications
                await _notificationService.SendEventNotificationAsync(
                    eventId, "Event Cancelled", $"Event '{eventItem.Name}' has been cancelled. Reason: {reason}");
            }

            return success;
        }

        public async Task<bool> ProcessEventAttendanceAsync(int eventId, List<EventParticipantDto> attendanceData, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            var eventItem = await _eventService.GetEventByIdAsync(eventId);
            if (currentUser == null || eventItem == null ||
                !_authorizationService.CanManageEvent(currentUser.SystemRole, null, currentUser.ClubID == eventItem.ClubID))
                return false;

            var success = true;
            foreach (var participant in attendanceData)
            {
                var result = await _eventService.UpdateParticipantStatusAsync(eventId, participant.UserID, participant.Status);
                if (!result)
                    success = false;
            }

            return success;
        }

        public async Task<ClubDto?> EstablishNewClubAsync(CreateClubDto createClubDto, int chairmanUserId, int currentUserId)
        {
            // Validate input
            if (!ValidationHelper.ClubValidation.IsValidClubName(createClubDto.Name) ||
                !ValidationHelper.ClubValidation.IsValidDescription(createClubDto.Description) ||
                !ValidationHelper.ClubValidation.IsValidEstablishedDate(createClubDto.EstablishedDate))
            {
                return null;
            }

            // Check authorization (only Admin can create clubs)
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !_authorizationService.CanAccessFeature(currentUser.SystemRole, "ClubManagement"))
                return null;

            // Create club
            var newClub = new Club
            {
                ClubName = createClubDto.Name,
                Description = createClubDto.Description,
                EstablishedDate = createClubDto.EstablishedDate,
                IsActive = true
            };

            var createdClub = await _clubService.CreateClubAsync(newClub);
            if (createdClub == null)
                return null;

            // Assign chairman - this functionality needs to be updated for the new schema
            // Club membership and roles are now handled through ClubMembers table
            // TODO: Implement club membership assignment through ClubService

            // Send notification
            await _notificationService.NotifyMembershipChangeAsync(chairmanUserId, createdClub.ClubID, "Club Leadership");

            return new ClubDto
            {
                ClubID = createdClub.ClubID,
                Name = createdClub.Name,
                Description = createdClub.Description ?? "",
                EstablishedDate = createdClub.EstablishedDate ?? DateTime.Now,
                IsActive = createdClub.IsActive
            };
        }

        public async Task<bool> RestructureClubLeadershipAsync(int clubId, ClubLeadershipDto newLeadership, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !_authorizationService.CanManageClub(currentUser.SystemRole, null, currentUser.ClubID == clubId))
                return false;

            // Implement leadership restructuring logic
            // This would involve updating multiple user roles and sending notifications
            // Implementation details would depend on specific business requirements

            return true; // Placeholder
        }

        public async Task<ClubStatisticsDto> GetComprehensiveClubStatisticsAsync(int clubId, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !_authorizationService.CanViewReports(currentUser.SystemRole, null, currentUser.ClubID == clubId))
                throw new UnauthorizedAccessException();

            var statistics = await _clubService.GetClubStatisticsAsync(clubId);
            var club = await _clubService.GetClubByIdAsync(clubId);

            return new ClubStatisticsDto
            {
                ClubID = clubId,
                ClubName = club?.Name ?? "",
                // Map other statistics properties
            };
        }

        public async Task<ReportDto?> GenerateComprehensiveReportAsync(ReportType reportType, int clubId, string semester, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !ValidationHelper.BusinessRules.CanGenerateReport(reportType, currentUser))
                return null;

            // Generate report based on type
            Report report = reportType switch
            {
                ReportType.MemberStatistics => await _reportService.GenerateMemberStatisticsReportAsync(clubId, semester, currentUserId),
                ReportType.EventOutcomes => await _reportService.GenerateEventOutcomesReportAsync(clubId, semester, currentUserId),
                ReportType.ActivityTracking => await _reportService.GenerateActivityTrackingReportAsync(clubId, semester, currentUserId),
                ReportType.SemesterSummary => await _reportService.GenerateSemesterSummaryReportAsync(clubId, semester, currentUserId),
                _ => throw new ArgumentException("Invalid report type")
            };

            if (report == null)
                return null;

            // Convert to DTO
            var club = await _clubService.GetClubByIdAsync(clubId);

            // Handle case where the report generator user might have been deleted
            var generatedByUserName = currentUser.FullName;
            if (report.GeneratedByUserID.HasValue && report.GeneratedByUserID.Value != currentUserId)
            {
                var originalUser = await _userService.GetUserByIdAsync(report.GeneratedByUserID.Value);
                generatedByUserName = originalUser?.FullName ?? "[Deleted User]";
            }
            else if (!report.GeneratedByUserID.HasValue)
            {
                generatedByUserName = "[Deleted User]";
            }

            return new ReportDto
            {
                ReportID = report.ReportID,
                Title = report.Title,
                Type = report.Type,
                Content = report.Content ?? "",
                GeneratedDate = report.GeneratedDate,
                Semester = report.Semester ?? "",
                ClubID = report.ClubID ?? clubId,
                ClubName = club?.Name ?? "",
                GeneratedByUserID = report.GeneratedByUserID,
                GeneratedByUserName = generatedByUserName
            };
        }

        public async Task<bool> SchedulePeriodicReportsAsync(int clubId, List<ReportType> reportTypes, string semester, int currentUserId)
        {
            // This would implement scheduled report generation
            // For now, generate all requested reports immediately
            foreach (var reportType in reportTypes)
            {
                await GenerateComprehensiveReportAsync(reportType, clubId, semester, currentUserId);
            }

            return true;
        }

        public async Task<bool> SendEventReminderNotificationsAsync(int eventId, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            var eventItem = await _eventService.GetEventByIdAsync(eventId);
            if (currentUser == null || eventItem == null ||
                !_authorizationService.CanManageEvent(currentUser.SystemRole, null, currentUser.ClubID == eventItem.ClubID))
                return false;

            await _notificationService.NotifyEventReminderAsync(eventId);
            return true;
        }

        public async Task<bool> SendMembershipChangeNotificationAsync(int userId, string changeType, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            var targetUser = await _userService.GetUserByIdAsync(userId);
            if (currentUser == null || targetUser == null ||
                !_authorizationService.CanManageUser(currentUser.SystemRole, targetUser.SystemRole, null, null))
                return false;

            if (targetUser.ClubID.HasValue)
            {
                await _notificationService.NotifyMembershipChangeAsync(userId, targetUser.ClubID.Value, changeType);
            }
            return true;
        }

        public async Task<bool> SendClubAnnouncementAsync(int clubId, string title, string message, int currentUserId)
        {
            // Check authorization
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !_authorizationService.CanManageClub(currentUser.SystemRole, null, currentUser.ClubID == clubId))
                return false;

            await _notificationService.SendClubNotificationAsync(clubId, title, message);
            return true;
        }
    }
}
