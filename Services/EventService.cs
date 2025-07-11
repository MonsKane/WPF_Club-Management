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
            Console.WriteLine("[EVENT_SERVICE] Getting all events from database...");
            var events = await _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            Console.WriteLine($"[EVENT_SERVICE] Retrieved {events.Count} events from database");
            return events;
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
            Console.WriteLine($"[EVENT_SERVICE] Getting event by ID: {eventId}");
            var eventItem = await _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem != null)
            {
                Console.WriteLine($"[EVENT_SERVICE] Found event: {eventItem.Name} on {eventItem.EventDate:yyyy-MM-dd} with {eventItem.Participants?.Count ?? 0} participants");
            }
            else
            {
                Console.WriteLine($"[EVENT_SERVICE] Event not found with ID: {eventId}");
            }
            return eventItem;
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
            try
            {
                Console.WriteLine($"[EVENT_SERVICE] Creating new event: {eventItem.Name}");
                Console.WriteLine($"[EVENT_SERVICE] Event date: {eventItem.EventDate:yyyy-MM-dd HH:mm}, Location: {eventItem.Location}, Club ID: {eventItem.ClubID}");

                // Input validation
                if (eventItem == null)
                    throw new ArgumentNullException(nameof(eventItem), "Event cannot be null");

                if (string.IsNullOrWhiteSpace(eventItem.Name))
                    throw new ArgumentException("Event name is required", nameof(eventItem));

                if (eventItem.EventDate <= DateTime.Now)
                    throw new ArgumentException("Event date must be in the future", nameof(eventItem));

                if (eventItem.ClubID <= 0)
                    throw new ArgumentException("Valid club ID is required", nameof(eventItem));

                // Verify club exists
                var club = await _context.Clubs.FindAsync(eventItem.ClubID);
                if (club == null)
                    throw new InvalidOperationException($"Club with ID {eventItem.ClubID} not found");

                _context.Events.Add(eventItem);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[EVENT_SERVICE] Event created successfully with ID: {eventItem.EventID}");
                return eventItem;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error creating event: {ex.Message}");
                throw;
            }
        }

        public async Task<Event> UpdateEventAsync(Event eventItem)
        {
            try
            {
                Console.WriteLine($"[EVENT_SERVICE] Updating event: {eventItem.Name} (ID: {eventItem.EventID})");

                // Input validation
                if (eventItem == null)
                    throw new ArgumentNullException(nameof(eventItem), "Event cannot be null");

                if (eventItem.EventID <= 0)
                    throw new ArgumentException("Invalid event ID", nameof(eventItem));

                if (string.IsNullOrWhiteSpace(eventItem.Name))
                    throw new ArgumentException("Event name is required", nameof(eventItem));

                // Check if event exists and get tracked entity
                var existingEvent = await _context.Events.FindAsync(eventItem.EventID);
                if (existingEvent == null)
                    throw new InvalidOperationException($"Event with ID {eventItem.EventID} not found");

                // Update properties of the tracked entity to avoid tracking conflicts
                existingEvent.Name = eventItem.Name;
                existingEvent.Description = eventItem.Description;
                existingEvent.EventDate = eventItem.EventDate;
                existingEvent.Location = eventItem.Location;
                existingEvent.ClubID = eventItem.ClubID;
                existingEvent.Status = eventItem.Status;
                existingEvent.MaxParticipants = eventItem.MaxParticipants;
                existingEvent.RegistrationDeadline = eventItem.RegistrationDeadline;

                await _context.SaveChangesAsync();
                Console.WriteLine($"[EVENT_SERVICE] Event updated successfully: {existingEvent.Name}");
                
                // Return the updated tracked entity
                return existingEvent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error updating event: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateEventStatusAsync(int eventId, EventStatus status)
        {
            Console.WriteLine($"[EVENT_SERVICE] Updating event {eventId} status to {status}");
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                Console.WriteLine($"[EVENT_SERVICE] Event not found for status update: {eventId}");
                return false;
            }

            var oldStatus = eventEntity.Status;
            eventEntity.Status = status;
            await _context.SaveChangesAsync();
            Console.WriteLine($"[EVENT_SERVICE] Event {eventEntity.Name} status changed from {oldStatus} to {status}");
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
            try
            {
                Console.WriteLine($"[EVENT_SERVICE] Deleting event with ID: {eventId}");

                // Input validation
                if (eventId <= 0)
                    throw new ArgumentException("Invalid event ID", nameof(eventId));

                var eventItem = await _context.Events.FindAsync(eventId);
                if (eventItem == null)
                {
                    Console.WriteLine($"[EVENT_SERVICE] Event with ID {eventId} not found");
                    return false;
                }

                // Check if event has participants
                var hasParticipants = await _context.EventParticipants
                    .AnyAsync(ep => ep.EventID == eventId);

                if (hasParticipants)
                    throw new InvalidOperationException($"Cannot delete event '{eventItem.Name}' because it has registered participants");

                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[EVENT_SERVICE] Event '{eventItem.Name}' deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error deleting event {eventId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RegisterUserForEventAsync(int eventId, int userId)
        {
            try
            {
                Console.WriteLine($"[EVENT_SERVICE] Registering user {userId} for event {eventId}");

                // Input validation
                if (eventId <= 0)
                    throw new ArgumentException("Invalid event ID", nameof(eventId));

                if (userId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(userId));

                // Verify event exists
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                    throw new InvalidOperationException($"Event with ID {eventId} not found");

                // Verify user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {userId} not found");

                // Check if user is already registered
                var existingParticipation = await _context.EventParticipants
                    .FirstOrDefaultAsync(ep => ep.EventID == eventId && ep.UserID == userId);

                if (existingParticipation != null)
                {
                    Console.WriteLine($"[EVENT_SERVICE] User {userId} already registered for event {eventId}");
                    return false; // Already registered
                }

                // Check if event has reached maximum participants
                if (eventEntity.MaxParticipants.HasValue)
                {
                    var currentParticipants = await _context.EventParticipants
                        .CountAsync(ep => ep.EventID == eventId);

                    if (currentParticipants >= eventEntity.MaxParticipants.Value)
                        throw new InvalidOperationException($"Event '{eventEntity.Name}' has reached maximum capacity of {eventEntity.MaxParticipants} participants");
                }

                var participation = new EventParticipant
                {
                    EventID = eventId,
                    UserID = userId,
                    Status = AttendanceStatus.Registered,
                    RegistrationDate = DateTime.Now
                };

                _context.EventParticipants.Add(participation);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[EVENT_SERVICE] User {userId} successfully registered for event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error registering user {userId} for event {eventId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateParticipantStatusAsync(int eventId, int userId, AttendanceStatus status)
        {
            try
            {
                Console.WriteLine($"[EVENT_SERVICE] Updating participant status for user {userId} in event {eventId} to {status}");

                // Input validation
                if (eventId <= 0)
                    throw new ArgumentException("Invalid event ID", nameof(eventId));

                if (userId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(userId));

                // Verify event exists
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                    throw new InvalidOperationException($"Event with ID {eventId} not found");

                // Verify user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {userId} not found");

                var participation = await _context.EventParticipants
                    .FirstOrDefaultAsync(ep => ep.EventID == eventId && ep.UserID == userId);

                if (participation == null)
                {
                    Console.WriteLine($"[EVENT_SERVICE] Participation record not found for user {userId} in event {eventId}");
                    return false;
                }

                var oldStatus = participation.Status;
                participation.Status = status;
                if (status == AttendanceStatus.Attended)
                {
                    participation.AttendanceDate = DateTime.Now;
                    Console.WriteLine($"[EVENT_SERVICE] Marked attendance time for user {userId} in event {eventId}");
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"[EVENT_SERVICE] Participant status updated: user {userId} in event {eventId} changed from {oldStatus} to {status}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error updating participant status: {ex.Message}");
                throw;
            }
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
            try
            {
                Console.WriteLine($"[EVENT_SERVICE] Unregistering user {userId} from event {eventId}");

                // Input validation
                if (eventId <= 0)
                    throw new ArgumentException("Invalid event ID", nameof(eventId));

                if (userId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(userId));

                // Verify event exists
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                    throw new InvalidOperationException($"Event with ID {eventId} not found");

                // Verify user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {userId} not found");

                var participation = await _context.EventParticipants
                    .FirstOrDefaultAsync(ep => ep.EventID == eventId && ep.UserID == userId);

                if (participation == null)
                {
                    Console.WriteLine($"[EVENT_SERVICE] User {userId} is not registered for event {eventId}");
                    return false;
                }

                // Check if event has already occurred
                if (eventEntity.EventDate <= DateTime.Now)
                    throw new InvalidOperationException($"Cannot unregister from event '{eventEntity.Name}' because it has already occurred");

                _context.EventParticipants.Remove(participation);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[EVENT_SERVICE] User {userId} successfully unregistered from event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error unregistering user {userId} from event {eventId}: {ex.Message}");
                throw;
            }
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

        /// <summary>
        /// Gets the total count of events in the database efficiently.
        ///
        /// Performance:
        /// - Uses COUNT query instead of loading all records
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, admin overview
        /// </summary>
        /// <returns>Total number of events in the system</returns>
        public async Task<int> GetTotalEventsCountAsync()
        {
            try
            {
                return await _context.Events.CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error getting total events count: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of active events (happening today).
        ///
        /// Performance:
        /// - Uses COUNT query with date filtering
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, current activity overview
        /// </summary>
        /// <returns>Number of events happening today</returns>
        public async Task<int> GetActiveEventsCountAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                return await _context.Events
                    .Where(e => e.EventDate >= today && e.EventDate < tomorrow)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error getting active events count: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of upcoming events (future events).
        ///
        /// Performance:
        /// - Uses COUNT query with date filtering
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, planning overview
        /// </summary>
        /// <returns>Number of events scheduled for the future</returns>
        public async Task<int> GetUpcomingEventsCountAsync()
        {
            try
            {
                var now = DateTime.Now;
                return await _context.Events
                    .Where(e => e.EventDate > now)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EVENT_SERVICE] Error getting upcoming events count: {ex.Message}");
                throw;
            }
        }
    }
}
