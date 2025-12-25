namespace CompileCares.Application.Features.Templates.DTOs
{
    public class CreateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsPublic { get; set; } = false;

        public List<TemplateComplaintRequest> Complaints { get; set; } = new();
        public List<TemplateMedicineRequest> Medicines { get; set; } = new();
        public List<TemplateAdvisedRequest> AdvisedItems { get; set; } = new();
    }

    public class TemplateComplaintRequest
    {
        public Guid? ComplaintId { get; set; }
        public string? CustomText { get; set; }
        public string? Duration { get; set; }
        public string? Severity { get; set; }
    }

    public class TemplateMedicineRequest
    {
        public Guid MedicineId { get; set; }
        public Guid DoseId { get; set; }
        public int DurationDays { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Instructions { get; set; }
    }

    public class TemplateAdvisedRequest
    {
        public Guid? AdvisedId { get; set; }
        public string? CustomText { get; set; }
    }
}