using ClubManagementApp.Data;
using ClubManagementApp.Models;
using ClubManagementApp.DTOs;
using ClubManagementApp.Helpers;
using ClubManagementApp.Exceptions;
using Microsoft.EntityFrameworkCore;
using SystemValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace ClubManagementApp.Services
{
    public interface IMemberService
    {
        // Basic CRUD operations
        Task<IEnumerable<UserDto>> GetAllMembersAsync();
        Task<UserDto?> GetMemberByIdAsync(int id);
        Task<UserDto?> GetMemberByEmailAsync(string email);
        Task<UserDto?> GetMemberByStudentIdAsync(string studentId);
        Task<UserDto> CreateMemberAsync(CreateUserDto createUserDto);
        Task<UserDto> UpdateMemberAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteMemberAsync(int id);
        
        // Search and filtering
        Task<IEnumerable<UserDto>> SearchMembersAsync(string searchTerm);
        Task<IEnumerable<UserDto>> GetMembersByRoleAsync(SystemRole role);
        Task<IEnumerable<UserDto>> GetMembersByClubAsync(int clubId);
        Task<IEnumerable<UserDto>> GetActiveMembers();
        Task<IEnumerable<UserDto>> GetInactiveMembers();
        
        // Participation tracking
        Task<UserParticipationDto> GetMemberParticipationAsync(int memberId);
        Task<IEnumerable<EventParticipationDto>> GetMemberEventHistoryAsync(int memberId);
        Task<decimal> GetMemberAttendanceRateAsync(int memberId);
        Task<int> GetMemberEventCountAsync(int memberId);
        
        // Role and club management
        Task<bool> AssignSystemRoleAsync(int memberId, SystemRole role);
        Task<bool> AddMemberToClubAsync(int memberId, int clubId, ClubRole clubRole);
        Task<bool> RemoveMemberFromClubAsync(int memberId, int clubId);
        Task<IEnumerable<ClubDto>> GetMemberClubsAsync(int memberId);
        
        // Member statistics
        Task<int> GetTotalMemberCountAsync();
        Task<int> GetActiveMemberCountAsync();
        Task<Dictionary<SystemRole, int>> GetMemberCountBySystemRoleAsync();
        Task<Dictionary<string, int>> GetMemberCountByClubAsync();
        
        // Validation and business rules
        Task<bool> CanDeleteMemberAsync(int memberId);
        Task<bool> CanAssignSystemRoleAsync(int memberId, SystemRole role);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);
        Task<bool> IsStudentIdUniqueAsync(string studentId, int? excludeUserId = null);
    }

    public class MemberService : IMemberService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private readonly IAuthorizationService _authorizationService;

        public MemberService(
            ClubManagementDbContext context,
            ILoggingService loggingService,
            IAuthorizationService authorizationService)
        {
            _context = context;
            _loggingService = loggingService;
            _authorizationService = authorizationService;
        }

        public async Task<IEnumerable<UserDto>> GetAllMembersAsync()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Club)
                    .ToListAsync();

                return users.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to retrieve all members", ex);
                throw new DatabaseConnectionException("Failed to retrieve members", ex);
            }
        }

        public async Task<UserDto?> GetMemberByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Club)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to retrieve member with ID {id}", ex);
                throw new DatabaseConnectionException($"Failed to retrieve member with ID {id}", ex);
            }
        }

        public async Task<UserDto?> GetMemberByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Club)
                    .FirstOrDefaultAsync(u => u.Email == email);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to retrieve member with email {email}", ex);
                throw new DatabaseConnectionException($"Failed to retrieve member with email {email}", ex);
            }
        }

        public async Task<UserDto?> GetMemberByStudentIdAsync(string studentId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Club)
                    .FirstOrDefaultAsync(u => u.StudentID == studentId);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to retrieve member with student ID {studentId}", ex);
                throw new DatabaseConnectionException($"Failed to retrieve member with student ID {studentId}", ex);
            }
        }

        public async Task<UserDto> CreateMemberAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Validate input
                if (!ValidationHelper.UserValidation.IsValidEmail(createUserDto.Email))
                throw new ValidationException("Email", "Invalid email format");

            if (!ValidationHelper.UserValidation.IsValidStudentID(createUserDto.StudentID))
                    throw new ValidationException("StudentID", "Invalid student ID format");

                if (!ValidationHelper.UserValidation.IsValidPassword(createUserDto.Password))
                    throw new ValidationException("Password", "Password does not meet requirements");

                // Check uniqueness
                if (!await IsEmailUniqueAsync(createUserDto.Email))
                    throw new ValidationException("Email", "Email already exists");

                if (!await IsStudentIdUniqueAsync(createUserDto.StudentID))
                    throw new ValidationException("StudentID", "Student ID already exists");

                var user = new User
                {
                    FullName = createUserDto.FullName,
                    Email = createUserDto.Email,
                    StudentID = createUserDto.StudentID,
                Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                    SystemRole = createUserDto.SystemRole,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _loggingService.LogUserActionAsync(user.UserID, "Member created", $"New member {user.FullName} created");

                return MapToUserDto(user);
            }
            catch (ClubManagementException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to create member", ex);
                throw new DatabaseConnectionException("Failed to create member", ex);
            }
        }

        public async Task<UserDto> UpdateMemberAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    throw new UserNotFoundException(id);

                // Validate input
                if (!string.IsNullOrEmpty(updateUserDto.Email) && !ValidationHelper.UserValidation.IsValidEmail(updateUserDto.Email))
                throw new ValidationException("Email", "Invalid email format");

            if (!string.IsNullOrEmpty(updateUserDto.StudentID) && !ValidationHelper.UserValidation.IsValidStudentID(updateUserDto.StudentID))
                    throw new ValidationException("StudentID", "Invalid student ID format");

                // Check uniqueness if email or student ID is being changed
                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    if (!await IsEmailUniqueAsync(updateUserDto.Email, id))
                        throw new ValidationException("Email", "Email already exists");
                }

                if (!string.IsNullOrEmpty(updateUserDto.StudentID) && updateUserDto.StudentID != user.StudentID)
                {
                    if (!await IsStudentIdUniqueAsync(updateUserDto.StudentID, id))
                        throw new ValidationException("StudentID", "Student ID already exists");
                }

                // Update fields
                if (!string.IsNullOrEmpty(updateUserDto.FullName))
                    user.FullName = updateUserDto.FullName;

                if (!string.IsNullOrEmpty(updateUserDto.Email))
                    user.Email = updateUserDto.Email;

                if (!string.IsNullOrEmpty(updateUserDto.StudentID))
                    user.StudentID = updateUserDto.StudentID;

                if (updateUserDto.SystemRole.HasValue)
                user.SystemRole = updateUserDto.SystemRole.Value;

                if (updateUserDto.IsActive.HasValue)
                    user.IsActive = updateUserDto.IsActive.Value;

                // User model doesn't have UpdatedAt property

                await _context.SaveChangesAsync();

                await _loggingService.LogUserActionAsync(user.UserID, "Member updated", $"Member {user.FullName} updated");

                return MapToUserDto(user);
            }
            catch (ClubManagementException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to update member with ID {id}", ex);
                throw new DatabaseConnectionException($"Failed to update member with ID {id}", ex);
            }
        }

        public async Task<bool> DeleteMemberAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                if (!await CanDeleteMemberAsync(id))
                    throw new BusinessRuleViolationException("MemberDeletion", "Cannot delete member due to business rules");

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await _loggingService.LogUserActionAsync(id, "Member deleted", $"Member {user.FullName} deleted");

                return true;
            }
            catch (ClubManagementException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to delete member with ID {id}", ex);
                throw new DatabaseConnectionException($"Failed to delete member with ID {id}", ex);
            }
        }

        public async Task<IEnumerable<UserDto>> SearchMembersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllMembersAsync();

                var users = await _context.Users
                    .Include(u => u.Club)
                    .Where(u => u.FullName.Contains(searchTerm) ||
                               u.Email.Contains(searchTerm) ||
                               (u.StudentID != null && u.StudentID.Contains(searchTerm)))
                    .ToListAsync();

                return users.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to search members with term '{searchTerm}'", ex);
                throw new DatabaseConnectionException("Failed to search members", ex);
            }
        }

        public async Task<IEnumerable<UserDto>> GetMembersByRoleAsync(SystemRole role)
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Club)
                    .Where(u => u.SystemRole == role)
                    .ToListAsync();

                return users.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to retrieve members with role {role}", ex);
                throw new DatabaseConnectionException($"Failed to retrieve members with role {role}", ex);
            }
        }

        public async Task<IEnumerable<UserDto>> GetMembersByClubAsync(int clubId)
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Club)
                    .Where(u => u.ClubID == clubId)
                    .ToListAsync();

                return users.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to retrieve members for club {clubId}", ex);
                throw new DatabaseConnectionException($"Failed to retrieve members for club {clubId}", ex);
            }
        }

        public async Task<IEnumerable<UserDto>> GetActiveMembers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Club)
                    .Where(u => u.IsActive)
                    .ToListAsync();

                return users.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to retrieve active members", ex);
                throw new DatabaseConnectionException("Failed to retrieve active members", ex);
            }
        }

        public async Task<IEnumerable<UserDto>> GetInactiveMembers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Club)
                    .Where(u => !u.IsActive)
                    .ToListAsync();

                return users.Select(MapToUserDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to retrieve inactive members", ex);
                throw new DatabaseConnectionException("Failed to retrieve inactive members", ex);
            }
        }

        public async Task<UserParticipationDto> GetMemberParticipationAsync(int memberId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.EventParticipations)
                        .ThenInclude(ep => ep.Event)
                    .FirstOrDefaultAsync(u => u.UserID == memberId);

                if (user == null)
                    throw new UserNotFoundException(memberId);

                var totalEvents = user.EventParticipations.Count;
                var attendedEvents = user.EventParticipations.Count(ep => ep.Status == AttendanceStatus.Attended);
                var attendanceRate = totalEvents > 0 ? (decimal)attendedEvents / totalEvents * 100 : 0;

                return new UserParticipationDto
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Email = user.Email,
                    TotalEvents = totalEvents,
                    AttendedEvents = attendedEvents,
                    RegisteredEvents = totalEvents,
                    AbsentEvents = totalEvents - attendedEvents,
                    AttendanceRate = (double)attendanceRate
                };
            }
            catch (ClubManagementException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get participation for member {memberId}", ex);
                throw new DatabaseConnectionException($"Failed to get participation for member {memberId}", ex);
            }
        }

        public async Task<IEnumerable<EventParticipationDto>> GetMemberEventHistoryAsync(int memberId)
        {
            try
            {
                var participations = await _context.EventParticipants
                    .Include(ep => ep.Event)
                    .Where(ep => ep.UserID == memberId)
                    .OrderByDescending(ep => ep.Event.EventDate)
                    .ToListAsync();

                return participations.Select(ep => new EventParticipationDto
                {
                    EventID = ep.EventID,
                    EventName = ep.Event.Name,
                    EventDate = ep.Event.EventDate,
                    Status = ep.Status,
                    RegistrationDate = ep.RegistrationDate
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get event history for member {memberId}", ex);
                throw new DatabaseConnectionException($"Failed to get event history for member {memberId}", ex);
            }
        }

        public async Task<decimal> GetMemberAttendanceRateAsync(int memberId)
        {
            try
            {
                var participations = await _context.EventParticipants
                    .Where(ep => ep.UserID == memberId)
                    .ToListAsync();

                if (!participations.Any())
                    return 0;

                var attendedCount = participations.Count(ep => ep.Status == AttendanceStatus.Attended);
                return (decimal)attendedCount / participations.Count * 100;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get attendance rate for member {memberId}", ex);
                throw new DatabaseConnectionException($"Failed to get attendance rate for member {memberId}", ex);
            }
        }

        public async Task<int> GetMemberEventCountAsync(int memberId)
        {
            try
            {
                return await _context.EventParticipants
                    .CountAsync(ep => ep.UserID == memberId);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get event count for member {memberId}", ex);
                throw new DatabaseConnectionException($"Failed to get event count for member {memberId}", ex);
            }
        }

        public async Task<bool> AssignSystemRoleAsync(int memberId, SystemRole role)
        {
            try
            {
                var user = await _context.Users.FindAsync(memberId);
                if (user == null)
                    return false;

                if (!await CanAssignSystemRoleAsync(memberId, role))
                    throw new BusinessRuleViolationException("RoleAssignment", $"Cannot assign role {role} to member");

                user.SystemRole = role;
                // User model doesn't have UpdatedAt property

                await _context.SaveChangesAsync();

                await _loggingService.LogUserActionAsync(memberId, "Role assigned", $"Role {role} assigned to {user.FullName}");

                return true;
            }
            catch (ClubManagementException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to assign role {role} to member {memberId}", ex);
                throw new DatabaseConnectionException($"Failed to assign role to member", ex);
            }
        }

        public async Task<bool> AddMemberToClubAsync(int memberId, int clubId, ClubRole clubRole)
        {
            try
            {
                var user = await _context.Users.FindAsync(memberId);
                var club = await _context.Clubs.FindAsync(clubId);

                if (user == null || club == null)
                    return false;

                if (user.ClubID == clubId)
                    return false; // Already a member

                user.ClubID = clubId;
                // Note: ClubRole is not stored in User model in this implementation
                
                await _context.SaveChangesAsync();

                await _loggingService.LogUserActionAsync(memberId, "Added to club", $"{user.FullName} added to club {club.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to add member {memberId} to club {clubId}", ex);
                throw new DatabaseConnectionException("Failed to add member to club", ex);
            }
        }

        public async Task<bool> RemoveMemberFromClubAsync(int memberId, int clubId)
        {
            try
            {
                var user = await _context.Users.FindAsync(memberId);
                if (user == null || user.ClubID != clubId)
                    return false;

                user.ClubID = null;
                // User model doesn't have UpdatedAt property
                await _context.SaveChangesAsync();

                var club = await _context.Clubs.FindAsync(clubId);

                await _loggingService.LogUserActionAsync(memberId, "Removed from club", 
                    $"{user?.FullName} removed from club {club?.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to remove member {memberId} from club {clubId}", ex);
                throw new DatabaseConnectionException("Failed to remove member from club", ex);
            }
        }

        public async Task<IEnumerable<ClubDto>> GetMemberClubsAsync(int memberId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Club)
                    .FirstOrDefaultAsync(u => u.UserID == memberId);
                
                if (user?.Club == null)
                    return new List<ClubDto>();
                
                var clubs = new List<Club> { user.Club! };

                return clubs.Select(c => new ClubDto
                {
                    ClubID = c.ClubID,
                    Name = c.Name,
                    Description = c.Description ?? string.Empty,
                    EstablishedDate = c.EstablishedDate ?? DateTime.Now,
                    IsActive = c.IsActive
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get clubs for member {memberId}", ex);
                throw new DatabaseConnectionException($"Failed to get clubs for member {memberId}", ex);
            }
        }

        public async Task<int> GetTotalMemberCountAsync()
        {
            try
            {
                return await _context.Users.CountAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get total member count", ex);
                throw new DatabaseConnectionException("Failed to get total member count", ex);
            }
        }

        public async Task<int> GetActiveMemberCountAsync()
        {
            try
            {
                return await _context.Users.CountAsync(u => u.IsActive);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get active member count", ex);
                throw new DatabaseConnectionException("Failed to get active member count", ex);
            }
        }

        public async Task<Dictionary<SystemRole, int>> GetMemberCountBySystemRoleAsync()
        {
            try
            {
                var roleCounts = await _context.Users
                    .GroupBy(u => u.SystemRole)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count);

                return roleCounts;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get member count by role", ex);
                throw new DatabaseConnectionException("Failed to get member count by role", ex);
            }
        }

        public async Task<Dictionary<string, int>> GetMemberCountByClubAsync()
        {
            try
            {
                var clubCounts = await _context.Users
                    .Include(u => u.Club)
                    .Where(u => u.ClubID.HasValue && u.IsActive && u.Club != null)
                    .GroupBy(u => u.Club!.Name)
                    .Select(g => new { ClubName = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ClubName, x => x.Count);

                return clubCounts;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get member count by club", ex);
                throw new DatabaseConnectionException("Failed to get member count by club", ex);
            }
        }

        public async Task<bool> CanDeleteMemberAsync(int memberId)
        {
            try
            {
                var user = await _context.Users.FindAsync(memberId);
                if (user == null)
                    return false;

                // Basic validation - can delete if user exists and is not an admin
                return user.SystemRole != SystemRole.Admin;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check if member {memberId} can be deleted", ex);
                return false;
            }
        }

        public async Task<bool> CanAssignSystemRoleAsync(int memberId, SystemRole role)
        {
            try
            {
                var user = await _context.Users.FindAsync(memberId);
                if (user == null)
                    return false;

                return ValidationHelper.UserValidation.CanAssignRole(user.SystemRole, role);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check if role {role} can be assigned to member {memberId}", ex);
                return false;
            }
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Email == email);
                
                if (excludeUserId.HasValue)
                    query = query.Where(u => u.UserID != excludeUserId.Value);

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check email uniqueness for {email}", ex);
                return false;
            }
        }

        public async Task<bool> IsStudentIdUniqueAsync(string studentId, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.StudentID == studentId);
                
                if (excludeUserId.HasValue)
                    query = query.Where(u => u.UserID != excludeUserId.Value);

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check student ID uniqueness for {studentId}", ex);
                return false;
            }
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                StudentID = user.StudentID ?? "",
                SystemRole = user.SystemRole,
                ActivityLevel = ActivityLevel.Active, // Default value since User model doesn't have ActivityLevel
                JoinDate = user.CreatedAt,
                IsActive = user.IsActive,
                ClubID = user.ClubID,
                ClubName = user.Club?.Name ?? "",
                ClubMemberships = user.Club != null ? new List<string> { user.Club.Name } : new List<string>()
            };
        }
    }
}