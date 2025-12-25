namespace CompileCares.Application.Features.Templates.DTOs
{
    public class CloneTemplateRequest
    {
        public string NewName { get; set; } = string.Empty;
        public string? NewDescription { get; set; }
        public string? NewCategory { get; set; }
        public bool? IsPublic { get; set; }
        public Guid? TargetDoctorId { get; set; } // If cloning to another doctor
    }
}