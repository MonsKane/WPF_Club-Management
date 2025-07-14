# Club Management Application - API/Service Documentation

## Overview

This document provides comprehensive documentation for all service interfaces and their implementations in the Club Management Application. The application follows a service-oriented architecture with clear separation between business logic and data access layers.

## Service Architecture

The application implements a three-layer architecture:
- **Presentation Layer**: WPF Views and ViewModels
- **Business Logic Layer**: Service interfaces and implementations
- **Data Access Layer**: Entity Framework DbContext and repositories

## Core Services

### IUserService

**Namespace**: `ClubManagementApp.Services`  
**Implementation**: `UserService`  
**Purpose**: Manages all user-related operations including authentication, profile management, and role administration.

#### Methods

##### User Retrieval

```csharp
Task<IEnumerable<User>> GetAllUsersAsync()
```
**Description**: Retrieves all users in the system  
**Returns**: Collection of all User entities  
**Authorization**: Admin, Chairman  
**Usage Example**: Loading user management grid

```csharp
Task<User?> GetUserByIdAsync(int userId)
```
**Description**: Retrieves a specific user by their ID  
**Parameters**: 
- `userId`: Unique identifier for the user
**Returns**: User entity or null if not found  
**Authorization**: Any authenticated user (limited to own profile unless admin/chairman)

```csharp
Task<User?> GetUserByEmailAsync(string email)
```
**Description**: Retrieves a user by their email address  
**Parameters**: 
- `email`: User's email address
**Returns**: User entity or null if not found  
**Authorization**: System internal use, authentication

```csharp
Task<IEnumerable<User>> GetUsersByClubAsync(int clubId)
```
**Description**: Retrieves all users belonging to a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of users in the specified club  
**Authorization**: Club leadership, Admin

```csharp
Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
```
**Description**: Searches users by name, email, or student ID  
**Parameters**: 
- `searchTerm`: Search criteria
**Returns**: Collection of matching users  
**Authorization**: Admin, Chairman

##### User Management

```csharp
Task<User> CreateUserAsync(User user)
```
**Description**: Creates a new user account  
**Parameters**: 
- `user`: User entity with required information
**Returns**: Created user with assigned ID  
**Authorization**: Admin, Chairman  
**Validation**: Email uniqueness, required fields, password strength

```csharp
Task<User> UpdateUserAsync(User user)
```
**Description**: Updates an existing user's information  
**Parameters**: 
- `user`: User entity with updated information
**Returns**: Updated user entity  
**Authorization**: Admin, Chairman, or user updating own profile  
**Validation**: Email uniqueness, data integrity

```csharp
Task<bool> DeleteUserAsync(int userId)
```
**Description**: Soft deletes a user account  
**Parameters**: 
- `userId`: User identifier
**Returns**: Success status  
**Authorization**: Admin only  
**Note**: Performs soft delete by setting IsActive = false

##### Authentication & Authorization

```csharp
Task<bool> ValidateUserCredentialsAsync(string email, string password)
```
**Description**: Validates user login credentials  
**Parameters**: 
- `email`: User's email address
- `password`: Plain text password
**Returns**: Validation success status  
**Security**: Uses BCrypt for password verification

```csharp
Task<User?> GetCurrentUserAsync()
```
**Description**: Retrieves the currently logged-in user  
**Returns**: Current user entity or null  
**Usage**: Session management, permission checking

```csharp
void SetCurrentUser(User? user)
```
**Description**: Sets the current user session  
**Parameters**: 
- `user`: User to set as current (null for logout)
**Usage**: Login/logout operations

##### Role & Activity Management

```csharp
Task UpdateActivityLevelAsync(int userId, ActivityLevel activityLevel)
```
**Description**: Updates a user's activity level  
**Parameters**: 
- `userId`: User identifier
- `activityLevel`: New activity level (Active, Normal, Inactive)
**Authorization**: Admin, Chairman

```csharp
Task<bool> UpdateUserRoleAsync(int userId, SystemRole newRole)
```
**Description**: Updates a user's role  
**Parameters**: 
- `userId`: User identifier
- `newRole`: New role assignment
**Returns**: Success status  
**Authorization**: Admin, Chairman (limited role changes)

```csharp
Task<bool> AssignUserToClubAsync(int userId, int clubId)
```
**Description**: Assigns a user to a specific club  
**Parameters**: 
- `userId`: User identifier
- `clubId`: Club identifier
**Returns**: Success status  
**Authorization**: Admin, Club Chairman

```csharp
Task<bool> RemoveUserFromClubAsync(int userId)
```
**Description**: Removes a user from their current club  
**Parameters**: 
- `userId`: User identifier
**Returns**: Success status  
**Authorization**: Admin, Club Chairman

##### Statistics & Reporting

```csharp
Task<Dictionary<ActivityLevel, int>> GetActivityStatisticsAsync(int? clubId = null)
```
**Description**: Retrieves activity level distribution statistics  
**Parameters**: 
- `clubId`: Optional club filter
**Returns**: Dictionary mapping activity levels to user counts  
**Authorization**: Admin, Chairman

```csharp
Task<IEnumerable<User>> GetMembersByRoleAsync(SystemRole role, int? clubId = null)
```
**Description**: Retrieves users by their role  
**Parameters**: 
- `role`: Target role
- `clubId`: Optional club filter
**Returns**: Collection of users with specified role  
**Authorization**: Admin, Chairman

```csharp
Task<Dictionary<string, object>> GetMemberParticipationHistoryAsync(int userId)
```
**Description**: Retrieves detailed participation history for a user  
**Parameters**: 
- `userId`: User identifier
**Returns**: Dictionary containing participation metrics and history  
**Authorization**: Admin, Chairman, or user viewing own history

```csharp
Task<IEnumerable<User>> GetClubLeadershipAsync(int clubId)
```
**Description**: Retrieves all leadership positions for a club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of users in leadership roles  
**Authorization**: Any authenticated user

---

### IClubService

**Namespace**: `ClubManagementApp.Services`  
**Implementation**: `ClubService`  
**Purpose**: Manages club operations, membership, and leadership assignments.

#### Methods

##### Club Management

```csharp
Task<IEnumerable<Club>> GetAllClubsAsync()
```
**Description**: Retrieves all active clubs  
**Returns**: Collection of all Club entities  
**Authorization**: Any authenticated user

```csharp
Task<Club?> GetClubByIdAsync(int clubId)
```
**Description**: Retrieves a specific club by ID  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Club entity or null if not found  
**Authorization**: Any authenticated user

```csharp
Task<Club?> GetClubByNameAsync(string name)
```
**Description**: Retrieves a club by its name  
**Parameters**: 
- `name`: Club name
**Returns**: Club entity or null if not found  
**Authorization**: Any authenticated user

```csharp
Task<Club> CreateClubAsync(Club club)
```
**Description**: Creates a new club  
**Parameters**: 
- `club`: Club entity with required information
**Returns**: Created club with assigned ID  
**Authorization**: Admin only  
**Validation**: Name uniqueness, required fields

```csharp
Task<Club> UpdateClubAsync(Club club)
```
**Description**: Updates an existing club's information  
**Parameters**: 
- `club`: Club entity with updated information
**Returns**: Updated club entity  
**Authorization**: Admin, Club Chairman  
**Validation**: Name uniqueness, data integrity

```csharp
Task<bool> DeleteClubAsync(int clubId)
```
**Description**: Soft deletes a club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Success status  
**Authorization**: Admin only  
**Note**: Performs soft delete by setting IsActive = false

##### Membership Management

```csharp
Task<int> GetMemberCountAsync(int clubId)
```
**Description**: Retrieves the total number of active members in a club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Member count  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<User>> GetClubMembersAsync(int clubId)
```
**Description**: Retrieves all members of a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of club members  
**Authorization**: Club members, Admin

##### Leadership Management

```csharp
Task<bool> AssignClubLeadershipAsync(int clubId, int userId, SystemRole role)
```
**Description**: Assigns a leadership role to a user within a club  
**Parameters**: 
- `clubId`: Club identifier
- `userId`: User identifier
- `role`: Leadership role (Chairman, ViceChairman, TeamLeader)
**Returns**: Success status  
**Authorization**: Admin, Club Chairman  
**Validation**: User must be club member, role hierarchy validation

```csharp
Task<User?> GetClubChairmanAsync(int clubId)
```
**Description**: Retrieves the chairman of a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Chairman user entity or null  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<User>> GetClubViceChairmenAsync(int clubId)
```
**Description**: Retrieves all vice chairmen of a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of vice chairman users  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<User>> GetClubTeamLeadersAsync(int clubId)
```
**Description**: Retrieves all team leaders of a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of team leader users  
**Authorization**: Any authenticated user

```csharp
Task<bool> RemoveClubLeadershipAsync(int clubId, int userId)
```
**Description**: Removes leadership role from a user  
**Parameters**: 
- `clubId`: Club identifier
- `userId`: User identifier
**Returns**: Success status  
**Authorization**: Admin, Club Chairman

##### Statistics & Analytics

```csharp
Task<Dictionary<SystemRole, int>> GetClubRoleDistributionAsync(int clubId)
```
**Description**: Retrieves role distribution statistics for a club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Dictionary mapping roles to user counts  
**Authorization**: Club leadership, Admin

```csharp
Task<Dictionary<string, object>> GetClubStatisticsAsync(int clubId)
```
**Description**: Retrieves comprehensive statistics for a club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Dictionary containing various club metrics  
**Authorization**: Club leadership, Admin  
**Metrics Include**: Member count, event count, activity levels, growth trends

---

### IEventService

**Namespace**: `ClubManagementApp.Services`  
**Implementation**: `EventService`  
**Purpose**: Manages event lifecycle, participant registration, and attendance tracking.

#### Methods

##### Event Retrieval

```csharp
Task<IEnumerable<Event>> GetAllEventsAsync()
```
**Description**: Retrieves all events in the system  
**Returns**: Collection of all Event entities  
**Authorization**: Any authenticated user

```csharp
Task<Event?> GetEventByIdAsync(int eventId)
```
**Description**: Retrieves a specific event by ID  
**Parameters**: 
- `eventId`: Event identifier
**Returns**: Event entity or null if not found  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<Event>> GetEventsByClubAsync(int clubId)
```
**Description**: Retrieves all events for a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of club events  
**Authorization**: Club members, Admin

```csharp
Task<IEnumerable<Event>> GetUpcomingEventsAsync(int? clubId = null)
```
**Description**: Retrieves upcoming events  
**Parameters**: 
- `clubId`: Optional club filter
**Returns**: Collection of future events  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<Event>> GetPastEventsAsync(int? clubId = null)
```
**Description**: Retrieves past events  
**Parameters**: 
- `clubId`: Optional club filter
**Returns**: Collection of completed events  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<Event>> GetUpcomingEventsWithinDaysAsync(int? clubId = null, int days = 30)
```
**Description**: Retrieves upcoming events within specified timeframe  
**Parameters**: 
- `clubId`: Optional club filter
- `days`: Number of days to look ahead
**Returns**: Collection of upcoming events  
**Authorization**: Any authenticated user

```csharp
Task<IEnumerable<Event>> GetUserEventsAsync(int userId, bool includeHistory = false)
```
**Description**: Retrieves events for a specific user  
**Parameters**: 
- `userId`: User identifier
- `includeHistory`: Include past events
**Returns**: Collection of user's events  
**Authorization**: User viewing own events, Admin, Club leadership

##### Event Management

```csharp
Task<Event> CreateEventAsync(Event eventItem)
```
**Description**: Creates a new event  
**Parameters**: 
- `eventItem`: Event entity with required information
**Returns**: Created event with assigned ID  
**Authorization**: Club leadership, Admin  
**Validation**: Date validation, location availability, capacity limits

```csharp
Task<Event> UpdateEventAsync(Event eventItem)
```
**Description**: Updates an existing event  
**Parameters**: 
- `eventItem`: Event entity with updated information
**Returns**: Updated event entity  
**Authorization**: Event creator, Club leadership, Admin  
**Validation**: Date validation, participant impact assessment

```csharp
Task<bool> DeleteEventAsync(int eventId)
```
**Description**: Cancels/deletes an event  
**Parameters**: 
- `eventId`: Event identifier
**Returns**: Success status  
**Authorization**: Event creator, Club Chairman, Admin  
**Note**: Notifies all registered participants

##### Participant Management

```csharp
Task<bool> RegisterUserForEventAsync(int eventId, int userId)
```
**Description**: Registers a user for an event  
**Parameters**: 
- `eventId`: Event identifier
- `userId`: User identifier
**Returns**: Success status  
**Authorization**: User registering self, Admin, Club leadership  
**Validation**: Registration deadline, capacity limits, duplicate registration

```csharp
Task<bool> UnregisterUserFromEventAsync(int eventId, int userId)
```
**Description**: Removes a user's registration from an event  
**Parameters**: 
- `eventId`: Event identifier
- `userId`: User identifier
**Returns**: Success status  
**Authorization**: User unregistering self, Admin, Club leadership

```csharp
Task<bool> UpdateParticipantStatusAsync(int eventId, int userId, AttendanceStatus status)
```
**Description**: Updates a participant's attendance status  
**Parameters**: 
- `eventId`: Event identifier
- `userId`: User identifier
- `status`: New attendance status (Registered, Attended, Absent)
**Returns**: Success status  
**Authorization**: Event organizers, Club leadership, Admin

```csharp
Task<IEnumerable<EventParticipant>> GetEventParticipantsAsync(int eventId)
```
**Description**: Retrieves all participants for an event  
**Parameters**: 
- `eventId`: Event identifier
**Returns**: Collection of event participants  
**Authorization**: Event organizers, Club leadership, Admin

```csharp
Task<IEnumerable<EventParticipant>> GetUserEventHistoryAsync(int userId)
```
**Description**: Retrieves a user's complete event participation history  
**Parameters**: 
- `userId`: User identifier
**Returns**: Collection of user's event participations  
**Authorization**: User viewing own history, Admin, Club leadership

##### Statistics & Analytics

```csharp
Task<Dictionary<AttendanceStatus, int>> GetEventAttendanceStatisticsAsync(int eventId)
```
**Description**: Retrieves attendance statistics for an event  
**Parameters**: 
- `eventId`: Event identifier
**Returns**: Dictionary mapping attendance status to counts  
**Authorization**: Event organizers, Club leadership, Admin

```csharp
Task<Dictionary<string, object>> GetEventStatisticsAsync(int eventId)
```
**Description**: Retrieves comprehensive statistics for an event  
**Parameters**: 
- `eventId`: Event identifier
**Returns**: Dictionary containing various event metrics  
**Authorization**: Event organizers, Club leadership, Admin  
**Metrics Include**: Registration rate, attendance rate, participant demographics

---

### IReportService

**Namespace**: `ClubManagementApp.Services`  
**Implementation**: `ReportService`  
**Purpose**: Generates, manages, and exports various types of reports and analytics.

#### Methods

##### Report Generation

```csharp
Task<Report> GenerateMemberStatisticsReportAsync(int? clubId, DateTime? startDate, DateTime? endDate)
```
**Description**: Generates member statistics report  
**Parameters**: 
- `clubId`: Optional club filter
- `startDate`: Report period start
- `endDate`: Report period end
**Returns**: Generated report entity  
**Authorization**: Admin, Club leadership  
**Content**: Member counts, activity levels, role distribution, growth trends

```csharp
Task<Report> GenerateEventOutcomesReportAsync(int? clubId, DateTime? startDate, DateTime? endDate)
```
**Description**: Generates event outcomes and attendance report  
**Parameters**: 
- `clubId`: Optional club filter
- `startDate`: Report period start
- `endDate`: Report period end
**Returns**: Generated report entity  
**Authorization**: Admin, Club leadership  
**Content**: Event success rates, attendance statistics, popular events

```csharp
Task<Report> GenerateActivityTrackingReportAsync(int? clubId, DateTime? startDate, DateTime? endDate)
```
**Description**: Generates activity tracking and engagement report  
**Parameters**: 
- `clubId`: Optional club filter
- `startDate`: Report period start
- `endDate`: Report period end
**Returns**: Generated report entity  
**Authorization**: Admin, Club leadership  
**Content**: Member engagement metrics, participation trends, activity analysis

```csharp
Task<Report> GenerateSemesterSummaryReportAsync(string semester, int? clubId)
```
**Description**: Generates comprehensive semester summary report  
**Parameters**: 
- `semester`: Semester identifier (e.g., "Fall 2024")
- `clubId`: Optional club filter
**Returns**: Generated report entity  
**Authorization**: Admin, Club leadership  
**Content**: Complete semester overview, achievements, statistics, trends

##### Report Management

```csharp
Task<IEnumerable<Report>> GetAllReportsAsync()
```
**Description**: Retrieves all reports in the system  
**Returns**: Collection of all Report entities  
**Authorization**: Admin, Club leadership

```csharp
Task<Report?> GetReportByIdAsync(int reportId)
```
**Description**: Retrieves a specific report by ID  
**Parameters**: 
- `reportId`: Report identifier
**Returns**: Report entity or null if not found  
**Authorization**: Report creator, Admin, Club leadership

```csharp
Task<IEnumerable<Report>> GetReportsByTypeAsync(ReportType type)
```
**Description**: Retrieves reports by their type  
**Parameters**: 
- `type`: Report type filter
**Returns**: Collection of reports of specified type  
**Authorization**: Admin, Club leadership

```csharp
Task<IEnumerable<Report>> GetReportsByClubAsync(int clubId)
```
**Description**: Retrieves all reports for a specific club  
**Parameters**: 
- `clubId`: Club identifier
**Returns**: Collection of club-specific reports  
**Authorization**: Club leadership, Admin

```csharp
Task<bool> DeleteReportAsync(int reportId)
```
**Description**: Deletes a report  
**Parameters**: 
- `reportId`: Report identifier
**Returns**: Success status  
**Authorization**: Report creator, Admin

##### Export Functionality

```csharp
Task<byte[]> ExportReportToPdfAsync(int reportId)
```
**Description**: Exports a report to PDF format  
**Parameters**: 
- `reportId`: Report identifier
**Returns**: PDF file as byte array  
**Authorization**: Report viewer, Admin, Club leadership  
**Format**: Professional PDF with charts and formatting

```csharp
Task<byte[]> ExportReportToExcelAsync(int reportId)
```
**Description**: Exports a report to Excel format  
**Parameters**: 
- `reportId`: Report identifier
**Returns**: Excel file as byte array  
**Authorization**: Report viewer, Admin, Club leadership  
**Format**: Excel workbook with multiple sheets and charts

```csharp
Task<string> ExportReportToCsvAsync(int reportId)
```
**Description**: Exports a report to CSV format  
**Parameters**: 
- `reportId`: Report identifier
**Returns**: CSV content as string  
**Authorization**: Report viewer, Admin, Club leadership  
**Format**: Comma-separated values for data analysis

---

## Supporting Services

### INotificationService

**Purpose**: Manages multi-channel notification delivery and templates.

#### Key Methods

```csharp
Task SendNotificationAsync(CreateNotificationRequest request)
Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, NotificationFilter? filter = null)
Task MarkAsReadAsync(string notificationId)
Task<bool> DeleteNotificationAsync(string notificationId)
Task SendBulkNotificationAsync(BulkNotificationRequest request)
```

### IAuthorizationService

**Purpose**: Handles role-based access control and permissions.

#### Key Methods

```csharp
bool CanAccessFeature(string feature, SystemRole userRole)
bool CanManageClub(int clubId, int userId)
bool CanManageEvent(int eventId, int userId)
bool CanViewReport(int reportId, int userId)
```

### INavigationService

**Purpose**: Manages view navigation and state in the MVVM pattern.

#### Key Methods

```csharp
void NavigateTo(string viewName, object? parameter = null)
void GoBack()
void ClearNavigationHistory()
```

## Error Handling

### Exception Types

All services implement comprehensive error handling using custom exception types:

- `ClubManagementException`: Base exception class
- `ValidationException`: Data validation errors
- `AuthorizationException`: Permission denied errors
- `BusinessRuleViolationException`: Business logic violations
- `DatabaseConnectionException`: Database connectivity issues

### Error Response Format

```csharp
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}
```

## Authentication & Authorization

### Role Hierarchy

1. **SystemAdmin**: Full system access
2. **ClubPresident/Chairman**: Club-wide management
3. **ViceChairman**: Limited club management
4. **TeamLeader**: Event and member coordination
5. **Member**: Basic participation features

### Permission Matrix

| Feature | Admin | Chairman | Vice Chairman | Team Leader | Member |
|---------|-------|----------|---------------|-------------|--------|
| User Management | ✓ | ✓ (Club only) | ✗ | ✗ | ✗ |
| Club Management | ✓ | ✓ (Own club) | ✗ | ✗ | ✗ |
| Event Creation | ✓ | ✓ | ✓ | ✓ | ✗ |
| Event Management | ✓ | ✓ | ✓ | ✓ (Own events) | ✗ |
| Report Generation | ✓ | ✓ | ✓ | Limited | ✗ |
| System Settings | ✓ | ✗ | ✗ | ✗ | ✗ |

## Performance Considerations

### Async/Await Pattern
All service methods use async/await for non-blocking operations, especially database calls.

### Caching Strategy
- User sessions cached in memory
- Frequently accessed data cached with expiration
- Database query result caching for reports

### Database Optimization
- Entity Framework query optimization
- Proper indexing on frequently queried columns
- Lazy loading for navigation properties
- Bulk operations for large data sets

## Testing

### Unit Testing
All service methods should be unit tested with:
- Valid input scenarios
- Invalid input handling
- Authorization checks
- Error condition handling

### Integration Testing
Service integration tests should cover:
- Database operations
- Cross-service communication
- End-to-end workflows

## Logging

All services implement comprehensive logging:
- Method entry/exit logging
- Parameter validation logging
- Error and exception logging
- Performance metrics logging

### Log Levels
- **Trace**: Detailed execution flow
- **Debug**: Development debugging information
- **Information**: General application flow
- **Warning**: Unexpected but handled situations
- **Error**: Error conditions and exceptions
- **Critical**: Critical failures requiring immediate attention

## Configuration

Services are configured through dependency injection and configuration files:

```json
{
  "ServiceConfiguration": {
    "DatabaseTimeout": 30,
    "CacheExpirationMinutes": 15,
    "MaxReportSize": 10485760,
    "NotificationRetryAttempts": 3
  }
}
```

This documentation provides a comprehensive reference for all service interfaces and their usage within the Club Management Application. For implementation details, refer to the actual service classes in the `/Services` directory.