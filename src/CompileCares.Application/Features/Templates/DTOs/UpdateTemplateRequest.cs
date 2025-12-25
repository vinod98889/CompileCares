namespace CompileCares.Application.Features.Templates.DTOs
{
    public class UpdateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsPublic { get; set; }
    }
}