namespace CompileCares.Application.Features.Doctors.DTOs
{
    public class DoctorSummaryDto
    {
        public Guid Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsActive { get; set; }
        public int TodaysVisits { get; set; }
        public int TotalVisits { get; set; }
    }
}