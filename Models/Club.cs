using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClubManagementApp.Models
{
    public class Club : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        [Key]
        public int ClubID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? FoundedDate { get; set; }
        
        [StringLength(200)]
        public string? MeetingSchedule { get; set; }
        
        [EmailAddress]
        [StringLength(150)]
        public string? ContactEmail { get; set; }
        
        [StringLength(20)]
        public string? ContactPhone { get; set; }
        
        [StringLength(200)]
        public string? Website { get; set; }
        
        // Alias for CreatedDate to match dialog expectations
        public DateTime CreatedAt => CreatedDate;
        
        // UI-only property for selection state
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Navigation properties
        public virtual ICollection<User> Members { get; set; } = new List<User>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        
        // Computed properties for UI
        public int MemberCount => Members?.Count ?? 0;
        public int EventCount => Events?.Count ?? 0;
        public string Status => IsActive ? "Active" : "Inactive";
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
