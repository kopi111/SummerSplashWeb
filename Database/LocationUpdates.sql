-- =============================================
-- Location Feature Updates SQL Script
-- Run this on SummerSplashDB to add new fields
-- =============================================

-- Add new columns to JobLocations table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'LockboxCode')
BEGIN
    ALTER TABLE JobLocations ADD LockboxCode NVARCHAR(8) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'SupervisorId')
BEGIN
    ALTER TABLE JobLocations ADD SupervisorId INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'SupervisorName')
BEGIN
    ALTER TABLE JobLocations ADD SupervisorName NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'SupervisorPhone')
BEGIN
    ALTER TABLE JobLocations ADD SupervisorPhone NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'PoolDepthFeet')
BEGIN
    ALTER TABLE JobLocations ADD PoolDepthFeet INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'PoolDepthInches')
BEGIN
    ALTER TABLE JobLocations ADD PoolDepthInches INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'HasWadingPool')
BEGIN
    ALTER TABLE JobLocations ADD HasWadingPool BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'WadingPoolSizeGallons')
BEGIN
    ALTER TABLE JobLocations ADD WadingPoolSizeGallons INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'HasSpa')
BEGIN
    ALTER TABLE JobLocations ADD HasSpa BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'SpaSizeGallons')
BEGIN
    ALTER TABLE JobLocations ADD SpaSizeGallons INT NULL;
END
GO

-- Create LocationContacts table for multiple contacts per location
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'LocationContacts') AND type in (N'U'))
BEGIN
    CREATE TABLE LocationContacts (
        ContactId INT IDENTITY(1,1) PRIMARY KEY,
        LocationId INT NOT NULL,
        ContactName NVARCHAR(100) NULL,
        ContactPhone NVARCHAR(20) NULL,
        ContactEmail NVARCHAR(100) NULL,
        ContactRole NVARCHAR(50) NULL,  -- Property Manager, Regional Manager, Maintenance, Pool Manager
        IsPrimary BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_LocationContacts_Location FOREIGN KEY (LocationId)
            REFERENCES JobLocations(LocationId) ON DELETE CASCADE
    );

    -- Create index for faster lookups
    CREATE INDEX IX_LocationContacts_LocationId ON LocationContacts(LocationId);
END
GO

PRINT 'Location updates completed successfully!'
