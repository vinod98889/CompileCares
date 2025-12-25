namespace CompileCares.Application.Features.Master.DTOs
{
    public class MasterStatisticsDto
    {
        // Complaints
        public int TotalComplaints { get; set; }
        public int ActiveComplaints { get; set; }
        public int CommonComplaints { get; set; }

        // Advised Items
        public int TotalAdvisedItems { get; set; }
        public int ActiveAdvisedItems { get; set; }
        public int CommonAdvisedItems { get; set; }

        // Doses
        public int TotalDoses { get; set; }
        public int ActiveDoses { get; set; }

        // OPD Items
        public int TotalOPDItems { get; set; }
        public int ActiveOPDItems { get; set; }
        public int ConsumableItems { get; set; }
        public int LowStockItems { get; set; }

        // Usage Statistics
        public Dictionary<string, int> MostUsedComplaints { get; set; } = new();
        public Dictionary<string, int> MostUsedAdvised { get; set; } = new();
        public Dictionary<string, int> MostUsedDoses { get; set; } = new();
        public Dictionary<string, int> MostUsedOPDItems { get; set; } = new();

        public Dictionary<string, int> ItemsByType { get; set; } = new();
        public Dictionary<string, int> ItemsByCategory { get; set; } = new();

        public decimal TotalOPDItemValue { get; set; }
        public decimal TotalConsumableStockValue { get; set; }
    }
}