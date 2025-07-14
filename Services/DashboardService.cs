using ClubManagementApp.Data;
using ClubManagementApp.Models;
using ClubManagementApp.DTOs;
using ClubManagementApp.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementApp.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10);
        Task<IEnumerable<UpcomingEventDto>> GetUpcomingEventsAsync(int count = 5);
        Task<IEnumerable<ClubStatisticsDto>> GetTopActiveClubsAsync(int count = 5);
        Task<IEnumerable<MemberActivityDto>> GetTopActiveMembersAsync(int count = 10);
        Task<Dictionary<string, int>> GetEventParticipationTrendsAsync(int months = 6);
        Task<Dictionary<SystemRole, int>> GetMembershipDistributionAsync();
        Task<Dictionary<string, decimal>> GetClubGrowthRatesAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;

        public DashboardService(
            ClubManagementDbContext context,
            ILoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            try
            {
                var totalMembers = await _context.Users.CountAsync();
                var activeMembers = await _context.Users.CountAsync(u => u.IsActive);
                var totalClubs = await _context.Clubs.CountAsync();
                var activeClubs = await _context.Clubs.CountAsync(c => c.IsActive);
                var totalEvents = await _context.Events.CountAsync();
                var upcomingEvents = await _context.Events
                    .CountAsync(e => e.EventDate > DateTime.UtcNow);
                var totalReports = await _context.Reports.CountAsync();
                var recentReports = await _context.Reports
                    .CountAsync(r => r.GeneratedDate > DateTime.UtcNow.AddDays(-30));

                // Calculate participation rate for current month
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var monthlyEvents = await _context.Events
                    .Where(e => e.EventDate.Month == currentMonth && e.EventDate.Year == currentYear)
                    .CountAsync();
                var monthlyParticipations = await _context.EventParticipants
                    .Include(ep => ep.Event)
                    .Where(ep => ep.Event.EventDate.Month == currentMonth && 
                               ep.Event.EventDate.Year == currentYear &&
                               ep.Status == AttendanceStatus.Attended)
                    .CountAsync();

                var participationRate = monthlyEvents > 0 ? 
                    (decimal)monthlyParticipations / (monthlyEvents * activeMembers) * 100 : 0;

                return new DashboardSummaryDto
                {
                    TotalMembers = totalMembers,
                    ActiveMembers = activeMembers,
                    TotalClubs = totalClubs,
                    ActiveClubs = activeClubs,
                    TotalEvents = totalEvents,
                    UpcomingEvents = upcomingEvents,
                    TotalReports = totalReports,
                    RecentReports = recentReports,
                    ParticipationRate = Math.Round(participationRate, 2),
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get dashboard summary", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10)
        {
            try
            {
                var activities = new List<RecentActivityDto>();

                // Recent user registrations
                var recentUsers = await _context.Users
                    .Where(u => u.CreatedAt > DateTime.UtcNow.AddDays(-7))
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(count / 2)
                    .Select(u => new RecentActivityDto
                    {
                        Id = u.UserID,
                        Type = "User Registration",
                        Description = $"{u.FullName} joined the system",
                        Timestamp = u.CreatedAt,
                        UserId = u.UserID,
                        UserName = u.FullName
                    })
                    .ToListAsync();

                activities.AddRange(recentUsers);

                // Recent event registrations
                var recentEventRegistrations = await _context.EventParticipants
                    .Include(ep => ep.User)
                    .Include(ep => ep.Event)
                    .Where(ep => ep.RegistrationDate > DateTime.UtcNow.AddDays(-7))
                    .OrderByDescending(ep => ep.RegistrationDate)
                    .Take(count / 2)
                    .Select(ep => new RecentActivityDto
                    {
                        Id = ep.ParticipantID,
                        Type = "Event Registration",
                        Description = $"{ep.User.FullName} registered for {ep.Event.Name}",
                        Timestamp = ep.RegistrationDate,
                        UserId = ep.UserID,
                        UserName = ep.User.FullName,
                        EventId = ep.EventID,
                        EventName = ep.Event.Name
                    })
                    .ToListAsync();

                activities.AddRange(recentEventRegistrations);

                return activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get recent activities", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<IEnumerable<UpcomingEventDto>> GetUpcomingEventsAsync(int count = 5)
        {
            try
            {
                var upcomingEvents = await _context.Events
                    .Include(e => e.Club)
                    .Where(e => e.EventDate > DateTime.UtcNow)
                    .OrderBy(e => e.EventDate)
                    .Take(count)
                    .Select(e => new UpcomingEventDto
                    {
                        Id = e.EventID,
                        Name = e.Name,
                        EventDate = e.EventDate,
                        Location = e.Location,
                        ClubName = e.Club.Name,
                        RegisteredCount = e.Participants.Count(),
                        MaxParticipants = e.MaxParticipants,
                        DaysUntilEvent = (int)(e.EventDate - DateTime.UtcNow).TotalDays
                    })
                    .ToListAsync();

                return upcomingEvents;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get upcoming events", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<IEnumerable<ClubStatisticsDto>> GetTopActiveClubsAsync(int count = 5)
        {
            try
            {
                var clubStats = await _context.Clubs
                    .Include(c => c.ClubMembers)
                    .Include(c => c.Events)
                        .ThenInclude(e => e.Participants)
                    .Where(c => c.IsActive)
                    .Select(c => new ClubStatisticsDto
                    {
                        ClubID = c.ClubID,
                        ClubName = c.Name,
                        EstablishedDate = c.EstablishedDate ?? DateTime.Now,
                        ActiveMembers = c.ClubMembers.Count(m => m.IsActive),
                         TotalMembers = c.ClubMembers.Count(),
                        TotalEvents = c.Events.Count(),
                        CompletedEvents = c.Events.Count(e => e.EventDate < DateTime.UtcNow),
                        UpcomingEvents = c.Events.Count(e => e.EventDate > DateTime.UtcNow),
                        AverageAttendanceRate = c.Events.Any() ? 
                            c.Events.Average(e => e.Participants.Count(ep => ep.Status == AttendanceStatus.Attended)) : 0
                    })
                    .OrderByDescending(cs => cs.TotalEvents)
                    .Take(count)
                    .ToListAsync();

                return clubStats;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get top active clubs", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<IEnumerable<MemberActivityDto>> GetTopActiveMembersAsync(int count = 10)
        {
            try
            {
                var memberActivities = await _context.Users
                    .Include(u => u.EventParticipations)
                        .ThenInclude(ep => ep.Event)
                    .Where(u => u.IsActive)
                    .Select(u => new MemberActivityDto
                    {
                        FullName = u.FullName,
                        Email = u.Email,
                        TotalEventsRegistered = u.EventParticipations.Count(),
                        EventsAttended = u.EventParticipations.Count(ep => ep.Status == AttendanceStatus.Attended),
                        AttendancePercentage = u.EventParticipations.Count() > 0 ? 
                            (double)u.EventParticipations.Count(ep => ep.Status == AttendanceStatus.Attended) / 
                            u.EventParticipations.Count() * 100 : 0,
                        ActivityLevel = u.EventParticipations.Count(ep => ep.Status == AttendanceStatus.Attended) >= 5 ? "Active" : 
                                       u.EventParticipations.Count(ep => ep.Status == AttendanceStatus.Attended) >= 2 ? "Normal" : "Inactive"
                    })
                    .OrderByDescending(ma => ma.TotalEventsRegistered)
                    .Take(count)
                    .ToListAsync();

                return memberActivities;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get top active members", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<Dictionary<string, int>> GetEventParticipationTrendsAsync(int months = 6)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddMonths(-months);
                var trends = new Dictionary<string, int>();

                for (int i = 0; i < months; i++)
                {
                    var monthStart = startDate.AddMonths(i);
                    var monthEnd = monthStart.AddMonths(1);
                    var monthName = monthStart.ToString("MMM yyyy");

                    var participationCount = await _context.EventParticipants
                        .Include(ep => ep.Event)
                        .Where(ep => ep.Event.EventDate >= monthStart && 
                                   ep.Event.EventDate < monthEnd &&
                                   ep.Status == AttendanceStatus.Attended)
                        .CountAsync();

                    trends[monthName] = participationCount;
                }

                return trends;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get event participation trends", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<Dictionary<SystemRole, int>> GetMembershipDistributionAsync()
        {
            try
            {
                var distribution = await _context.Users
                    .Where(u => u.IsActive)
                    .GroupBy(u => u.SystemRole)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                return distribution;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get membership distribution", ex);
                throw new DatabaseConnectionException(ex);
            }
        }

        public async Task<Dictionary<string, decimal>> GetClubGrowthRatesAsync()
        {
            try
            {
                var growthRates = new Dictionary<string, decimal>();
                var clubs = await _context.Clubs
                    .Include(c => c.ClubMembers)
                    .Where(c => c.IsActive)
                    .ToListAsync();

                foreach (var club in clubs)
                {
                    var currentMonth = DateTime.UtcNow.Month;
                    var currentYear = DateTime.UtcNow.Year;
                    var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
                    var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

                    var currentMemberCount = club.ClubMembers
                         .Count(m => m.IsActive && 
                                m.JoinDate.Month <= currentMonth && 
                                m.JoinDate.Year <= currentYear);

                     var previousMemberCount = club.ClubMembers
                         .Count(m => m.IsActive && 
                                m.JoinDate.Month <= previousMonth && 
                               m.JoinDate.Year <= previousYear);

                    var growthRate = previousMemberCount > 0 ? 
                        (decimal)(currentMemberCount - previousMemberCount) / previousMemberCount * 100 : 0;

                    growthRates[club.Name] = Math.Round(growthRate, 2);
                }

                return growthRates;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get club growth rates", ex);
                throw new DatabaseConnectionException(ex);
            }
        }
    }

    // Additional DTOs for dashboard
    public class DashboardSummaryDto
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalClubs { get; set; }
        public int ActiveClubs { get; set; }
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int TotalReports { get; set; }
        public int RecentReports { get; set; }
        public decimal ParticipationRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? EventId { get; set; }
        public string? EventName { get; set; }
        public int? ClubId { get; set; }
        public string? ClubName { get; set; }
    }

    public class UpcomingEventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public int RegisteredCount { get; set; }
        public int? MaxParticipants { get; set; }
        public int DaysUntilEvent { get; set; }
    }
}