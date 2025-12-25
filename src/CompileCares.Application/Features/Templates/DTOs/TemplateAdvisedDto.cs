namespace CompileCares.Application.Features.Templates.DTOs
{
    public class TemplateAdvisedDto
    {
        public Guid Id { get; set; }
        public Guid? AdvisedId { get; set; }
        public string? AdvisedText { get; set; }
        public string? CustomText { get; set; }
        public string DisplayText => CustomText ?? AdvisedText ?? "Unknown";

        public string? Category { get; set; }

        public bool IsFromMaster => AdvisedId.HasValue;
    }
}