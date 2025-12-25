namespace CompileCares.Application.Features.Pharmacy.DTOs
{
    public class MedicineStatisticsDto
    {
        public int TotalMedicines { get; set; }
        public int ActiveMedicines { get; set; }
        public int ExpiredMedicines { get; set; }

        public int LowStockMedicines { get; set; }
        public int OutOfStockMedicines { get; set; }
        public int CriticalStockMedicines { get; set; }

        public decimal TotalStockValue { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalPotentialRevenue { get; set; }

        public Dictionary<string, int> MedicinesByCategory { get; set; } = new();
        public Dictionary<string, int> MedicinesByType { get; set; } = new();
        public Dictionary<string, int> TopPrescribedMedicines { get; set; } = new();

        public decimal MonthlySales { get; set; }
        public decimal MonthlyProfit { get; set; }
        public decimal AverageProfitMargin { get; set; }

        public List<ExpiringSoonMedicineDto> ExpiringSoon { get; set; } = new();
        public List<LowStockMedicineDto> LowStockList { get; set; } = new();
    }

    public class ExpiringSoonMedicineDto
    {
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public int CurrentStock { get; set; }
        public decimal StockValue { get; set; }
    }

    public class LowStockMedicineDto
    {
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStockLevel { get; set; }
        public int ReorderLevel { get; set; }
        public int Deficiency { get; set; }
        public bool IsCritical { get; set; }
    }
}