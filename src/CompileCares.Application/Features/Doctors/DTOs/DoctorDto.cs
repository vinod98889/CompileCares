using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Doctors.DTOs
{
    public class DoctorDto
    {
        public Guid Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Title Title { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public DoctorType DoctorType { get; set; }

        // Professional Info
        public string? Qualification { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string? Department { get; set; }
        public int YearsOfExperience { get; set; }

        // Contact Info
        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }

        // Work Details
        public string? HospitalName { get; set; }
        public string? HospitalAddress { get; set; }
        public string? ConsultationHours { get; set; }
        public string? AvailableDays { get; set; }

        // Fees
        public decimal ConsultationFee { get; set; }
        public decimal FollowUpFee { get; set; }
        public decimal EmergencyFee { get; set; }

        // Status
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsVerified { get; set; }

        // Signature
        public string? SignaturePath { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}