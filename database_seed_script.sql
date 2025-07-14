-- Club Management Application Database Schema and Seed Script
-- Updated to reflect simplified database structure matching User Manual documentation
-- This script creates a minimal setup with:
-- - 1 Admin user: admin@university.edu / admin123
-- - 5 Member users: john.doe, jane.smith, mike.johnson, sarah.wilson, david.brown (password: password123)
-- - 2 Clubs: Computer Science Club (3 members) and Photography Club (2 members)
-- - Club memberships as per DatabaseInitializer.cs implementation
--
-- IMPORTANT: This script must be executed as a whole to ensure proper table creation
-- before data insertion. If running in parts, ensure the schema creation section
-- (lines 1-220) is executed before the data insertion section (lines 221+).

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ClubManagementDB')
BEGIN
    CREATE DATABASE ClubManagementDB;
END
GO

USE ClubManagementDB;
GO

-- Verify we're in the correct database
IF DB_NAME() != 'ClubManagementDB'
BEGIN
    PRINT 'ERROR: Not connected to ClubManagementDB database. Please ensure USE ClubManagementDB; was executed successfully.';
    RETURN;
END
GO

-- Drop existing tables if they exist (in correct order to handle foreign key constraints)
IF OBJECT_ID('EventParticipants', 'U') IS NOT NULL DROP TABLE EventParticipants;
IF OBJECT_ID('Reports', 'U') IS NOT NULL DROP TABLE Reports;
IF OBJECT_ID('Events', 'U') IS NOT NULL DROP TABLE Events;
IF OBJECT_ID('ClubMembers', 'U') IS NOT NULL DROP TABLE ClubMembers;
IF OBJECT_ID('AuditLogs', 'U') IS NOT NULL DROP TABLE AuditLogs;
IF OBJECT_ID('Notifications', 'U') IS NOT NULL DROP TABLE Notifications;
IF OBJECT_ID('NotificationTemplates', 'U') IS NOT NULL DROP TABLE NotificationTemplates;
IF OBJECT_ID('ScheduledNotifications', 'U') IS NOT NULL DROP TABLE ScheduledNotifications;
IF OBJECT_ID('Settings', 'U') IS NOT NULL DROP TABLE Settings;
IF OBJECT_ID('Clubs', 'U') IS NOT NULL DROP TABLE Clubs;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
GO

-- Create Users table (System-wide users)
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    StudentID NVARCHAR(20) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    SystemRole INT NOT NULL CHECK (SystemRole IN (0, 1, 2)),
    ActivityLevel INT NOT NULL DEFAULT 0 CHECK (ActivityLevel IN (0, 1, 2)),
    ClubID INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
GO

-- Create unique index on User Email
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
GO

-- Create Clubs table
CREATE TABLE Clubs (
    ClubID INT PRIMARY KEY IDENTITY(1,1),
    ClubName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    EstablishedDate DATETIME2 NULL,
    CreatedUserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    MeetingSchedule NVARCHAR(200) NULL,
    ContactEmail NVARCHAR(150) NULL,
    ContactPhone NVARCHAR(20) NULL,
    Website NVARCHAR(200) NULL,
    FOREIGN KEY (CreatedUserId) REFERENCES Users(UserID)
);
GO

-- Create unique index on Club Name
CREATE UNIQUE INDEX IX_Clubs_Name ON Clubs (ClubName);
GO

-- Add foreign key constraint for Users.ClubID
ALTER TABLE Users ADD CONSTRAINT FK_Users_ClubID FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID);
GO

-- Create ClubMembers table (Maps users to clubs with club-specific roles)
CREATE TABLE ClubMembers (
    ClubMemberID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ClubID INT NOT NULL,
    ClubRole INT NOT NULL CHECK (ClubRole IN (0, 1, 2)), -- 0=Admin, 1=Chairman, 2=Member
    IsActive BIT NOT NULL DEFAULT 1,
    JoinDate DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID)
);
GO

-- Create unique index to ensure a user can only have one role per club
CREATE UNIQUE INDEX IX_ClubMembers_UserID_ClubID ON ClubMembers (UserID, ClubID);
GO

-- Create Events table (matching C# Event model exactly)
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(300) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    RegistrationDeadline DATETIME2 NULL,
    MaxParticipants INT NULL,
    Status INT NOT NULL DEFAULT 0 CHECK (Status IN (0, 1, 2, 3, 4)), -- 0=Scheduled, 1=InProgress, 2=Completed, 3=Cancelled, 4=Postponed
    ClubID INT NOT NULL,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE CASCADE
);
GO

-- Create EventParticipants table (matching C# EventParticipant model)
CREATE TABLE EventParticipants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    EventID INT NOT NULL,
    Status INT NOT NULL DEFAULT 0 CHECK (Status IN (0, 1, 2)), -- 0=Registered, 1=Attended, 2=Absent
    RegistrationDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    AttendanceDate DATETIME2 NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE
);
GO

-- Create unique index to ensure a user can only register once per event
CREATE UNIQUE INDEX IX_EventParticipants_UserID_EventID ON EventParticipants (UserID, EventID);
GO

-- Create Reports table
CREATE TABLE Reports (
    ReportID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Type INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    GeneratedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Semester NVARCHAR(50) NULL,
    ClubID INT NULL,
    GeneratedByUserID INT NOT NULL,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE SET NULL,
    FOREIGN KEY (GeneratedByUserID) REFERENCES Users(UserID)
);
GO

-- Create AuditLogs table
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(255) NOT NULL,
    Details NVARCHAR(2000) NOT NULL,
    LogType INT NOT NULL,
    IpAddress NVARCHAR(45) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    AdditionalData NVARCHAR(MAX) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserID) ON DELETE SET NULL
);
GO

-- Create indexes for AuditLogs
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs (Timestamp);
CREATE INDEX IX_AuditLogs_LogType ON AuditLogs (LogType);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs (UserId);
GO

-- Create NotificationTemplates table
CREATE TABLE NotificationTemplates (
    Id NVARCHAR(450) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    TitleTemplate NVARCHAR(200) NOT NULL,
    MessageTemplate NVARCHAR(2000) NOT NULL,
    Type INT NOT NULL,
    Priority INT NOT NULL DEFAULT 1,
    Category INT NOT NULL,
    ChannelsJson NVARCHAR(500) NULL,
    ParametersJson NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL
);
GO

-- Create indexes for NotificationTemplates
CREATE UNIQUE INDEX IX_NotificationTemplates_Name ON NotificationTemplates (Name);
CREATE INDEX IX_NotificationTemplates_Type ON NotificationTemplates (Type);
CREATE INDEX IX_NotificationTemplates_Category ON NotificationTemplates (Category);
CREATE INDEX IX_NotificationTemplates_IsActive ON NotificationTemplates (IsActive);
GO

-- Create ScheduledNotifications table
CREATE TABLE ScheduledNotifications (
    Id NVARCHAR(450) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    NotificationRequest NVARCHAR(MAX) NOT NULL,
    ScheduledTime DATETIME2 NOT NULL,
    RecurrencePattern NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastProcessedAt DATETIME2 NULL,
    CancelledAt DATETIME2 NULL,
    ProcessCount INT NOT NULL DEFAULT 0
);
GO

-- Create indexes for ScheduledNotifications
CREATE INDEX IX_ScheduledNotifications_ScheduledTime ON ScheduledNotifications (ScheduledTime);
CREATE INDEX IX_ScheduledNotifications_IsActive ON ScheduledNotifications (IsActive);
CREATE INDEX IX_ScheduledNotifications_Name ON ScheduledNotifications (Name);
GO

-- Create Notifications table
CREATE TABLE Notifications (
    Id NVARCHAR(450) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(2000) NOT NULL,
    Type INT NOT NULL,
    Priority INT NOT NULL DEFAULT 1,
    Category INT NOT NULL,
    UserID INT NULL,
    ClubID INT NULL,
    EventID INT NULL,
    Data NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    ExpiresAt DATETIME2 NULL,
    ReadAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    IsDeleted BIT NOT NULL DEFAULT 0,
    ChannelsJson NVARCHAR(500) NULL,
    CONSTRAINT FK_Notifications_UserID FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Notifications_ClubID FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Notifications_EventID FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- Create indexes for Notifications
CREATE INDEX IX_Notifications_UserID ON Notifications (UserID);
CREATE INDEX IX_Notifications_CreatedAt ON Notifications (CreatedAt);
CREATE INDEX IX_Notifications_IsRead ON Notifications (IsRead);
CREATE INDEX IX_Notifications_Type ON Notifications (Type);
CREATE INDEX IX_Notifications_Category ON Notifications (Category);
GO

-- Create Settings table
CREATE TABLE Settings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NULL,
    ClubID INT NULL,
    [Key] NVARCHAR(100) NOT NULL,
    Value NVARCHAR(MAX) NOT NULL,
    Scope INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE CASCADE
);
GO

-- Create indexes for Settings
CREATE UNIQUE INDEX IX_Settings_Key_Scope_UserID_ClubID ON Settings ([Key], Scope, UserID, ClubID);
CREATE INDEX IX_Settings_Scope ON Settings (Scope);
CREATE INDEX IX_Settings_CreatedAt ON Settings (CreatedAt);
GO

PRINT 'Database schema created successfully!';
GO

-- ========================================
-- DATA INSERTION SECTION
-- ========================================
-- SCHEMA FIXES APPLIED:
-- 1. Clubs table: Added all required columns (CreatedUserId, CreatedAt, IsActive, MeetingSchedule, ContactEmail, ContactPhone, Website)
-- 2. Events table: Added Status CHECK constraint with proper enum values (0=Scheduled, 1=InProgress, 2=Completed, 3=Cancelled, 4=Postponed)
-- 3. EventParticipants table: Added Status CHECK constraint with proper enum values (0=Registered, 1=Attended, 2=Absent)
-- 4. ClubMembers table: Added IsActive column and proper enum documentation (0=Admin, 1=Chairman, 2=Member)
-- 5. All INSERT statements updated with complete column sets and proper datetime formatting
-- ========================================

-- Insert Users first (without ClubID to avoid foreign key constraint)
-- SystemRole: 0=Admin, 1=ClubOwner, 2=Member
-- ActivityLevel: 0=Active, 1=Normal, 2=Inactive
-- Insert Users with correct SHA256 password hashes
-- IMPORTANT: These password hashes are generated using SHA256 and must match the application's hashing method
-- Admin password: 'admin123' -> 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk='
-- User password: 'password123' -> 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg='

INSERT INTO Users (FullName, Email, Password, StudentID, PhoneNumber, SystemRole, ActivityLevel, ClubID, IsActive, TwoFactorEnabled, CreatedAt) VALUES
-- System Administrator (Password: admin123)
('System Administrator', 'admin@university.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'ADM001', '+1-555-0001', 0, 0, NULL, 1, 0, '2024-01-01'),

-- Club Members (Password: password123)
('John Doe', 'john.doe@university.edu', 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=', 'STU001', '+1-555-0101', 1, 0, NULL, 1, 0, '2024-01-15'),
('Jane Smith', 'jane.smith@university.edu', 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=', 'STU002', '+1-555-0102', 2, 0, NULL, 1, 0, '2024-02-01'),
('Mike Johnson', 'mike.johnson@university.edu', 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=', 'STU003', '+1-555-0103', 2, 0, NULL, 1, 0, '2024-02-15'),
('Sarah Wilson', 'sarah.wilson@university.edu', 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=', 'STU004', '+1-555-0104', 2, 0, NULL, 1, 0, '2024-03-01'),
('David Brown', 'david.brown@university.edu', 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=', 'STU005', '+1-555-0105', 2, 0, NULL, 1, 0, '2024-03-15');
GO

-- Insert Clubs (matches current DatabaseInitializer.cs implementation)
INSERT INTO Clubs (ClubName, Description, EstablishedDate, CreatedUserId, CreatedAt, IsActive, MeetingSchedule, ContactEmail, ContactPhone, Website) VALUES
('Computer Science Club', 'A club for computer science enthusiasts and programming', '2023-07-01 10:00:00', 1, '2024-01-01 10:30:00', 1, 'Wednesdays 6:00 PM - Room CS101', 'cs.club@university.edu', '+1-555-2001', 'https://university.edu/clubs/cs'),
('Photography Club', 'Capturing moments and learning photography techniques', '2023-09-01 14:00:00', 1, '2024-01-01 09:15:00', 1, 'Fridays 7:00 PM - Art Building', 'photo.club@university.edu', '+1-555-2002', 'https://university.edu/clubs/photography');
GO

-- Insert ClubMembers (Maps users to clubs with club-specific roles)
-- ClubRole: 0=Admin, 1=Chairman, 2=Member
INSERT INTO ClubMembers (UserID, ClubID, ClubRole, IsActive, JoinDate) VALUES
-- Computer Science Club (ClubID: 1) - 3 members
(2, 1, 1, 1, '2024-01-15 10:30:00'),   -- John Doe as Chairman
(3, 1, 2, 1, '2024-02-01 10:30:00'),   -- Jane Smith as Member
(4, 1, 2, 1, '2024-02-15 10:30:00'),   -- Mike Johnson as Member

-- Photography Club (ClubID: 2) - 2 members
(5, 2, 0, 1, '2024-03-01 10:30:00'),   -- Sarah Wilson as Admin
(6, 2, 2, 1, '2024-03-15 10:30:00');   -- David Brown as Member
GO

-- Update Users ClubID after clubs are created
-- No need to update Users.ClubID as they are now directly inserted with ClubID
GO

-- Insert Events with proper Status enum values (0=Scheduled, 1=InProgress, 2=Completed, 3=Cancelled, 4=Postponed)
INSERT INTO Events (Name, Description, EventDate, Location, CreatedDate, IsActive, RegistrationDeadline, MaxParticipants, Status, ClubID) VALUES
-- Computer Science Club Events (ClubID: 1)
('Programming Workshop', 'Learn advanced programming techniques and best practices', '2024-12-15 14:00:00', 'Computer Lab A', '2024-11-01 10:00:00', 1, '2024-12-10 23:59:59', 30, 0, 1), -- Scheduled
('Hackathon 2024', '24-hour coding competition with exciting prizes', '2024-12-20 09:00:00', 'Main Auditorium', '2024-11-05 09:30:00', 1, '2024-12-15 23:59:59', 100, 0, 1), -- Scheduled

-- Photography Club Events (ClubID: 2)
('Photography Exhibition', 'Showcase of student photography work and techniques', '2024-12-22 19:00:00', 'Art Gallery', '2024-11-09 11:20:00', 1, '2024-12-20 18:00:00', 200, 0, 2), -- Scheduled
('Photography Workshop', 'Learn digital photography and photo editing', '2025-01-08 14:00:00', 'Photography Studio', '2024-11-11 16:45:00', 1, '2025-01-05 23:59:59', 20, 0, 2); -- Scheduled
GO

-- Insert Event Participants with proper Status enum values (0=Registered, 1=Attended, 2=Absent)
INSERT INTO EventParticipants (UserID, EventID, Status, RegistrationDate, AttendanceDate) VALUES
-- Programming Workshop (EventID: 1) - Computer Science Club
(2, 1, 1, '2024-11-15 14:30:00', '2024-12-15 14:00:00'), -- John Doe (Attended)
(3, 1, 1, '2024-11-16 09:15:00', '2024-12-15 14:00:00'), -- Jane Smith (Attended)
(4, 1, 0, '2024-11-17 16:45:00', NULL), -- Mike Johnson (Registered)

-- Hackathon 2024 (EventID: 2) - Computer Science Club
(2, 2, 0, '2024-11-20 11:20:00', NULL), -- John Doe (Registered)
(3, 2, 0, '2024-11-21 13:00:00', NULL), -- Jane Smith (Registered)
(4, 2, 0, '2024-11-22 10:30:00', NULL), -- Mike Johnson (Registered)

-- Photography Exhibition (EventID: 3) - Photography Club
(5, 3, 1, '2024-11-25 15:45:00', '2024-12-22 19:00:00'), -- Sarah Wilson (Attended)
(6, 3, 1, '2024-11-26 12:15:00', '2024-12-22 19:00:00'), -- David Brown (Attended)

-- Photography Workshop (EventID: 4) - Photography Club
(5, 4, 0, '2024-11-27 08:30:00', NULL), -- Sarah Wilson (Registered)
(6, 4, 0, '2024-11-28 10:15:00', NULL); -- David Brown (Registered)
GO

-- Insert Reports
INSERT INTO Reports (Title, Type, Content, GeneratedDate, Semester, ClubID, GeneratedByUserID) VALUES
('Computer Science Club - Fall 2024 Member Statistics', 0, '{"totalMembers": 3, "activeMembers": 3, "newMembers": 3, "membersByRole": {"Chairman": 1, "Member": 2}}', '2024-11-15', 'Fall 2024', 1, 1),
('Photography Club - Event Outcomes Report', 1, '{"eventsHeld": 2, "totalParticipants": 4, "averageAttendance": 75, "topEvent": "Photography Exhibition"}', '2024-11-20', 'Fall 2024', 2, 1),
('University-wide Semester Summary', 3, '{"totalClubs": 2, "totalMembers": 5, "totalEvents": 4, "overallEngagement": "High", "topPerformingClubs": ["Computer Science Club", "Photography Club"]}', '2024-11-30', 'Fall 2024', NULL, 1);
GO

-- Insert Audit Logs
INSERT INTO AuditLogs (UserId, Action, Details, LogType, IpAddress, Timestamp, AdditionalData) VALUES
(1, 'User Login', 'System Administrator logged into the system', 0, '192.168.1.100', '2024-11-01 08:00:00', '{"userAgent": "Mozilla/5.0", "sessionId": "sess_001"}'),
(1, 'Club Created', 'Computer Science Club was created', 2, '192.168.1.101', '2024-01-01 10:30:00', '{"clubId": 1, "clubName": "Computer Science Club"}'),
(1, 'Club Created', 'Photography Club was created', 2, '192.168.1.101', '2024-01-01 10:31:00', '{"clubId": 2, "clubName": "Photography Club"}'),
(1, 'Event Created', 'Programming Workshop event was created', 0, '192.168.1.101', '2024-11-01 14:20:00', '{"eventId": 1, "eventName": "Programming Workshop"}'),
(2, 'Event Registration', 'User registered for Programming Workshop', 0, '192.168.1.102', '2024-11-16 09:15:00', '{"eventId": 1, "userId": 2}'),
(2, 'Club Member Added', 'User added to Computer Science Club as Chairman', 2, '192.168.1.103', '2024-01-15 11:45:00', '{"clubId": 1, "userId": 2, "role": "Chairman"}'),
(5, 'Club Member Added', 'User added to Photography Club as Admin', 2, '192.168.1.104', '2024-03-01 11:45:00', '{"clubId": 2, "userId": 5, "role": "Admin"}'),
(1, 'Report Generated', 'University-wide semester summary report generated', 2, '192.168.1.100', '2024-11-30 16:00:00', '{"reportId": 3, "reportType": "SemesterSummary"}');
GO

-- Insert sample notification templates
INSERT INTO NotificationTemplates (Id, Name, Description, TitleTemplate, MessageTemplate, Type, Priority, Category, ChannelsJson, ParametersJson, IsActive, CreatedAt) VALUES
('welcome-template', 'Welcome Template', 'Welcome message for new users', 'Welcome to {ClubName}!', 'Hello {UserName}, welcome to {ClubName}. We are excited to have you join us!', 0, 1, 0, '["Email", "InApp"]', '{"UserName": "string", "ClubName": "string"}', 1, GETDATE()),
('event-reminder', 'Event Reminder', 'Reminder for upcoming events', 'Event Reminder: {EventName}', 'Don''t forget about {EventName} happening on {EventDate} at {Location}!', 1, 2, 1, '["Email", "InApp"]', '{"EventName": "string", "EventDate": "datetime", "Location": "string"}', 1, GETDATE()),
('membership-approval', 'Membership Approval', 'Notification for membership approval', 'Membership Approved', 'Congratulations! Your membership to {ClubName} has been approved.', 2, 1, 0, '["Email", "InApp"]', '{"ClubName": "string"}', 1, GETDATE());
GO

-- Insert sample settings
INSERT INTO Settings (UserID, ClubID, [Key], Value, Scope, CreatedAt, UpdatedAt) VALUES
(NULL, NULL, 'SystemMaintenanceMode', 'false', 2, GETDATE(), GETDATE()),
(NULL, NULL, 'DefaultEventRegistrationDeadlineDays', '3', 2, GETDATE(), GETDATE()),
(NULL, NULL, 'MaxEventsPerClubPerMonth', '10', 2, GETDATE(), GETDATE()),
(1, NULL, 'EmailNotifications', 'true', 0, GETDATE(), GETDATE()),
(1, NULL, 'Theme', 'Light', 0, GETDATE(), GETDATE()),
(NULL, 1, 'AutoApproveMembers', 'false', 1, GETDATE(), GETDATE()),
(NULL, 1, 'EventCapacityLimit', '100', 1, GETDATE(), GETDATE());
GO

PRINT 'Database seeded successfully with corrected role structure!';
GO
