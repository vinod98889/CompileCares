namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class PrescriptionMedicineDto
    {
        public Guid Id { get; set; }
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? Strength { get; set; }
        public string? Form { get; set; }

        // Dosage
        public Guid DoseId { get; set; }
        public string DoseCode { get; set; } = string.Empty; // "BD", "TDS", etc.
        public string DoseName { get; set; } = string.Empty; // "Twice Daily"
        public string? CustomDosage { get; set; }
        public string DisplayDosage => CustomDosage ?? DoseName;

        // Duration & Quantity
        public int DurationDays { get; set; }
        public int Quantity { get; set; }
        public int TotalUnits { get; set; }

        // Instructions
        public string? Instructions { get; set; }
        public string? AdditionalNotes { get; set; }

        // Status
        public bool IsDispensed { get; set; }
        public DateTime? DispensedDate { get; set; }
        public string? DispensedBy { get; set; }

        // Stock Info
        public int AvailableStock { get; set; }
        public bool IsInStock => AvailableStock >= TotalUnits;
    }
}