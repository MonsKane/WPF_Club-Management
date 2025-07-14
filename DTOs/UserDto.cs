using ClubManagementApp.Models;

namespace ClubManagementApp.DTOs
{
    public class UserDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentID { get; set; } = string.Empty;
        public SystemRole SystemRole { get; set; }
        public ActivityLevel ActivityLevel { get; set; }
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }
        public int? ClubID { get; set; }
        public string? ClubName { get; set; }
        public List<string> ClubMemberships { get; set; } = new();
    }

    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public SystemRole SystemRole { get; set; } = SystemRole.Member;
        public int? ClubID { get; set; }
    }

    public class UpdateUserDto
    {
        public int UserID { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? StudentID { get; set; }
        public SystemRole? SystemRole { get; set; }
        public ActivityLevel? ActivityLevel { get; set; }
        public bool? IsActive { get; set; }
        public int? ClubID { get; set; }
    }

    public class UserParticipationDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalEvents { get; set; }
        public int AttendedEvents { get; set; }
        public int RegisteredEvents { get; set; }
        public int AbsentEvents { get; set; }
        public double AttendanceRate { get; set; }
        public List<EventParticipationDto> RecentEvents { get; set; } = new();
    }

    public class EventParticipationDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? AttendanceDate { get; set; }
    }
}