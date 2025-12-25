namespace CompileCares.Application.Features.Billing.DTOs
{
    public class RefundRequest
    {
        public decimal RefundAmount { get; set; }
        public string RefundMode { get; set; } = string.Empty; // Cash, Card, BankTransfer
        public string Reason { get; set; } = string.Empty;

        // Bank Transfer Details
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? IFSCCode { get; set; }
        public string? AccountHolderName { get; set; }

        // Card Refund
        public string? OriginalTransactionId { get; set; }

        public string? Notes { get; set; }
    }
}