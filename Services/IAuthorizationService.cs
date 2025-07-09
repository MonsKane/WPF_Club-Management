using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IAuthorizationService
    {
        bool CanAccessFeature(UserRole role, string feature);
        bool CanManageClub(UserRole role, int? userClubId, int targetClubId);
        bool CanManageUser(UserRole role, UserRole targetUserRole, int? userClubId, int? targetUserClubId);
        bool CanManageEvent(UserRole role, int? userClubId, int eventClubId);
        bool CanViewReports(UserRole role, int? userClubId, int? reportClubId);
        bool CanGenerateReports(UserRole role);
        Task<bool> IsAuthorizedAsync(int userId, string action, object? resource = null);
    }
}