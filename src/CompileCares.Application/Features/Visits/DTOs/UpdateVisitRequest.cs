namespace CompileCares.Application.Features.Visits.DTOs
{
    public class UpdateVisitRequest
    {
        public string? ChiefComplaint { get; set; }
        public string? HistoryOfPresentIllness { get; set; }
        public string? PastMedicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public string? ClinicalNotes { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }

        // Vitals
        public string? BloodPressure { get; set; }
        public decimal? Temperature { get; set; }
        public int? Pulse { get; set; }
        public int? RespiratoryRate { get; set; }
        public int? SPO2 { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }

        // Examination
        public string? GeneralExamination { get; set; }
        public string? SystemicExamination { get; set; }
        public string? LocalExamination { get; set; }

        // Investigations
        public string? InvestigationsOrdered { get; set; }
        public string? Advice { get; set; }

        // Follow-up
        public string? FollowUpInstructions { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public int? FollowUpDays { get; set; }
    }
}