using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Features.Consultations.DTOs
{
    public class ConsultationDashboardDto
    {
        public DateTime Date { get; set; }
        public Guid DoctorId { get; set; }
        public int TodaysConsultationCount { get; set; }
        public decimal TodaysRevenue { get; set; }
        public Dictionary<string, int> CommonDiagnoses { get; set; } = new();
        public string AverageConsultationTime { get; set; } = string.Empty;
        public string PatientSatisfaction { get; set; } = string.Empty;
        public int FollowUpAppointments { get; set; }
    }
}
