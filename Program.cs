using SummerSplashWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Clear default logging providers and add only console logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SummerSplash Mobile API",
        Version = "v1",
        Description = "API for SummerSplash Field Mobile Application",
        Contact = new OpenApiContact
        {
            Name = "SummerSplash Admin",
            Email = "admin@summersplash.com"
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Data Protection to persist keys (prevents session loss on app pool recycle)
var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "keys");
if (!Directory.Exists(keysDirectory))
{
    Directory.CreateDirectory(keysDirectory);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("SummerSplashWeb");

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Max expiration (Remember Me)
        options.SlidingExpiration = true; // Refresh cookie on each request
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Register services
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IClockService, ClockService>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddSingleton<IScheduleService, ScheduleService>();
builder.Services.AddSingleton<ITrainingService, TrainingService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();

var app = builder.Build();

// Run database migrations on startup
RunDatabaseMigrations(app.Services.GetRequiredService<IDatabaseService>());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Enable Swagger UI (available in all environments for testing)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SummerSplash Mobile API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
    c.DocumentTitle = "SummerSplash API Documentation";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Database migration method
void RunDatabaseMigrations(IDatabaseService databaseService)
{
    Console.WriteLine("Running database migrations...");

    var migrations = new[]
    {
        // 1. Add Status column to Users
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'Status')
          ALTER TABLE Users ADD Status NVARCHAR(50) DEFAULT 'Pending'",

        // Update existing users status
        @"UPDATE Users SET Status =
            CASE
                WHEN IsApproved = 1 AND IsActive = 1 THEN 'Approved'
                WHEN IsApproved = 1 AND IsActive = 0 THEN 'Terminated'
                ELSE 'Pending'
            END
          WHERE Status IS NULL OR Status = ''",

        // 2. Add Country to JobLocations
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Country')
          ALTER TABLE JobLocations ADD Country NVARCHAR(100) DEFAULT 'USA'",

        // Add Latitude to JobLocations
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Latitude')
          ALTER TABLE JobLocations ADD Latitude FLOAT NULL",

        // Add Longitude to JobLocations
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Longitude')
          ALTER TABLE JobLocations ADD Longitude FLOAT NULL",

        // Add Radius to JobLocations
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'JobLocations') AND name = 'Radius')
          ALTER TABLE JobLocations ADD Radius INT DEFAULT 100",

        // 3. Add ScheduleId to ClockRecords
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ClockRecords') AND name = 'ScheduleId')
          ALTER TABLE ClockRecords ADD ScheduleId INT NULL",

        // Add IsLate to ClockRecords
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ClockRecords') AND name = 'IsLate')
          ALTER TABLE ClockRecords ADD IsLate BIT DEFAULT 0",

        // Add LateMinutes to ClockRecords
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ClockRecords') AND name = 'LateMinutes')
          ALTER TABLE ClockRecords ADD LateMinutes INT NULL",

        // Update existing JobLocations
        "UPDATE JobLocations SET Country = 'USA' WHERE Country IS NULL",

        // Update existing ClockRecords
        "UPDATE ClockRecords SET IsLate = 0 WHERE IsLate IS NULL",

        // Create SiteEvaluations table
        @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SiteEvaluations')
          CREATE TABLE SiteEvaluations (
              EvaluationId INT IDENTITY(1,1) PRIMARY KEY,
              UserId INT NOT NULL,
              LocationId INT NOT NULL,
              ClockRecordId INT NULL,
              EvaluationType NVARCHAR(50) NOT NULL,
              EvaluationDate DATETIME NOT NULL DEFAULT GETDATE(),
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
              StaffOnDuty BIT NULL,
              ScanningRotationDiscussed BIT NULL,
              ZonesEstablished BIT NULL,
              BreakTimeDiscussed BIT NULL,
              GateControlDiscussed BIT NULL,
              CellphonePolicyDiscussed BIT NULL,
              PumproomCleaned BIT NULL,
              BalancingChemicalsTestedLogged BIT NULL,
              ClosingProceduresDiscussed BIT NULL,
              StaffWearingUniform BIT NULL,
              FacilityEntryProcedures BIT NULL,
              MSDS BIT NULL,
              SafetySuppliesNeeded BIT NULL,
              SafetyConcernsNotes NVARCHAR(MAX) NULL,
              Notes NVARCHAR(MAX) NULL,
              CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
              UpdatedAt DATETIME NULL
          )",

        // Add new columns to ServiceTechReports
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'Flowrate')
          ALTER TABLE ServiceTechReports ADD Flowrate DECIMAL(10,2) NULL",

        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'FilterPressure')
          ALTER TABLE ServiceTechReports ADD FilterPressure DECIMAL(10,2) NULL",

        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'WaterTemp')
          ALTER TABLE ServiceTechReports ADD WaterTemp DECIMAL(10,2) NULL",

        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'ControllerORP')
          ALTER TABLE ServiceTechReports ADD ControllerORP DECIMAL(10,2) NULL",

        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ServiceTechReports') AND name = 'ControllerPH')
          ALTER TABLE ServiceTechReports ADD ControllerPH DECIMAL(10,2) NULL",

        // Add missing columns to SiteEvaluations
        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SiteEvaluations') AND name = 'EvaluationDate')
          ALTER TABLE SiteEvaluations ADD EvaluationDate DATETIME NULL",

        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SiteEvaluations') AND name = 'BalancingChemicalsTestedLogged')
          ALTER TABLE SiteEvaluations ADD BalancingChemicalsTestedLogged BIT NULL",

        @"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SiteEvaluations') AND name = 'SafetyConcernsNotes')
          ALTER TABLE SiteEvaluations ADD SafetyConcernsNotes NVARCHAR(MAX) NULL",

        @"UPDATE SiteEvaluations SET EvaluationDate = CreatedAt WHERE EvaluationDate IS NULL",

        // Insert sample Site Evaluations data
        @"IF NOT EXISTS (SELECT 1 FROM SiteEvaluations WHERE EvaluationType = 'Supervisor')
          BEGIN
              DECLARE @UserId1 INT, @LocationId1 INT, @LocationId2 INT;
              SELECT TOP 1 @UserId1 = UserId FROM Users WHERE IsActive = 1;
              SELECT TOP 1 @LocationId1 = LocationId FROM JobLocations WHERE IsActive = 1;
              SELECT TOP 1 @LocationId2 = LocationId FROM JobLocations WHERE IsActive = 1 AND LocationId <> @LocationId1;
              IF @LocationId2 IS NULL SET @LocationId2 = @LocationId1;

              IF @UserId1 IS NOT NULL AND @LocationId1 IS NOT NULL
              BEGIN
                  INSERT INTO SiteEvaluations (UserId, LocationId, EvaluationType, EvaluationDate,
                      PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent, BackboardPresent,
                      FirstAidKit, BloodbornePathogenKit, HazMatKit, GateFenceSecured, EmergencyPhoneWorking,
                      StaffOnDuty, ScanningRotationDiscussed, ZonesEstablished, BreakTimeDiscussed,
                      GateControlDiscussed, CellphonePolicyDiscussed, PumproomCleaned,
                      BalancingChemicalsTestedLogged, ClosingProceduresDiscussed, CreatedAt)
                  VALUES
                  (@UserId1, @LocationId1, 'Supervisor', DATEADD(day, -1, GETDATE()),
                      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, DATEADD(day, -1, GETDATE())),
                  (@UserId1, @LocationId2, 'Supervisor', DATEADD(day, -3, GETDATE()),
                      1, 1, 1, 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, DATEADD(day, -3, GETDATE()));
              END
          END",

        @"IF NOT EXISTS (SELECT 1 FROM SiteEvaluations WHERE EvaluationType = 'Manager')
          BEGIN
              DECLARE @UserId2 INT, @LocId1 INT, @LocId2 INT;
              SELECT TOP 1 @UserId2 = UserId FROM Users WHERE IsActive = 1;
              SELECT TOP 1 @LocId1 = LocationId FROM JobLocations WHERE IsActive = 1;
              SELECT TOP 1 @LocId2 = LocationId FROM JobLocations WHERE IsActive = 1 AND LocationId <> @LocId1;
              IF @LocId2 IS NULL SET @LocId2 = @LocId1;

              IF @UserId2 IS NOT NULL AND @LocId1 IS NOT NULL
              BEGIN
                  INSERT INTO SiteEvaluations (UserId, LocationId, EvaluationType, EvaluationDate,
                      PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent, BackboardPresent,
                      FirstAidKit, BloodbornePathogenKit, HazMatKit, GateFenceSecured, EmergencyPhoneWorking,
                      StaffWearingUniform, StaffOnDuty, SafetyConcernsNotes, CreatedAt)
                  VALUES
                  (@UserId2, @LocId1, 'Manager', DATEADD(day, -2, GETDATE()),
                      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, NULL, DATEADD(day, -2, GETDATE())),
                  (@UserId2, @LocId2, 'Manager', DATEADD(day, -5, GETDATE()),
                      1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 'Staff uniforms need replacement', DATEADD(day, -5, GETDATE()));
              END
          END",

        @"IF NOT EXISTS (SELECT 1 FROM SiteEvaluations WHERE EvaluationType = 'Safety Audit')
          BEGIN
              DECLARE @UserId3 INT, @Loc1 INT, @Loc2 INT;
              SELECT TOP 1 @UserId3 = UserId FROM Users WHERE IsActive = 1;
              SELECT TOP 1 @Loc1 = LocationId FROM JobLocations WHERE IsActive = 1;
              SELECT TOP 1 @Loc2 = LocationId FROM JobLocations WHERE IsActive = 1 AND LocationId <> @Loc1;
              IF @Loc2 IS NULL SET @Loc2 = @Loc1;

              IF @UserId3 IS NOT NULL AND @Loc1 IS NOT NULL
              BEGIN
                  INSERT INTO SiteEvaluations (UserId, LocationId, EvaluationType, EvaluationDate,
                      PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent, BackboardPresent,
                      FirstAidKit, BloodbornePathogenKit, HazMatKit, GateFenceSecured, EmergencyPhoneWorking,
                      FacilityEntryProcedures, MSDS, SafetySuppliesNeeded, SafetyConcernsNotes, CreatedAt)
                  VALUES
                  (@UserId3, @Loc1, 'Safety Audit', DATEADD(day, -1, GETDATE()),
                      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, NULL, DATEADD(day, -1, GETDATE())),
                  (@UserId3, @Loc2, 'Safety Audit', DATEADD(day, -4, GETDATE()),
                      1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 'Main drain cover needs inspection. Haz-mat kit missing goggles.', DATEADD(day, -4, GETDATE()));
              END
          END"
    };

    try
    {
        using var connection = databaseService.CreateConnection();
        connection.Open();

        foreach (var sql in migrations)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration warning: {ex.Message}");
            }
        }

        Console.WriteLine("Database migrations completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration error: {ex.Message}");
    }
}
