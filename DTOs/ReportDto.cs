using ClubManagementApp.Models;

namespace ClubManagementApp.DTOs
{
    public class ReportDto
    {
        public int ReportID { get; set; }
        public string Title { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string Semester { get; set; } = string.Empty;
        public int ClubID { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int GeneratedByUserID { get; set; }
        public string GeneratedByUserName { get; set; } = string.Empty;
    }

    public class CreateReportDto
    {
        public string Title { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public int ClubID { get; set; }
        public int GeneratedByUserID { get; set; }
    }

    public class ReportSummaryDto
    {
        public int ReportID { get; set; }
        public string Title { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string Semester { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public string GeneratedByUserName { get; set; } = string.Empty;
    }

    public class MemberStatisticsReportDto
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int NormalMembers { get; set; }
        public int InactiveMembers { get; set; }
        public int NewMembersThisSemester { get; set; }
        public Dictionary<string, int> MembersByRole { get; set; } = new();
    }

    public class EventOutcomesReportDto
    {
        public int TotalEvents { get; set; }
        public int CompletedEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int TotalParticipants { get; set; }
        public double AverageAttendance { get; set; }
        public List<EventSummaryDto> EventDetails { get; set; } = new();
    }

    public class EventSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Registered { get; set; }
        public int Attended { get; set; }
        public int Absent { get; set; }
    }

    public class ActivityTrackingReportDto
    {
        public int TotalMembers { get; set; }
        public int HighPerformers { get; set; }
        public double AverageAttendance { get; set; }
        public List<MemberActivityDto> MemberDetails { get; set; } = new();
    }

    public class MemberActivityDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalEventsRegistered { get; set; }
        public int EventsAttended { get; set; }
        public double AttendancePercentage { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
    }
}