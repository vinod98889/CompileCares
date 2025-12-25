using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Pharmacy;

namespace CompileCares.Core.Entities.Clinical
{
    public class PrescriptionMedicine : BaseEntity
    {
        // References
        public Guid PrescriptionId { get; private set; }
        public Guid MedicineId { get; private set; }
        public Guid DoseId { get; private set; }            // ✅ ADDED: Reference to Dose Master

        // Dosage Information
        public string? CustomDosage { get; private set; }   // ✅ CHANGED: Optional custom dosage
        public int DurationDays { get; private set; }
        public int Quantity { get; private set; }

        // Instructions
        public string? Instructions { get; private set; } // e.g., "Before food", "After food"
        public string? AdditionalNotes { get; private set; }

        // Status
        public bool IsDispensed { get; private set; }
        public DateTime? DispensedDate { get; private set; }
        public string? DispensedBy { get; private set; }

        // Navigation Properties
        public virtual Prescription? Prescription { get; private set; }
        public virtual Medicine? Medicine { get; private set; }
        public virtual Dose? Dose { get; private set; }

        // Private constructor for EF Core
        private PrescriptionMedicine() { }

        // Main Constructor (using DoseId)
        public PrescriptionMedicine(
            Guid prescriptionId,
            Guid medicineId,
            Guid doseId,
            int durationDays,
            int quantity = 1,
            string? instructions = null,
            string? customDosage = null)                    // ✅ ADDED: Optional custom dosage
        {
            ValidateInput(prescriptionId, medicineId, doseId, durationDays, quantity);

            PrescriptionId = prescriptionId;
            MedicineId = medicineId;
            DoseId = doseId;                               // ✅ USING: DoseId instead of Dosage string
            DurationDays = durationDays;
            Quantity = quantity;
            Instructions = instructions;
            CustomDosage = customDosage;                   // ✅ STORING: Custom dosage if provided
            IsDispensed = false;
        }

        // Validation
        private static void ValidateInput(
            Guid prescriptionId,
            Guid medicineId,
            Guid doseId,
            int durationDays,
            int quantity)
        {
            if (prescriptionId == Guid.Empty)
                throw new ArgumentException("Prescription ID is required");

            if (medicineId == Guid.Empty)
                throw new ArgumentException("Medicine ID is required");

            if (doseId == Guid.Empty)                      // ✅ CHANGED: Validate DoseId
                throw new ArgumentException("Dose ID is required");

            if (durationDays <= 0)
                throw new ArgumentException("Duration days must be positive");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");
        }

        // ========== DOMAIN METHODS ==========

        // Update dosage information (now using DoseId)
        public void UpdateDosageInfo(
            Guid doseId,
            int durationDays,
            int quantity,
            string? instructions = null,
            string? customDosage = null,                   // ✅ ADDED: Custom dosage parameter
            Guid? updatedBy = null)
        {
            if (doseId == Guid.Empty)
                throw new ArgumentException("Dose ID is required");

            if (durationDays <= 0)
                throw new ArgumentException("Duration days must be positive");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            DoseId = doseId;                               // ✅ USING: DoseId
            DurationDays = durationDays;
            Quantity = quantity;
            Instructions = instructions;
            CustomDosage = customDosage;                   // ✅ STORING: Custom dosage

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Get the actual dosage string (from Dose or custom)
        public string GetDosageDisplay()
        {
            // If custom dosage provided, use it
            if (!string.IsNullOrWhiteSpace(CustomDosage))
                return CustomDosage;

            // Otherwise use the Dose's code
            return Dose?.Code ?? "N/A";
        }

        // Get the dosage name (user-friendly)
        public string GetDosageName()
        {
            // If custom dosage provided, return as-is
            if (!string.IsNullOrWhiteSpace(CustomDosage))
                return CustomDosage;

            // Otherwise use the Dose's name
            return Dose?.Name ?? "Unknown Dosage";
        }

        // Add additional notes
        public void AddNotes(string notes, Guid? updatedBy = null)
        {
            AdditionalNotes = string.IsNullOrEmpty(AdditionalNotes)
                ? notes
                : $"{AdditionalNotes}\n{notes}";

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Mark as dispensed
        public void MarkAsDispensed(string dispensedBy, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(dispensedBy))
                throw new ArgumentException("Dispenser name is required");

            if (IsDispensed)
                throw new InvalidOperationException("Medicine is already dispensed");

            IsDispensed = true;
            DispensedDate = DateTime.UtcNow;
            DispensedBy = dispensedBy;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Calculate total quantity needed
        public int CalculateTotalUnits()
        {
            // If using Dose master, check if it's "SOS" (as needed)
            if (Dose != null && Dose.Code == "SOS")
                return Quantity; // For SOS, quantity is total units needed

            // Parse dosage from Dose code or custom dosage
            string dosageToParse = !string.IsNullOrWhiteSpace(CustomDosage)
                ? CustomDosage
                : Dose?.Code ?? "";

            var dosageParts = dosageToParse.Split('-');
            int dosesPerDay = 0;

            foreach (var part in dosageParts)
            {
                if (int.TryParse(part, out int dose) && dose > 0)
                {
                    dosesPerDay += dose;
                }
            }

            // If dosage parsing fails, assume 1 dose per day
            if (dosesPerDay == 0)
                dosesPerDay = 1;

            return dosesPerDay * DurationDays * Quantity;
        }

        // Check if this uses a custom dosage
        public bool HasCustomDosage()
        {
            return !string.IsNullOrWhiteSpace(CustomDosage);
        }

        // Update medicine
        public void UpdateMedicine(Guid medicineId, Guid? updatedBy = null)
        {
            if (medicineId == Guid.Empty)
                throw new ArgumentException("Medicine ID is required");

            MedicineId = medicineId;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Factory method for testing
        public static PrescriptionMedicine CreateForTest(
            Guid id,
            Guid prescriptionId,
            Guid medicineId,
            Guid doseId,
            int durationDays)
        {
            var prescriptionMedicine = new PrescriptionMedicine(
                prescriptionId, medicineId, doseId, durationDays);

            prescriptionMedicine.SetId(id);
            return prescriptionMedicine;
        }

        // Factory method with custom dosage
        public static PrescriptionMedicine CreateWithCustomDosage(
            Guid id,
            Guid prescriptionId,
            Guid medicineId,
            string customDosage,
            int durationDays,
            int quantity = 1)
        {
            // For custom dosage, we need a dummy DoseId
            // In real system, you might have a "Custom" dose in master
            var dummyDoseId = Guid.Empty; // This should be a real "Custom" dose ID

            var prescriptionMedicine = new PrescriptionMedicine(
                prescriptionId, medicineId, dummyDoseId, durationDays, quantity,
                customDosage: customDosage);

            prescriptionMedicine.SetId(id);
            return prescriptionMedicine;
        }
    }
}