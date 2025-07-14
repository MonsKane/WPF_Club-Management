-- Database Cleanup Script for Club Management Application
-- This script fixes data type mismatches between existing data and the C# model expectations
-- Run this script before running the main database_seed_script.sql

USE ClubManagementDB;
GO

-- Check if we're in the correct database
IF DB_NAME() != 'ClubManagementDB'
BEGIN
    PRINT 'ERROR: Not connected to ClubManagementDB database. Please ensure USE ClubManagementDB; was executed successfully.';
    RETURN;
END
GO

PRINT 'Starting database cleanup for data type consistency...';
GO

-- Fix SystemRole values if they are stored as strings instead of integers
-- SystemRole enum: 0=Admin, 1=ClubOwner, 2=Member
IF EXISTS (SELECT 1 FROM Users WHERE TRY_CAST(SystemRole AS INT) IS NULL)
BEGIN
    PRINT 'Fixing SystemRole string values to integer enum values...';
    
    -- Update string values to integer enum values
    UPDATE Users SET SystemRole = 0 WHERE SystemRole = 'Admin' OR SystemRole = 'admin';
    UPDATE Users SET SystemRole = 1 WHERE SystemRole = 'ClubOwner' OR SystemRole = 'clubowner' OR SystemRole = 'Club Owner';
    UPDATE Users SET SystemRole = 2 WHERE SystemRole = 'Member' OR SystemRole = 'member';
    
    PRINT 'SystemRole values updated successfully.';
END
ELSE
BEGIN
    PRINT 'SystemRole values are already in correct integer format.';
END
GO

-- Fix ClubRole values if they are stored as strings instead of integers
-- ClubRole enum: 0=Admin, 1=Chairman, 2=Member
IF EXISTS (SELECT 1 FROM ClubMembers WHERE TRY_CAST(ClubRole AS INT) IS NULL)
BEGIN
    PRINT 'Fixing ClubRole string values to integer enum values...';
    
    -- Update string values to integer enum values
    UPDATE ClubMembers SET ClubRole = 0 WHERE ClubRole = 'Admin' OR ClubRole = 'admin';
    UPDATE ClubMembers SET ClubRole = 1 WHERE ClubRole = 'Chairman' OR ClubRole = 'chairman' OR ClubRole = 'Chair';
    UPDATE ClubMembers SET ClubRole = 2 WHERE ClubRole = 'Member' OR ClubRole = 'member';
    
    PRINT 'ClubRole values updated successfully.';
END
ELSE
BEGIN
    PRINT 'ClubRole values are already in correct integer format.';
END
GO

-- Fix ActivityLevel values if they are stored as strings instead of integers
-- ActivityLevel enum: 0=Active, 1=Normal, 2=Inactive
IF EXISTS (SELECT 1 FROM Users WHERE TRY_CAST(ActivityLevel AS INT) IS NULL)
BEGIN
    PRINT 'Fixing ActivityLevel string values to integer enum values...';
    
    -- Update string values to integer enum values
    UPDATE Users SET ActivityLevel = 0 WHERE ActivityLevel = 'Active' OR ActivityLevel = 'active';
    UPDATE Users SET ActivityLevel = 1 WHERE ActivityLevel = 'Normal' OR ActivityLevel = 'normal';
    UPDATE Users SET ActivityLevel = 2 WHERE ActivityLevel = 'Inactive' OR ActivityLevel = 'inactive';
    
    PRINT 'ActivityLevel values updated successfully.';
END
ELSE
BEGIN
    PRINT 'ActivityLevel values are already in correct integer format.';
END
GO

-- Fix AttendanceStatus values in EventParticipants if they are inconsistent
-- Expected values: 'Registered', 'Attended', 'Absent'
IF EXISTS (SELECT 1 FROM EventParticipants WHERE Status NOT IN ('Registered', 'Attended', 'Absent'))
BEGIN
    PRINT 'Fixing AttendanceStatus values to match enum expectations...';
    
    -- Standardize status values
    UPDATE EventParticipants SET Status = 'Registered' WHERE Status IN ('registered', 'Register', 'Pending');
    UPDATE EventParticipants SET Status = 'Attended' WHERE Status IN ('attended', 'Present', 'Confirmed');
    UPDATE EventParticipants SET Status = 'Absent' WHERE Status IN ('absent', 'No Show', 'Missing');
    
    PRINT 'AttendanceStatus values updated successfully.';
END
ELSE
BEGIN
    PRINT 'AttendanceStatus values are already in correct format.';
END
GO

-- Verify data integrity after cleanup
PRINT 'Verifying data integrity after cleanup...';
GO

-- Check SystemRole values
DECLARE @invalidSystemRoles INT;
SELECT @invalidSystemRoles = COUNT(*) FROM Users WHERE SystemRole NOT IN (0, 1, 2);
IF @invalidSystemRoles > 0
BEGIN
    PRINT 'WARNING: Found ' + CAST(@invalidSystemRoles AS NVARCHAR(10)) + ' users with invalid SystemRole values.';
    SELECT UserID, FullName, Email, SystemRole FROM Users WHERE SystemRole NOT IN (0, 1, 2);
END
ELSE
BEGIN
    PRINT 'All SystemRole values are valid.';
END
GO

-- Check ClubRole values
DECLARE @invalidClubRoles INT;
SELECT @invalidClubRoles = COUNT(*) FROM ClubMembers WHERE ClubRole NOT IN (0, 1, 2);
IF @invalidClubRoles > 0
BEGIN
    PRINT 'WARNING: Found ' + CAST(@invalidClubRoles AS NVARCHAR(10)) + ' club members with invalid ClubRole values.';
    SELECT ClubMemberID, UserID, ClubID, ClubRole FROM ClubMembers WHERE ClubRole NOT IN (0, 1, 2);
END
ELSE
BEGIN
    PRINT 'All ClubRole values are valid.';
END
GO

-- Check ActivityLevel values
DECLARE @invalidActivityLevels INT;
SELECT @invalidActivityLevels = COUNT(*) FROM Users WHERE ActivityLevel NOT IN (0, 1, 2);
IF @invalidActivityLevels > 0
BEGIN
    PRINT 'WARNING: Found ' + CAST(@invalidActivityLevels AS NVARCHAR(10)) + ' users with invalid ActivityLevel values.';
    SELECT UserID, FullName, Email, ActivityLevel FROM Users WHERE ActivityLevel NOT IN (0, 1, 2);
END
ELSE
BEGIN
    PRINT 'All ActivityLevel values are valid.';
END
GO

PRINT 'Database cleanup completed successfully!';
PRINT 'You can now run the main database_seed_script.sql safely.';
GO