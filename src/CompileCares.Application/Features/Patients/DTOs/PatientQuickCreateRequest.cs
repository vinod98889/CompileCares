using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientQuickCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public Title Title { get; set; }
        public Gender Gender { get; set; }
        public int? Age { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
    }
}