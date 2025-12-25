namespace CompileCares.Application.Features.Master.DTOs
{
    public class MasterSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; } // For OPD items: "Consultation", "Procedure", etc.
        public bool? IsActive { get; set; }
        public bool? IsCommon { get; set; } // For complaints/advised
        public bool? IsConsumable { get; set; } // For OPD items
        public bool? IsLowStock { get; set; } // For OPD items
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
}