using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Visits.DTOs
{
    public class VisitSummaryDto
    {
        public Guid Id { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public VisitType VisitType { get; set; }
        public VisitStatus Status { get; set; }

        // Doctor Info
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;

        // Clinical Info
        public string? ChiefComplaint { get; set; }
        public string? Diagnosis { get; set; }

        // Billing Info
        public decimal? ConsultationFee { get; set; }
        public decimal? TotalBillAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public BillStatus? BillStatus { get; set; }

        // Follow-up
        public DateTime? FollowUpDate { get; set; }
        public bool HasFollowUp { get; set; }
        public bool HasPrescription { get; set; }

        // Duration
        public TimeSpan? ConsultationDuration { get; set; }
    }
}