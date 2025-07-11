using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ClubManagementApp.Models
{
    public enum EventStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Postponed
    }
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime EventDate { get; set; }
        
        [Required]
        [StringLength(300)]
        public string Location { get; set; } = null!;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? RegistrationDeadline { get; set; }
        
        public int? MaxParticipants { get; set; }
        
        public EventStatus Status { get; set; } = EventStatus.Scheduled;
        
        // Foreign key
        [Required]
        public int ClubID { get; set; }
        
        // Navigation properties
        public virtual Club Club { get; set; } = null!;
        public virtual ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
        
        // Computed properties for UI
        public int ParticipantCount => Participants?.Count ?? 0;
    }
}
