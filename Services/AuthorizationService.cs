using ClubManagementApp.Models;
using ClubManagementApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementApp.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ClubManagementDbContext _context;

        public AuthorizationService(ClubManagementDbContext context)
        {
            _context = context;
        }

        public bool CanAccessFeature(UserRole role, string feature)
        {
            return role switch
            {
                UserRole.SystemAdmin => true, // SystemAdmin has access to everything
                UserRole.Admin => true, // Admin has access to everything
                UserRole.ClubPresident => feature != "SystemConfig" && feature != "UserManagement",
                UserRole.Chairman => feature != "SystemConfig" && feature != "UserManagement",
                UserRole.ViceChairman => feature != "SystemConfig" && feature != "UserManagement" && feature != "ApproveMember",
                UserRole.ClubOfficer => feature is "EventManagement" or "MemberView" or "ReportView" or "ClubManagement",
                UserRole.TeamLeader => feature is "EventManagement" or "MemberView" or "ReportView",
                UserRole.Member => feature is "EventView" or "ProfileEdit",
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

        public bool CanGenerateReports(UserRole role)
        {
            return role is UserRole.SystemAdmin or UserRole.Admin or UserRole.ClubPresident or UserRole.Chairman or UserRole.ViceChairman;
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
    }
}