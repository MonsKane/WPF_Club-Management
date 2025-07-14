using System.ComponentModel.DataAnnotations;

namespace ClubManagementApp.Models
{
    public class ClubMember
    {
        [Key]
        public int ClubMemberID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int ClubID { get; set; }

        [Required]
        public ClubRole ClubRole { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime JoinDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Club Club { get; set; } = null!;
    }
}