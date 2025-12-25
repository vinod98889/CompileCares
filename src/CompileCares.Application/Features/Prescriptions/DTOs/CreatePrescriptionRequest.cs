namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class CreatePrescriptionRequest
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid OPDVisitId { get; set; }

        public string? Diagnosis { get; set; }
        public string? Instructions { get; set; }
        public string? FollowUpInstructions { get; set; }

        public int ValidityDays { get; set; } = 7;

        public List<PrescriptionMedicineRequest> Medicines { get; set; } = new();
        public List<PatientComplaintRequest> Complaints { get; set; } = new();
        public List<PrescriptionAdvisedRequest> AdvisedItems { get; set; } = new();
    }

    public class PrescriptionMedicineRequest
    {
        public Guid MedicineId { get; set; }
        public Guid DoseId { get; set; }
        public string? CustomDosage { get; set; }
        public int DurationDays { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Instructions { get; set; }
    }

    public class PatientComplaintRequest
    {
        public Guid? ComplaintId { get; set; }
        public string? CustomComplaint { get; set; }
        public string? Duration { get; set; }
        public string? Severity { get; set; }
    }

    public class PrescriptionAdvisedRequest
    {
        public Guid? AdvisedId { get; set; }
        public string? CustomAdvice { get; set; }
    }
}