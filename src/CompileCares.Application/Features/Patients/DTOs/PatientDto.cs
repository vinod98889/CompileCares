using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientDto
    {
        public Guid Id { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Title Title { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public MaritalStatus MaritalStatus { get; set; }
        public PatientStatus Status { get; set; }

        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }

        public string? BloodGroup { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? Occupation { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}