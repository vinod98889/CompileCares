// File: CompileCares.Application/Features/Master/DTOs/UpdateStockRequest.cs
using CompileCares.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CompileCares.Application.Features.Master.DTOs
{
    public class UpdateStockRequest
    {
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Action is required")]
        public StockAction Action { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Purchase price cannot be negative")]
        public decimal? PurchasePrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters")]
        public string? ReferenceNumber { get; set; }
    }
}