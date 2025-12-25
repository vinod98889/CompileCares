using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class PrescriptionDto
    {
        public Guid Id { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public DateTime PrescriptionDate { get; set; }

        // References
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;

        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorRegistrationNumber { get; set; } = string.Empty;

        public Guid OPDVisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;

        // Clinical Information
        public string? Diagnosis { get; set; }
        public string? Instructions { get; set; }
        public string? FollowUpInstructions { get; set; }

        // Status
        public PrescriptionStatus Status { get; set; }

        // Validity
        public int ValidityDays { get; set; }
        public DateTime? ValidUntil { get; set; }
        public bool IsValid { get; set; }

        // Items Count
        public int MedicineCount { get; set; }
        public int ComplaintCount { get; set; }
        public int AdviceCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}