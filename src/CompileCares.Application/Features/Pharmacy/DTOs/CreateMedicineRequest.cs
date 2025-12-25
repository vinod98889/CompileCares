using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Pharmacy.DTOs
{
    public class CreateMedicineRequest
    {
        public string Name { get; set; } = string.Empty;
        public MedicineType MedicineType { get; set; }
        public decimal SellingPrice { get; set; }

        public string? GenericName { get; set; }
        public string? BrandName { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? TherapeuticClass { get; set; }

        public string? Form { get; set; }
        public string? Strength { get; set; }
        public string? PackSize { get; set; }
        public string? Composition { get; set; }

        public string? Manufacturer { get; set; }
        public string? ManufacturerCode { get; set; }

        public int InitialStock { get; set; } = 0;
        public int? MinimumStockLevel { get; set; }
        public int? ReorderLevel { get; set; }
        public string? StockUnit { get; set; }

        public decimal? PurchasePrice { get; set; }
        public decimal? MRP { get; set; }
        public decimal? GSTPercentage { get; set; }

        public string? HSNCode { get; set; }
        public string? Schedule { get; set; }
        public bool? RequiresPrescription { get; set; }

        public string? SideEffects { get; set; }
        public string? Contraindications { get; set; }
        public string? Precautions { get; set; }
        public string? StorageInstructions { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}