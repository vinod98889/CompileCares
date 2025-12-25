namespace CompileCares.Application.Features.Prescriptions.DTOs
{
    public class AddMedicineRequest
    {
        public Guid MedicineId { get; set; }
        public Guid DoseId { get; set; }
        public string? CustomDosage { get; set; }
        public int DurationDays { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Instructions { get; set; }
    }
}