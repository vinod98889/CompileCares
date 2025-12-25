namespace CompileCares.Application.Features.Billing.DTOs
{
    public class AddPaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty; // Cash, Card, UPI, Insurance

        // Card Details (if card payment)
        public string? CardNumber { get; set; }
        public string? CardHolderName { get; set; }
        public string? CardType { get; set; } // Visa, MasterCard
        public string? CardExpiry { get; set; }
        public string? CVV { get; set; }

        // UPI Details
        public string? UPIId { get; set; }
        public string? UPIReference { get; set; }

        // Cheque Details
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }

        // Insurance Details
        public string? InsuranceProvider { get; set; }
        public string? InsuranceClaimNumber { get; set; }

        // Transaction Reference
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
    }
}