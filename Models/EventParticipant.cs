using System.ComponentModel.DataAnnotations;

namespace ClubManagementApp.Models
{
    public enum AttendanceStatus
    {
        Registered,
        Attended,
        Absent
    }
    
    public class EventParticipant
    {
        [Key]
        public int ParticipantID { get; set; }
        
        // Foreign keys
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public int EventID { get; set; }
        
        [Required]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Registered;
        
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        
        public DateTime? AttendanceDate { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Event Event { get; set; } = null!;
    }
}
