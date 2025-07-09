using ClubManagementApp.Models;

namespace ClubManagementApp.DTOs
{
    public class EventDto
    {
        public int EventID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int ClubID { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int RegisteredCount { get; set; }
        public int AttendedCount { get; set; }
        public int AbsentCount { get; set; }
        public bool IsUpcoming { get; set; }
    }

    public class CreateEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int ClubID { get; set; }
    }

    public class UpdateEventDto
    {
        public int EventID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
    }

    public class EventParticipantDto
    {
        public int ParticipantID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string StudentID { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? AttendanceDate { get; set; }
    }

    public class EventStatisticsDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int TotalRegistered { get; set; }
        public int TotalAttended { get; set; }
        public int TotalAbsent { get; set; }
        public double AttendanceRate { get; set; }
        public Dictionary<AttendanceStatus, int> StatusBreakdown { get; set; } = new();
        public List<EventParticipantDto> Participants { get; set; } = new();
    }

    public class EventRegistrationDto
    {
        public int EventID { get; set; }
        public int UserID { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}