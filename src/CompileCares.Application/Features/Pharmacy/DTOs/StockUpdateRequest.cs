using CompileCares.Shared.Enums;

namespace CompileCares.Application.Features.Pharmacy.DTOs
{
    public class StockUpdateRequest
    {
        public StockAction Action { get; set; }
        public int Quantity { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        public string? Supplier { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Notes { get; set; }
    }        
}