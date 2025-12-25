namespace CompileCares.Application.Features.Templates.DTOs
{
    public class PrescriptionTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }

        public Guid? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public bool IsPublic { get; set; }

        // Item Counts
        public int MedicineCount { get; set; }
        public int ComplaintCount { get; set; }
        public int AdviceCount { get; set; }

        // Usage Statistics
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}