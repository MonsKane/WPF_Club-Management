using ClubManagementApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ClubManagementApp.Helpers
{
    public static class ValidationHelper
    {
        public static class UserValidation
        {
            public static bool IsValidEmail(string email)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                try
                {
                    var emailAttribute = new EmailAddressAttribute();
                    return emailAttribute.IsValid(email);
                }
                catch
                {
                    return false;
                }
            }

            public static bool IsValidStudentID(string studentId)
            {
                if (string.IsNullOrWhiteSpace(studentId))
                    return false;

                // Assuming student ID format: 8-10 digits
                return Regex.IsMatch(studentId, @"^\d{8,10}$");
            }

            public static bool IsValidPassword(string password)
            {
                if (string.IsNullOrWhiteSpace(password))
                    return false;

                // Password must be at least 8 characters, contain uppercase, lowercase, digit, and special character
                return password.Length >= 8 &&
                       Regex.IsMatch(password, @"[A-Z]") &&
                       Regex.IsMatch(password, @"[a-z]") &&
                       Regex.IsMatch(password, @"\d") &&
                       Regex.IsMatch(password, @"[^\w\s]");
            }

            public static bool IsValidFullName(string fullName)
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return false;

                // Name should be 2-100 characters, letters, spaces, hyphens, and apostrophes only
                return fullName.Length >= 2 && fullName.Length <= 100 &&
                       Regex.IsMatch(fullName, @"^[a-zA-Z\s\-']+$");
            }

            public static bool CanAssignRole(UserRole currentUserRole, UserRole targetRole)
            {
                return currentUserRole switch
                {
                    UserRole.SystemAdmin => true,
                    UserRole.Admin => targetRole != UserRole.SystemAdmin,
                    UserRole.ClubPresident => targetRole != UserRole.SystemAdmin && targetRole != UserRole.Admin,
                    UserRole.Chairman => targetRole != UserRole.SystemAdmin && targetRole != UserRole.Admin && targetRole != UserRole.ClubPresident,
                    UserRole.ViceChairman => targetRole == UserRole.Member || targetRole == UserRole.TeamLeader || targetRole == UserRole.ClubOfficer,
                    UserRole.ClubOfficer => targetRole == UserRole.Member || targetRole == UserRole.TeamLeader,
                    UserRole.TeamLeader => targetRole == UserRole.Member,
                    _ => false
                };
            }
        }

        public static class ClubValidation
        {
            public static bool IsValidClubName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                // Club name should be 3-100 characters
                return name.Length >= 3 && name.Length <= 100;
            }

            public static bool IsValidDescription(string description)
            {
                if (string.IsNullOrWhiteSpace(description))
                    return false;

                // Description should be 10-1000 characters
                return description.Length >= 10 && description.Length <= 1000;
            }

            public static bool IsValidEstablishedDate(DateTime establishedDate)
            {
                // Established date should not be in the future and not before 1900
                return establishedDate <= DateTime.Now && establishedDate >= new DateTime(1900, 1, 1);
            }
        }

        public static class EventValidation
        {
            public static bool IsValidEventName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                // Event name should be 3-200 characters
                return name.Length >= 3 && name.Length <= 200;
            }

            public static bool IsValidEventDate(DateTime eventDate)
            {
                // Event date should be at least 1 hour in the future for new events
                return eventDate >= DateTime.Now.AddHours(1);
            }

            public static bool IsValidLocation(string location)
            {
                if (string.IsNullOrWhiteSpace(location))
                    return false;

                // Location should be 3-200 characters
                return location.Length >= 3 && location.Length <= 200;
            }

            public static bool CanRegisterForEvent(DateTime eventDate, DateTime registrationDate)
            {
                // Can register up to 1 hour before event
                return registrationDate <= eventDate.AddHours(-1);
            }

            public static bool CanMarkAttendance(DateTime eventDate, DateTime attendanceDate)
            {
                // Can mark attendance from 1 hour before to 24 hours after event
                return attendanceDate >= eventDate.AddHours(-1) && 
                       attendanceDate <= eventDate.AddHours(24);
            }
        }

        public static class ReportValidation
        {
            public static bool IsValidReportTitle(string title)
            {
                if (string.IsNullOrWhiteSpace(title))
                    return false;

                // Report title should be 5-200 characters
                return title.Length >= 5 && title.Length <= 200;
            }

            public static bool IsValidSemester(string semester)
            {
                if (string.IsNullOrWhiteSpace(semester))
                    return false;

                // Semester format: "Fall 2024", "Spring 2024", "Summer 2024"
                return Regex.IsMatch(semester, @"^(Spring|Summer|Fall)\s\d{4}$");
            }

            public static bool IsValidReportContent(string content)
            {
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                // Content should not be empty and not exceed 50KB
                return content.Length > 0 && content.Length <= 50000;
            }
        }

        public static class BusinessRules
        {
            public static bool CanDeleteUser(User user, User currentUser)
            {
                // Cannot delete yourself
                if (user.UserID == currentUser.UserID)
                    return false;

                // SystemAdmin can delete anyone except other SystemAdmins
                if (currentUser.Role == UserRole.SystemAdmin)
                    return user.Role != UserRole.SystemAdmin;

                // Admin can delete anyone except SystemAdmin and other Admins
                if (currentUser.Role == UserRole.Admin)
                    return user.Role != UserRole.SystemAdmin && user.Role != UserRole.Admin;

                // ClubPresident can delete members, team leaders, vice chairmen, and club officers
                if (currentUser.Role == UserRole.ClubPresident)
                    return user.Role != UserRole.SystemAdmin && user.Role != UserRole.Admin && user.Role != UserRole.ClubPresident;

                // Chairman can delete members, team leaders, vice chairmen, and club officers
                if (currentUser.Role == UserRole.Chairman)
                    return user.Role != UserRole.SystemAdmin && user.Role != UserRole.Admin && user.Role != UserRole.ClubPresident && user.Role != UserRole.Chairman;

                return false;
            }

            public static bool CanDeleteEvent(Event eventItem, User currentUser)
            {
                // Cannot delete past events
                if (eventItem.EventDate <= DateTime.Now)
                    return false;

                // SystemAdmin and Admin can delete any event
                if (currentUser.Role == UserRole.SystemAdmin || currentUser.Role == UserRole.Admin)
                    return true;

                // ClubPresident and Chairman can delete any event
                if (currentUser.Role == UserRole.ClubPresident || currentUser.Role == UserRole.Chairman)
                    return true;

                // Vice Chairman, Club Officer, and Team Leader can delete events from their club
                if ((currentUser.Role == UserRole.ViceChairman || currentUser.Role == UserRole.ClubOfficer || currentUser.Role == UserRole.TeamLeader) &&
                    currentUser.ClubID == eventItem.ClubID)
                    return true;

                return false;
            }

            public static bool CanGenerateReport(ReportType reportType, User currentUser)
            {
                return currentUser.Role switch
                {
                    UserRole.SystemAdmin => true,
                    UserRole.Admin => true,
                    UserRole.ClubPresident => true,
                    UserRole.Chairman => true,
                    UserRole.ViceChairman => reportType != ReportType.SemesterSummary,
                    UserRole.ClubOfficer => reportType == ReportType.EventOutcomes || reportType == ReportType.ActivityTracking,
                    UserRole.TeamLeader => reportType == ReportType.EventOutcomes || reportType == ReportType.ActivityTracking,
                    _ => false
                };
            }
        }
    }
}