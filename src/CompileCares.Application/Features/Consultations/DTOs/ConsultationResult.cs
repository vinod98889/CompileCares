using CompileCares.Application.Features.Billing.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Application.Features.Visits.DTOs;

namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class ConsultationResult
    {
        public Guid ConsultationId { get; set; }

        // Created Entities
        public PatientDto? Patient { get; set; }
        public OPDVisitDto Visit { get; set; } = null!;
        public PrescriptionDetailDto Prescription { get; set; } = null!;
        public BillDetailDto Bill { get; set; } = null!;

        // Output Documents
        public string PrescriptionPrintUrl { get; set; } = string.Empty;
        public string BillReceiptUrl { get; set; } = string.Empty;
        public string CombinedPrintUrl { get; set; } = string.Empty;

        // Summary
        public string Summary { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public bool IsFullyPaid { get; set; }
        public bool IsDispensed { get; set; }

        // Timestamps
        public DateTime ConsultationDate { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }
}