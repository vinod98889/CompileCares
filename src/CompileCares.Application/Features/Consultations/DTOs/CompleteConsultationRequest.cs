using CompileCares.Application.Features.Billing.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Application.Features.Visits.DTOs;

namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class CompleteConsultationRequest
    {
        // Patient Information (New or Existing)
        public bool IsNewPatient { get; set; }
        public Guid? ExistingPatientId { get; set; }
        public PatientQuickCreateRequest? NewPatient { get; set; }

        // Visit & Clinical Information
        public Guid DoctorId { get; set; }
        public UpdateVisitRequest ConsultationDetails { get; set; } = new();

        // Treatment
        public List<AddMedicineRequest> Medicines { get; set; } = new();
        public List<PrescriptionAdvisedRequest> Advice { get; set; } = new();

        // Template (Optional)
        public bool ApplyTemplate { get; set; }
        public Guid? TemplateId { get; set; }

        // Billing
        public decimal ConsultationFee { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal TaxPercentage { get; set; } = 5.0m;

        // Payment
        public AddPaymentRequest Payment { get; set; } = new();

        // Pharmacy (Optional - if dispensing now)
        public bool DispenseNow { get; set; }
        public string? DispensedBy { get; set; }

        // Follow-up
        public bool SetFollowUp { get; set; }
        public int? FollowUpDays { get; set; }
        public string? FollowUpInstructions { get; set; }

        // Notes
        public string? ConsultationNotes { get; set; }

        public bool OverrideExisting { get; set; } = false;
        public bool AllowMultipleVisitsPerDay { get; set; } = true;
    }
}