namespace CompileCares.Application.Features.Visits.DTOs
{
    public class VisitVitalsDto
    {
        public string? BloodPressure { get; set; }
        public decimal? Temperature { get; set; }
        public int? Pulse { get; set; }
        public int? RespiratoryRate { get; set; }
        public int? SPO2 { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? BMI { get; set; }
        public DateTime RecordedAt { get; set; }
        public string? RecordedBy { get; set; }
    }
}