-- Club Management Database Diagnostic Script
-- Check ClubMembers table and related data

USE ClubManagementDB;
GO

PRINT '=== Club Management Database Diagnostic ===';
PRINT '';

-- Check if ClubMembers table exists
IF OBJECT_ID('ClubMembers', 'U') IS NOT NULL
BEGIN
    PRINT '✓ ClubMembers table exists';

    -- Check ClubMembers table structure
    PRINT '';
    PRINT '--- ClubMembers Table Structure ---';
    SELECT
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClubMembers'
    ORDER BY ORDINAL_POSITION;

    -- Check ClubMembers data count
    DECLARE @ClubMembersCount INT;
    SELECT @ClubMembersCount = COUNT(*) FROM ClubMembers;
    PRINT '';
    PRINT '--- ClubMembers Data Count ---';
    PRINT 'Total ClubMembers records: ' + CAST(@ClubMembersCount AS VARCHAR(10));

    -- Show ClubMembers data by club
    PRINT '';
    PRINT '--- ClubMembers by Club ---';
    SELECT
        c.ClubID,
        c.ClubName,
        COUNT(cm.ClubMemberID) as MemberCount,
        COUNT(CASE WHEN cm.IsActive = 1 THEN 1 END) as ActiveMemberCount
    FROM Clubs c
    LEFT JOIN ClubMembers cm ON c.ClubID = cm.ClubID
    GROUP BY c.ClubID, c.ClubName
    ORDER BY c.ClubID;

    -- Show detailed ClubMembers data
    PRINT '';
    PRINT '--- Detailed ClubMembers Data ---';
    SELECT
        cm.ClubMemberID,
        cm.UserID,
        u.FullName,
        u.Email,
        cm.ClubID,
        c.ClubName,
        cm.ClubRole,
        CASE cm.ClubRole
            WHEN 0 THEN 'Admin'
            WHEN 1 THEN 'Chairman'
            WHEN 2 THEN 'Member'
            ELSE 'Unknown'
        END as ClubRoleName,
        cm.IsActive,
        cm.JoinDate
    FROM ClubMembers cm
    INNER JOIN Users u ON cm.UserID = u.UserID
    INNER JOIN Clubs c ON cm.ClubID = c.ClubID
    ORDER BY cm.ClubID, cm.ClubRole, u.FullName;
END
ELSE
BEGIN
    PRINT '✗ ClubMembers table does not exist';

    -- Check if we need to create it
    PRINT '';
    PRINT '--- Creating ClubMembers Table ---';

    CREATE TABLE ClubMembers (
        ClubMemberID INT PRIMARY KEY IDENTITY(1,1),
        UserID INT NOT NULL,
        ClubID INT NOT NULL,
        ClubRole INT NOT NULL CHECK (ClubRole IN (0, 1, 2)),
        IsActive BIT NOT NULL DEFAULT 1,
        JoinDate DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserID) REFERENCES Users(UserID),
        FOREIGN KEY (ClubID) REFERENCES Clubs(ClubID)
    );

    -- Create unique index to ensure a user can only have one role per club
    CREATE UNIQUE INDEX IX_ClubMembers_UserID_ClubID ON ClubMembers (UserID, ClubID);

    PRINT '✓ ClubMembers table created successfully';
END
GO

-- Check Users table data
PRINT '';
PRINT '--- Users Table Data ---';
SELECT
    UserID,
    FullName,
    Email,
    SystemRole,
    CASE SystemRole
        WHEN 0 THEN 'Admin'
        WHEN 1 THEN 'ClubOwner'
        WHEN 2 THEN 'Member'
        ELSE 'Unknown'
    END as SystemRoleName,
    ClubID,
    IsActive
FROM Users
ORDER BY UserID;

-- Check Clubs table data
PRINT '';
PRINT '--- Clubs Table Data ---';
SELECT
    ClubID,
    ClubName,
    Description,
    EstablishedDate,
    CreatedUserId,
    IsActive
FROM Clubs
ORDER BY ClubID;

-- If ClubMembers table is empty, populate it with sample data
IF OBJECT_ID('ClubMembers', 'U') IS NOT NULL
BEGIN
    DECLARE @ExistingClubMembersCount INT;
    SELECT @ExistingClubMembersCount = COUNT(*) FROM ClubMembers;

    IF @ExistingClubMembersCount = 0
    BEGIN
        PRINT '';
        PRINT '--- Populating ClubMembers with Sample Data ---';

        -- Insert sample club memberships
        INSERT INTO ClubMembers (UserID, ClubID, ClubRole, IsActive, JoinDate) VALUES
        -- Computer Science Club (ClubID: 1)
        (3, 1, 0, 1, '2024-01-15 10:30:00'),    -- Alice Johnson as Club Admin
        (6, 1, 2, 1, '2024-05-01 14:20:00'),   -- Kate Williams as Member
        (8, 1, 2, 1, '2024-04-01 09:45:00'),   -- Kevin Martinez as Member

        -- Music Club (ClubID: 2)
        (4, 2, 1, 1, '2024-03-01 09:15:00'),   -- Michael Chen as Chairman
        (9, 2, 2, 1, '2024-04-15 16:30:00'),   -- Member
        (10, 2, 2, 1, '2024-04-20 11:10:00'),  -- Member

        -- Drama Society (ClubID: 3)
        (5, 3, 0, 1, '2024-01-20 11:45:00'),   -- Bob Smith as Club Admin
        (11, 3, 2, 1, '2024-05-05 13:25:00'),  -- Member

        -- Environmental Club (ClubID: 4)
        (3, 4, 0, 1, '2024-02-01 08:00:00'),   -- Alice Johnson as Club Admin (multi-club)
        (12, 4, 1, 1, '2024-05-10 15:30:00'),  -- Member as Chairman
        (13, 4, 2, 1, '2024-05-15 12:45:00'),  -- Member

        -- Science Innovation Lab (ClubID: 5)
        (7, 5, 1, 1, '2024-03-15 10:20:00'),   -- Lisa Thompson as Chairman
        (14, 5, 2, 1, '2024-05-25 14:15:00'),  -- Member
        (15, 5, 2, 1, '2024-06-05 16:40:00');  -- Member

        PRINT '✓ Sample ClubMembers data inserted successfully';

        -- Show the inserted data
        SELECT COUNT(*) as 'Total ClubMembers Inserted' FROM ClubMembers;
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT '--- ClubMembers table already has data ---';
    END
END
GO

PRINT '';
PRINT '=== Diagnostic Complete ===';
