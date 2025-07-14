using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IAuthorizationService
    {
        // User Management Permissions

        bool IsAdmin(User? user);
        bool CanCreateUsers(UserRole role, int? userClubId = null);
        bool CanEditUsers(UserRole role, int? userClubId = null, int? targetUserClubId = null, bool isSelf = false);
        bool CanDeleteUsers(UserRole role);
        bool CanAssignRoles(UserRole role, int? userClubId = null);

        // Club Management Permissions
        bool CanCreateClubs(UserRole role);
        bool CanEditClubs(UserRole role, int? userClubId = null, int? targetClubId = null);
        bool CanDeleteClubs(UserRole role);

        // Event Management Permissions
        bool CanCreateEvents(UserRole role);
        bool CanJoinEvents(UserRole role);
        bool CanEditEvents(UserRole role, int? userClubId = null, int? eventClubId = null, bool isOwnEvent = false);
        bool CanDeleteEvents(UserRole role, int? userClubId = null, int? eventClubId = null, bool isOwnEvent = false);
        bool CanRegisterForEvents(UserRole role);

        // Reporting Permissions
        bool CanGenerateReports(UserRole role);
        bool CanExportReports(UserRole role);
        bool CanViewStatistics(UserRole role);

        // System Settings Permissions
        bool CanAccessGlobalSettings(UserRole role);
        bool CanAccessClubSettings(UserRole role, int? userClubId = null, int? targetClubId = null);

        // Legacy methods for backward compatibility
        bool CanAccessFeature(UserRole role, string feature);
        bool CanManageClub(UserRole role, int? userClubId, int targetClubId);
        bool CanManageUser(UserRole role, UserRole targetUserRole, int? userClubId, int? targetUserClubId);
        bool CanManageEvent(UserRole role, int? userClubId, int eventClubId);
        bool CanViewReports(UserRole role, int? userClubId, int? reportClubId);
        bool CanViewEvent(UserRole role);
        bool CanViewClub(UserRole role);
        bool CanViewUser(UserRole role);
        Task<bool> IsAuthorizedAsync(int userId, string action, object? resource = null);
    }
}