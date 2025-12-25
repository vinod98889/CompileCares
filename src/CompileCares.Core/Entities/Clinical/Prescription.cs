using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Patients;
using CompileCares.Core.Entities.Pharmacy;
using CompileCares.Core.Entities.Templates;
using CompileCares.Shared.Enums;
using System;

namespace CompileCares.Core.Entities.Clinical
{
    public class Prescription : BaseEntity
    {
        // Core References (All required)
        public Guid PatientId { get; private set; }
        public Guid DoctorId { get; private set; }
        public Guid OPDVisitId { get; private set; }

        // Prescription Information
        public string PrescriptionNumber { get; private set; }
        public DateTime PrescriptionDate { get; private set; }
        public string? Diagnosis { get; private set; }
        public string? Instructions { get; private set; }
        public string? FollowUpInstructions { get; private set; }

        // ✅ REMOVED: Advice property (now stored in PrescriptionAdvised junction table)

        // Status
        public PrescriptionStatus Status { get; private set; }

        // Validity
        public int ValidityDays { get; private set; } = 7; // Default 7 days
        public DateTime? ValidUntil { get; private set; }

        // Navigation Properties
        public virtual OPDVisit? OPDVisit { get; private set; }
        public virtual Patient? Patient { get; private set; }
        public virtual Doctor? Doctor { get; private set; }
        public virtual ICollection<PrescriptionMedicine> Medicines { get; private set; } = new List<PrescriptionMedicine>();

        // ✅ ADDED: New navigation properties for complaints and advice
        public virtual ICollection<PatientComplaint> Complaints { get; private set; } = new List<PatientComplaint>();
        public virtual ICollection<PrescriptionAdvised> AdvisedItems { get; private set; } = new List<PrescriptionAdvised>();

        // Private constructor for EF Core
        private Prescription() { }

        // Main Constructor
        public Prescription(
            Guid patientId,
            Guid doctorId,
            Guid opdVisitId,
            Guid? createdBy = null)
        {
            ValidateInput(patientId, doctorId, opdVisitId);

            PatientId = patientId;
            DoctorId = doctorId;
            OPDVisitId = opdVisitId;
            PrescriptionDate = DateTime.UtcNow;
            Status = PrescriptionStatus.Active;
            PrescriptionNumber = GeneratePrescriptionNumber();
            ValidUntil = DateTime.UtcNow.AddDays(ValidityDays);

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Validation
        private static void ValidateInput(Guid patientId, Guid doctorId, Guid opdVisitId)
        {
            if (patientId == Guid.Empty)
                throw new ArgumentException("Patient ID is required");

            if (doctorId == Guid.Empty)
                throw new ArgumentException("Doctor ID is required");

            if (opdVisitId == Guid.Empty)
                throw new ArgumentException("OPD Visit ID is required");
        }

        // Generate prescription number
        private static string GeneratePrescriptionNumber()
        {
            return $"RX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        // ========== DOMAIN METHODS ==========

        // Update diagnosis
        public void UpdateDiagnosis(string diagnosis, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
                throw new ArgumentException("Diagnosis is required");

            Diagnosis = diagnosis;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update instructions
        public void UpdateInstructions(string instructions, Guid? updatedBy = null)
        {
            Instructions = instructions;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // ✅ REMOVED: UpdateAdvice method (now using AddAdvised/RemoveAdvised)

        // Set validity
        public void SetValidity(int validityDays, Guid? updatedBy = null)
        {
            if (validityDays <= 0)
                throw new ArgumentException("Validity days must be positive");

            ValidityDays = validityDays;
            ValidUntil = DateTime.UtcNow.AddDays(validityDays);

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // ✅ UPDATED: Add medicine with DoseId
        public void AddMedicine(
            Guid medicineId,
            Guid doseId,
            int durationDays,
            int quantity = 1,
            string? instructions = null,
            string? customDosage = null,
            Guid? addedBy = null)
        {
            var prescriptionMedicine = new PrescriptionMedicine(
                Id,
                medicineId,
                doseId,
                durationDays,
                quantity,
                instructions,
                customDosage);

            Medicines.Add(prescriptionMedicine);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // ✅ ADDED: Add medicine with Medicine and Dose objects
        public void AddMedicine(
            Medicine medicine,
            Dose dose,
            int durationDays,
            int quantity = 1,
            string? instructions = null,
            string? customDosage = null,
            Guid? addedBy = null)
        {
            var prescriptionMedicine = new PrescriptionMedicine(
                Id,
                medicine.Id,
                dose.Id,
                durationDays,
                quantity,
                instructions,
                customDosage);

            Medicines.Add(prescriptionMedicine);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Remove medicine
        public void RemoveMedicine(Guid prescriptionMedicineId, Guid? removedBy = null)
        {
            var medicine = Medicines.FirstOrDefault(m => m.Id == prescriptionMedicineId);
            if (medicine != null)
            {
                Medicines.Remove(medicine);

                if (removedBy.HasValue)
                    SetUpdatedBy(removedBy.Value);
            }
        }

       

        // ✅ ADDED: Remove complaint
        public void RemoveComplaint(Guid patientComplaintId, Guid? removedBy = null)
        {
            var complaint = Complaints.FirstOrDefault(c => c.Id == patientComplaintId);
            if (complaint != null)
            {
                Complaints.Remove(complaint);

                if (removedBy.HasValue)
                    SetUpdatedBy(removedBy.Value);
            }
        }

        // In Prescription.cs - Update AddComplaint method:
        public void AddComplaint(
            Guid? complaintId,
            string? customComplaint = null,
            string? duration = null,
            string? severity = null,
            Guid? addedBy = null)
        {
            if (complaintId == null && string.IsNullOrWhiteSpace(customComplaint))
                throw new ArgumentException("Either ComplaintId or CustomComplaint is required");

            // ✅ FIXED: Pass all parameters to constructor
            var patientComplaint = new PatientComplaint(
                Id,               // prescriptionId
                complaintId,      // complaintId (nullable)
                customComplaint,  // customComplaint (nullable)
                duration,         // duration (nullable)
                severity);        // severity (nullable)

            Complaints.Add(patientComplaint);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Update AddAdvised method:
        public void AddAdvised(
            Guid? advisedId,
            string? customAdvice = null,
            Guid? addedBy = null)
        {
            if (advisedId == null && string.IsNullOrWhiteSpace(customAdvice))
                throw new ArgumentException("Either AdvisedId or CustomAdvice is required");

            // ✅ FIXED: Pass all parameters to constructor
            var prescriptionAdvised = new PrescriptionAdvised(
                Id,           // prescriptionId
                advisedId,    // advisedId (nullable)
                customAdvice);// customAdvice (nullable)

            AdvisedItems.Add(prescriptionAdvised);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Update AddComplaint with Complaint object:
        public void AddComplaint(
            Complaint complaint,
            string? duration = null,
            string? severity = null,
            Guid? addedBy = null)
        {
            // ✅ FIXED: Pass Complaint.Id instead of null
            var patientComplaint = new PatientComplaint(
                Id,
                complaint.Id,  // Pass the complaint ID
                null,          // Not custom
                duration,
                severity);

            Complaints.Add(patientComplaint);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Update AddAdvised with Advised object:
        public void AddAdvised(
            Advised advised,
            Guid? addedBy = null)
        {
            // ✅ FIXED: Pass Advised.Id instead of null
            var prescriptionAdvised = new PrescriptionAdvised(
                Id,
                advised.Id,  // Pass the advised ID
                null);       // Not custom

            AdvisedItems.Add(prescriptionAdvised);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // ✅ ADDED: Remove advised item
        public void RemoveAdvised(Guid prescriptionAdvisedId, Guid? removedBy = null)
        {
            var advised = AdvisedItems.FirstOrDefault(a => a.Id == prescriptionAdvisedId);
            if (advised != null)
            {
                AdvisedItems.Remove(advised);

                if (removedBy.HasValue)
                    SetUpdatedBy(removedBy.Value);
            }
        }

        // ✅ ADDED: Apply template to prescription
        public void ApplyTemplate(PrescriptionTemplate template, Guid? appliedBy = null)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            // Add template complaints
            foreach (var templateComplaint in template.Complaints)
            {
                // ✅ NOW CORRECT: All parameters match
                AddComplaint(
                    templateComplaint.ComplaintId,      // complaintId
                    templateComplaint.CustomText,       // customComplaint
                    templateComplaint.Duration,         // duration
                    templateComplaint.Severity,         // severity
                    appliedBy);                         // addedBy
            }

            // Add template medicines
            foreach (var templateMedicine in template.Medicines)
            {
                AddMedicine(
                    templateMedicine.MedicineId,
                    templateMedicine.DoseId,
                    templateMedicine.DurationDays,
                    templateMedicine.Quantity,
                    templateMedicine.Instructions,
                    null,                              // customDosage
                    appliedBy);                        // addedBy
            }

            // Add template advised items
            foreach (var templateAdvised in template.AdvisedItems)
            {
                // ✅ NOW CORRECT: All parameters match
                AddAdvised(
                    templateAdvised.AdvisedId,         // advisedId
                    templateAdvised.CustomText,        // customAdvice
                    appliedBy);                        // addedBy
            }

            if (appliedBy.HasValue)
                SetUpdatedBy(appliedBy.Value);
        }

        // ✅ ADDED: Get all advice text (master + custom)
        public List<string> GetAllAdviceText()
        {
            var adviceList = new List<string>();

            foreach (var advisedItem in AdvisedItems)
            {
                if (!string.IsNullOrWhiteSpace(advisedItem.CustomAdvice))
                {
                    adviceList.Add(advisedItem.CustomAdvice);
                }
                else if (advisedItem.Advised != null)
                {
                    adviceList.Add(advisedItem.Advised.Text);
                }
            }

            return adviceList;
        }

        // ✅ ADDED: Get all complaint text (master + custom)
        public List<string> GetAllComplaintText()
        {
            var complaintList = new List<string>();

            foreach (var complaint in Complaints)
            {
                if (!string.IsNullOrWhiteSpace(complaint.CustomComplaint))
                {
                    complaintList.Add(complaint.CustomComplaint);
                }
                else if (complaint.Complaint != null)
                {
                    complaintList.Add(complaint.Complaint.Name);
                }
            }

            return complaintList;
        }

        // Complete prescription
        public void Complete(Guid? updatedBy = null)
        {
            Status = PrescriptionStatus.Completed;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Cancel prescription
        public void Cancel(string reason, Guid? updatedBy = null)
        {
            Status = PrescriptionStatus.Cancelled;
            // Note: We removed Advice property, cancellation note can go in Instructions
            Instructions = $"[CANCELLED: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {reason}\n{Instructions}";

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Check if prescription is valid
        public bool IsValid()
        {
            if (Status != PrescriptionStatus.Active)
                return false;

            if (ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow)
                return false;

            return true;
        }

        // ✅ ADDED: Check if prescription has medicines
        public bool HasMedicines()
        {
            return Medicines.Any();
        }

        // ✅ ADDED: Check if prescription has complaints
        public bool HasComplaints()
        {
            return Complaints.Any();
        }

        // ✅ ADDED: Check if prescription has advice
        public bool HasAdvice()
        {
            return AdvisedItems.Any();
        }

        // Factory method for testing
        public static Prescription CreateForTest(
            Guid id,
            Guid patientId,
            Guid doctorId,
            Guid opdVisitId)
        {
            var prescription = new Prescription(patientId, doctorId, opdVisitId);
            prescription.SetId(id);
            return prescription;
        }
    }
}