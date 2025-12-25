namespace CompileCares.Application.Features.Templates.DTOs
{
    public class TemplateSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public Guid? DoctorId { get; set; }
        public bool? IsPublic { get; set; }
        public bool? HasMedicines { get; set; }
        public bool? HasComplaints { get; set; }
        public bool? HasAdvice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;
    }
}