namespace CompileCares.Application.Features.Master.DTOs
{
    public class CreateOPDItemRequest
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }

        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }

        public decimal? DoctorCommission { get; set; }
        public bool? IsCommissionPercentage { get; set; } = true;

        public string? Instructions { get; set; }
        public string? PreparationRequired { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }

        public bool RequiresSpecialist { get; set; } = false;
        public string? RequiredEquipment { get; set; }
        public string? RequiredConsent { get; set; }

        public bool IsConsumable { get; set; } = false;
        public int? InitialStock { get; set; }
        public int? ReorderLevel { get; set; }

        public bool IsTaxable { get; set; } = true;
        public bool IsDiscountable { get; set; } = true;
        public string? HSNCode { get; set; }
        public decimal? GSTPercentage { get; set; }
    }
}