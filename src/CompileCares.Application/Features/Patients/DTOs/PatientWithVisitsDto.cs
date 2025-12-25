using CompileCares.Application.Features.Visits.DTOs;

namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientWithVisitsDto : PatientDto
    {
        public List<VisitSummaryDto> Visits { get; set; } = new();
        public int TotalVisits { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public decimal TotalBillingAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
    }
}