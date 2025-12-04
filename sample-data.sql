-- ================================================
-- SAMPLE DATA FOR SUMMERSPLASH WEB
-- Position-Based Reports
-- ================================================

-- Create SiteEvaluations table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SiteEvaluations')
BEGIN
    CREATE TABLE SiteEvaluations (
        EvaluationId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        LocationId INT NOT NULL,
        ClockRecordId INT NULL,
        EvaluationType NVARCHAR(50) NOT NULL,
        EvaluationDate DATETIME NOT NULL,

        -- Safety Equipment Checklist
        PoolOpen BIT NULL,
        MainDrainVisible BIT NULL,
        AEDPresent BIT NULL,
        RescueTubePresent BIT NULL,
        BackboardPresent BIT NULL,
        FirstAidKit BIT NULL,
        BloodbornePathogenKit BIT NULL,
        HazMatKit BIT NULL,
        GateFenceSecured BIT NULL,
        EmergencyPhoneWorking BIT NULL,
        MSDSSafetySuppliesNeeded BIT NULL,

        -- Supervisor Checklist
        StaffOnDuty BIT NULL,
        ScanningRotationDiscussed BIT NULL,
        ZonesEstablished BIT NULL,
        BreakTimeDiscussed BIT NULL,
        GateControlDiscussed BIT NULL,
        CellphonePolicyDiscussed BIT NULL,
        PumproomCleaned BIT NULL,
        BalancingChemicalsTestedLogged BIT NULL,
        ClosingProceduresDiscussed BIT NULL,

        -- Manager Checklist
        StaffWearingUniform BIT NULL,

        -- Safety Audit
        FacilityEntryProcedures BIT NULL,
        MSDS BIT NULL,
        SafetySuppliesNeeded BIT NULL,

        -- Notes
        SafetyConcernsNotes NVARCHAR(MAX) NULL,
        Notes NVARCHAR(MAX) NULL,

        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    );
END

-- Add new columns to ServiceTechReports if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'Flowrate')
    ALTER TABLE ServiceTechReports ADD Flowrate DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'FilterPressure')
    ALTER TABLE ServiceTechReports ADD FilterPressure DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'WaterTemp')
    ALTER TABLE ServiceTechReports ADD WaterTemp DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'ControllerORP')
    ALTER TABLE ServiceTechReports ADD ControllerORP DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'ControllerPH')
    ALTER TABLE ServiceTechReports ADD ControllerPH DECIMAL(10,2) NULL;

-- Insert Sample Site Evaluations (Supervisor)
DECLARE @SupervisorId INT, @ManagerId INT, @LocationId1 INT, @LocationId2 INT;

-- Get a supervisor user (or use first user)
SELECT TOP 1 @SupervisorId = UserId FROM Users WHERE Position = 'Supervisor';
IF @SupervisorId IS NULL SELECT TOP 1 @SupervisorId = UserId FROM Users;

-- Get a manager user
SELECT TOP 1 @ManagerId = UserId FROM Users WHERE Position = 'Manager';
IF @ManagerId IS NULL SET @ManagerId = @SupervisorId;

-- Get locations
SELECT TOP 1 @LocationId1 = LocationId FROM JobLocations WHERE IsActive = 1;
SELECT TOP 1 @LocationId2 = LocationId FROM JobLocations WHERE IsActive = 1 AND LocationId <> @LocationId1;
IF @LocationId2 IS NULL SET @LocationId2 = @LocationId1;

-- Only insert if we have valid IDs
IF @SupervisorId IS NOT NULL AND @LocationId1 IS NOT NULL
BEGIN
    -- Sample Supervisor Evaluations
    IF NOT EXISTS (SELECT 1 FROM SiteEvaluations WHERE EvaluationType = 'Supervisor')
    BEGIN
        INSERT INTO SiteEvaluations (UserId, LocationId, EvaluationType, EvaluationDate,
            PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent, BackboardPresent,
            FirstAidKit, BloodbornePathogenKit, HazMatKit, GateFenceSecured, EmergencyPhoneWorking,
            StaffOnDuty, ScanningRotationDiscussed, ZonesEstablished, BreakTimeDiscussed,
            GateControlDiscussed, CellphonePolicyDiscussed, PumproomCleaned,
            BalancingChemicalsTestedLogged, ClosingProceduresDiscussed, CreatedAt)
        VALUES
        (@SupervisorId, @LocationId1, 'Supervisor', DATEADD(day, -1, GETDATE()),
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, DATEADD(day, -1, GETDATE())),
        (@SupervisorId, @LocationId2, 'Supervisor', DATEADD(day, -3, GETDATE()),
            1, 1, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, DATEADD(day, -3, GETDATE()));
    END

    -- Sample Manager Evaluations
    IF NOT EXISTS (SELECT 1 FROM SiteEvaluations WHERE EvaluationType = 'Manager')
    BEGIN
        INSERT INTO SiteEvaluations (UserId, LocationId, EvaluationType, EvaluationDate,
            PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent, BackboardPresent,
            FirstAidKit, BloodbornePathogenKit, HazMatKit, GateFenceSecured, EmergencyPhoneWorking,
            StaffWearingUniform, StaffOnDuty, SafetyConcernsNotes, CreatedAt)
        VALUES
        (@ManagerId, @LocationId1, 'Manager', DATEADD(day, -2, GETDATE()),
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, NULL, DATEADD(day, -2, GETDATE())),
        (@ManagerId, @LocationId2, 'Manager', DATEADD(day, -5, GETDATE()),
            1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 'Staff uniforms need replacement', DATEADD(day, -5, GETDATE()));
    END

    -- Sample Safety Audits
    IF NOT EXISTS (SELECT 1 FROM SiteEvaluations WHERE EvaluationType = 'Safety Audit')
    BEGIN
        INSERT INTO SiteEvaluations (UserId, LocationId, EvaluationType, EvaluationDate,
            PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent, BackboardPresent,
            FirstAidKit, BloodbornePathogenKit, HazMatKit, GateFenceSecured, EmergencyPhoneWorking,
            FacilityEntryProcedures, MSDS, SafetySuppliesNeeded, SafetyConcernsNotes, CreatedAt)
        VALUES
        (@SupervisorId, @LocationId1, 'Safety Audit', DATEADD(day, -1, GETDATE()),
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, NULL, DATEADD(day, -1, GETDATE())),
        (@SupervisorId, @LocationId2, 'Safety Audit', DATEADD(day, -4, GETDATE()),
            1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 'Main drain cover needs inspection. Haz-mat kit missing goggles.', DATEADD(day, -4, GETDATE()));
    END
END

PRINT 'Sample data inserted successfully!';
