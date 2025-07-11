using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetUsersByClubAsync(int clubId);
        Task<IEnumerable<User>> GetUsersWithoutClubAsync();
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        Task UpdateActivityLevelAsync(int userId, ActivityLevel activityLevel);
        Task<Dictionary<ActivityLevel, int>> GetActivityStatisticsAsync(int? clubId = null);
        Task<IEnumerable<User>> GetMembersByRoleAsync(UserRole role, int? clubId = null);
        Task<Dictionary<string, object>> GetMemberParticipationHistoryAsync(int userId);
        Task<bool> AssignUserToClubAsync(int userId, int clubId);
        Task<bool> RemoveUserFromClubAsync(int userId);
        Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole);
        Task<IEnumerable<User>> GetClubLeadershipAsync(int clubId);
        Task<User?> GetCurrentUserAsync();
        void SetCurrentUser(User? user);
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetNewMembersThisMonthCountAsync();
    }
}
