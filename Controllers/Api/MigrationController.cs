using Microsoft.AspNetCore.Mvc;
using SummerSplashWeb.Services;
using Dapper;

namespace SummerSplashWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MigrationController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(IDatabaseService databaseService, ILogger<MigrationController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        [HttpGet("run-location-updates")]
        public async Task<IActionResult> RunLocationUpdates()
        {
            var results = new List<string>();

            try
            {
                using var connection = _databaseService.CreateConnection();

                // Try direct ALTER without permission checks
                var sqls = new Dictionary<string, string>
                {
                    ["LockboxCode"] = "ALTER TABLE JobLocations ADD LockboxCode NVARCHAR(8) NULL",
                    ["SupervisorId"] = "ALTER TABLE JobLocations ADD SupervisorId INT NULL",
                    ["SupervisorName"] = "ALTER TABLE JobLocations ADD SupervisorName NVARCHAR(100) NULL",
                    ["SupervisorPhone"] = "ALTER TABLE JobLocations ADD SupervisorPhone NVARCHAR(20) NULL",
                    ["PoolDepthFeet"] = "ALTER TABLE JobLocations ADD PoolDepthFeet INT NULL",
                    ["PoolDepthInches"] = "ALTER TABLE JobLocations ADD PoolDepthInches INT NULL",
                    ["HasWadingPool"] = "ALTER TABLE JobLocations ADD HasWadingPool BIT NOT NULL DEFAULT 0",
                    ["WadingPoolSizeGallons"] = "ALTER TABLE JobLocations ADD WadingPoolSizeGallons INT NULL",
                    ["HasSpa"] = "ALTER TABLE JobLocations ADD HasSpa BIT NOT NULL DEFAULT 0",
                    ["SpaSizeGallons"] = "ALTER TABLE JobLocations ADD SpaSizeGallons INT NULL"
                };

                foreach (var kv in sqls)
                {
                    try
                    {
                        await connection.ExecuteAsync(kv.Value);
                        results.Add($"OK: Added {kv.Key} column");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"SKIP: {kv.Key} - {ex.Message}");
                    }
                }

                // Create LocationContacts table
                try
                {
                    var createTableSql = @"
                        CREATE TABLE LocationContacts (
                            ContactId INT IDENTITY(1,1) PRIMARY KEY,
                            LocationId INT NOT NULL,
                            ContactName NVARCHAR(100) NULL,
                            ContactPhone NVARCHAR(20) NULL,
                            ContactEmail NVARCHAR(100) NULL,
                            ContactRole NVARCHAR(50) NULL,
                            IsPrimary BIT NOT NULL DEFAULT 0,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_LocationContacts_Location FOREIGN KEY (LocationId)
                                REFERENCES JobLocations(LocationId) ON DELETE CASCADE
                        )";
                    await connection.ExecuteAsync(createTableSql);
                    results.Add("OK: LocationContacts table created");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("already an object"))
                        results.Add("OK: LocationContacts table already exists");
                    else
                        results.Add($"SKIP: LocationContacts table - {ex.Message}");
                }

                // Create index
                try
                {
                    var indexSql = "CREATE INDEX IX_LocationContacts_LocationId ON LocationContacts(LocationId)";
                    await connection.ExecuteAsync(indexSql);
                    results.Add("OK: Index created");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("already exists"))
                        results.Add("OK: Index already exists");
                    else
                        results.Add($"SKIP: Index - {ex.Message}");
                }

                // Verify columns
                var columns = await connection.QueryAsync<string>(
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'JobLocations' ORDER BY ORDINAL_POSITION");
                results.Add($"JobLocations columns: {string.Join(", ", columns)}");

                return Ok(new
                {
                    success = true,
                    message = "Migration completed!",
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration error");
                return Ok(new
                {
                    success = false,
                    error = ex.Message,
                    results
                });
            }
        }
    }
}
