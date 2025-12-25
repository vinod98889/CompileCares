using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Visits.DTOs
{
    public class CreateVisitRequest
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public VisitType VisitType { get; set; } = VisitType.New;
        public string? ChiefComplaint { get; set; }
    }
}