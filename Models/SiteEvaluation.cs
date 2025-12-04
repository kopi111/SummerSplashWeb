using System;

namespace SummerSplashWeb.Models
{
    public class SiteEvaluation
    {
        public int EvaluationId { get; set; }
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public int? ClockRecordId { get; set; }

        // Evaluation Type: Manager, Supervisor, SafetyAudit
        public string EvaluationType { get; set; } = string.Empty;

        // ==========================================
        // SAFETY EQUIPMENT CHECKLIST (All Positions)
        // ==========================================
        public bool? PoolOpen { get; set; }
        public bool? MainDrainVisible { get; set; }
        public bool? AEDPresent { get; set; }
        public bool? RescueTubePresent { get; set; }
        public bool? BackboardPresent { get; set; }
        public bool? FirstAidKit { get; set; }
        public bool? BloodbornePathogenKit { get; set; }
        public bool? HazMatKit { get; set; } // gloves, apron, goggles, mask
        public bool? GateFenceSecured { get; set; }
        public bool? EmergencyPhoneWorking { get; set; }
        public bool? MSDSSafetySuppliesNeeded { get; set; }

        // ==========================================
        // SUPERVISOR CHECKLIST (Additional Items)
        // ==========================================
        public bool? StaffOnDuty { get; set; }
        public bool? ScanningRotationDiscussed { get; set; }
        public bool? ZonesEstablished { get; set; }
        public bool? BreakTimeDiscussed { get; set; }
        public bool? GateControlDiscussed { get; set; }
        public bool? CellphonePolicyDiscussed { get; set; }
        public bool? PumproomCleaned { get; set; }
        public bool? BalancingChemicalsTestedLogged { get; set; } // Weekly
        public bool? ClosingProceduresDiscussed { get; set; }

        // ==========================================
        // MANAGER CHECKLIST (Additional Items)
        // ==========================================
        public bool? StaffWearingUniform { get; set; }

        // ==========================================
        // SAFETY AUDIT SPECIFIC
        // ==========================================
        public bool? FacilityEntryProcedures { get; set; }
        public bool? MSDS { get; set; }
        public bool? SafetySuppliesNeeded { get; set; }

        // Notes and timestamps
        public string? SafetyConcernsNotes { get; set; }
        public string? Notes { get; set; }
        public DateTime EvaluationDate { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? UserName { get; set; }
        public string? LocationName { get; set; }
        public string? LocationAddress { get; set; }

        // Compliance percentage based on evaluation type
        public int SafetyCompliancePercentage
        {
            get
            {
                var safetyItems = new bool?[] {
                    PoolOpen, MainDrainVisible, AEDPresent, RescueTubePresent,
                    BackboardPresent, FirstAidKit, BloodbornePathogenKit,
                    HazMatKit, GateFenceSecured, EmergencyPhoneWorking
                };

                int total = safetyItems.Length;
                int passed = 0;

                foreach (var item in safetyItems)
                {
                    if (item == true) passed++;
                }

                return total > 0 ? (int)((passed / (double)total) * 100) : 0;
            }
        }

        public int SupervisorChecklistPercentage
        {
            get
            {
                var items = new bool?[] {
                    StaffOnDuty, ScanningRotationDiscussed, ZonesEstablished,
                    BreakTimeDiscussed, GateControlDiscussed, CellphonePolicyDiscussed,
                    PumproomCleaned, BalancingChemicalsTestedLogged, ClosingProceduresDiscussed
                };

                int total = items.Length;
                int completed = 0;

                foreach (var item in items)
                {
                    if (item == true) completed++;
                }

                return total > 0 ? (int)((completed / (double)total) * 100) : 0;
            }
        }
    }

    // Evaluation types as constants
    public static class EvaluationTypes
    {
        public const string Manager = "Manager";
        public const string Supervisor = "Supervisor";
        public const string SafetyAudit = "Safety Audit";
    }
}
