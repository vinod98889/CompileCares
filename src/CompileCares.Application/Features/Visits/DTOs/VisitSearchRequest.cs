using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Visits.DTOs
{
    public class VisitSearchRequest
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public string? VisitNumber { get; set; }
        public VisitType? VisitType { get; set; }
        public VisitStatus? Status { get; set; }
        public DateTime? VisitDateFrom { get; set; }
        public DateTime? VisitDateTo { get; set; }
        public bool? HasFollowUp { get; set; }
        public bool? HasPrescription { get; set; }
        public bool? HasBill { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "VisitDate";
        public bool SortDescending { get; set; } = true;
    }
}