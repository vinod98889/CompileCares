namespace CompileCares.Application.Features.Templates.DTOs
{
    public class TemplateComplaintDto
    {
        public Guid Id { get; set; }
        public Guid? ComplaintId { get; set; }
        public string? ComplaintName { get; set; }
        public string? CustomText { get; set; }
        public string DisplayText => CustomText ?? ComplaintName ?? "Unknown";

        public string? Duration { get; set; }
        public string? Severity { get; set; }

        public bool IsFromMaster => ComplaintId.HasValue;
    }
}