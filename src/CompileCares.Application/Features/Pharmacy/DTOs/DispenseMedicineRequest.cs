namespace CompileCares.Application.Features.Pharmacy.DTOs
{
    public class DispenseMedicineRequest
    {
        public Guid PrescriptionId { get; set; }
        public List<DispenseItemRequest> Items { get; set; } = new();
        public string DispensedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class DispenseItemRequest
    {
        public Guid PrescriptionMedicineId { get; set; }
        public int QuantityDispensed { get; set; }
        public string? BatchNumber { get; set; }
        public decimal? SellingPrice { get; set; } // Override if different from standard
        public string? Notes { get; set; }
    }
}