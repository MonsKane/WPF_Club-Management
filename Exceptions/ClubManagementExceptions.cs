using System;

namespace ClubManagementApp.Exceptions
{
    // Base exception for all club management related exceptions
    public abstract class ClubManagementException : Exception
    {
        protected ClubManagementException(string message) : base(message) { }
        protected ClubManagementException(string message, Exception innerException) : base(message, innerException) { }
    }

    // User-related exceptions
    public class UserNotFoundException : ClubManagementException
    {
        public int UserId { get; }
        
        public UserNotFoundException(int userId) 
            : base($"User with ID {userId} was not found.")
        {
            UserId = userId;
        }
    }

    public class UserAlreadyExistsException : ClubManagementException
    {
        public string Email { get; }
        
        public UserAlreadyExistsException(string email) 
            : base($"User with email {email} already exists.")
        {
            Email = email;
        }
    }

    public class InvalidUserCredentialsException : ClubManagementException
    {
        public InvalidUserCredentialsException() 
            : base("Invalid email or password provided.") { }
    }

    public class InsufficientPermissionsException : ClubManagementException
    {
        public string RequiredRole { get; }
        public string Action { get; }
        
        public InsufficientPermissionsException(string action, string requiredRole) 
            : base($"Insufficient permissions to perform action '{action}'. Required role: {requiredRole}")
        {
            Action = action;
            RequiredRole = requiredRole;
        }
    }

    // Club-related exceptions
    public class ClubNotFoundException : ClubManagementException
    {
        public int ClubId { get; }
        
        public ClubNotFoundException(int clubId) 
            : base($"Club with ID {clubId} was not found.")
        {
            ClubId = clubId;
        }
    }

    public class ClubAlreadyExistsException : ClubManagementException
    {
        public string ClubName { get; }
        
        public ClubAlreadyExistsException(string clubName) 
            : base($"Club with name '{clubName}' already exists.")
        {
            ClubName = clubName;
        }
    }

    public class ClubInactiveException : ClubManagementException
    {
        public int ClubId { get; }
        
        public ClubInactiveException(int clubId) 
            : base($"Club with ID {clubId} is inactive and cannot be accessed.")
        {
            ClubId = clubId;
        }
    }

    // Event-related exceptions
    public class EventNotFoundException : ClubManagementException
    {
        public int EventId { get; }
        
        public EventNotFoundException(int eventId) 
            : base($"Event with ID {eventId} was not found.")
        {
            EventId = eventId;
        }
    }

    public class EventRegistrationClosedException : ClubManagementException
    {
        public int EventId { get; }
        public DateTime EventDate { get; }
        
        public EventRegistrationClosedException(int eventId, DateTime eventDate) 
            : base($"Registration for event {eventId} is closed. Event date: {eventDate}")
        {
            EventId = eventId;
            EventDate = eventDate;
        }
    }

    public class UserAlreadyRegisteredException : ClubManagementException
    {
        public int UserId { get; }
        public int EventId { get; }
        
        public UserAlreadyRegisteredException(int userId, int eventId) 
            : base($"User {userId} is already registered for event {eventId}.")
        {
            UserId = userId;
            EventId = eventId;
        }
    }

    public class EventCapacityExceededException : ClubManagementException
    {
        public int EventId { get; }
        public int MaxCapacity { get; }
        
        public EventCapacityExceededException(int eventId, int maxCapacity) 
            : base($"Event {eventId} has reached maximum capacity of {maxCapacity} participants.")
        {
            EventId = eventId;
            MaxCapacity = maxCapacity;
        }
    }

    // Report-related exceptions
    public class ReportNotFoundException : ClubManagementException
    {
        public int ReportId { get; }
        
        public ReportNotFoundException(int reportId) 
            : base($"Report with ID {reportId} was not found.")
        {
            ReportId = reportId;
        }
    }

    public class ReportGenerationException : ClubManagementException
    {
        public string ReportType { get; }
        
        public ReportGenerationException(string reportType, Exception innerException) 
            : base($"Failed to generate {reportType} report.", innerException)
        {
            ReportType = reportType;
        }
    }

    public class InvalidReportParametersException : ClubManagementException
    {
        public string Parameter { get; }
        
        public InvalidReportParametersException(string parameter, string message) 
            : base($"Invalid report parameter '{parameter}': {message}")
        {
            Parameter = parameter;
        }
    }

    // Data validation exceptions
    public class ValidationException : ClubManagementException
    {
        public string Field { get; }
        public string ValidationRule { get; }
        
        public ValidationException(string field, string validationRule) 
            : base($"Validation failed for field '{field}': {validationRule}")
        {
            Field = field;
            ValidationRule = validationRule;
        }
    }

    public class BusinessRuleViolationException : ClubManagementException
    {
        public string Rule { get; }
        
        public BusinessRuleViolationException(string rule, string message) 
            : base($"Business rule violation '{rule}': {message}")
        {
            Rule = rule;
        }
    }

    // Database-related exceptions
    public class DatabaseConnectionException : ClubManagementException
    {
        public DatabaseConnectionException(Exception innerException) 
            : base("Failed to connect to the database.", innerException) { }
        
        public DatabaseConnectionException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    public class DataIntegrityException : ClubManagementException
    {
        public string Operation { get; }
        
        public DataIntegrityException(string operation, Exception innerException) 
            : base($"Data integrity violation during operation: {operation}", innerException)
        {
            Operation = operation;
        }
    }

    // Notification-related exceptions
    public class NotificationDeliveryException : ClubManagementException
    {
        public string NotificationType { get; }
        public string Recipient { get; }
        
        public NotificationDeliveryException(string notificationType, string recipient, Exception innerException) 
            : base($"Failed to deliver {notificationType} notification to {recipient}.", innerException)
        {
            NotificationType = notificationType;
            Recipient = recipient;
        }
    }

    // Configuration-related exceptions
    public class ConfigurationException : ClubManagementException
    {
        public string ConfigurationKey { get; }
        
        public ConfigurationException(string configurationKey, string message) 
            : base($"Configuration error for key '{configurationKey}': {message}")
        {
            ConfigurationKey = configurationKey;
        }
        
        public ConfigurationException(string message, Exception innerException) 
            : base(message, innerException)
        {
            ConfigurationKey = string.Empty;
        }
    }
}