namespace CompileCares.Application.Features.Patients.DTOs
{
    public class EmergencyContactDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Relationship { get; set; }
    }
}