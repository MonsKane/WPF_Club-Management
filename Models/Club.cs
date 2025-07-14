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
        public string ClubName { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime? EstablishedDate { get; set; }

        [Required]
        public int CreatedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Temporarily keep as property but not mapped to database
        public bool IsActive { get; set; } = true;

        public string Status => IsActive ? "Active" : "Inactive";

        [StringLength(200)]
        public string? MeetingSchedule { get; set; }

        [StringLength(150)]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

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
        public virtual User CreatedUser { get; set; } = null!;
        public virtual ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMember>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();

        // Computed properties for UI
        public int MemberCount => ClubMembers?.Count ?? 0;
        public int EventCount => Events?.Count ?? 0;

        // Alias for backward compatibility
        public string Name => ClubName;
        public DateTime? FoundedDate => EstablishedDate;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Public method to refresh member and event counts
        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(MemberCount));
            OnPropertyChanged(nameof(EventCount));
        }
    }
}
