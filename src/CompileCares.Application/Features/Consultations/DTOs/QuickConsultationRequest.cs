namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class QuickConsultationRequest
    {
        // Patient
        public Guid? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? PatientMobile { get; set; }

        // Doctor
        public Guid DoctorId { get; set; }

        // Simple Clinical
        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;

        // Quick Treatment
        public List<string> MedicineCodes { get; set; } = new(); // "PARA500", "VITC"
        public List<string> AdviceCodes { get; set; } = new(); // "REST", "WARM_WATER"

        // Simple Billing
        public decimal Fee { get; set; }
        public string PaymentMode { get; set; } = "Cash";

        // Follow-up
        public int? FollowUpDays { get; set; }
    }
}