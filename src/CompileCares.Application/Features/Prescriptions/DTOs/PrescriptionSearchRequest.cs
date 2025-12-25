using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class PrescriptionSearchRequest
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid? VisitId { get; set; }
        public string? PrescriptionNumber { get; set; }
        public PrescriptionStatus? Status { get; set; }
        public DateTime? PrescriptionDateFrom { get; set; }
        public DateTime? PrescriptionDateTo { get; set; }
        public bool? IsValid { get; set; }
        public bool? IsDispensed { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "PrescriptionDate";
        public bool SortDescending { get; set; } = true;
    }
}