namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class PatientComplaintDto
    {
        public Guid Id { get; set; }
        public Guid? ComplaintId { get; set; }
        public string? ComplaintName { get; set; }
        public string? CustomComplaint { get; set; }
        public string DisplayComplaint => CustomComplaint ?? ComplaintName ?? "Unknown";

        public string? Duration { get; set; }
        public string? Severity { get; set; }

        public bool IsFromMaster => ComplaintId.HasValue;
    }
}