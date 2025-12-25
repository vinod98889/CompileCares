namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class PrescriptionAdvisedDto
    {
        public Guid Id { get; set; }
        public Guid? AdvisedId { get; set; }
        public string? AdvisedText { get; set; }
        public string? CustomAdvice { get; set; }
        public string DisplayAdvice => CustomAdvice ?? AdvisedText ?? "Unknown";

        public string? Category { get; set; }

        public bool IsFromMaster => AdvisedId.HasValue;
    }
}