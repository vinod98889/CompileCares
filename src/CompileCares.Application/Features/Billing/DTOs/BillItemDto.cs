namespace CompileCares.Application.Features.Billing.DTOs
{
    public class BillItemDto
    {
        public Guid Id { get; set; }
        public Guid? ItemMasterId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }

        // Discount
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal NetAmount => TotalPrice - DiscountAmount;

        // Commission
        public decimal? DoctorCommission { get; set; }
        public bool HasCommission => DoctorCommission.HasValue && DoctorCommission > 0;

        // Tax
        public bool IsTaxable { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalWithTax => NetAmount + TaxAmount;

        // Status
        public bool IsAdministered { get; set; }
        public DateTime? AdministeredDate { get; set; }
        public string? AdministeredBy { get; set; }
        public string? AdministrationNotes { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}