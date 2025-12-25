namespace CompileCares.Application.Features.Master.DTOs
{
    public class OPDItemMasterDto
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Classification
        public string ItemType { get; set; } = string.Empty; // Consultation, Procedure, Injection, etc.
        public string? Category { get; set; }
        public string? SubCategory { get; set; }

        // Pricing
        public decimal StandardPrice { get; set; }
        public decimal? DoctorCommission { get; set; }
        public bool IsCommissionPercentage { get; set; } = true;

        // Medical Properties
        public string? Instructions { get; set; }
        public string? PreparationRequired { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }

        // Requirements
        public bool RequiresSpecialist { get; set; }
        public string? RequiredEquipment { get; set; }
        public string? RequiredConsent { get; set; }

        // Stock
        public bool IsConsumable { get; set; }
        public int CurrentStock { get; set; }
        public int? ReorderLevel { get; set; }
        public bool IsStockLow { get; set; }

        // Status
        public bool IsActive { get; set; }
        public bool IsTaxable { get; set; } = true;
        public bool IsDiscountable { get; set; } = true;

        // Tax Information
        public string? HSNCode { get; set; }
        public decimal? GSTPercentage { get; set; }

        // Usage Statistics
        public int UsageCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime LastUsed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}