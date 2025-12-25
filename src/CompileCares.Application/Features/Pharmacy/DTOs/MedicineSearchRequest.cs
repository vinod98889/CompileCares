namespace CompileCares.Application.Features.Pharmacy.DTOs
{
    public class MedicineSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? TherapeuticClass { get; set; }
        public string? Manufacturer { get; set; }
        public bool? RequiresPrescription { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsExpired { get; set; }
        public bool? IsLowStock { get; set; }
        public bool? IsOutOfStock { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;
    }
}