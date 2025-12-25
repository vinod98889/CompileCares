using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Doctors.DTOs
{
    public class UpdateDoctorRequest
    {
        public string Name { get; set; } = string.Empty;
        public Title Title { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DoctorType DoctorType { get; set; }

        public string Specialization { get; set; } = string.Empty;
        public string? Qualification { get; set; }
        public string? Department { get; set; }
        public int YearsOfExperience { get; set; }

        public string Mobile { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }

        public string? HospitalName { get; set; }
        public string? HospitalAddress { get; set; }
        public string? ConsultationHours { get; set; }
        public string? AvailableDays { get; set; }

        public decimal ConsultationFee { get; set; }
        public decimal? FollowUpFee { get; set; }
        public decimal? EmergencyFee { get; set; }

        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsVerified { get; set; }
    }
}