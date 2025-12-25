namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class ApplyTemplateRequest
    {
        public Guid TemplateId { get; set; }
        public bool OverrideExisting { get; set; } = false;
    }
}