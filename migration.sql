-- Migration script for SummerSplash database updates
-- Run this script to add new columns for the updated features

-- =============================================
-- 1. Add Status column to Users table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'Status')
BEGIN
    ALTER TABLE Users ADD Status NVARCHAR(50) DEFAULT 'Pending';
    PRINT 'Added Status column to Users table';

    -- Update existing records based on IsApproved and IsActive
    UPDATE Users SET Status =
        CASE
            WHEN IsApproved = 1 AND IsActive = 1 THEN 'Approved'
            WHEN IsApproved = 1 AND IsActive = 0 THEN 'Terminated'
            ELSE 'Pending'
        END;
    PRINT 'Updated existing user statuses';
END
ELSE
    PRINT 'Status column already exists in Users table';

-- =============================================
-- 2. Add new columns to JobLocations table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Country')
BEGIN
    ALTER TABLE JobLocations ADD Country NVARCHAR(100) DEFAULT 'USA';
    PRINT 'Added Country column to JobLocations table';
END
ELSE
    PRINT 'Country column already exists in JobLocations table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Latitude')
BEGIN
    ALTER TABLE JobLocations ADD Latitude FLOAT NULL;
    PRINT 'Added Latitude column to JobLocations table';
END
ELSE
    PRINT 'Latitude column already exists in JobLocations table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Longitude')
BEGIN
    ALTER TABLE JobLocations ADD Longitude FLOAT NULL;
    PRINT 'Added Longitude column to JobLocations table';
END
ELSE
    PRINT 'Longitude column already exists in JobLocations table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Radius')
BEGIN
    ALTER TABLE JobLocations ADD Radius INT DEFAULT 100;
    PRINT 'Added Radius column to JobLocations table';
END
ELSE
    PRINT 'Radius column already exists in JobLocations table';

-- =============================================
-- 3. Add new columns to ClockRecords table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ClockRecords') AND name = 'ScheduleId')
BEGIN
    ALTER TABLE ClockRecords ADD ScheduleId INT NULL;
    PRINT 'Added ScheduleId column to ClockRecords table';
END
ELSE
    PRINT 'ScheduleId column already exists in ClockRecords table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ClockRecords') AND name = 'IsLate')
BEGIN
    ALTER TABLE ClockRecords ADD IsLate BIT DEFAULT 0;
    PRINT 'Added IsLate column to ClockRecords table';
END
ELSE
    PRINT 'IsLate column already exists in ClockRecords table';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ClockRecords') AND name = 'LateMinutes')
BEGIN
    ALTER TABLE ClockRecords ADD LateMinutes INT NULL;
    PRINT 'Added LateMinutes column to ClockRecords table';
END
ELSE
    PRINT 'LateMinutes column already exists in ClockRecords table';

-- =============================================
-- 4. Update existing JobLocations with default Country
-- =============================================
UPDATE JobLocations SET Country = 'USA' WHERE Country IS NULL;
PRINT 'Updated existing locations with default country';

-- =============================================
-- 5. Update existing ClockRecords with default values
-- =============================================
UPDATE ClockRecords SET IsLate = 0 WHERE IsLate IS NULL;
PRINT 'Updated existing clock records with default IsLate value';

PRINT '';
PRINT '===========================================';
PRINT 'Migration completed successfully!';
PRINT '===========================================';
