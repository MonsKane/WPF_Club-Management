using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementApp.Models
{
    public class Setting
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        [ForeignKey("Club")]
        public int? ClubId { get; set; }
        public Club? Club { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        [Required]
        public SettingsScope Scope { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum SettingsScope
    {
        User,
        Club,
        Global
    }
}