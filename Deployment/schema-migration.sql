-- =============================================
-- Club Management Application - Schema Migration Script
-- Version: 1.1.0
-- Description: Migrates existing databases to correct Club table schema
-- =============================================

USE ClubManagementDB;
GO

PRINT 'Starting Club Management Database Schema Migration...';
GO

-- Check if migration is needed
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'Name')
BEGIN
    PRINT 'Old schema detected. Starting migration...';

    -- Step 1: Add missing columns with temporary names
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'ClubName')
    BEGIN
        ALTER TABLE Clubs ADD ClubName_temp NVARCHAR(100);
        PRINT 'Added ClubName_temp column';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'CreatedUserId')
    BEGIN
        ALTER TABLE Clubs ADD CreatedUserId INT;
        PRINT 'Added CreatedUserId column';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'CreatedAt')
    BEGIN
        ALTER TABLE Clubs ADD CreatedAt_temp DATETIME2;
        PRINT 'Added CreatedAt_temp column';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'MeetingSchedule')
    BEGIN
        ALTER TABLE Clubs ADD MeetingSchedule NVARCHAR(200);
        PRINT 'Added MeetingSchedule column';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'ContactEmail')
    BEGIN
        ALTER TABLE Clubs ADD ContactEmail NVARCHAR(150);
        PRINT 'Added ContactEmail column';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'ContactPhone')
    BEGIN
        ALTER TABLE Clubs ADD ContactPhone NVARCHAR(20);
        PRINT 'Added ContactPhone column';
    END

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'Website')
    BEGIN
        ALTER TABLE Clubs ADD Website NVARCHAR(200);
        PRINT 'Added Website column';
    END

    -- Step 2: Migrate data from old columns to new columns
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'Name')
    BEGIN
        UPDATE Clubs SET ClubName_temp = Name WHERE ClubName_temp IS NULL;
        PRINT 'Migrated data from Name to ClubName_temp';
    END

    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'CreatedDate')
    BEGIN
        UPDATE Clubs SET CreatedAt_temp = CreatedDate WHERE CreatedAt_temp IS NULL;
        PRINT 'Migrated data from CreatedDate to CreatedAt_temp';
    END

    -- Set default CreatedUserId to Admin user (UserID = 1) if not set
    UPDATE Clubs SET CreatedUserId = 1 WHERE CreatedUserId IS NULL OR CreatedUserId = 0;
    PRINT 'Set default CreatedUserId to Admin user';

    -- Set default CreatedAt if not set
    UPDATE Clubs SET CreatedAt_temp = GETUTCDATE() WHERE CreatedAt_temp IS NULL;
    PRINT 'Set default CreatedAt values';

    -- Step 3: Drop old constraints and indexes
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Clubs_Name' AND object_id = OBJECT_ID('Clubs'))
    BEGIN
        DROP INDEX IX_Clubs_Name ON Clubs;
        PRINT 'Dropped old IX_Clubs_Name index';
    END

    -- Step 4: Remove old columns
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'Name')
    BEGIN
        ALTER TABLE Clubs DROP COLUMN Name;
        PRINT 'Dropped old Name column';
    END

    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'CreatedDate')
    BEGIN
        ALTER TABLE Clubs DROP COLUMN CreatedDate;
        PRINT 'Dropped old CreatedDate column';
    END

    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'UpdatedDate')
    BEGIN
        ALTER TABLE Clubs DROP COLUMN UpdatedDate;
        PRINT 'Dropped old UpdatedDate column';
    END

    -- Step 5: Rename temporary columns to correct names
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'ClubName_temp')
    BEGIN
        EXEC sp_rename 'Clubs.ClubName_temp', 'ClubName', 'COLUMN';
        PRINT 'Renamed ClubName_temp to ClubName';
    END

    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'CreatedAt_temp')
    BEGIN
        EXEC sp_rename 'Clubs.CreatedAt_temp', 'CreatedAt', 'COLUMN';
        PRINT 'Renamed CreatedAt_temp to CreatedAt';
    END

    -- Step 6: Add constraints and indexes
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_Clubs_CreatedUserId')
    BEGIN
        ALTER TABLE Clubs ADD CONSTRAINT FK_Clubs_CreatedUserId FOREIGN KEY (CreatedUserId) REFERENCES Users(UserID);
        PRINT 'Added FK_Clubs_CreatedUserId foreign key constraint';
    END

    -- Make required columns NOT NULL
    ALTER TABLE Clubs ALTER COLUMN ClubName NVARCHAR(100) NOT NULL;
    ALTER TABLE Clubs ALTER COLUMN CreatedUserId INT NOT NULL;
    ALTER TABLE Clubs ALTER COLUMN CreatedAt DATETIME2 NOT NULL;
    PRINT 'Set required columns to NOT NULL';

    -- Create unique index on ClubName
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Clubs_ClubName' AND object_id = OBJECT_ID('Clubs'))
    BEGIN
        CREATE UNIQUE INDEX IX_Clubs_ClubName ON Clubs (ClubName);
        PRINT 'Created unique index on ClubName';
    END

    -- Step 7: Update EstablishedDate type if needed
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clubs' AND COLUMN_NAME = 'EstablishedDate' AND DATA_TYPE = 'date')
    BEGIN
        ALTER TABLE Clubs ALTER COLUMN EstablishedDate DATETIME2;
        PRINT 'Updated EstablishedDate to DATETIME2';
    END

    PRINT 'Schema migration completed successfully!';
END
ELSE
BEGIN
    PRINT 'Schema is already up-to-date. No migration needed.';
END
GO

-- Verify the final schema
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Clubs'
ORDER BY ORDINAL_POSITION;

PRINT 'Schema migration script completed.';
GO
