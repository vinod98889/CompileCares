using CompileCares.Core.Common;
using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Patients;
using CompileCares.Shared.Enums;
using System;

namespace CompileCares.Core.Entities.Clinical
{
    public class OPDVisit : BaseEntity
    {
        // Core References
        public Guid PatientId { get; private set; }
        public Guid DoctorId { get; private set; }

        // Visit Information
        public string VisitNumber { get; private set; }
        public DateTime VisitDate { get; private set; }
        public VisitType VisitType { get; private set; }
        public VisitStatus Status { get; private set; }

        // Clinical Information
        public string? ChiefComplaint { get; private set; }
        public string? HistoryOfPresentIllness { get; private set; }
        public string? PastMedicalHistory { get; private set; }
        public string? FamilyHistory { get; private set; }
        public string? ClinicalNotes { get; private set; }
        public string? Diagnosis { get; private set; }
        public string? TreatmentPlan { get; private set; }

        // Vitals (Snapshot at time of visit)
        public string? BloodPressure { get; private set; } // "120/80"
        public decimal? Temperature { get; private set; } // in Celsius
        public int? Pulse { get; private set; } // beats per minute
        public int? RespiratoryRate { get; private set; } // breaths per minute
        public int? SPO2 { get; private set; } // Oxygen saturation %
        public decimal? Weight { get; private set; } // in kg
        public decimal? Height { get; private set; } // in cm
        public decimal? BMI { get; private set; } // Calculated

        // Examination Findings
        public string? GeneralExamination { get; private set; }
        public string? SystemicExamination { get; private set; }
        public string? LocalExamination { get; private set; }

        // Investigations Ordered
        public string? InvestigationsOrdered { get; private set; }

        // Advice & Follow-up
        public string? Advice { get; private set; }
        public string? FollowUpInstructions { get; private set; }
        public DateTime? FollowUpDate { get; private set; }
        public int? FollowUpDays { get; private set; }

        // Referrals
        public Guid? ReferredToDoctorId { get; private set; }
        public string? ReferralReason { get; private set; }
        
        // Foreign keys
        public Guid? PrescriptionId { get; private set; }
        public Guid? OPDBillId { get; private set; }

        // Duration
        public TimeSpan? ConsultationDuration { get; private set; }

        // Navigation Properties
        public virtual Patient? Patient { get; private set; }
        public virtual Doctor? Doctor { get; private set; }
        public virtual Doctor? ReferredToDoctor { get; private set; }
        public virtual Prescription? Prescription { get; private set; }
        public virtual OPDBill? Bill { get; private set; }

        // Private constructor for EF Core
        private OPDVisit() { }

        // Main Constructor
        public OPDVisit(
            Guid patientId,
            Guid doctorId,
            VisitType visitType = VisitType.New,
            Guid? createdBy = null)
        {
            ValidateInput(patientId, doctorId);

            PatientId = patientId;
            DoctorId = doctorId;
            VisitType = visitType;
            VisitDate = DateTime.UtcNow;
            Status = VisitStatus.CheckedIn; // Automatically checked in when created
            VisitNumber = GenerateVisitNumber();

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Validation
        private static void ValidateInput(Guid patientId, Guid doctorId)
        {
            if (patientId == Guid.Empty)
                throw new ArgumentException("Patient ID is required", nameof(patientId));

            if (doctorId == Guid.Empty)
                throw new ArgumentException("Doctor ID is required", nameof(doctorId));
        }

        // Generate visit number
        private static string GenerateVisitNumber()
        {
            return $"OPD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        // ========== DOMAIN METHODS ==========

        // Start consultation
        public void StartConsultation(Guid? updatedBy = null)
        {
            if (Status != VisitStatus.CheckedIn)
                throw new InvalidOperationException($"Cannot start consultation from {Status} status");

            Status = VisitStatus.InProgress;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update chief complaint and history
        public void UpdateChiefComplaint(
            string chiefComplaint,
            string? historyOfPresentIllness = null,
            string? pastMedicalHistory = null,
            string? familyHistory = null,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(chiefComplaint))
                throw new ArgumentException("Chief complaint is required");

            ChiefComplaint = chiefComplaint;
            HistoryOfPresentIllness = historyOfPresentIllness;
            PastMedicalHistory = pastMedicalHistory;
            FamilyHistory = familyHistory;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update vitals
        public void UpdateVitals(
            string? bloodPressure = null,
            decimal? temperature = null,
            int? pulse = null,
            int? respiratoryRate = null,
            int? spo2 = null,
            decimal? weight = null,
            decimal? height = null,
            Guid? updatedBy = null)
        {
            BloodPressure = bloodPressure;
            Temperature = temperature;
            Pulse = pulse;
            RespiratoryRate = respiratoryRate;
            SPO2 = spo2;
            Weight = weight;
            Height = height;

            // Calculate BMI if weight and height provided
            if (weight.HasValue && height.HasValue && height > 0)
            {
                var heightInMeters = height.Value / 100; // Convert cm to meters
                BMI = weight.Value / (heightInMeters * heightInMeters);
            }

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update examination findings
        public void UpdateExaminationFindings(
            string? generalExamination = null,
            string? systemicExamination = null,
            string? localExamination = null,
            Guid? updatedBy = null)
        {
            GeneralExamination = generalExamination;
            SystemicExamination = systemicExamination;
            LocalExamination = localExamination;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set diagnosis
        public void SetDiagnosis(string diagnosis, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
                throw new ArgumentException("Diagnosis is required");

            Diagnosis = diagnosis;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set treatment plan
        public void SetTreatmentPlan(string treatmentPlan, Guid? updatedBy = null)
        {
            TreatmentPlan = treatmentPlan;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Order investigations
        public void OrderInvestigations(string investigations, Guid? updatedBy = null)
        {
            InvestigationsOrdered = investigations;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add clinical notes
        public void AddClinicalNotes(string notes, Guid? updatedBy = null)
        {
            ClinicalNotes = string.IsNullOrEmpty(ClinicalNotes)
                ? notes
                : $"{ClinicalNotes}\n{notes}";

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set follow-up
        public void SetFollowUp(
            DateTime followUpDate,
            string? instructions = null,
            int? followUpDays = null,
            Guid? updatedBy = null)
        {
            FollowUpDate = followUpDate;
            FollowUpInstructions = instructions;
            FollowUpDays = followUpDays;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Refer to another doctor
        public void ReferToDoctor(Guid referredToDoctorId, string reason, Guid? updatedBy = null)
        {
            if (referredToDoctorId == Guid.Empty)
                throw new ArgumentException("Referred doctor ID is required");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Referral reason is required");

            ReferredToDoctorId = referredToDoctorId;
            ReferralReason = reason;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Complete consultation
        public void CompleteConsultation(TimeSpan? consultationDuration = null, Guid? updatedBy = null)
        {
            if (Status != VisitStatus.InProgress)
                throw new InvalidOperationException($"Cannot complete consultation from {Status} status");

            Status = VisitStatus.Completed;
            ConsultationDuration = consultationDuration;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Cancel visit
        public void CancelVisit(string reason, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason is required");

            Status = VisitStatus.Cancelled;
            ClinicalNotes = $"[CANCELLED: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {reason}\n{ClinicalNotes}";

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Mark as no-show
        public void MarkAsNoShow(Guid? updatedBy = null)
        {
            Status = VisitStatus.NoShow;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Link prescription (called from Prescription entity)
        public void LinkPrescription(Prescription prescription)
        {
            if (Prescription != null)
                throw new InvalidOperationException("This visit already has a prescription");

            Prescription = prescription;
        }

        // Link bill
        public void LinkBill(OPDBill bill)
        {
            if (OPDBillId.HasValue)
                throw new InvalidOperationException("This visit already has a bill");

            Bill = bill;
            OPDBillId = bill.Id;
        }

        // Factory method for testing
        public static OPDVisit CreateForTest(
            Guid id,
            Guid patientId,
            Guid doctorId,
            VisitType visitType = VisitType.New)
        {
            var visit = new OPDVisit(patientId, doctorId, visitType);
            visit.SetId(id);
            return visit;
        }
    }
}