namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientSummaryDto
    {
        public Guid Id { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? BloodGroup { get; set; }
        public DateTime LastVisitDate { get; set; }
        public int TotalVisits { get; set; }
        public bool IsActive { get; set; }
    }
}