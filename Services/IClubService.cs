using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetAllClubsAsync();
        Task<Club?> GetClubByIdAsync(int clubId);
        Task<Club?> GetClubByNameAsync(string name);
        Task<Club> CreateClubAsync(Club club);
        Task<Club> UpdateClubAsync(Club club);
        Task<bool> DeleteClubAsync(int clubId);
        Task<int> GetMemberCountAsync(int clubId);
        Task<IEnumerable<User>> GetClubMembersAsync(int clubId);
        Task<bool> AssignClubLeadershipAsync(int clubId, int userId, UserRole role);
        Task<User?> GetClubChairmanAsync(int clubId);
        Task<IEnumerable<User>> GetClubViceChairmenAsync(int clubId);
        Task<IEnumerable<User>> GetClubTeamLeadersAsync(int clubId);
        Task<Dictionary<UserRole, int>> GetClubRoleDistributionAsync(int clubId);
        Task<bool> RemoveClubLeadershipAsync(int clubId, int userId);
        Task<Dictionary<string, object>> GetClubStatisticsAsync(int clubId);
        Task<bool> AddUserToClubAsync(int userId, int clubId, UserRole role);
        Task<int> GetTotalClubsCountAsync();
    }
}
