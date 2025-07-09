using ClubManagementApp.Models;

namespace ClubManagementApp.DTOs
{
    public class ClubDto
    {
        public int ClubID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EstablishedDate { get; set; }
        public bool IsActive { get; set; }
        public int MemberCount { get; set; }
        public int ActiveMemberCount { get; set; }
        public List<UserDto> Leadership { get; set; } = new();
    }

    public class CreateClubDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EstablishedDate { get; set; } = DateTime.Now;
    }

    public class UpdateClubDto
    {
        public int ClubID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ClubLeadershipDto
    {
        public int ClubID { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public UserDto? Chairman { get; set; }
        public List<UserDto> ViceChairmen { get; set; } = new();
        public List<UserDto> TeamLeaders { get; set; } = new();
    }

    public class ClubStatisticsDto
    {
        public int ClubID { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveMembers { get; set; }
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int CompletedEvents { get; set; }
        public double AverageAttendanceRate { get; set; }
        public DateTime EstablishedDate { get; set; }
        public Dictionary<string, int> MembersByRole { get; set; } = new();
        public Dictionary<string, int> EventsByMonth { get; set; } = new();
        public List<EventDto> RecentEvents { get; set; } = new();
        public List<UserDto> TopActiveMembers { get; set; } = new();
    }
}