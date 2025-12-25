namespace CompileCares.Application.Features.Doctors.DTOs
{
    public class DoctorStatisticsDto
    {
        public int TotalDoctors { get; set; }
        public int ActiveDoctors { get; set; }
        public int AvailableDoctors { get; set; }
        public int VerifiedDoctors { get; set; }

        public Dictionary<string, int> DoctorsBySpecialization { get; set; } = new();
        public Dictionary<string, int> DoctorsByDepartment { get; set; } = new();

        public decimal AverageConsultationFee { get; set; }
        public decimal TotalMonthlyRevenue { get; set; }
        public decimal TotalCommissionPaid { get; set; }

        public List<TopPerformingDoctorDto> TopPerformingDoctors { get; set; } = new();
    }

    public class TopPerformingDoctorDto
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int TotalVisits { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal PatientSatisfactionScore { get; set; }
    }
}