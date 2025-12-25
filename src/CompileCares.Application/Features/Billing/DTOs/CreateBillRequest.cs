namespace CompileCares.Application.Features.Billing.DTOs
{
    public class CreateBillRequest
    {
        public Guid VisitId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public decimal ConsultationFee { get; set; }

        public List<BillItemRequest> Items { get; set; } = new();
        public decimal DiscountPercentage { get; set; }
        public decimal TaxPercentage { get; set; } = 5.0m;

        // Insurance Info (optional)
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }
        public decimal? InsuranceCoveredAmount { get; set; }
    }

    public class BillItemRequest
    {
        public Guid ItemMasterId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? CustomPrice { get; set; }
        public decimal? ItemDiscountPercentage { get; set; }
    }
}