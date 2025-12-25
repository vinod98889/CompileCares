namespace CompileCares.Application.Features.Master.DTOs
{
    public class ComplaintDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public bool IsCommon { get; set; }

        // Usage Statistics
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}