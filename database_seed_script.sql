-- Club Management Application Database Schema and Seed Script
-- This script creates the database schema and populates it with sample data for all features
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
IF OBJECT_ID('AuditLogs', 'U') IS NOT NULL DROP TABLE AuditLogs;
IF OBJECT_ID('Notifications', 'U') IS NOT NULL DROP TABLE Notifications;
IF OBJECT_ID('NotificationTemplates', 'U') IS NOT NULL DROP TABLE NotificationTemplates;
IF OBJECT_ID('ScheduledNotifications', 'U') IS NOT NULL DROP TABLE ScheduledNotifications;
IF OBJECT_ID('Settings', 'U') IS NOT NULL DROP TABLE Settings;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Events', 'U') IS NOT NULL DROP TABLE Events;
IF OBJECT_ID('Clubs', 'U') IS NOT NULL DROP TABLE Clubs;
GO

-- Create Clubs table
CREATE TABLE Clubs (
    ClubID int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(500) NULL,
    ContactEmail nvarchar(150) NULL,
    ContactPhone nvarchar(20) NULL,
    Website nvarchar(200) NULL,
    MeetingSchedule nvarchar(200) NULL,
    FoundedDate datetime2 NULL,
    IsActive bit NOT NULL DEFAULT 1,
    IsSelected bit NOT NULL DEFAULT 0,
    CreatedDate datetime2 NOT NULL DEFAULT GETDATE()
);
GO

-- Create unique index on Club Name
CREATE UNIQUE INDEX IX_Clubs_Name ON Clubs (Name);
GO

-- Create Users table
CREATE TABLE Users (
    UserID int IDENTITY(1,1) PRIMARY KEY,
    FullName nvarchar(100) NOT NULL,
    Email nvarchar(150) NOT NULL,
    Password nvarchar(255) NOT NULL,
    StudentID nvarchar(20) NULL,
    PhoneNumber nvarchar(20) NULL,
    Role nvarchar(50) NOT NULL,
    ActivityLevel nvarchar(50) NOT NULL DEFAULT 'Normal',
    JoinDate datetime2 NOT NULL DEFAULT GETDATE(),
    IsActive bit NOT NULL DEFAULT 1,
    TwoFactorEnabled bit NOT NULL DEFAULT 0,
    ClubID int NULL,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE SET NULL
);
GO

-- Create unique index on User Email
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
GO

-- Create Events table
CREATE TABLE Events (
    EventID int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    EventDate datetime2 NOT NULL,
    Location nvarchar(300) NOT NULL,
    CreatedDate datetime2 NOT NULL DEFAULT GETDATE(),
    IsActive bit NOT NULL DEFAULT 1,
    RegistrationDeadline datetime2 NULL,
    MaxParticipants int NULL,
    Status nvarchar(50) NOT NULL DEFAULT 'Scheduled',
    ClubID int NOT NULL,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE CASCADE
);
GO

-- Create EventParticipants table
CREATE TABLE EventParticipants (
    ParticipantID int IDENTITY(1,1) PRIMARY KEY,
    UserID int NOT NULL,
    EventID int NOT NULL,
    Status nvarchar(50) NOT NULL DEFAULT 'Registered',
    RegistrationDate datetime2 NOT NULL DEFAULT GETDATE(),
    AttendanceDate datetime2 NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE
);
GO

-- Create unique index to ensure a user can only register once per event
CREATE UNIQUE INDEX IX_EventParticipants_UserID_EventID ON EventParticipants (UserID, EventID);
GO

-- Create Reports table
CREATE TABLE Reports (
    ReportID int IDENTITY(1,1) PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    Type nvarchar(50) NOT NULL,
    Content nvarchar(max) NOT NULL,
    GeneratedDate datetime2 NOT NULL DEFAULT GETDATE(),
    Semester nvarchar(50) NULL,
    ClubID int NULL,
    GeneratedByUserID int NOT NULL,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE SET NULL,
    FOREIGN KEY (GeneratedByUserID) REFERENCES Users(UserID)
);
GO

-- Create AuditLogs table
CREATE TABLE AuditLogs (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int NULL,
    Action nvarchar(255) NOT NULL,
    Details nvarchar(2000) NOT NULL,
    LogType nvarchar(50) NOT NULL,
    IpAddress nvarchar(45) NULL,
    Timestamp datetime2 NOT NULL DEFAULT GETDATE(),
    AdditionalData nvarchar(max) NULL,
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
    Id nvarchar(450) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(500) NULL,
    TitleTemplate nvarchar(200) NOT NULL,
    MessageTemplate nvarchar(2000) NOT NULL,
    Type nvarchar(50) NOT NULL,
    Priority nvarchar(50) NOT NULL DEFAULT 'Normal',
    Category nvarchar(50) NOT NULL,
    ChannelsJson nvarchar(500) NULL,
    ParametersJson nvarchar(1000) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt datetime2 NULL
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
    Id nvarchar(450) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    NotificationRequest nvarchar(max) NOT NULL,
    ScheduledTime datetime2 NOT NULL,
    RecurrencePattern nvarchar(500) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL DEFAULT GETDATE(),
    LastProcessedAt datetime2 NULL,
    CancelledAt datetime2 NULL,
    ProcessCount int NOT NULL DEFAULT 0
);
GO

-- Create indexes for ScheduledNotifications
CREATE INDEX IX_ScheduledNotifications_ScheduledTime ON ScheduledNotifications (ScheduledTime);
CREATE INDEX IX_ScheduledNotifications_IsActive ON ScheduledNotifications (IsActive);
CREATE INDEX IX_ScheduledNotifications_Name ON ScheduledNotifications (Name);
GO

-- Create Notifications table
CREATE TABLE Notifications (
    Id nvarchar(450) PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    Message nvarchar(2000) NOT NULL,
    Type nvarchar(50) NOT NULL,
    Priority nvarchar(50) NOT NULL DEFAULT 'Normal',
    Category nvarchar(50) NOT NULL,
    UserID int NULL,
    ClubID int NULL,
    EventID int NULL,
    Data nvarchar(max) NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETDATE(),
    ExpiresAt datetime2 NULL,
    ReadAt datetime2 NULL,
    DeletedAt datetime2 NULL,
    IsRead bit NOT NULL DEFAULT 0,
    IsDeleted bit NOT NULL DEFAULT 0,
    ChannelsJson nvarchar(500) NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE NO ACTION ,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE NO ACTION ,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE NO ACTION 
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
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserID int NULL,
    ClubID int NULL,
    [Key] nvarchar(100) NOT NULL,
    Value nvarchar(max) NOT NULL,
    Scope nvarchar(50) NOT NULL,
    CreatedAt datetime2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt datetime2 NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE CASCADE
);
GO

-- Create unique index for Settings
CREATE UNIQUE INDEX IX_Settings_Key_Scope_UserID_ClubID ON Settings ([Key], Scope, UserID, ClubID);
CREATE INDEX IX_Settings_Scope ON Settings (Scope);
CREATE INDEX IX_Settings_CreatedAt ON Settings (CreatedAt);
GO

PRINT 'Database schema created successfully!';
GO

-- ========================================
-- DATA INSERTION SECTION
-- ========================================

-- Insert Clubs
INSERT INTO Clubs (Name, Description, ContactEmail, ContactPhone, Website, MeetingSchedule, FoundedDate, IsActive, IsSelected, CreatedDate) VALUES
('Computer Science Club', 'A club for computer science enthusiasts to learn and share knowledge about programming, algorithms, and technology.', 'cs.club@university.edu', '555-0101', 'https://csclub.university.edu', 'Fridays 3:00 PM - Room 201', '2020-09-15', 1, 0, '2025-01-15'),
('Drama Society', 'Dedicated to theatrical performances, script writing, and dramatic arts education.', 'drama@university.edu', '555-0102', 'https://drama.university.edu', 'Wednesdays 4:00 PM - Theater', '2019-03-20', 1, 0, '2025-01-20'),
('Environmental Club', 'Focused on environmental conservation, sustainability projects, and eco-friendly initiatives.', 'eco.club@university.edu', '555-0103', 'https://ecoclub.university.edu', 'Tuesdays 2:00 PM - Green Room', '2021-04-22', 1, 0, '2025-02-01'),
('Photography Club', 'For photography enthusiasts to improve skills, share techniques, and organize photo walks.', 'photo@university.edu', '555-0104', 'https://photoclub.university.edu', 'Saturdays 10:00 AM - Studio', '2020-11-10', 1, 0, '2025-02-10'),
('Debate Society', 'Enhancing public speaking, critical thinking, and argumentation skills through structured debates.', 'debate@university.edu', '555-0105', 'https://debate.university.edu', 'Thursdays 5:00 PM - Hall A', '2018-01-15', 1, 0, '2025-02-15'),
('Music Club', 'Bringing together musicians of all levels to perform, collaborate, and appreciate various music genres.', 'music@university.edu', '555-0106', 'https://musicclub.university.edu', 'Mondays 6:00 PM - Music Room', '2017-08-30', 1, 0, '2025-03-01'),
('Sports Club', 'Organizing various sports activities, tournaments, and promoting physical fitness among students.', 'sports@university.edu', '555-0107', 'https://sportsclub.university.edu', 'Daily 7:00 AM - Gym', '2016-05-12', 1, 0, '2025-03-05'),
('Literature Society', 'For book lovers, creative writers, and those passionate about literature and poetry.', 'literature@university.edu', '555-0108', 'https://litclub.university.edu', 'Sundays 3:00 PM - Library', '2019-10-05', 1, 0, '2025-03-10'),
('Science Innovation Lab', 'Encouraging scientific research, innovation projects, and STEM education initiatives.', 'scilab@university.edu', '555-0109', 'https://scilab.university.edu', 'Wednesdays 1:00 PM - Lab 301', '2021-02-28', 1, 0, '2025-03-15'),
('Cultural Heritage Club', 'Preserving and promoting cultural traditions, organizing cultural events and festivals.', 'culture@university.edu', '555-0110', 'https://culture.university.edu', 'Fridays 4:00 PM - Cultural Center', '2020-07-18', 1, 0, '2025-03-20');
GO

-- Insert Users (1 Admin, 2 Chairman, 5 Member)
INSERT INTO Users (FullName, Email, Password, StudentID, Role, ActivityLevel, JoinDate, IsActive, TwoFactorEnabled, ClubID) VALUES
-- Admin (1 account)
('System Administrator', 'admin@university.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', NULL, 'Admin', 'Active', '2025-01-01', 1, 1, NULL),

-- Chairman (2 accounts)
('Alice Johnson', 'alice.johnson@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'CS2025001', 'Chairman', 'Active', '2025-01-15', 1, 1, 1),
('Bob Smith', 'bob.smith@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'DR2025002', 'Chairman', 'Active', '2025-01-20', 1, 0, 2),

-- Member (5 accounts)
('Carol Davis', 'carol.davis@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'EN2025003', 'Member', 'Active', '2025-02-01', 1, 1, 3),
('David Wilson', 'david.wilson@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'PH2025004', 'Member', 'Normal', '2025-02-10', 1, 0, 4),
('Emma Brown', 'emma.brown@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'DB2025005', 'Member', 'Active', '2025-02-15', 1, 1, 5),
('Frank Miller', 'frank.miller@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'MU2025006', 'Member', 'Normal', '2025-03-01', 1, 0, 6),
('Grace Lee', 'grace.lee@student.edu', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'SP2025007', 'Member', 'Active', '2025-03-05', 1, 1, 7);
GO

-- Insert Events
INSERT INTO Events (Name, Description, EventDate, Location, CreatedDate, IsActive, RegistrationDeadline, MaxParticipants, ClubID) VALUES
-- Computer Science Club Events
('Python Workshop', 'Learn Python programming from basics to advanced concepts', '2025-12-15 14:00:00', 'Computer Lab A', '2025-11-01', 1, '2025-12-10', 30, 1),
('Hackathon 2025', '24-hour coding competition with exciting prizes', '2025-12-20 09:00:00', 'Main Auditorium', '2025-11-05', 1, '2025-12-15', 100, 1),
('AI and Machine Learning Seminar', 'Industry experts discuss the future of AI', '2026-01-10 16:00:00', 'Conference Room B', '2025-11-10', 1, '2026-01-05', 50, 1),

-- Photography Club Events
('Nature Photography Walk', 'Capture the beauty of campus nature', '2025-12-12 08:00:00', 'University Gardens', '2025-11-02', 1, '2025-12-08', 20, 4),
('Portrait Photography Workshop', 'Learn professional portrait techniques', '2025-12-18 13:00:00', 'Studio Room 1', '2025-11-06', 1, '2025-12-15', 15, 4),
('Photo Exhibition Setup', 'Prepare for the annual photo exhibition', '2026-01-15 10:00:00', 'Gallery Hall', '2025-11-12', 1, '2026-01-10', 25, 4),

-- Drama Society Events
('Romeo and Juliet Auditions', 'Auditions for the spring play', '2025-12-14 15:00:00', 'Theater Room', '2025-11-03', 1, '2025-12-12', 40, 2),
('Acting Workshop', 'Improve your acting skills with professional coaches', '2025-12-21 11:00:00', 'Drama Studio', '2025-11-07', 1, '2025-12-18', 25, 2),

-- Environmental Club Events
('Campus Cleanup Drive', 'Help keep our campus clean and green', '2025-12-13 07:00:00', 'Campus Grounds', '2025-11-04', 1, '2025-12-11', 50, 3),
('Sustainability Fair', 'Learn about sustainable living practices', '2025-12-19 12:00:00', 'Student Center', '2025-11-08', 1, '2025-12-16', 100, 3),

-- Music Club Events
('Winter Concert', 'Showcase of student musical talents', '2025-12-22 19:00:00', 'Music Hall', '2025-11-09', 1, '2025-12-20', 200, 6),
('Songwriting Workshop', 'Learn to write your own songs', '2026-01-08 14:00:00', 'Music Room 2', '2025-11-11', 1, '2026-01-05', 20, 6);
GO

-- Insert Event Participants
INSERT INTO EventParticipants (UserID, EventID, Status, RegistrationDate, AttendanceDate) VALUES
-- Python Workshop (EventID: 1)
(2, 1, 'Attended', '2025-11-15', '2025-12-15'), -- Alice (Chairman) - Attended
(3, 1, 'Registered', '2025-11-16', NULL), -- Bob (Chairman) - Registered
(4, 1, 'Registered', '2025-11-17', NULL), -- Carol (Member) - Registered
(5, 1, 'Absent', '2025-11-18', NULL), -- David (Member) - Absent

-- Hackathon 2025 (EventID: 2)
(2, 2, 'Registered', '2025-11-20', NULL), -- Alice (Chairman) - Registered
(3, 2, 'Registered', '2025-11-21', NULL), -- Bob (Chairman) - Registered
(4, 2, 'Registered', '2025-11-22', NULL), -- Carol (Member) - Registered
(5, 2, 'Registered', '2025-11-23', NULL), -- David (Member) - Registered
(6, 2, 'Registered', '2025-11-24', NULL), -- Emma (Member) - Registered

-- Nature Photography Walk (EventID: 4)
(5, 4, 'Attended', '2025-11-25', '2025-12-12'), -- David (Member) - Attended
(6, 4, 'Attended', '2025-11-26', '2025-12-12'), -- Emma (Member) - Attended
(7, 4, 'Attended', '2025-11-27', '2025-12-12'), -- Frank (Member) - Attended
(8, 4, 'Absent', '2025-11-28', NULL), -- Grace (Member) - Absent

-- Campus Cleanup Drive (EventID: 9)
(4, 9, 'Attended', '2025-11-29', '2025-12-13'), -- Carol (Member) - Attended
(5, 9, 'Attended', '2025-11-30', '2025-12-13'), -- David (Member) - Attended
(6, 9, 'Attended', '2025-12-01', '2025-12-13'), -- Emma (Member) - Attended
(2, 9, 'Attended', '2025-12-02', '2025-12-13'), -- Alice (Chairman) - Cross-club participation
(7, 9, 'Attended', '2025-12-03', '2025-12-13'); -- Frank (Member) - Cross-club participation
GO

-- Insert Reports
INSERT INTO Reports (Title, Type, Content, GeneratedDate, Semester, ClubID, GeneratedByUserID) VALUES
('Computer Science Club - Fall 2025 Member Statistics', 'MemberStatistics', '{"totalMembers": 1, "activeMembers": 1, "newMembers": 1, "membersByRole": {"Chairman": 1, "Member": 0}}', '2025-11-15', 'Fall 2025', 1, 2),
('Photography Club - Event Outcomes Report', 'EventOutcomes', '{"eventsHeld": 2, "totalParticipants": 7, "averageAttendance": 85, "topEvent": "Nature Photography Walk"}', '2025-11-20', 'Fall 2025', 4, 5),
('Environmental Club - Activity Tracking', 'ActivityTracking', '{"activitiesCompleted": 1, "volunteersParticipated": 5, "impactMetrics": {"wasteCollected": "50kg", "areasCleaned": 3}}', '2025-11-25', 'Fall 2025', 3, 4),
('University-wide Semester Summary', 'SemesterSummary', '{"totalClubs": 10, "totalMembers": 8, "totalEvents": 12, "overallEngagement": "High", "topPerformingClubs": ["Computer Science Club", "Photography Club", "Environmental Club"]}', '2025-11-30', 'Fall 2025', NULL, 1),
('Drama Society - Member Statistics', 'MemberStatistics', '{"totalMembers": 1, "activeMembers": 1, "upcomingProductions": 1, "auditionParticipants": 15}', '2025-12-01', 'Fall 2025', 2, 3),
('Music Club - Event Outcomes', 'EventOutcomes', '{"concertsPlanned": 1, "workshopsHeld": 0, "expectedAttendance": 200, "repertoireSize": 12}', '2025-12-05', 'Fall 2025', 6, 7);
GO

-- Insert Audit Logs
INSERT INTO AuditLogs (UserId, Action, Details, LogType, IpAddress, Timestamp, AdditionalData) VALUES
(1, 'User Login', 'Administrator logged into the system', 'UserActivity', '192.168.1.100', '2025-11-01 08:00:00', '{"userAgent": "Mozilla/5.0", "sessionId": "sess_001"}'),
(2, 'Club Created', 'Computer Science Club was created', 'DataChange', '192.168.1.101', '2025-01-15 10:30:00', '{"clubId": 1, "clubName": "Computer Science Club"}'),
(2, 'Event Created', 'Python Workshop event was created', 'UserActivity', '192.168.1.101', '2025-11-01 14:20:00', '{"eventId": 1, "eventName": "Python Workshop"}'),
(3, 'Event Registration', 'User registered for Python Workshop', 'UserActivity', '192.168.1.102', '2025-11-16 09:15:00', '{"eventId": 1, "userId": 3}'),
(5, 'Event Created', 'Nature Photography Walk event was created', 'UserActivity', '192.168.1.103', '2025-11-02 11:45:00', '{"eventId": 4, "eventName": "Nature Photography Walk"}'),
(1, 'Report Generated', 'University-wide semester summary report generated', 'DataChange', '192.168.1.100', '2025-11-30 16:00:00', '{"reportId": 4, "reportType": "SemesterSummary"}'),
(4, 'Event Attendance', 'User marked as attended for Campus Cleanup Drive', 'UserActivity', '192.168.1.104', '2025-12-13 07:30:00', '{"eventId": 9, "userId": 4, "status": "Attended"}'),
(1, 'System Maintenance', 'Database backup completed successfully', 'DataChange', '192.168.1.100', '2025-12-01 02:00:00', '{"backupSize": "2.5GB", "duration": "15 minutes"}'),
(3, 'User Profile Update', 'User updated their profile information', 'UserActivity', '192.168.1.105', '2025-11-28 13:22:00', '{"userId": 3, "fieldsUpdated": ["email", "phone"]}'),
(1, 'Security Event', 'Failed login attempt detected', 'SecurityEvent', '192.168.1.200', '2025-12-02 23:45:00', '{"attemptedEmail": "unknown@test.com", "attempts": 3}');
GO

-- Insert Notification Templates
INSERT INTO NotificationTemplates (Id, Name, Description, TitleTemplate, MessageTemplate, Type, Priority, Category, ChannelsJson, ParametersJson, IsActive, CreatedAt, UpdatedAt) VALUES
('welcome-template', 'Welcome New Member', 'Template for welcoming new club members', 'Welcome to {clubName}!', 'Hi {memberName}, welcome to {clubName}! We''re excited to have you join our community. Your journey with us starts now!', 'Welcome', 'Normal', 'ClubActivity', '["InApp", "Email"]', '["memberName", "clubName"]', 1, '2025-01-01', NULL),
('event-reminder', 'Event Reminder', 'Template for event reminders', 'Reminder: {eventName} Tomorrow', 'Don''t forget about {eventName} happening tomorrow at {eventTime} in {eventLocation}. We look forward to seeing you there!', 'EventReminder', 'Normal', 'ClubActivity', '["InApp", "Email"]', '["eventName", "eventTime", "eventLocation"]', 1, '2025-01-01', NULL),
('event-registration', 'Event Registration Confirmation', 'Template for event registration confirmations', 'Registration Confirmed: {eventName}', 'Your registration for {eventName} has been confirmed. Event details: Date: {eventDate}, Time: {eventTime}, Location: {eventLocation}', 'EventRegistration', 'Normal', 'ClubActivity', '["InApp", "Email"]', '["eventName", "eventDate", "eventTime", "eventLocation"]', 1, '2025-01-01', NULL),
('club-update', 'Club Update', 'Template for club announcements', 'Update from {clubName}', 'Hello {memberName}, here''s an important update from {clubName}: {updateMessage}', 'ClubUpdate', 'Normal', 'ClubActivity', '["InApp", "Email"]', '["clubName", "memberName", "updateMessage"]', 1, '2025-01-01', NULL),
('report-generated', 'Report Generated', 'Template for report generation notifications', 'New Report Available: {reportTitle}', 'A new report "{reportTitle}" has been generated and is now available for review. Generated on: {generatedDate}', 'ReportGenerated', 'Normal', 'Administrative', '["InApp"]', '["reportTitle", "generatedDate"]', 1, '2025-01-01', NULL);
GO

-- Insert Scheduled Notifications
INSERT INTO ScheduledNotifications (Id, Name, NotificationRequest, ScheduledTime, RecurrencePattern, IsActive, CreatedAt, LastProcessedAt, CancelledAt, ProcessCount) VALUES
('weekly-reminder', 'Weekly Event Reminders', '{"title": "Weekly Events Summary", "message": "Here are the upcoming events for this week", "type": 10, "priority": 1, "category": 1, "channels": [0, 1]}', '2025-12-09 09:00:00', 'WEEKLY', 1, '2025-11-01', NULL, NULL, 0),
('monthly-report', 'Monthly Club Reports', '{"title": "Monthly Club Activity Report", "message": "Your monthly club activity report is ready", "type": 9, "priority": 1, "category": 5, "channels": [0]}', '2025-12-31 23:59:00', 'MONTHLY', 1, '2025-11-01', NULL, NULL, 0),
('event-day-reminder', 'Event Day Reminders', '{"title": "Event Today!", "message": "Don''t forget about your event today", "type": 2, "priority": 2, "category": 1, "channels": [0, 1]}', '2025-12-15 08:00:00', 'DAILY', 1, '2025-11-01', NULL, NULL, 0);
GO

-- Insert Notifications
INSERT INTO Notifications (Id, Title, Message, Type, Priority, Category, UserID, ClubID, EventID, Data, CreatedAt, ExpiresAt, ReadAt, DeletedAt, IsRead, IsDeleted, ChannelsJson) VALUES
('notif-001', 'Welcome to Computer Science Club!', 'Hi Alice, welcome to Computer Science Club! We''re excited to have you join our community.', 'Welcome', 'Normal', 'ClubActivity', 2, 1, NULL, '{"welcomePackage": true}', '2025-01-16', NULL, '2025-01-16', NULL, 1, 0, '["InApp", "Email"]'),
('notif-002', 'Event Registration Confirmed', 'Your registration for Python Workshop has been confirmed.', 'EventRegistration', 'Normal', 'ClubActivity', 3, 1, 1, '{"registrationId": "reg-001"}', '2025-11-16', NULL, NULL, NULL, 0, 0, '["InApp", "Email"]'),
('notif-003', 'Reminder: Nature Photography Walk Tomorrow', 'Don''t forget about Nature Photography Walk happening tomorrow at 08:00 in University Gardens.', 'EventReminder', 'Normal', 'ClubActivity', 5, 4, 4, '{"weatherForecast": "sunny"}', '2025-12-11', '2025-12-13', '2025-12-11', NULL, 1, 0, '["InApp", "Email"]'),
('notif-004', 'New Report Available', 'A new report "Computer Science Club - Fall 2025 Member Statistics" has been generated.', 'ReportGenerated', 'Normal', 'Administrative', 2, 1, NULL, '{"reportId": 1}', '2025-11-15', NULL, NULL, NULL, 0, 0, '["InApp"]'),
('notif-005', 'Club Update from Environmental Club', 'Great job everyone on the Campus Cleanup Drive! We collected 50kg of waste.', 'ClubUpdate', 'Normal', 'ClubActivity', 4, 3, NULL, '{"achievement": "cleanup_success"}', '2025-12-13', NULL, '2025-12-13', NULL, 1, 0, '["InApp", "Email"]'),
('notif-006', 'Event Cancelled', 'Unfortunately, the outdoor event has been cancelled due to weather conditions.', 'EventCancellation', 'High', 'ClubActivity', 6, 4, NULL, '{"reason": "weather"}', '2025-12-10', NULL, NULL, NULL, 0, 0, '["InApp", "Email"]'),
('notif-007', 'Security Alert', 'Multiple failed login attempts detected on your account.', 'SecurityAlert', 'Critical', 'Security', 1, NULL, NULL, '{"attempts": 3, "lastAttempt": "2025-12-02T23:45:00"}', '2025-12-02', NULL, '2025-12-03', NULL, 1, 0, '["InApp", "Email"]'),
('notif-008', 'System Maintenance', 'Scheduled system maintenance will occur tonight from 2:00 AM to 4:00 AM.', 'SystemMaintenance', 'Normal', 'System', NULL, NULL, NULL, '{"duration": "2 hours"}', '2025-12-05', '2025-12-06', NULL, NULL, 0, 0, '["InApp"]');
GO

-- Insert Settings
INSERT INTO Settings (UserID, ClubID, [Key], Value, Scope, CreatedAt, UpdatedAt) VALUES
-- Global Settings
(NULL, NULL, 'system.maintenance_mode', 'false', 'Global', '2025-01-01', '2025-01-01'),
(NULL, NULL, 'system.max_file_upload_size', '10485760', 'Global', '2025-01-01', '2025-01-01'),
(NULL, NULL, 'notifications.email_enabled', 'true', 'Global', '2025-01-01', '2025-01-01'),
(NULL, NULL, 'security.session_timeout', '3600', 'Global', '2025-01-01', '2025-01-01'),
(NULL, NULL, 'backup.auto_backup_enabled', 'true', 'Global', '2025-01-01', '2025-01-01'),

-- User Settings
(2, NULL, 'notifications.email_frequency', 'daily', 'User', '2025-01-16', '2025-01-16'),
(2, NULL, 'ui.theme', 'light', 'User', '2025-01-16', '2025-01-16'),
(2, NULL, 'privacy.profile_visibility', 'club_members', 'User', '2025-01-16', '2025-01-16'),
(5, NULL, 'notifications.email_frequency', 'weekly', 'User', '2025-01-21', '2025-01-21'),
(5, NULL, 'ui.theme', 'dark', 'User', '2025-01-21', '2025-01-21'),
(4, NULL, 'notifications.push_enabled', 'true', 'User', '2025-02-11', '2025-02-11'),

-- Club Settings
(NULL, 1, 'events.auto_approval', 'false', 'Club', '2025-01-15', '2025-01-15'),
(NULL, 1, 'members.max_count', '100', 'Club', '2025-01-15', '2025-01-15'),
(NULL, 1, 'notifications.event_reminders', 'true', 'Club', '2025-01-15', '2025-01-15'),
(NULL, 2, 'events.auto_approval', 'true', 'Club', '2025-01-20', '2025-01-20'),
(NULL, 2, 'members.max_count', '50', 'Club', '2025-01-20', '2025-01-20'),
(NULL, 3, 'events.require_approval', 'false', 'Club', '2025-02-10', '2025-02-10'),
(NULL, 3, 'members.auto_accept', 'true', 'Club', '2025-02-10', '2025-02-10');
GO

PRINT 'Database seeded successfully!';
PRINT 'Summary:';
PRINT '- 10 Clubs created';
PRINT '- 8 Users created (1 Admin, 2 Chairman, 5 Member)';
PRINT '- 12 Events created across different clubs';
PRINT '- 15 Event Participants registered';
PRINT '- 6 Reports generated';
PRINT '- 10 Audit Log entries created';
PRINT '- 5 Notification Templates created';
PRINT '- 3 Scheduled Notifications created';
PRINT '- 8 Notifications created';
PRINT '- 17 Settings configured';
PRINT '';
PRINT 'Default Test Accounts (Password: admin123 for all):';
PRINT 'Admin: admin@university.edu';
PRINT 'Chairman: alice.johnson@student.edu (Computer Science Club)';
PRINT 'Chairman: bob.smith@student.edu (Drama Society)';
PRINT 'Member: carol.davis@student.edu (Environmental Club)';
PRINT 'Member: david.wilson@student.edu (Photography Club)';
PRINT 'Member: emma.brown@student.edu (Debate Society)';
PRINT 'Member: frank.miller@student.edu (Music Club)';
PRINT 'Member: grace.lee@student.edu (Sports Club)';
PRINT '';
PRINT 'All passwords are hashed versions of "admin123"';
PRINT 'Please change default passwords after first login for security.';