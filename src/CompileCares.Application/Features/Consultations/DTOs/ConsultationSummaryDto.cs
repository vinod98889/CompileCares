namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class ConsultationSummaryDto
    {
        public Guid VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public DateTime ConsultationDate { get; set; }

        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;

        public List<string> Medicines { get; set; } = new();
        public List<string> Advice { get; set; } = new();

        public decimal ConsultationFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;

        public bool HasPrescription { get; set; }
        public bool HasBill { get; set; }
        public bool IsDispensed { get; set; }

        public string? FollowUpDate { get; set; }
        public string? Notes { get; set; }
    }
}