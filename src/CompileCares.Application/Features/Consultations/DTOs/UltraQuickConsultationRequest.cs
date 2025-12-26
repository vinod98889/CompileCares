using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class UltraQuickConsultationRequest
    {
        public bool IsNewPatient { get; set; }
        public Application.Features.Patients.DTOs.CreatePatientRequest? NewPatient { get; set; }
        public Guid? ExistingPatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;

        // Simple vitals (optional)
        public string? BloodPressure { get; set; }
        public decimal? Temperature { get; set; }
        public int? Pulse { get; set; }

        public List<UltraQuickMedicine> Medicines { get; set; } = new();
        public decimal ConsultationFee { get; set; }

        // Follow-up
        public bool SetFollowUp { get; set; } = false;
        public int? FollowUpDays { get; set; }
        public string? FollowUpInstructions { get; set; }
    }
    public class UltraQuickMedicine
    {
        public Guid MedicineId { get; set; }
        public Guid DoseId { get; set; }
        public int DurationDays { get; set; } = 5;
        public int Quantity { get; set; } = 1;
        public string? Instructions { get; set; }
    }
}
