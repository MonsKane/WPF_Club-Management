using ClubManagementApp.Models;
using ClubManagementApp.DTOs;
using ClubManagementApp.Services;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ClubManagementApp.Helpers
{
    /// <summary>
    /// Centralized validation helper for service layer operations
    /// </summary>
    public static class ServiceValidationHelper
    {
        public static class Security
        {
            public const int MaxFailedLoginAttempts = 5;
            public const int MaxUniqueIpAddresses = 3;
            public const int DefaultLockoutDurationMinutes = 30;
            public const int TwoFactorCodeExpirationMinutes = 5;
            
            public static bool IsValidSecurityConfiguration(SecurityConfiguration config)
            {
                return config != null &&
                       config.MaxFailedLoginAttempts > 0 &&
                       config.AccountLockoutDurationMinutes > 0 &&
                       config.MinPasswordLength >= 6;
            }
        }

        public static class Notification
        {
            public const int MaxNotificationTitleLength = 200;
            public const int MaxNotificationMessageLength = 2000;
            public const int NotificationRetentionDays = 30;
            
            public static bool IsValidNotificationRequest(CreateNotificationRequest request)
            {
                return request != null &&
                       !string.IsNullOrWhiteSpace(request.Title) &&
                       request.Title.Length <= MaxNotificationTitleLength &&
                       !string.IsNullOrWhiteSpace(request.Message) &&
                       request.Message.Length <= MaxNotificationMessageLength &&
                       request.Channels?.Count > 0;
            }
            
            public static bool IsValidTemplateRequest(CreateTemplateRequest request)
            {
                return request != null &&
                       !string.IsNullOrWhiteSpace(request.Name) &&
                       !string.IsNullOrWhiteSpace(request.TitleTemplate) &&
                       !string.IsNullOrWhiteSpace(request.MessageTemplate);
            }
        }

        public static class Event
        {
            public static bool IsValidEventCreation(CreateEventDto eventDto)
            {
                return eventDto != null &&
                       !string.IsNullOrWhiteSpace(eventDto.Name) &&
                       eventDto.Name.Length <= 100 &&
                       eventDto.EventDate > DateTime.Now &&
                       !string.IsNullOrWhiteSpace(eventDto.Location) &&
                       eventDto.Location.Length <= 200;
            }
            
            public static bool IsEventUpcoming(DateTime eventDate)
            {
                return eventDate > DateTime.Now;
            }
        }

        public static class UserValidation
        {
            public static bool IsValidUserCreation(CreateUserDto userDto)
            {
                return userDto != null &&
                       ValidationHelper.UserValidation.IsValidEmail(userDto.Email) &&
                       ValidationHelper.UserValidation.IsValidFullName(userDto.FullName) &&
                       ValidationHelper.UserValidation.IsValidStudentID(userDto.StudentID) &&
                       ValidationHelper.UserValidation.IsValidPassword(userDto.Password);
            }
            
            public static bool CanUserPerformAction(Models.User user, string action)
            {
                return user != null && user.IsActive && !string.IsNullOrWhiteSpace(action);
            }
        }

        public static class DataImport
        {
            public const int MaxBatchSize = 1000;
            public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
            
            public static bool IsValidImportFile(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return false;
                    
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length <= MaxFileSizeBytes;
            }
            
            public static bool IsValidExportOptions(ExportOptions options)
            {
                return options == null || // null options are valid (use defaults)
                       (options.FromDate == null || options.ToDate == null || 
                        options.FromDate <= options.ToDate);
            }
        }

        public static class Business
        {
            public static bool CanTransferMembership(Models.User user, Models.Club targetClub)
            {
                return user != null && user.IsActive &&
                       targetClub != null &&
                       user.ClubID != targetClub.ClubID;
            }
            
            public static bool CanPromoteUser(Models.User currentUser, Models.User targetUser, SystemRole newRole)
            {
                return currentUser != null && targetUser != null &&
                       currentUser.IsActive && targetUser.IsActive &&
                       ValidationHelper.UserValidation.CanAssignRole(currentUser.SystemRole, newRole);
            }
            
            public static bool CanDeactivateUser(Models.User currentUser, Models.User targetUser)
            {
                return currentUser != null && targetUser != null &&
                       targetUser.IsActive &&
                       ValidationHelper.BusinessRules.CanDeleteUser(targetUser, currentUser);
            }
        }

        public static class General
        {
            public static bool IsValidId(int? id)
            {
                return id.HasValue && id.Value > 0;
            }
            
            public static bool IsValidDateRange(DateTime? fromDate, DateTime? toDate)
            {
                if (!fromDate.HasValue && !toDate.HasValue)
                    return true;
                    
                if (fromDate.HasValue && toDate.HasValue)
                    return fromDate.Value <= toDate.Value;
                    
                return true; // One date is null, which is valid
            }
            
            public static bool IsValidPaginationParameters(int page, int pageSize)
            {
                return page > 0 && pageSize > 0 && pageSize <= 100;
            }
        }
        
        public static bool IsValidEmail(string email)
        {
            return ValidationHelper.UserValidation.IsValidEmail(email);
        }
    }
}