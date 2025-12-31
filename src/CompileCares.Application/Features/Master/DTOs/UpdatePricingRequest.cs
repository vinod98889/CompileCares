// File: CompileCares.Application/Features/Master/DTOs/UpdatePricingRequest.cs
using System.ComponentModel.DataAnnotations;

namespace CompileCares.Application.Features.Master.DTOs
{
    public class UpdatePricingRequest
    {
        [Required(ErrorMessage = "Standard price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Standard price cannot be negative")]
        public decimal StandardPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Doctor commission percentage must be between 0 and 100")]
        public decimal? DoctorCommission { get; set; }

        public bool? IsCommissionPercentage { get; set; } = true;

        [Range(0, double.MaxValue, ErrorMessage = "MRP cannot be negative")]
        public decimal? MRP { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Purchase price cannot be negative")]
        public decimal? PurchasePrice { get; set; }

        [Range(0, 100, ErrorMessage = "GST percentage must be between 0 and 100")]
        public decimal? GSTPercentage { get; set; }

        public string? Notes { get; set; }
    }
}