using CompileCares.Core.Entities.Billing;

namespace CompileCares.Application.Services
{
    public interface IBillingService
    {
        Task<OPDBill> CreateBillAsync(Guid visitId, Guid patientId, Guid doctorId, decimal consultationFee, Guid createdBy);
        Task<OPDBill> AddBillItemAsync(Guid billId, Guid itemMasterId, int quantity, decimal? customPrice, Guid updatedBy);
        Task<OPDBill> AddPaymentAsync(Guid billId, decimal amount, string paymentMode, string? transactionId, Guid updatedBy);
        Task<OPDBill> ApplyDiscountAsync(Guid billId, decimal discountPercentage, Guid updatedBy);
        Task<OPDBill> GenerateFinalBillAsync(Guid billId, Guid updatedBy);
        Task<OPDBill> CancelBillAsync(Guid billId, string reason, Guid updatedBy);
        Task<OPDBill> RefundBillAsync(Guid billId, decimal refundAmount, string reason, Guid updatedBy);
        Task<decimal> CalculateDoctorCommissionAsync(Guid billId);
        Task<string> GenerateBillReceiptAsync(Guid billId);
    }
}