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

        // User Management Permissions
        public bool CanCreateUsers(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin, // Club admins can create users
                _ => false
            };
        }

        public bool CanEditUsers(SystemRole systemRole, ClubRole? clubRole = null, bool isSelf = false)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => isSelf, // Self only
                _ => false
            };
        }

        public bool CanDeleteUsers(SystemRole systemRole)
        {
            return systemRole == SystemRole.Admin;
        }

        public bool CanAssignRoles(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin, // Club admins can assign club roles
                _ => false
            };
        }

        // Club Management Permissions
        public bool CanCreateClubs(SystemRole systemRole)
        {
            return systemRole == SystemRole.Admin;
        }

        public bool CanEditClubs(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnClub = false)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                _ => false
            };
        }

        public bool CanDeleteClubs(SystemRole systemRole)
        {
            return systemRole == SystemRole.Admin;
        }

        // Event Management Permissions
        public bool CanCreateEvents(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                _ => false
            };
        }

        public bool CanJoinEvents(SystemRole systemRole)
        {
            return true;
        }

        public bool CanEditEvents(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnEvent = false)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman) || isOwnEvent,
                _ => false
            };
        }

        public bool CanDeleteEvents(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnEvent = false)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman) || isOwnEvent,
                _ => false
            };
        }

        public bool CanRegisterForEvents(SystemRole systemRole)
        {
            return true; // All roles can register for events
        }

        // Reporting Permissions
        public bool CanGenerateReports(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                _ => false
            };
        }

        public bool CanExportReports(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                _ => false
            };
        }

        public bool CanViewStatistics(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                _ => false
            };
        }

        // System Settings Permissions
        public bool CanAccessGlobalSettings(SystemRole systemRole)
        {
            return systemRole == SystemRole.Admin;
        }

        public bool CanAccessClubSettings(SystemRole systemRole, ClubRole? clubRole = null, bool isOwnClub = false)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                _ => false
            };
        }

        // Legacy methods for backward compatibility

        public bool CanAccessFeature(SystemRole systemRole, string feature, ClubRole? clubRole = null)
        {
            return feature switch
            {
                "CreateUsers" => CanCreateUsers(systemRole, clubRole),
                "EditUsers" => CanEditUsers(systemRole, clubRole),
                "DeleteUsers" => CanDeleteUsers(systemRole),
                "AssignRoles" => CanAssignRoles(systemRole, clubRole),
                "CreateClubs" => CanCreateClubs(systemRole),
                "EditClubs" => CanEditClubs(systemRole, clubRole),
                "DeleteClubs" => CanDeleteClubs(systemRole),
                "CreateEvents" => CanCreateEvents(systemRole, clubRole),
                "EditEvents" => CanEditEvents(systemRole, clubRole),
                "DeleteEvents" => CanDeleteEvents(systemRole, clubRole),
                "RegisterForEvents" => CanRegisterForEvents(systemRole),
                "GenerateReports" => CanGenerateReports(systemRole, clubRole),
                "ExportReports" => CanExportReports(systemRole, clubRole),
                "GlobalSettings" => CanAccessGlobalSettings(systemRole),
                "ClubSettings" => CanAccessClubSettings(systemRole, clubRole),
                "UserManagement" => CanCreateUsers(systemRole, clubRole) || CanEditUsers(systemRole, clubRole) || CanDeleteUsers(systemRole),
                "MemberManagement" => CanCreateUsers(systemRole, clubRole),
                "ClubManagement" => CanCreateClubs(systemRole) || CanEditClubs(systemRole, clubRole) || CanDeleteClubs(systemRole),
                "EventManagement" => CanJoinEvents(systemRole) || CanCreateEvents(systemRole, clubRole) || CanEditEvents(systemRole, clubRole) || CanDeleteEvents(systemRole, clubRole),
                "ReportView" => CanGenerateReports(systemRole, clubRole),
                "Dashboard" => true, // Everyone can access dashboard
                _ => false
            };
        }

        public bool CanManageClub(SystemRole systemRole, ClubRole? clubRole, bool isOwnClub)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                SystemRole.Member => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                _ => false
            };
        }

        public bool CanManageUser(SystemRole systemRole, SystemRole targetSystemRole, ClubRole? clubRole, ClubRole? targetClubRole)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => targetSystemRole != SystemRole.Admin && 
                                      (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                SystemRole.Member => targetSystemRole == SystemRole.Member && 
                                   (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman) &&
                                   targetClubRole == ClubRole.Member,
                _ => false
            };
        }

        public bool CanManageEvent(SystemRole systemRole, ClubRole? clubRole, bool isOwnClub)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                SystemRole.Member => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                _ => false
            };
        }

        public bool CanViewReports(SystemRole systemRole, ClubRole? clubRole, bool isOwnClub = true)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                SystemRole.Member => isOwnClub && (clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman),
                _ => false
            };
        }



        public async Task<bool> IsAuthorizedAsync(int userId, string action, object? resource = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // For now, use basic system role authorization
            // TODO: Implement club-specific role checking based on resource context
            return action switch
            {
                "ViewDashboard" => CanAccessFeature(user.SystemRole, "Dashboard"),
                "ManageUsers" => CanAccessFeature(user.SystemRole, "UserManagement"),
                "ManageClubs" => CanAccessFeature(user.SystemRole, "ClubManagement"),
                "ManageEvents" => CanAccessFeature(user.SystemRole, "EventManagement"),
                "ViewReports" => CanAccessFeature(user.SystemRole, "ReportView"),
                "GenerateReports" => CanGenerateReports(user.SystemRole),
                _ => false
            };
        }

        public bool CanViewEvent(SystemRole systemRole)
        {
            return true; // All users can view events
        }

        public bool CanViewClub(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => true,
                SystemRole.Member => clubRole != null, // Members can view clubs they belong to
                _ => false
            };
        }

        public bool CanViewUser(SystemRole systemRole, ClubRole? clubRole = null)
        {
            return systemRole switch
            {
                SystemRole.Admin => true,
                SystemRole.ClubOwner => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                SystemRole.Member => clubRole == ClubRole.Admin || clubRole == ClubRole.Chairman,
                _ => false
            };
        }
    }
}