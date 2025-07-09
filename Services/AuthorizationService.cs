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
                UserRole.Admin => true, // Admin has access to everything
                UserRole.Chairman => feature != "SystemConfig" && feature != "UserManagement",
                UserRole.ViceChairman => feature != "SystemConfig" && feature != "UserManagement" && feature != "ApproveMember",
                UserRole.TeamLeader => feature is "EventManagement" or "MemberView" or "ReportView",
                UserRole.Member => feature is "EventView" or "ProfileEdit",
                _ => false
            };
        }

        public bool CanManageClub(UserRole role, int? userClubId, int targetClubId)
        {
            return role switch
            {
                UserRole.Admin => true,
                UserRole.Chairman => userClubId == targetClubId,
                UserRole.ViceChairman => userClubId == targetClubId,
                _ => false
            };
        }

        public bool CanManageUser(UserRole role, UserRole targetUserRole, int? userClubId, int? targetUserClubId)
        {
            return role switch
            {
                UserRole.Admin => true,
                UserRole.Chairman => userClubId == targetUserClubId && targetUserRole != UserRole.Admin,
                UserRole.ViceChairman => userClubId == targetUserClubId && 
                                       targetUserRole != UserRole.Admin && 
                                       targetUserRole != UserRole.Chairman,
                UserRole.TeamLeader => userClubId == targetUserClubId && 
                                     targetUserRole == UserRole.Member,
                _ => false
            };
        }

        public bool CanManageEvent(UserRole role, int? userClubId, int eventClubId)
        {
            return role switch
            {
                UserRole.Admin => true,
                UserRole.Chairman => userClubId == eventClubId,
                UserRole.ViceChairman => userClubId == eventClubId,
                UserRole.TeamLeader => userClubId == eventClubId,
                _ => false
            };
        }

        public bool CanViewReports(UserRole role, int? userClubId, int? reportClubId)
        {
            return role switch
            {
                UserRole.Admin => true,
                UserRole.Chairman => reportClubId == null || userClubId == reportClubId,
                UserRole.ViceChairman => reportClubId == null || userClubId == reportClubId,
                UserRole.TeamLeader => userClubId == reportClubId,
                _ => false
            };
        }

        public bool CanGenerateReports(UserRole role)
        {
            return role is UserRole.Admin or UserRole.Chairman or UserRole.ViceChairman;
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