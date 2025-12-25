namespace CompileCares.Application.Features.Templates.DTOs
{
    public class TemplateMedicineDto
    {
        public Guid Id { get; set; }
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? Strength { get; set; }

        public Guid DoseId { get; set; }
        public string DoseCode { get; set; } = string.Empty;
        public string DoseName { get; set; } = string.Empty;

        public int DurationDays { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Instructions { get; set; }
    }
}