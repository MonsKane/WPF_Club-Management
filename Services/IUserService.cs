using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetUsersByClubAsync(int clubId);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        Task<IEnumerable<User>> GetMembersBySystemRoleAsync(SystemRole role);
        Task<IEnumerable<User>> GetMembersByClubRoleAsync(ClubRole role, int clubId);
        Task<Dictionary<string, object>> GetMemberParticipationHistoryAsync(int userId);
        Task<bool> UpdateUserSystemRoleAsync(int userId, SystemRole newRole);
        Task<bool> AssignUserToClubAsync(int userId, int clubId);
        Task<bool> RemoveUserFromClubAsync(int userId);
        Task<IEnumerable<User>> GetUsersWithoutClubAsync();
        Task<bool> UpdateUserRoleAsync(int userId, SystemRole newRole);
        Task<User?> GetCurrentUserAsync();
        void SetCurrentUser(User? user);
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetNewMembersThisMonthCountAsync();
    }
}
