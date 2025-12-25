using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Billing.DTOs
{
    public class OPDBillDto
    {
        public Guid Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public BillStatus Status { get; set; }

        // References
        public Guid OPDVisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;

        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;

        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorRegistrationNumber { get; set; } = string.Empty;

        // Charges Breakdown
        public decimal ConsultationFee { get; set; }
        public decimal ProcedureFee { get; set; }
        public decimal MedicineFee { get; set; }
        public decimal LabTestFee { get; set; }
        public decimal OtherCharges { get; set; }

        // Calculations
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        // Payment Information
        public string? PaymentMode { get; set; }
        public string? TransactionId { get; set; }

        // Insurance
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }
        public decimal? InsuranceCoveredAmount { get; set; }

        // Doctor Commission
        public decimal TotalDoctorCommission { get; set; }

        // Notes
        public string? Notes { get; set; }

        // Bill Items Count
        public int ItemCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}