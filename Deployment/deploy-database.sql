-- =============================================
-- Club Management Application - Database Deployment Script
-- Version: 1.0.0
-- Description: Creates database schema and initial data
-- =============================================

-- Set database context
USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ClubManagementDB')
BEGIN
    CREATE DATABASE ClubManagementDB;
    PRINT 'Database ClubManagementDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database ClubManagementDB already exists.';
END
GO

-- Switch to the application database
USE ClubManagementDB;
GO

-- Create application user if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'clubapp_user')
BEGIN
    CREATE USER clubapp_user FOR LOGIN clubapp_user;
    PRINT 'User clubapp_user created successfully.';
END
GO

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER clubapp_user;
ALTER ROLE db_datawriter ADD MEMBER clubapp_user;
ALTER ROLE db_ddladmin ADD MEMBER clubapp_user;
GO

PRINT 'Permissions granted to clubapp_user.';
GO

-- Create tables if they don't exist
-- Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        UserID int IDENTITY(1,1) PRIMARY KEY,
        FullName nvarchar(100) NOT NULL,
        Email nvarchar(255) NOT NULL UNIQUE,
        Password nvarchar(255) NOT NULL,
        StudentID nvarchar(20) NULL,
        PhoneNumber nvarchar(20) NULL,
        SystemRole int NOT NULL DEFAULT 2 CHECK (SystemRole IN (0, 1, 2)),
        ActivityLevel int NOT NULL DEFAULT 0 CHECK (ActivityLevel IN (0, 1, 2)),
        ClubID int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        TwoFactorEnabled bit NOT NULL DEFAULT 0,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Users table created.';
END
GO

-- Clubs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Clubs' AND xtype='U')
BEGIN
    CREATE TABLE Clubs (
        ClubID int IDENTITY(1,1) PRIMARY KEY,
        ClubName nvarchar(100) NOT NULL UNIQUE,
        Description nvarchar(max) NULL,
        EstablishedDate datetime2 NULL,
        CreatedUserId int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        IsActive bit NOT NULL DEFAULT 1,
        MeetingSchedule nvarchar(200) NULL,
        ContactEmail nvarchar(150) NULL,
        ContactPhone nvarchar(20) NULL,
        Website nvarchar(200) NULL,
        CONSTRAINT FK_Clubs_CreatedUserId FOREIGN KEY (CreatedUserId) REFERENCES Users(UserID)
    );
    PRINT 'Clubs table created.';
END
GO

-- ClubMembers table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClubMembers' AND xtype='U')
BEGIN
    CREATE TABLE ClubMembers (
        ClubMemberID int IDENTITY(1,1) PRIMARY KEY,
        UserID int NOT NULL,
        ClubID int NOT NULL,
        ClubRole int NOT NULL CHECK (ClubRole IN (0, 1, 2)),
        JoinDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        IsActive bit NOT NULL DEFAULT 1,
        FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
        FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE CASCADE,
        UNIQUE(UserID, ClubID)
    );
    PRINT 'ClubMembers table created.';
END
GO

-- Events table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Events' AND xtype='U')
BEGIN
    CREATE TABLE Events (
        EventID int IDENTITY(1,1) PRIMARY KEY,
        Title nvarchar(100) NOT NULL,
        Description nvarchar(1000),
        EventDate datetime2 NOT NULL,
        Location nvarchar(200),
        MaxParticipants int,
        Status nvarchar(50) NOT NULL DEFAULT 'Scheduled',
        ClubID int NOT NULL,
        CreatedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedDate datetime2 NULL,
        IsActive bit NOT NULL DEFAULT 1,
        FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE CASCADE
    );
    PRINT 'Events table created.';
END
GO

-- EventParticipants table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EventParticipants' AND xtype='U')
BEGIN
    CREATE TABLE EventParticipants (
        ParticipantID int IDENTITY(1,1) PRIMARY KEY,
        UserID int NOT NULL,
        EventID int NOT NULL,
        Status nvarchar(50) NOT NULL DEFAULT 'Registered',
        RegistrationDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        AttendanceDate datetime2 NULL,
        FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
        FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE,
        UNIQUE(UserID, EventID)
    );
    PRINT 'EventParticipants table created.';
END
GO

-- Reports table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Reports' AND xtype='U')
BEGIN
    CREATE TABLE Reports (
        ReportID int IDENTITY(1,1) PRIMARY KEY,
        Title nvarchar(200) NOT NULL,
        Type nvarchar(50) NOT NULL,
        Content nvarchar(max),
        GeneratedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        Semester nvarchar(20),
        ClubID int NULL,
        GeneratedByUserID int NOT NULL,
        FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE SET NULL,
        FOREIGN KEY (GeneratedByUserID) REFERENCES Users(UserID)
    );
    PRINT 'Reports table created.';
END
GO

-- AuditLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
BEGIN
    CREATE TABLE AuditLogs (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NULL,
        Action nvarchar(255) NOT NULL,
        Details nvarchar(2000),
        LogType nvarchar(50) NOT NULL,
        IpAddress nvarchar(45),
        Timestamp datetime2 NOT NULL DEFAULT GETUTCDATE(),
        AdditionalData nvarchar(max),
        FOREIGN KEY (UserId) REFERENCES Users(UserID) ON DELETE SET NULL
    );
    PRINT 'AuditLogs table created.';
END
GO

-- Settings table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Settings' AND xtype='U')
BEGIN
    CREATE TABLE Settings (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NULL,
        ClubId int NULL,
        [Key] nvarchar(100) NOT NULL,
        Value nvarchar(max) NOT NULL,
        Scope nvarchar(50) NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NULL,
        FOREIGN KEY (UserId) REFERENCES Users(UserID) ON DELETE CASCADE,
        FOREIGN KEY (ClubId) REFERENCES Clubs(ClubID) ON DELETE CASCADE
    );
    PRINT 'Settings table created.';
END
GO

-- Notifications table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
    CREATE TABLE Notifications (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId int NOT NULL,
        ClubId int NULL,
        EventId int NULL,
        Title nvarchar(200) NOT NULL,
        Message nvarchar(2000) NOT NULL,
        Type nvarchar(50) NOT NULL,
        Priority nvarchar(50) NOT NULL,
        Category nvarchar(50) NOT NULL,
        IsRead bit NOT NULL DEFAULT 0,
        ReadAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ScheduledFor datetime2 NULL,
        ExpiresAt datetime2 NULL,
        ChannelsJson nvarchar(500),
        FOREIGN KEY (UserId) REFERENCES Users(UserID) ON DELETE CASCADE,
        FOREIGN KEY (ClubId) REFERENCES Clubs(ClubID) ON DELETE SET NULL,
        FOREIGN KEY (EventId) REFERENCES Events(EventID) ON DELETE SET NULL
    );
    PRINT 'Notifications table created.';
END
GO

-- NotificationTemplates table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NotificationTemplates' AND xtype='U')
BEGIN
    CREATE TABLE NotificationTemplates (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Name nvarchar(100) NOT NULL UNIQUE,
        Description nvarchar(500),
        Type nvarchar(50) NOT NULL,
        Priority nvarchar(50) NOT NULL,
        Category nvarchar(50) NOT NULL,
        TitleTemplate nvarchar(200) NOT NULL,
        MessageTemplate nvarchar(2000) NOT NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NULL,
        ChannelsJson nvarchar(500),
        ParametersJson nvarchar(1000)
    );
    PRINT 'NotificationTemplates table created.';
END
GO

-- ScheduledNotifications table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ScheduledNotifications' AND xtype='U')
BEGIN
    CREATE TABLE ScheduledNotifications (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        NotificationRequest nvarchar(max) NOT NULL,
        ScheduledFor datetime2 NOT NULL,
        IsProcessed bit NOT NULL DEFAULT 0,
        ProcessedAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ErrorMessage nvarchar(1000) NULL,
        RetryCount int NOT NULL DEFAULT 0
    );
    PRINT 'ScheduledNotifications table created.';
END
GO

-- Create indexes for performance
PRINT 'Creating indexes...';

-- Users indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
    CREATE INDEX IX_Users_Email ON Users(Email);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_ClubID')
    CREATE INDEX IX_Users_ClubID ON Users(ClubID);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_SystemRole')
    CREATE INDEX IX_Users_SystemRole ON Users(SystemRole);

-- ClubMembers indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClubMembers_UserID')
    CREATE INDEX IX_ClubMembers_UserID ON ClubMembers(UserID);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClubMembers_ClubID')
    CREATE INDEX IX_ClubMembers_ClubID ON ClubMembers(ClubID);

-- Events indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_ClubID')
    CREATE INDEX IX_Events_ClubID ON Events(ClubID);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_EventDate')
    CREATE INDEX IX_Events_EventDate ON Events(EventDate);

-- EventParticipants indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EventParticipants_UserID')
    CREATE INDEX IX_EventParticipants_UserID ON EventParticipants(UserID);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EventParticipants_EventID')
    CREATE INDEX IX_EventParticipants_EventID ON EventParticipants(EventID);

-- AuditLogs indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_Timestamp')
    CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_UserId')
    CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);

-- Notifications indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_UserId')
    CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_CreatedAt')
    CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_IsRead')
    CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);

PRINT 'Indexes created successfully.';
GO

-- Add foreign key constraint for Users.ClubID if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_Clubs')
BEGIN
    ALTER TABLE Users ADD CONSTRAINT FK_Users_Clubs
        FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID) ON DELETE SET NULL;
    PRINT 'Foreign key constraint FK_Users_Clubs added.';
END
GO

PRINT 'Database deployment completed successfully!';
PRINT 'Database: ClubManagementDB';
PRINT 'Version: 1.0.0';
PRINT 'Deployment Date: ' + CONVERT(varchar, GETDATE(), 120);
GO
