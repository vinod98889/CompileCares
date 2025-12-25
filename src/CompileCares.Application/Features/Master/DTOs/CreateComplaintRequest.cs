namespace CompileCares.Application.Features.Master.DTOs
{
    public class CreateComplaintRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsCommon { get; set; } = true;
    }
}