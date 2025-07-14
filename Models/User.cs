using System.ComponentModel.DataAnnotations;

namespace ClubManagementApp.Models
{
    public enum SystemRole
    {
        Admin,
        ClubOwner,
        Member
    }

    public enum ClubRole
    {
        Admin,
        Chairman,
        Member
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

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public SystemRole SystemRole { get; set; }

        public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Active;

        public int? ClubID { get; set; }

        public bool IsActive { get; set; } = true;

        public bool TwoFactorEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Club? Club { get; set; }
        public virtual ICollection<ClubMember> ClubMemberships { get; set; } = new List<ClubMember>();
        public virtual ICollection<Club> CreatedClubs { get; set; } = new List<Club>();
        public virtual ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
        public virtual ICollection<Report> GeneratedReports { get; set; } = new List<Report>();
    }
}