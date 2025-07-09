using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ClubManagementApp.Models
{
    public class Club
    {
        [Key]
        public int ClubID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<User> Members { get; set; } = new List<User>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
