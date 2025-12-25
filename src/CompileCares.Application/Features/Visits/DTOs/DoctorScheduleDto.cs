using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Visits.DTOs
{
    public class DoctorScheduleDto
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;

        public List<VisitSlotDto> TodaysSchedule { get; set; } = new();
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int PendingAppointments { get; set; }
    }

    public class VisitSlotDto
    {
        public Guid VisitId { get; set; }
        public string VisitNumber { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public VisitStatus Status { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool IsUrgent { get; set; }
    }
}