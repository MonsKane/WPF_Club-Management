using ClubManagementApp.Data;
using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementApp.Services
{
    /// <summary>
    /// Business service for managing event operations, participation, and attendance tracking.
    /// Handles event lifecycle management, user registration, and comprehensive analytics.
    /// 
    /// Responsibilities:
    /// - Event CRUD operations and lifecycle management
    /// - User registration and participation tracking
    /// - Attendance monitoring and status updates
    /// - Event analytics and reporting
    /// - Timeline-based event filtering (upcoming, past, date ranges)
    /// 
    /// Data Flow:
    /// ViewModels -> EventService -> DbContext -> Database
    /// 
    /// Key Features:
    /// - Multi-status event management (Planned, Active, Completed, Cancelled)
    /// - Comprehensive attendance tracking (Registered, Attended, Absent, Cancelled)
    /// - Real-time participation statistics and analytics
    /// - Timeline-based event organization and filtering
    /// - User-centric event history and participation tracking
    /// - Club-specific event management and reporting
    /// </summary>
    public class EventService : IEventService
    {
        /// <summary>Database context for event, participation, and related data operations</summary>
        private readonly ClubManagementDbContext _context;

        /// <summary>
        /// Initializes the EventService with database context dependency.
        /// 
        /// Data Flow:
        /// - Dependency injection provides DbContext instance
        /// - Service becomes ready for event management operations
        /// - All database operations flow through this context
        /// </summary>
        /// <param name="context">Entity Framework database context for data access</param>
        public EventService(ClubManagementDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all events across the entire system with complete participation data.
        /// Includes club information and participant details for comprehensive event overview.
        /// 
        /// Data Flow:
        /// 1. Query Events table with related data
        /// 2. Include Club information via navigation property
        /// 3. Include Participants with nested User data
        /// 4. Sort by event date (newest first) for chronological display
        /// 
        /// Usage: System-wide event dashboards, administrative overviews, global reporting
        /// </summary>
        /// <returns>Collection of all events with club and participant information</returns>
        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific event by its unique identifier.
        /// Includes complete club and participant data for detailed event management.
        /// 
        /// Data Flow:
        /// 1. Query Events table for specific event ID
        /// 2. Include related Club information
        /// 3. Include Participants collection with User details
        /// 4. Return complete event object or null if not found
        /// 
        /// Usage: Event detail pages, participation management, attendance tracking
        /// </summary>
        /// <param name="eventId">Unique identifier of the event to retrieve</param>
        /// <returns>Event object with relationships, or null if not found</returns>
        public async Task<Event?> GetEventByIdAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.EventID == eventId);
        }

        /// <summary>
        /// Retrieves all events organized by a specific club.
        /// Includes participant data for club-specific event management and analytics.
        /// 
        /// Data Flow:
        /// 1. Query Events table filtered by club ID
        /// 2. Include Club and Participants with User details
        /// 3. Sort by event date (newest first) for chronological display
        /// 4. Return club-specific event collection
        /// 
        /// Usage: Club dashboards, club-specific reporting, member event views
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Collection of events organized by the specified club</returns>
        public async Task<IEnumerable<Event>> GetEventsByClubAsync(int clubId)
        {
            return await _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Where(e => e.ClubID == clubId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int? clubId = null)
        {
            var query = _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Where(e => e.EventDate > DateTime.Now);

            if (clubId.HasValue)
                query = query.Where(e => e.ClubID == clubId.Value);

            return await query
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetPastEventsAsync(int? clubId = null)
        {
            var query = _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Where(e => e.EventDate <= DateTime.Now);

            if (clubId.HasValue)
                query = query.Where(e => e.ClubID == clubId.Value);

            return await query
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<Event> CreateEventAsync(Event eventItem)
        {
            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();
            return eventItem;
        }

        public async Task<Event> UpdateEventAsync(Event eventItem)
        {
            _context.Events.Update(eventItem);
            await _context.SaveChangesAsync();
            return eventItem;
        }

        public async Task<bool> UpdateEventStatusAsync(int eventId, EventStatus status)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null) return false;

            eventEntity.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Event>> GetEventsByStatusAsync(EventStatus status, int? clubId = null)
        {
            var query = _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Where(e => e.Status == status);

            if (clubId.HasValue)
                query = query.Where(e => e.ClubID == clubId.Value);

            return await query
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            var eventToDelete = await _context.Events.FindAsync(eventId);
            if (eventToDelete == null) return false;

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RegisterUserForEventAsync(int eventId, int userId)
        {
            // Check if user is already registered
            var existingParticipation = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventID == eventId && ep.UserID == userId);

            if (existingParticipation != null)
                return false; // Already registered

            var participation = new EventParticipant
            {
                EventID = eventId,
                UserID = userId,
                Status = AttendanceStatus.Registered,
                RegistrationDate = DateTime.Now
            };

            _context.EventParticipants.Add(participation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateParticipantStatusAsync(int eventId, int userId, AttendanceStatus status)
        {
            var participation = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventID == eventId && ep.UserID == userId);

            if (participation == null) return false;

            participation.Status = status;
            if (status == AttendanceStatus.Attended)
            {
                participation.AttendanceDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<EventParticipant>> GetEventParticipantsAsync(int eventId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.User)
                .Include(ep => ep.Event)
                .Where(ep => ep.EventID == eventId)
                .OrderBy(ep => ep.User.FullName)
                .ToListAsync();
        }

        public async Task<Dictionary<AttendanceStatus, int>> GetEventAttendanceStatisticsAsync(int eventId)
        {
            return await _context.EventParticipants
                .Where(ep => ep.EventID == eventId)
                .GroupBy(ep => ep.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsWithinDaysAsync(int? clubId = null, int days = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(days);
            var query = _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                .Where(e => e.EventDate >= DateTime.Now && e.EventDate <= cutoffDate);

            if (clubId.HasValue)
                query = query.Where(e => e.ClubID == clubId.Value);

            return await query
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetUserEventsAsync(int userId, bool includeHistory = false)
        {
            var query = _context.EventParticipants
                .Include(ep => ep.Event)
                    .ThenInclude(e => e.Club)
                .Where(ep => ep.UserID == userId);

            if (!includeHistory)
                query = query.Where(ep => ep.Event.EventDate >= DateTime.Now);

            return await query
                .Select(ep => ep.Event)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<bool> UnregisterUserFromEventAsync(int eventId, int userId)
        {
            var participation = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventID == eventId && ep.UserID == userId);

            if (participation == null) return false;

            _context.EventParticipants.Remove(participation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, object>> GetEventStatisticsAsync(int eventId)
        {
            var eventEntity = await GetEventByIdAsync(eventId);
            if (eventEntity == null) return new Dictionary<string, object>();

            var attendanceStats = await GetEventAttendanceStatisticsAsync(eventId);
            var totalParticipants = attendanceStats.Values.Sum();
            var attendedCount = attendanceStats.GetValueOrDefault(AttendanceStatus.Attended, 0);
            var attendanceRate = totalParticipants > 0 ? (double)attendedCount / totalParticipants * 100 : 0;

            return new Dictionary<string, object>
            {
                ["TotalRegistered"] = totalParticipants,
                ["Attended"] = attendedCount,
                ["AttendanceRate"] = attendanceRate,
                ["AttendanceBreakdown"] = attendanceStats,
                ["EventDate"] = eventEntity.EventDate,
                ["Location"] = eventEntity.Location,
                ["ClubName"] = eventEntity.Club?.Name ?? "Unknown"
            };
        }

        public async Task<IEnumerable<EventParticipant>> GetUserEventHistoryAsync(int userId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Event)
                    .ThenInclude(e => e.Club)
                .Include(ep => ep.User)
                .Where(ep => ep.UserID == userId)
                .OrderByDescending(ep => ep.Event.EventDate)
                .ToListAsync();
        }
    }
}