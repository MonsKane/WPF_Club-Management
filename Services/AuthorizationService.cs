using ClubManagementApp.Data;
using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ClubManagementDbContext _context;

        public AuthorizationService(ClubManagementDbContext context)
        {
            _context = context;
        }

        public bool IsAdmin(User? role)
        {
            return role?.Role is UserRole.SystemAdmin or UserRole.Admin;
        }

        // User Management Permissions
        public bool CanCreateUsers(UserRole role, int? userClubId = null)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => true, // Club only - will be validated in business logic
                _ => false
            };
        }

        public bool CanEditUsers(UserRole role, int? userClubId = null, int? targetUserClubId = null, bool isSelf = false)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => userClubId == targetUserClubId, // Club only
                UserRole.Member => isSelf, // Self only
                _ => false
            };
        }

        public bool CanDeleteUsers(UserRole role)
        {
            return role == UserRole.SystemAdmin || role == UserRole.Admin;
        }

        public bool CanAssignRoles(UserRole role, int? userClubId = null)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => true, // Limited - will be validated in business logic
                _ => false
            };
        }

        // Club Management Permissions
        public bool CanCreateClubs(UserRole role)
        {
            return role == UserRole.SystemAdmin || role == UserRole.Admin;
        }

        public bool CanEditClubs(UserRole role, int? userClubId = null, int? targetClubId = null)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => userClubId == targetClubId, // Own club only
                _ => false
            };
        }

        public bool CanDeleteClubs(UserRole role)
        {
            return role == UserRole.SystemAdmin || role == UserRole.Admin;
        }

        // Event Management Permissions
        public bool CanCreateEvents(UserRole role)
        {
            return role is UserRole.SystemAdmin or UserRole.Admin or UserRole.Chairman;
        }

        public bool CanJoinEvents(UserRole role)
        {
            return true;
        }

        public bool CanEditEvents(UserRole role, int? userClubId = null, int? eventClubId = null, bool isOwnEvent = false)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => true,
                UserRole.ViceChairman => true,
                UserRole.TeamLeader => isOwnEvent, // Own events only
                _ => false
            };
        }

        public bool CanDeleteEvents(UserRole role, int? userClubId = null, int? eventClubId = null, bool isOwnEvent = false)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => true,
                UserRole.TeamLeader => isOwnEvent, // Own events only
                _ => false
            };
        }

        public bool CanRegisterForEvents(UserRole role)
        {
            return true; // All roles can register for events
        }

        // Reporting Permissions
        public bool CanGenerateReports(UserRole role)
        {
            return role is UserRole.SystemAdmin or UserRole.Admin;
        }

        public bool CanExportReports(UserRole role)
        {
            return role is UserRole.SystemAdmin or UserRole.Admin;
        }

        public bool CanViewStatistics(UserRole role)
        {
            return role is UserRole.SystemAdmin or UserRole.Admin;
        }

        // System Settings Permissions
        public bool CanAccessGlobalSettings(UserRole role)
        {
            return role == UserRole.SystemAdmin || role == UserRole.Admin;
        }

        public bool CanAccessClubSettings(UserRole role, int? userClubId = null, int? targetClubId = null)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.Chairman => userClubId == targetClubId, // Own club only
                _ => false
            };
        }

        // Legacy methods for backward compatibility

        public bool CanAccessFeature(UserRole role, string feature)
        {
            return feature switch
            {
                "CreateUsers" => CanCreateUsers(role),
                "EditUsers" => CanEditUsers(role),
                "DeleteUsers" => CanDeleteUsers(role),
                "AssignRoles" => CanAssignRoles(role),
                "CreateClubs" => CanCreateClubs(role),
                "EditClubs" => CanEditClubs(role),
                "DeleteClubs" => CanDeleteClubs(role),
                "CreateEvents" => CanCreateEvents(role),
                "EditEvents" => CanEditEvents(role),
                "DeleteEvents" => CanDeleteEvents(role),
                "RegisterForEvents" => CanRegisterForEvents(role),
                "GenerateReports" => CanGenerateReports(role),
                "ExportReports" => CanExportReports(role),
                "GlobalSettings" => CanAccessGlobalSettings(role),
                "ClubSettings" => CanAccessClubSettings(role),
                "UserManagement" => CanCreateUsers(role) || CanEditUsers(role) || CanDeleteUsers(role),
                "MemberManagement" => CanCreateUsers(role) || CanCreateUsers(role) || CanCreateUsers(role),
                "ClubManagement" => CanCreateClubs(role) || CanEditClubs(role) || CanDeleteClubs(role),
                "EventManagement" => CanJoinEvents(role) || CanCreateEvents(role) || CanEditEvents(role) || CanDeleteEvents(role),
                "ReportView" => CanGenerateReports(role),
                "Dashboard" => true, // Everyone can access dashboard
                _ => false
            };
        }

        public bool CanManageClub(UserRole role, int? userClubId, int targetClubId)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.ClubPresident => userClubId == targetClubId,
                UserRole.Chairman => userClubId == targetClubId,
                UserRole.ViceChairman => userClubId == targetClubId,
                UserRole.ClubOfficer => userClubId == targetClubId,
                _ => false
            };
        }

        public bool CanManageUser(UserRole role, UserRole targetUserRole, int? userClubId, int? targetUserClubId)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => targetUserRole != UserRole.SystemAdmin,
                UserRole.ClubPresident => userClubId == targetUserClubId &&
                                        targetUserRole != UserRole.SystemAdmin &&
                                        targetUserRole != UserRole.Admin,
                UserRole.Chairman => userClubId == targetUserClubId &&
                                   targetUserRole != UserRole.SystemAdmin &&
                                   targetUserRole != UserRole.Admin &&
                                   targetUserRole != UserRole.ClubPresident,
                UserRole.ViceChairman => userClubId == targetUserClubId &&
                                       targetUserRole != UserRole.SystemAdmin &&
                                       targetUserRole != UserRole.Admin &&
                                       targetUserRole != UserRole.ClubPresident &&
                                       targetUserRole != UserRole.Chairman,
                UserRole.ClubOfficer => userClubId == targetUserClubId &&
                                      (targetUserRole == UserRole.TeamLeader || targetUserRole == UserRole.Member),
                UserRole.TeamLeader => userClubId == targetUserClubId &&
                                     targetUserRole == UserRole.Member,
                _ => false
            };
        }

        public bool CanManageEvent(UserRole role, int? userClubId, int eventClubId)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.ClubPresident => userClubId == eventClubId,
                UserRole.Chairman => userClubId == eventClubId,
                UserRole.ViceChairman => userClubId == eventClubId,
                UserRole.ClubOfficer => userClubId == eventClubId,
                UserRole.TeamLeader => userClubId == eventClubId,
                _ => false
            };
        }

        public bool CanViewReports(UserRole role, int? userClubId, int? reportClubId)
        {
            return role switch
            {
                UserRole.SystemAdmin => true,
                UserRole.Admin => true,
                UserRole.ClubPresident => reportClubId == null || userClubId == reportClubId,
                UserRole.Chairman => reportClubId == null || userClubId == reportClubId,
                UserRole.ViceChairman => reportClubId == null || userClubId == reportClubId,
                UserRole.ClubOfficer => userClubId == reportClubId,
                UserRole.TeamLeader => userClubId == reportClubId,
                _ => false
            };
        }



        public async Task<bool> IsAuthorizedAsync(int userId, string action, object? resource = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            return action switch
            {
                "ViewDashboard" => CanAccessFeature(user.Role, "Dashboard"),
                "ManageUsers" => CanAccessFeature(user.Role, "UserManagement"),
                "ManageClubs" => CanAccessFeature(user.Role, "ClubManagement"),
                "ManageEvents" => CanAccessFeature(user.Role, "EventManagement"),
                "ViewReports" => CanAccessFeature(user.Role, "ReportView"),
                "GenerateReports" => CanGenerateReports(user.Role),
                _ => false
            };
        }

        public bool CanViewEvent(UserRole role)
        {
            return role switch
            {
                _ => true
            };
        }

        public bool CanViewClub(UserRole role)
        {
            return role switch
            {
                UserRole.Member => false,
                _ => true
            };
        }

        public bool CanViewUser(UserRole role)
        {
            return role switch
            {
                UserRole.Member => false,
                _ => true
            };
        }
    }
}