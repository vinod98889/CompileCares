namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? PatientNumber { get; set; }
        public string? Mobile { get; set; }
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }
}