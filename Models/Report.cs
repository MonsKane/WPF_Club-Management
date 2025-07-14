using System.ComponentModel.DataAnnotations;

namespace ClubManagementApp.Models
{
    public enum ReportType
    {
        MemberStatistics,
        EventOutcomes,
        ActivityTracking,
        SemesterSummary
    }

    public class Report
    {
        [Key]
        public int ReportID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public ReportType Type { get; set; }

        [Required]
        public string Content { get; set; } = null!; // JSON or formatted text content

        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? Semester { get; set; } // e.g., "Fall 2024"

        public int? ClubID { get; set; } // Nullable for system-wide reports

        public int? GeneratedByUserID { get; set; } // Nullable to allow null when user is deleted

        // Navigation properties
        public virtual Club? Club { get; set; }
        public virtual User? GeneratedByUser { get; set; }
    }
}
