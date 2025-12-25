namespace CompileCares.Application.Features.Billing.DTOs
{
    public class BillingStatisticsDto
    {
        public int TotalBills { get; set; }
        public int PaidBills { get; set; }
        public int UnpaidBills { get; set; }
        public int PartiallyPaidBills { get; set; }
        public int CancelledBills { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal TotalTaxCollected { get; set; }
        public decimal TotalCommissionPaid { get; set; }

        public decimal TodaysRevenue { get; set; }
        public decimal ThisWeekRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }

        public Dictionary<string, decimal> RevenueByCategory { get; set; } = new()
        {
            { "Consultation", 0 },
            { "Procedure", 0 },
            { "Medicine", 0 },
            { "LabTest", 0 },
            { "Other", 0 }
        };

        public Dictionary<string, int> PaymentModeDistribution { get; set; } = new();
        public Dictionary<string, decimal> DailyRevenue { get; set; } = new();

        public List<TopBillingDoctorDto> TopBillingDoctors { get; set; } = new();
    }

    public class TopBillingDoctorDto
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int TotalBills { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal CollectionRate { get; set; } // Percentage
    }
}