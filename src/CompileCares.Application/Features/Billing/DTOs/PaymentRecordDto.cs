namespace CompileCares.Application.Features.Billing.DTOs
{
    public class PaymentRecordDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty; // Cash, Card, UPI, Insurance
        public string? TransactionId { get; set; }
        public string? BankName { get; set; }
        public string? CardLastFour { get; set; }
        public string? UPIReference { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }

        public DateTime PaymentDate { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public bool IsRefund { get; set; }
        public string? RefundReason { get; set; }
    }
}