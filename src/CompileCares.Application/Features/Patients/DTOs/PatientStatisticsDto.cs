namespace CompileCares.Application.Features.Patients.DTOs
{
    public class PatientStatisticsDto
    {
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int NewPatientsThisWeek { get; set; }
        public Dictionary<string, int> PatientsByGender { get; set; } = new();
        public Dictionary<string, int> PatientsByAgeGroup { get; set; } = new();
        public Dictionary<string, int> PatientsByCity { get; set; } = new();
        public int PatientsWithAllergies { get; set; }
    }
}