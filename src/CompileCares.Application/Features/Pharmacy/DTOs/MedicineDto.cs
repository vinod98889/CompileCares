using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Pharmacy.DTOs
{
    public class MedicineDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? BrandName { get; set; }
        public MedicineType MedicineType { get; set; }

        // Classification
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? TherapeuticClass { get; set; }

        // Form & Strength
        public string? Form { get; set; }
        public string? Strength { get; set; }
        public string? PackSize { get; set; }
        public string? Composition { get; set; }

        // Manufacturer
        public string? Manufacturer { get; set; }
        public string? ManufacturerCode { get; set; }

        // Stock Management
        public int CurrentStock { get; set; }
        public int MinimumStockLevel { get; set; }
        public int ReorderLevel { get; set; }
        public string? StockUnit { get; set; }
        public string StockStatus { get; set; } = string.Empty; // "In Stock", "Low", "Out of Stock"

        // Pricing
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? MRP { get; set; }
        public decimal? GSTPercentage { get; set; }
        public decimal ProfitMargin => SellingPrice > 0 ? ((SellingPrice - PurchasePrice) / SellingPrice) * 100 : 0;

        // Regulatory
        public string? HSNCode { get; set; }
        public string? Schedule { get; set; }
        public bool RequiresPrescription { get; set; }

        // Safety Information
        public string? SideEffects { get; set; }
        public string? Contraindications { get; set; }
        public string? Precautions { get; set; }
        public string? StorageInstructions { get; set; }

        // Status
        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public bool IsStockLow { get; set; }
        public bool IsStockCritical { get; set; }

        // Usage Statistics
        public int TotalPrescribed { get; set; }
        public int TotalDispensed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public enum StockUpdateType
        {
            Received,
            Dispensed,
            Adjusted,
            Damaged,
            Expired
        }
    }
}