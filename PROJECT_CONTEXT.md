# Club Management Application - Project Context & Rules

## Project Overview

**Project Type**: WPF Desktop Application for University Club Management  
**Framework**: .NET 8.0 Windows with WPF (Windows Presentation Foundation)  
**Architecture**: MVVM Pattern with Three-Layer Architecture  
**Database**: SQL Server with Entity Framework Core 9.0.6  
**Language**: C# with Vietnamese UI localization  

### Purpose
A comprehensive desktop solution designed to streamline the administration and coordination of university clubs and student organizations. The application provides centralized management for members, events, reporting, and organizational activities with role-based access control.

## Core Business Domain

### Primary Entities
1. **Users** - Club members with hierarchical roles (Admin, Chairman, ViceChairman, TeamLeader, Member)
2. **Clubs** - Student organizations with members and events
3. **Events** - Club activities with registration, attendance tracking, and status management
4. **Reports** - Analytics and data exports for decision-making
5. **Notifications** - Multi-channel communication system
6. **Audit Logs** - Complete activity tracking for compliance

### Key Business Rules

#### User Management Rules
- Users must have unique email addresses
- Password must be BCrypt hashed (never store plain text)
- Users can belong to only one club (nullable for admins)
- Activity levels: Active, Normal, Inactive
- Role hierarchy: Admin > Chairman > ViceChairman > TeamLeader > Member

#### Club Management Rules
- Club names must be unique across the system
- Only Admins can create new clubs
- Chairman and ViceChairman can manage their club's members
- Each club can have multiple events

#### Event Management Rules
- Events must belong to a club
- Registration deadline is optional but recommended
- Maximum participants is optional (unlimited if not set)
- Event status: Scheduled, InProgress, Completed, Cancelled, Postponed
- Users can only register once per event (unique constraint)
- Attendance can be marked separately from registration

#### Permission Matrix
| Feature | Admin | Chairman | ViceChairman | TeamLeader | Member |
|---------|-------|----------|--------------|------------|--------|
| User Management | ✓ | ✗ | ✗ | ✗ | ✗ |
| Club Management | ✓ | ✓ | ✓ | ✗ | ✗ |
| Event Management | ✓ | ✓ | ✓ | ✓ | ✗ |
| Event Registration | ✓ | ✓ | ✓ | ✓ | ✓ |
| Reports Access | ✓ | ✓ | ✓ | Limited | Limited |

## Technical Architecture

### Project Structure
```
├── Models/              # Entity classes and business models
├── ViewModels/          # MVVM ViewModels with business logic
├── Views/               # XAML UI files and code-behind
├── Services/            # Business logic and data access services
├── Data/                # Entity Framework DbContext and configuration
├── Commands/            # ICommand implementations for MVVM
├── Converters/          # Value converters for data binding
├── DTOs/                # Data Transfer Objects
├── Helpers/             # Utility and validation helpers
├── Configuration/       # Service configuration and DI setup
├── Exceptions/          # Custom exception classes
├── Deployment/          # PowerShell deployment scripts
└── Documentation/       # Comprehensive project documentation
```

### Database Schema

#### Core Tables
- **Users**: UserID (PK), FullName, Email (Unique), Password (Hashed), Role, ActivityLevel, ClubID (FK)
- **Clubs**: ClubID (PK), Name (Unique), Description, IsActive, CreatedDate
- **Events**: EventID (PK), Name, Description, EventDate, Location, Status, ClubID (FK)
- **EventParticipants**: ParticipantID (PK), UserID (FK), EventID (FK), Status, RegistrationDate
- **Reports**: ReportID (PK), Title, Type, Content (JSON), GeneratedByUserID (FK)
- **Notifications**: Id (PK), Title, Message, Type, Priority, UserID (FK), ClubID (FK)
- **AuditLogs**: Id (PK), UserId (FK), Action, Details, LogType, Timestamp
- **Settings**: Id (PK), Key, Value, Scope (User/Club/Global), UserId (FK), ClubId (FK)

### Service Layer Architecture

#### Core Services (All Transient)
- **IUserService**: User CRUD, authentication, activity tracking
- **IClubService**: Club management, membership operations
- **IEventService**: Event lifecycle, registration, attendance
- **IReportService**: Report generation, data aggregation, exports
- **INotificationService**: Multi-channel notifications (Email, In-App)
- **IAuthorizationService**: Role-based access control
- **IAuditService**: Activity logging and compliance tracking
- **INavigationService**: Window management and view transitions

#### Configuration Services (Singleton)
- **IConfigurationService**: Application settings management
- **ClubManagementDbContext**: Entity Framework database context

### MVVM Implementation

#### ViewModels
- **MainViewModel**: Main application coordinator
- **LoginViewModel**: Authentication handling
- **DashboardViewModel**: Statistics and overview
- **MemberListViewModel**: User management interface
- **EventManagementViewModel**: Event operations
- **ClubManagementViewModel**: Club administration
- **ReportsViewModel**: Report generation and viewing

#### Key MVVM Patterns
- **BaseViewModel**: INotifyPropertyChanged implementation
- **RelayCommand**: ICommand implementation for button actions
- **Value Converters**: Boolean to Visibility, Status to Color, etc.
- **Data Binding**: Two-way binding for forms, one-way for displays

## Development Guidelines

### Code Standards
1. **Naming Conventions**: PascalCase for public members, camelCase for private
2. **Async/Await**: All database operations must be asynchronous
3. **Error Handling**: Use try-catch blocks with proper logging
4. **Validation**: Server-side validation for all user inputs
5. **Security**: Never store plain text passwords, validate all inputs

### Database Guidelines
1. **Migrations**: Use Entity Framework migrations for schema changes
2. **Relationships**: Properly configure foreign keys and navigation properties
3. **Indexing**: Add indexes for frequently queried columns
4. **Constraints**: Use unique constraints where business rules require
5. **Soft Deletes**: Use IsActive flags instead of hard deletes where appropriate

### UI/UX Guidelines
1. **Localization**: All UI text in Vietnamese with English fallbacks
2. **Responsive Design**: Support different screen resolutions
3. **Accessibility**: Proper tab order and keyboard navigation
4. **Modern Design**: Material Design inspired with gradients and shadows
5. **Role-Based UI**: Hide/show features based on user permissions

## Configuration Management

### Application Settings (appsettings.json)
- **ConnectionStrings**: Database connections for different environments
- **EmailSettings**: SMTP configuration for notifications
- **SecuritySettings**: Password policies, session timeouts, lockout settings
- **ApplicationSettings**: General app configuration and feature flags
- **NotificationSettings**: Email and in-app notification preferences

### Environment Support
- **Development**: Local SQL Server Express with detailed logging
- **Production**: Full SQL Server with optimized settings
- **Azure**: Cloud deployment configuration

## Deployment Strategy

### Deployment Components
1. **PowerShell Scripts**: Automated deployment with backup and rollback
2. **Database Scripts**: Schema creation and seed data
3. **Configuration Management**: Environment-specific settings
4. **Service Installation**: Optional Windows service deployment
5. **Desktop Shortcuts**: User-friendly application access

### Deployment Process
1. Backup existing application (if exists)
2. Deploy database schema and migrations
3. Publish application with self-contained runtime
4. Update configuration for target environment
5. Create shortcuts and register file associations
6. Verify deployment and run smoke tests

## Security Implementation

### Authentication & Authorization
- **Password Hashing**: BCrypt with salt for secure storage
- **Session Management**: In-memory session tracking with timeouts
- **Role-Based Access**: Hierarchical permissions with least privilege
- **Audit Logging**: Complete activity tracking for compliance

### Data Protection
- **Input Validation**: Server-side validation for all user inputs
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **XSS Prevention**: Proper data encoding in UI
- **Connection Security**: Encrypted database connections

## Testing Strategy

### Testing Levels
1. **Unit Tests**: Service layer business logic
2. **Integration Tests**: Database operations and API calls
3. **UI Tests**: Critical user workflows
4. **Performance Tests**: Load testing with concurrent users
5. **Security Tests**: Authentication and authorization scenarios

## Maintenance & Support

### Monitoring
- **Application Logs**: Structured logging with Serilog
- **Performance Metrics**: Response times and resource usage
- **Error Tracking**: Exception logging and alerting
- **Audit Reports**: Regular compliance and security reviews

### Backup Strategy
- **Database Backups**: Daily automated backups with 30-day retention
- **Application Backups**: Version-controlled deployment artifacts
- **Configuration Backups**: Settings and customization preservation

## Application Flow & User Journey

### Complete Application Workflow

#### 1. Application Startup Flow
```
App.xaml.cs (Entry Point)
├── ConfigureServices() - Dependency Injection Setup
│   ├── Register DbContext (Singleton)
│   ├── Register Services (Transient)
│   └── Register ViewModels (Transient)
├── Database Initialization
│   ├── Check Connection
│   ├── Apply Pending Migrations
│   └── Seed Initial Data (if empty)
└── Show LoginWindow
```

#### 2. Authentication Flow
```
LoginWindow.xaml
├── User Input (Email + Password)
├── LoginViewModel.LoginCommand
│   ├── Input Validation
│   ├── UserService.AuthenticateAsync()
│   │   ├── Find User by Email
│   │   ├── Verify BCrypt Password
│   │   └── Update LastLoginDate
│   ├── AuthorizationService.SetCurrentUser()
│   └── AuditService.LogActivity("Login")
├── Success: Navigate to MainWindow
└── Failure: Show Error Message
```

#### 3. Main Application Flow
```
MainWindow.xaml (MVVM Container)
├── MainViewModel (Coordinator)
│   ├── Initialize Dashboard
│   ├── Load User Permissions
│   └── Setup Navigation Commands
├── Navigation Service
│   ├── DashboardView (Default)
│   ├── MemberListView
│   ├── ClubManagementView
│   ├── EventManagementView
│   └── ReportsView
└── Role-Based UI Visibility
```

#### 4. Dashboard Flow (Landing Page)
```
DashboardViewModel
├── Load Statistics
│   ├── Total Members Count
│   ├── Active Events Count
│   ├── Recent Activities
│   └── Upcoming Events
├── Load Recent Notifications
├── Load Quick Actions (Role-Based)
│   ├── Admin: User Management, Club Creation
│   ├── Chairman/Vice: Member Management, Event Creation
│   ├── TeamLeader: Event Management
│   └── Member: Event Registration, Profile
└── Real-time Updates (Timer-based refresh)
```

#### 5. User Management Flow (Admin Only)
```
MemberListView
├── MemberListViewModel
│   ├── Load All Users (with Club info)
│   ├── Search/Filter Functionality
│   └── CRUD Operations
├── Add User Flow
│   ├── AddUserDialog.xaml
│   ├── Input Validation
│   ├── UserService.CreateUserAsync()
│   │   ├── Check Email Uniqueness
│   │   ├── Hash Password (BCrypt)
│   │   ├── Save to Database
│   │   └── Send Welcome Email
│   └── Refresh User List
├── Edit User Flow
│   ├── EditUserDialog.xaml
│   ├── Load Existing Data
│   ├── UserService.UpdateUserAsync()
│   └── Audit Log Entry
└── Delete User Flow
    ├── Confirmation Dialog
    ├── Soft Delete (IsActive = false)
    └── Audit Log Entry
```

#### 6. Club Management Flow
```
ClubManagementView
├── ClubManagementViewModel
│   ├── Load User's Club (or All for Admin)
│   ├── Member Management
│   └── Club Settings
├── Add Club Flow (Admin Only)
│   ├── AddClubDialog.xaml
│   ├── ClubService.CreateClubAsync()
│   │   ├── Validate Unique Name
│   │   ├── Save to Database
│   │   └── Create Initial Settings
│   └── Refresh Club List
├── Edit Club Flow
│   ├── EditClubDialog.xaml
│   ├── ClubService.UpdateClubAsync()
│   └── Update Club Members
├── Manage Leadership Flow
│   ├── ManageLeadershipDialog.xaml
│   ├── Role Assignment/Change
│   ├── UserService.UpdateUserRoleAsync()
│   └── Notification to Affected Users
└── Club Details Flow
    ├── ClubDetailsDialog.xaml
    ├── Member Statistics
    ├── Event History
    └── Activity Reports
```

#### 7. Event Management Flow
```
EventManagementView
├── EventManagementViewModel
│   ├── Load Club Events (filtered by role)
│   ├── Event Status Management
│   └── Participant Management
├── Add Event Flow
│   ├── AddEventDialog.xaml
│   ├── Event Details Input
│   ├── EventService.CreateEventAsync()
│   │   ├── Validate Event Data
│   │   ├── Save to Database
│   │   └── Send Notifications
│   └── Refresh Event List
├── Edit Event Flow
│   ├── EditEventDialog.xaml
│   ├── EventService.UpdateEventAsync()
│   └── Notify Participants of Changes
├── Event Registration Flow
│   ├── Check Registration Eligibility
│   │   ├── Registration Deadline
│   │   ├── Maximum Participants
│   │   └── User Club Membership
│   ├── EventService.RegisterUserAsync()
│   │   ├── Create EventParticipant Record
│   │   ├── Send Confirmation Email
│   │   └── Update Event Statistics
│   └── Refresh Participant List
├── Participant Management Flow
│   ├── ParticipantManagementDialog.xaml
│   ├── View Registered Users
│   ├── Mark Attendance
│   ├── Manage Registration Status
│   └── Export Participant List
└── Event Details Flow
    ├── EventDetailsDialog.xaml
    ├── Event Information
    ├── Participant List
    ├── Attendance Statistics
    └── Event Actions (Cancel, Postpone, Complete)
```

#### 8. Reports Flow
```
ReportsView
├── ReportsViewModel
│   ├── Load Available Reports (Role-Based)
│   ├── Report Generation
│   └── Export Functionality
├── Report Types
│   ├── Member Reports
│   │   ├── Active Members List
│   │   ├── Member Activity Report
│   │   └── Role Distribution
│   ├── Event Reports
│   │   ├── Event Attendance Report
│   │   ├── Event Statistics
│   │   └── Upcoming Events
│   ├── Club Reports
│   │   ├── Club Activity Summary
│   │   ├── Member Engagement
│   │   └── Leadership Report
│   └── System Reports (Admin Only)
│       ├── Audit Log Report
│       ├── System Usage Statistics
│       └── Security Report
├── Report Generation Flow
│   ├── Select Report Type
│   ├── Set Parameters (Date Range, Filters)
│   ├── ReportService.GenerateReportAsync()
│   │   ├── Query Database
│   │   ├── Process Data
│   │   ├── Format Report
│   │   └── Save Report Record
│   └── Display/Export Report
└── Export Options
    ├── PDF Export
    ├── Excel Export
    ├── CSV Export
    └── Print Preview
```

#### 9. Notification System Flow
```
NotificationService (Background)
├── Event-Driven Notifications
│   ├── User Registration → Welcome Email
│   ├── Event Creation → Club Members Notification
│   ├── Event Registration → Confirmation Email
│   ├── Event Reminder → 24h Before Event
│   ├── Role Change → User Notification
│   └── System Alerts → Admin Notifications
├── Scheduled Notifications
│   ├── Daily Digest (Optional)
│   ├── Weekly Reports
│   └── Monthly Statistics
├── Notification Channels
│   ├── Email Notifications
│   │   ├── SMTP Configuration
│   │   ├── Template System
│   │   └── Delivery Tracking
│   └── In-App Notifications
│       ├── Notification Panel
│       ├── Real-time Updates
│       └── Mark as Read/Unread
└── Notification Preferences
    ├── User Settings
    ├── Notification Types
    └── Delivery Methods
```

#### 10. Security & Audit Flow
```
Security Layer (Cross-Cutting)
├── Authentication
│   ├── Login Validation
│   ├── Session Management
│   └── Password Policies
├── Authorization
│   ├── Role-Based Access Control
│   ├── Permission Checking
│   └── UI Element Visibility
├── Audit Logging
│   ├── User Actions
│   │   ├── Login/Logout
│   │   ├── CRUD Operations
│   │   └── Permission Changes
│   ├── System Events
│   │   ├── Application Start/Stop
│   │   ├── Database Operations
│   │   └── Error Occurrences
│   └── Security Events
│       ├── Failed Login Attempts
│       ├── Unauthorized Access
│       └── Data Export Operations
└── Data Protection
    ├── Input Validation
    ├── SQL Injection Prevention
    ├── Password Hashing
    └── Sensitive Data Handling
```

#### 11. Error Handling & Recovery Flow
```
Error Management
├── Exception Handling
│   ├── Try-Catch Blocks
│   ├── Custom Exceptions
│   └── Error Logging
├── User-Friendly Messages
│   ├── Validation Errors
│   ├── Business Rule Violations
│   └── System Errors
├── Recovery Mechanisms
│   ├── Retry Logic
│   ├── Fallback Options
│   └── Graceful Degradation
└── Logging & Monitoring
    ├── Application Logs
    ├── Error Tracking
    └── Performance Monitoring
```

#### 12. Application Shutdown Flow
```
Application Exit
├── Save User Preferences
├── Close Database Connections
├── Log Logout Activity
├── Clean Up Resources
└── Graceful Shutdown
```

### Data Flow Architecture

#### Request-Response Pattern
```
View (XAML)
├── User Interaction (Button Click, Input)
├── Command Binding (RelayCommand)
├── ViewModel Method Call
├── Service Layer Call
│   ├── Business Logic Validation
│   ├── Database Operation (EF Core)
│   └── Audit Logging
├── Result Processing
├── UI Update (PropertyChanged)
└── User Feedback (Success/Error)
```

#### Database Transaction Flow
```
Service Method
├── Begin Transaction (if needed)
├── Validate Business Rules
├── Execute Database Operations
│   ├── Entity Framework Queries
│   ├── Change Tracking
│   └── SaveChangesAsync()
├── Commit Transaction
├── Log Activity
└── Return Result
```

### Integration Points

#### External Systems
- **Email Service**: SMTP integration for notifications
- **File System**: Report exports and backups
- **Windows Services**: Optional service deployment
- **Active Directory**: Future authentication integration

#### Internal Components
- **Database**: SQL Server with Entity Framework
- **Configuration**: JSON-based settings management
- **Logging**: Structured logging with Serilog
- **Validation**: Multi-layer validation system

## Future Enhancements

### Planned Features
1. **Mobile Application**: Companion mobile app for event check-ins
2. **Web Portal**: Browser-based access for basic functionality
3. **Integration APIs**: Connect with university systems
4. **Advanced Analytics**: Machine learning for engagement prediction
5. **Multi-Language Support**: Additional language localizations

### Scalability Considerations
- **Microservices**: Potential service decomposition for large deployments
- **Cloud Migration**: Azure or AWS deployment options
- **Performance Optimization**: Caching and query optimization
- **Multi-Tenancy**: Support for multiple universities

---

## Quick Reference for Developers

### Common Tasks

#### Adding a New Entity
1. Create model class in `/Models`
2. Add DbSet to `ClubManagementDbContext`
3. Configure relationships in `OnModelCreating`
4. Create migration: `Add-Migration AddNewEntity`
5. Update database: `Update-Database`

#### Adding a New Service
1. Create interface in `/Services/I{ServiceName}.cs`
2. Implement service in `/Services/{ServiceName}.cs`
3. Register in `App.xaml.cs` ConfigureServices method
4. Inject into ViewModels as needed

#### Adding a New View
1. Create XAML in `/Views/{ViewName}.xaml`
2. Create ViewModel in `/ViewModels/{ViewName}ViewModel.cs`
3. Set DataContext in view constructor
4. Add navigation command in MainViewModel

### Key Connection Strings
- **Development**: `Server=DESKTOP-N3D6J5K\SQLEXPRESS;Database=ClubManagementDB;Trusted_Connection=True;TrustServerCertificate=True;`
- **Production**: Configure in appsettings.Production.json

### Important File Locations
- **Main Entry Point**: `App.xaml.cs`
- **Database Context**: `Data/ClubManagementDbContext.cs`
- **Main Window**: `MainWindow.xaml`
- **Configuration**: `appsettings.json`
- **Deployment**: `Deployment/deploy-application.ps1`

This context document should be referenced for all development work to ensure consistency with the established architecture and business rules.

## Identified Issues and Resolutions

### Critical Issues Found During Flow Analysis

#### 1. Login Flow Issues
**Issue**: Multiple password hashing implementations
- `UserService.cs` uses SHA256 hashing
- `SecurityService.cs` uses RFC2898DeriveBytes with SHA256
- `BusinessLogicManager.cs` and `MemberService.cs` use BCrypt
- `DataImportExportService.cs` uses SecurityService

**Resolution Required**: Standardize on BCrypt across all services for security consistency.

**Issue**: Missing password validation in LoginViewModel
- No client-side validation for empty credentials
- No password strength requirements enforcement

**Resolution Required**: Add input validation and integrate with SecurityService password policies.

#### 2. User Management Flow Issues
**Issue**: Inconsistent authentication methods
- Multiple password hashing approaches create security vulnerabilities
- No centralized authentication service

**Resolution Required**: Implement centralized authentication using SecurityService exclusively.

#### 3. Club Management Flow Issues
**Issue**: Leadership role management complexity
- Leadership permissions stored in club description field as JSON
- No proper role-based access control implementation
- Automatic succession logic may cause conflicts

**Resolution Required**: Implement proper RBAC with dedicated permission tables.

**Issue**: Member assignment validation gaps
- No validation for maximum club capacity
- Missing checks for duplicate memberships

**Resolution Required**: Add comprehensive validation rules for club membership.

#### 4. Event Management Flow Issues
**Issue**: Attendance tracking limitations
- Basic status tracking without detailed analytics
- No integration with activity level calculations

**Resolution Required**: Enhance attendance tracking with comprehensive analytics.

**Issue**: Event capacity management
- Basic capacity checking without waitlist functionality
- No overflow handling for popular events

**Resolution Required**: Implement waitlist and overflow management features.

#### 5. Report Management Flow Issues
**Issue**: Missing ReportService implementation details
- Constructor and dependency injection not fully examined
- Potential issues with database context usage

**Resolution Required**: Review ReportService implementation for proper dependency injection and error handling.

**Issue**: Export functionality limitations
- Limited format support (PDF, Excel, CSV)
- No real-time report generation

**Resolution Required**: Enhance export capabilities and add real-time reporting features.

### Security Recommendations
1. **Standardize Password Hashing**: Use BCrypt exclusively across all services
2. **Implement Proper RBAC**: Replace JSON-based permissions with dedicated tables
3. **Add Input Validation**: Comprehensive validation at all entry points
4. **Enhance Audit Logging**: Track all security-related operations
5. **Implement Session Management**: Proper user session handling and timeout

### Performance Recommendations
1. **Database Query Optimization**: Review and optimize Entity Framework queries
2. **Caching Strategy**: Implement caching for frequently accessed data
3. **Async Operations**: Ensure all database operations are properly async
4. **Memory Management**: Review object lifecycle and disposal patterns

### Maintainability Recommendations
1. **Code Consistency**: Standardize coding patterns across all services
2. **Error Handling**: Implement consistent error handling and logging
3. **Documentation**: Update API documentation with identified issues
4. **Testing**: Add comprehensive unit and integration tests

These issues should be prioritized and addressed in the next development cycle to improve application security, performance, and maintainability.