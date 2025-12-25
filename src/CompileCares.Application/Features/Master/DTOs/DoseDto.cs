namespace CompileCares.Application.Features.Master.DTOs
{
    public class DoseDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty; // "OD", "BD", "TDS", "1-0-1"
        public string Name { get; set; } = string.Empty; // "Once Daily", "Twice Daily"
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }

        // Usage Statistics
        public int UsageCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}