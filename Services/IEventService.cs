using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IEventService
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByIdAsync(int eventId);
        Task<IEnumerable<Event>> GetEventsByClubAsync(int clubId);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync(int? clubId = null);
        Task<IEnumerable<Event>> GetPastEventsAsync(int? clubId = null);
        Task<Event> CreateEventAsync(Event eventItem);
        Task<Event> UpdateEventAsync(Event eventItem);
        Task<bool> DeleteEventAsync(int eventId);
        Task<bool> RegisterUserForEventAsync(int eventId, int userId);
        Task<bool> UpdateParticipantStatusAsync(int eventId, int userId, AttendanceStatus status);
        Task<IEnumerable<EventParticipant>> GetEventParticipantsAsync(int eventId);
        Task<Dictionary<AttendanceStatus, int>> GetEventAttendanceStatisticsAsync(int eventId);
        Task<IEnumerable<Event>> GetUpcomingEventsWithinDaysAsync(int? clubId = null, int days = 30);
        Task<IEnumerable<Event>> GetUserEventsAsync(int userId, bool includeHistory = false);
        Task<bool> UnregisterUserFromEventAsync(int eventId, int userId);
        Task<Dictionary<string, object>> GetEventStatisticsAsync(int eventId);
        Task<IEnumerable<EventParticipant>> GetUserEventHistoryAsync(int userId);
        Task<int> GetTotalEventsCountAsync();
        Task<int> GetActiveEventsCountAsync();
        Task<int> GetUpcomingEventsCountAsync();
    }
}
