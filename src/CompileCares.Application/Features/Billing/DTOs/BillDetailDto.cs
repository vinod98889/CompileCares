namespace CompileCares.Application.Features.Billing.DTOs
{
    public class BillDetailDto : OPDBillDto
    {
        public List<BillItemDto> Items { get; set; } = new();
        public List<PaymentRecordDto> PaymentHistory { get; set; } = new();

        // Visit Details
        public DateTime VisitDate { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Diagnosis { get; set; }

        // Prescription Details
        public bool HasPrescription { get; set; }
        public string? PrescriptionNumber { get; set; }
    }
}