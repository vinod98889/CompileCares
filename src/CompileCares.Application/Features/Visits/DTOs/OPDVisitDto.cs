using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Visits.DTOs
{
    public class OPDVisitDto
    {
        public Guid Id { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public VisitType VisitType { get; set; }
        public VisitStatus Status { get; set; }

        // Patient Info
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;

        // Doctor Info
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;

        // Clinical Information
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
        public decimal? BMI { get; set; }

        // Examination
        public string? GeneralExamination { get; set; }
        public string? SystemicExamination { get; set; }
        public string? LocalExamination { get; set; }

        // Investigations & Advice
        public string? InvestigationsOrdered { get; set; }
        public string? Advice { get; set; }
        public string? FollowUpInstructions { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public int? FollowUpDays { get; set; }

        // Referrals
        public Guid? ReferredToDoctorId { get; set; }
        public string? ReferredToDoctorName { get; set; }
        public string? ReferralReason { get; set; }

        // Billing Reference
        public Guid? OPDBillId { get; set; }
        public string? BillNumber { get; set; }

        // Duration
        public TimeSpan? ConsultationDuration { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}