using Microsoft.AspNetCore.Mvc;
using SummerSplashWeb.Models;
using SummerSplashWeb.Services;

namespace SummerSplashWeb.Controllers.Api
{
    [Route("api/checklist")]
    [ApiController]
    public class ChecklistApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ChecklistApiController> _logger;

        public ChecklistApiController(IReportService reportService, ILogger<ChecklistApiController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// Submit a service checklist from mobile app
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitChecklist([FromBody] ChecklistSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting checklist for location {LocationId} by user {UserId}",
                    request.LocationId, request.UserId);

                var report = new ServiceTechReport
                {
                    UserId = request.UserId,
                    LocationId = request.LocationId,
                    ServiceDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    Notes = request.Notes,

                    // Checklist items from mobile app
                    PoolVacuumed = GetBoolValue(request.ChecklistData, "poolVacuumed"),
                    PoolBrushed = GetBoolValue(request.ChecklistData, "poolBrushed"),
                    SkimmersEmpty = GetBoolValue(request.ChecklistData, "skimmersEmpty"),
                    TilesCleaned = GetBoolValue(request.ChecklistData, "tilesCleaned"),
                    FurnitureArranged = GetBoolValue(request.ChecklistData, "furnitureArranged"),
                    CleanedStrainer = GetBoolValue(request.ChecklistData, "cleanedStrainer"),
                    BackwashFilters = GetBoolValue(request.ChecklistData, "backwashFilters"),
                    CleanedCartridges = GetBoolValue(request.ChecklistData, "cleanedCartridges") ? "1" : "0",
                    EmptyTrash = GetBoolValue(request.ChecklistData, "emptyTrash"),
                    BroomBucketHoseDeck = GetBoolValue(request.ChecklistData, "broomBucketHoseDeck"),
                    FurnitureOrganized = GetBoolValue(request.ChecklistData, "furnitureOrganized"),
                    SkimWaterSurface = GetBoolValue(request.ChecklistData, "skimWaterSurface"),
                    CalibratedChemicalController = GetBoolValue(request.ChecklistData, "calibratedChemicalController"),

                    // Equipment readings
                    Flowrate = GetDecimalFromDict(request.ChecklistData, "flowrate"),
                    FilterPressure = GetDecimalFromDict(request.ChecklistData, "filterPressure"),
                    WaterTemp = GetDecimalFromDict(request.ChecklistData, "waterTemp"),
                    ControllerORP = GetDecimalFromDict(request.ChecklistData, "controllerORP"),
                    ControllerPH = GetDecimalFromDict(request.ChecklistData, "controllerPH")
                };

                var reportId = await _reportService.CreateServiceTechReportAsync(report);

                // Add chemical readings if provided
                if (request.ChemicalReadings != null && request.ChemicalReadings.Any())
                {
                    foreach (var reading in request.ChemicalReadings)
                    {
                        var chemicalReading = new ChemicalReading
                        {
                            ReportId = reportId,
                            PoolType = reading.GetValueOrDefault("bodyOfWater", "Main pool")?.ToString() ?? "Main pool",
                            ChlorineBromine = GetDecimalValue(reading, "chlorine"),
                            pH = GetDecimalValue(reading, "phLevel"),
                            CalciumHardness = GetDecimalValue(reading, "calciumHardness"),
                            TotalAlkalinity = GetDecimalValue(reading, "alkalinity"),
                            CyanuricAcid = GetDecimalValue(reading, "cyanuricAcid"),
                            Salt = GetDecimalValue(reading, "saltLevel"),
                            Phosphates = GetDecimalValue(reading, "phosphates"),
                            CreatedAt = DateTime.Now
                        };

                        await _reportService.AddChemicalReadingAsync(chemicalReading);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Checklist submitted successfully",
                    data = new { checklistId = reportId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit checklist");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Add chemical reading to a checklist
        /// </summary>
        [HttpPost("chemical-reading")]
        public async Task<IActionResult> AddChemicalReading([FromBody] ChemicalReadingRequest request)
        {
            try
            {
                var reading = new ChemicalReading
                {
                    ReportId = request.ServiceChecklistId,
                    PoolType = request.BodyOfWater ?? "Main pool",
                    ChlorineBromine = request.Chlorine ?? request.Bromine,
                    pH = request.PhLevel,
                    CalciumHardness = request.CalciumHardness,
                    TotalAlkalinity = request.Alkalinity,
                    CyanuricAcid = request.CyanuricAcid,
                    Salt = request.SaltLevel,
                    Phosphates = request.Phosphates,
                    CreatedAt = DateTime.Now
                };

                await _reportService.AddChemicalReadingAsync(reading);

                return Ok(new { success = true, message = "Chemical reading added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add chemical reading");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get checklists for authenticated user
        /// </summary>
        [HttpGet("my-checklists")]
        public async Task<IActionResult> GetMyChecklists(
            [FromQuery] int userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1); // End of today

                var reports = await _reportService.GetReportsByDateRangeAsync(start, end);
                var userReports = reports.Where(r => r.UserId == userId).ToList();

                return Ok(new
                {
                    success = true,
                    data = userReports.Select(r => new
                    {
                        checklistId = r.ReportId,
                        locationId = r.LocationId,
                        locationName = r.LocationName,
                        serviceDate = r.ServiceDate,
                        completionPercentage = r.ChecklistCompletionPercentage,
                        notes = r.Notes,
                        createdAt = r.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get checklists");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get checklist by ID
        /// </summary>
        [HttpGet("{checklistId}")]
        public async Task<IActionResult> GetChecklist(int checklistId)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(checklistId);
                if (report == null)
                {
                    return NotFound(new { success = false, message = "Checklist not found" });
                }

                var chemicalReadings = await _reportService.GetChemicalReadingsForReportAsync(checklistId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        checklistId = report.ReportId,
                        userId = report.UserId,
                        locationId = report.LocationId,
                        locationName = report.LocationName,
                        serviceDate = report.ServiceDate,
                        checklistData = new
                        {
                            poolVacuumed = report.PoolVacuumed,
                            poolBrushed = report.PoolBrushed,
                            skimmersEmpty = report.SkimmersEmpty,
                            tilesCleaned = report.TilesCleaned,
                            furnitureArranged = report.FurnitureArranged,
                            cleanedStrainer = report.CleanedStrainer,
                            backwashFilters = report.BackwashFilters,
                            cleanedCartridges = report.CleanedCartridgesBool,
                            emptyTrash = report.EmptyTrash,
                            broomBucketHoseDeck = report.BroomBucketHoseDeck,
                            furnitureOrganized = report.FurnitureOrganized,
                            skimWaterSurface = report.SkimWaterSurface,
                            calibratedChemicalController = report.CalibratedChemicalController
                        },
                        chemicalReadings = chemicalReadings.Select(c => new
                        {
                            bodyOfWater = c.PoolType,
                            chlorine = c.ChlorineBromine,
                            phLevel = c.pH,
                            calciumHardness = c.CalciumHardness,
                            alkalinity = c.TotalAlkalinity,
                            cyanuricAcid = c.CyanuricAcid,
                            saltLevel = c.Salt,
                            phosphates = c.Phosphates
                        }),
                        notes = report.Notes,
                        createdAt = report.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get checklist");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private decimal? GetDecimalValue(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (decimal.TryParse(value.ToString(), out var result))
                    return result;
            }
            return null;
        }

        private bool GetBoolValue(Dictionary<string, object>? dict, string key, bool defaultValue = false)
        {
            if (dict == null) return defaultValue;
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is bool boolVal) return boolVal;
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                }
                if (bool.TryParse(value.ToString(), out var result)) return result;
            }
            return defaultValue;
        }

        private decimal GetDecimalFromDict(Dictionary<string, object>? dict, string key, decimal defaultValue = 0m)
        {
            if (dict == null) return defaultValue;
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is decimal decVal) return decVal;
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.TryGetDecimal(out var decResult)) return decResult;
                }
                if (decimal.TryParse(value.ToString(), out var result)) return result;
            }
            return defaultValue;
        }
    }

    public class ChecklistSubmitRequest
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public Dictionary<string, object>? ChecklistData { get; set; }
        public List<Dictionary<string, object>>? ChemicalReadings { get; set; }
        public string? Notes { get; set; }
    }

    public class ChemicalReadingRequest
    {
        public int ServiceChecklistId { get; set; }
        public string? BodyOfWater { get; set; }
        public decimal? Chlorine { get; set; }
        public decimal? Bromine { get; set; }
        public decimal? PhLevel { get; set; }
        public decimal? CalciumHardness { get; set; }
        public decimal? Alkalinity { get; set; }
        public decimal? CyanuricAcid { get; set; }
        public decimal? SaltLevel { get; set; }
        public decimal? Phosphates { get; set; }
    }
}
