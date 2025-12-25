using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Billing.DTOs
{
    public class BillSearchRequest
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid? VisitId { get; set; }
        public string? BillNumber { get; set; }
        public BillStatus? Status { get; set; }
        public DateTime? BillDateFrom { get; set; }
        public DateTime? BillDateTo { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool? HasDue { get; set; }
        public string? PaymentMode { get; set; }
        public bool? HasInsurance { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "BillDate";
        public bool SortDescending { get; set; } = true;
    }
}