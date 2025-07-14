using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetAllClubsAsync();
        Task<Club?> GetClubByIdAsync(int clubId);
        Task<Club?> GetClubByNameAsync(string clubName);
        Task<Club> CreateClubAsync(Club club);
        Task<Club> UpdateClubAsync(Club club);
        Task<bool> DeleteClubAsync(int clubId);
        Task<int> GetMemberCountAsync(int clubId);
        Task<IEnumerable<ClubMember>> GetClubMembersAsync(int clubId);
        Task<bool> AssignClubRoleAsync(int clubId, int userId, ClubRole role);
        Task<bool> AssignClubLeadershipAsync(int clubId, int userId, SystemRole role);
        Task<ClubMember?> GetClubChairmanAsync(int clubId);
        Task<IEnumerable<ClubMember>> GetClubAdminsAsync(int clubId);
        Task<Dictionary<ClubRole, int>> GetClubRoleDistributionAsync(int clubId);
        Task<bool> RemoveClubMembershipAsync(int clubId, int userId);
        Task<Dictionary<string, object>> GetClubStatisticsAsync(int clubId);
        Task<bool> AddUserToClubAsync(int userId, int clubId, ClubRole role);
        Task<int> GetTotalClubsCountAsync();
    }
}
