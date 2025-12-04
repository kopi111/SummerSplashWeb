using Microsoft.AspNetCore.Mvc;
using SummerSplashWeb.Models;
using SummerSplashWeb.Services;

namespace SummerSplashWeb.Controllers.Api
{
    [Route("api/safety-audit")]
    [ApiController]
    public class SafetyAuditApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<SafetyAuditApiController> _logger;

        public SafetyAuditApiController(IReportService reportService, ILogger<SafetyAuditApiController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// Submit a safety audit from mobile app
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAudit([FromBody] SafetyAuditSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting safety audit type {AuditType} for location {LocationId} by user {UserId}",
                    request.AuditType, request.LocationId, request.UserId);

                var evaluation = new SiteEvaluation
                {
                    UserId = request.UserId,
                    LocationId = request.LocationId,
                    EvaluationType = request.AuditType ?? "Safety Audit",
                    EvaluationDate = DateTime.Now,
                    CreatedAt = DateTime.Now,

                    // Safety Equipment Checklist
                    PoolOpen = GetBoolValue(request.AuditData, "poolOpen"),
                    MainDrainVisible = GetBoolValue(request.AuditData, "mainDrainVisible"),
                    AEDPresent = GetBoolValue(request.AuditData, "aedPresent"),
                    RescueTubePresent = GetBoolValue(request.AuditData, "rescueTubePresent"),
                    BackboardPresent = GetBoolValue(request.AuditData, "backboardPresent"),
                    FirstAidKit = GetBoolValue(request.AuditData, "firstAidKit"),
                    BloodbornePathogenKit = GetBoolValue(request.AuditData, "bloodbornePathogenKit"),
                    HazMatKit = GetBoolValue(request.AuditData, "hazMatKit"),
                    GateFenceSecured = GetBoolValue(request.AuditData, "gateFenceSecured"),
                    EmergencyPhoneWorking = GetBoolValue(request.AuditData, "emergencyPhoneWorking"),

                    // Supervisor Checklist
                    StaffOnDuty = GetBoolValue(request.AuditData, "staffOnDuty"),
                    ScanningRotationDiscussed = GetBoolValue(request.AuditData, "scanningRotationDiscussed"),
                    ZonesEstablished = GetBoolValue(request.AuditData, "zonesEstablished"),
                    BreakTimeDiscussed = GetBoolValue(request.AuditData, "breakTimeDiscussed"),
                    GateControlDiscussed = GetBoolValue(request.AuditData, "gateControlDiscussed"),
                    CellphonePolicyDiscussed = GetBoolValue(request.AuditData, "cellphonePolicyDiscussed"),
                    PumproomCleaned = GetBoolValue(request.AuditData, "pumproomCleaned"),
                    ClosingProceduresDiscussed = GetBoolValue(request.AuditData, "closingProceduresDiscussed"),

                    // Manager Checklist
                    StaffWearingUniform = GetBoolValue(request.AuditData, "staffWearingUniform"),

                    // Safety Audit specific
                    FacilityEntryProcedures = GetBoolValue(request.AuditData, "facilityEntryProcedures"),
                    MSDS = GetBoolValue(request.AuditData, "msds"),
                    SafetySuppliesNeeded = GetBoolValue(request.AuditData, "safetySuppliesNeeded")
                };

                var evaluationId = await _reportService.CreateSiteEvaluationAsync(evaluation);

                return Ok(new
                {
                    success = true,
                    message = $"{request.AuditType ?? "Safety Audit"} submitted successfully",
                    data = new { auditId = evaluationId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit safety audit");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get audits for authenticated user
        /// </summary>
        [HttpGet("my-audits")]
        public async Task<IActionResult> GetMyAudits(
            [FromQuery] int userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                var evaluations = await _reportService.GetSiteEvaluationsAsync(start, end);
                var userEvaluations = evaluations.Where(e => e.UserId == userId).ToList();

                return Ok(new
                {
                    success = true,
                    data = userEvaluations.Select(e => new
                    {
                        auditId = e.EvaluationId,
                        locationId = e.LocationId,
                        locationName = e.LocationName,
                        auditType = e.EvaluationType,
                        safetyCompliancePercentage = e.SafetyCompliancePercentage,
                        createdAt = e.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audits");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get audit by ID
        /// </summary>
        [HttpGet("{auditId}")]
        public async Task<IActionResult> GetAudit(int auditId)
        {
            try
            {
                var evaluation = await _reportService.GetSiteEvaluationByIdAsync(auditId);
                if (evaluation == null)
                {
                    return NotFound(new { success = false, message = "Audit not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        auditId = evaluation.EvaluationId,
                        userId = evaluation.UserId,
                        userName = evaluation.UserName,
                        locationId = evaluation.LocationId,
                        locationName = evaluation.LocationName,
                        auditType = evaluation.EvaluationType,
                        auditData = new
                        {
                            // Safety Equipment
                            poolOpen = evaluation.PoolOpen,
                            mainDrainVisible = evaluation.MainDrainVisible,
                            aedPresent = evaluation.AEDPresent,
                            rescueTubePresent = evaluation.RescueTubePresent,
                            backboardPresent = evaluation.BackboardPresent,
                            firstAidKit = evaluation.FirstAidKit,
                            bloodbornePathogenKit = evaluation.BloodbornePathogenKit,
                            hazMatKit = evaluation.HazMatKit,
                            gateFenceSecured = evaluation.GateFenceSecured,
                            emergencyPhoneWorking = evaluation.EmergencyPhoneWorking,

                            // Supervisor
                            staffOnDuty = evaluation.StaffOnDuty,
                            scanningRotationDiscussed = evaluation.ScanningRotationDiscussed,
                            zonesEstablished = evaluation.ZonesEstablished,
                            breakTimeDiscussed = evaluation.BreakTimeDiscussed,
                            gateControlDiscussed = evaluation.GateControlDiscussed,
                            cellphonePolicyDiscussed = evaluation.CellphonePolicyDiscussed,
                            pumproomCleaned = evaluation.PumproomCleaned,
                            closingProceduresDiscussed = evaluation.ClosingProceduresDiscussed,

                            // Manager
                            staffWearingUniform = evaluation.StaffWearingUniform,

                            // Safety Audit
                            facilityEntryProcedures = evaluation.FacilityEntryProcedures,
                            msds = evaluation.MSDS,
                            safetySuppliesNeeded = evaluation.SafetySuppliesNeeded
                        },
                        safetyCompliancePercentage = evaluation.SafetyCompliancePercentage,
                        createdAt = evaluation.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get available audit types
        /// </summary>
        [HttpGet("types")]
        public IActionResult GetAuditTypes()
        {
            var types = new[] { "Supervisor", "Manager", "Safety Audit" };
            return Ok(new { success = true, data = types });
        }

        private bool? GetBoolValue(Dictionary<string, object>? dict, string key)
        {
            if (dict == null) return null;
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is bool boolVal) return boolVal;
                if (bool.TryParse(value.ToString(), out var result)) return result;
            }
            return null;
        }
    }

    public class SafetyAuditSubmitRequest
    {
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public string? AuditType { get; set; }
        public Dictionary<string, object>? AuditData { get; set; }
        public string? SafetyConcerns { get; set; }
        public string? SafetySuppliesNeeded { get; set; }
    }
}
