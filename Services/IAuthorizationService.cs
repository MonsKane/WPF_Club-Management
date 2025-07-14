using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IAuthorizationService
    {
        // User Management Permissions
        bool CanCreateUsers(SystemRole systemRole, ClubRole? clubRole = null);
        bool CanEditUsers(SystemRole systemRole, ClubRole? clubRole = null, bool isSelf = false);
        bool CanDeleteUsers(SystemRole systemRole);
        bool CanAssignRoles(SystemRole systemRole, ClubRole? clubRole = null);

        // Club Management Permissions
        bool CanCreateClubs(SystemRole systemRole);
        bool CanEditClubs(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnClub = false);
        bool CanDeleteClubs(SystemRole systemRole);

        // Event Management Permissions
        bool CanCreateEvents(SystemRole systemRole, ClubRole? clubRole = null);
        bool CanJoinEvents(SystemRole systemRole);
        bool CanEditEvents(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnEvent = false);
        bool CanDeleteEvents(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnEvent = false);
        bool CanRegisterForEvents(SystemRole systemRole);

        // Reporting Permissions
        bool CanGenerateReports(SystemRole systemRole, ClubRole? clubRole = null);
        bool CanExportReports(SystemRole systemRole, ClubRole? clubRole = null);
        bool CanViewStatistics(SystemRole systemRole, ClubRole? clubRole = null);

        // System Settings Permissions
        bool CanAccessGlobalSettings(SystemRole systemRole);
        bool CanAccessClubSettings(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnClub = false);

        // Legacy methods for backward compatibility
        bool CanAccessFeature(SystemRole systemRole, string feature, ClubRole? clubRole = null);
        bool CanManageClub(SystemRole systemRole, ClubRole? clubRole, bool isOwnClub);
        bool CanManageUser(SystemRole systemRole, SystemRole targetSystemRole, ClubRole? clubRole, ClubRole? targetClubRole);
        bool CanManageEvent(SystemRole systemRole, ClubRole? clubRole, bool isOwnClub);
        bool CanViewReports(SystemRole systemRole, ClubRole? clubRole, bool isOwnClub = true);
        bool CanViewEvent(SystemRole systemRole);
        bool CanViewClub(SystemRole systemRole, ClubRole? clubRole = null);
        bool CanViewUser(SystemRole systemRole, ClubRole? clubRole = null);
        Task<bool> IsAuthorizedAsync(int userId, string action, object? resource = null);
    }
}