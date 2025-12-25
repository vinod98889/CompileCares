using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Visits.DTOs
{
    public class VisitStatisticsDto
    {
        public int TotalVisits { get; set; }
        public int TodaysVisits { get; set; }
        public int ThisWeekVisits { get; set; }
        public int ThisMonthVisits { get; set; }

        public Dictionary<VisitType, int> VisitsByType { get; set; } = new();
        public Dictionary<VisitStatus, int> VisitsByStatus { get; set; } = new();
        public Dictionary<string, int> VisitsByDoctor { get; set; } = new();

        public int CompletedVisits { get; set; }
        public int CancelledVisits { get; set; }
        public int NoShowVisits { get; set; }

        public decimal AverageConsultationDuration { get; set; } // in minutes
        public int VisitsWithFollowUp { get; set; }
        public int VisitsWithPrescription { get; set; }

        // Daily visits for last 7 days
        public Dictionary<DateTime, int> DailyVisits { get; set; } = new();
    }
}