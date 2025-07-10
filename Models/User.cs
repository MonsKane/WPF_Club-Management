using System.ComponentModel.DataAnnotations;

namespace ClubManagementApp.Models
{
    public enum UserRole
    {
        Admin,
        Chairman,
        ViceChairman,
        TeamLeader,
        Member,
        SystemAdmin,
        ClubPresident,
        ClubOfficer
    }

    public enum ActivityLevel
    {
        Active,
        Normal,
        Inactive
    }

    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = null!; // Should be hashed in real application

        [StringLength(20)]
        public string? StudentID { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Normal;

        public DateTime JoinDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public bool TwoFactorEnabled { get; set; } = false;

        // Foreign key
        public int? ClubID { get; set; } // Nullable for admins or users without clubs

        // Navigation properties
        public virtual Club? Club { get; set; }
        public virtual ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
        public virtual ICollection<Report> GeneratedReports { get; set; } = new List<Report>();
    }
}