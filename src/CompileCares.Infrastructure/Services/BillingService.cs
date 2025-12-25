using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Features.Billing.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Master;

namespace CompileCares.Infrastructure.Services
{
    public class BillingService : IBillingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BillingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OPDBill> CreateBillAsync(Guid visitId, Guid patientId, Guid doctorId, decimal consultationFee, Guid createdBy)
        {
            var bill = new OPDBill(visitId, patientId, doctorId, consultationFee, createdBy);
            await _unitOfWork.Repository<OPDBill>().AddAsync(bill);
            await _unitOfWork.SaveChangesAsync();
            return bill;
        }

        public async Task<OPDBill> AddBillItemAsync(Guid billId, Guid itemMasterId, int quantity, decimal? customPrice, Guid updatedBy)
        {
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            var itemMaster = await _unitOfWork.Repository<OPDItemMaster>().GetByIdAsync(itemMasterId);
            if (itemMaster == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDItemMaster), itemMasterId);

            bill.AddBillItem(itemMaster, quantity, customPrice, updatedBy);

            await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
            await _unitOfWork.SaveChangesAsync();

            return bill;
        }

        public async Task<OPDBill> AddPaymentAsync(Guid billId, decimal amount, string paymentMode, string? transactionId, Guid updatedBy)
        {
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            bill.AddPayment(amount, paymentMode, transactionId, updatedBy);

            await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
            await _unitOfWork.SaveChangesAsync();

            return bill;
        }

        public async Task<OPDBill> ApplyDiscountAsync(Guid billId, decimal discountPercentage, Guid updatedBy)
        {
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            bill.UpdateDiscount(discountPercentage, updatedBy);

            await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
            await _unitOfWork.SaveChangesAsync();

            return bill;
        }

        public async Task<OPDBill> GenerateFinalBillAsync(Guid billId, Guid updatedBy)
        {
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            bill.GenerateBill(updatedBy);

            await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
            await _unitOfWork.SaveChangesAsync();

            return bill;
        }

        public async Task<OPDBill> CancelBillAsync(Guid billId, string reason, Guid updatedBy)
        {
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            bill.CancelBill(reason, updatedBy);

            await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
            await _unitOfWork.SaveChangesAsync();

            return bill;
        }

        public async Task<OPDBill> RefundBillAsync(Guid billId, decimal refundAmount, string reason, Guid updatedBy)
        {
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            bill.RefundBill(refundAmount, reason, updatedBy);

            await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
            await _unitOfWork.SaveChangesAsync();

            return bill;
        }

        public async Task<decimal> CalculateDoctorCommissionAsync(Guid billId)
        {
            var bill = await _unitOfWork.Bills.GetWithItemsAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            return bill.GetTotalDoctorCommission();
        }

        public async Task<string> GenerateBillReceiptAsync(Guid billId)
        {
            var bill = await _unitOfWork.Bills.GetWithItemsAsync(billId);
            if (bill == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDBill), billId);

            return GenerateReceiptText(bill);
        }

        private string GenerateReceiptText(OPDBill bill)
        {
            var receipt = $"TAX INVOICE / RECEIPT\n" +
                         $"=====================\n" +
                         $"Bill #: {bill.BillNumber}\n" +
                         $"Date: {bill.BillDate:dd/MM/yyyy HH:mm}\n" +
                         $"Patient: {bill.Patient?.Name} ({bill.Patient?.PatientNumber})\n" +
                         $"Doctor: {bill.Doctor?.Name}\n" +
                         $"Visit #: {bill.OPDVisit?.VisitNumber}\n\n" +
                         $"BREAKDOWN:\n" +
                         $"----------\n";

            // Add items
            foreach (var item in bill.BillItems)
            {
                receipt += $"{item.ItemName} x {item.Quantity}: ₹{item.GetTotalWithTax}\n";
            }

            receipt += $"\n" +
                      $"SUBTOTAL: ₹{bill.SubTotal}\n" +
                      $"DISCOUNT ({bill.DiscountPercentage}%): ₹{bill.DiscountAmount}\n" +
                      $"TAX ({bill.TaxPercentage}%): ₹{bill.TaxAmount}\n" +
                      $"TOTAL: ₹{bill.TotalAmount}\n" +
                      $"PAID: ₹{bill.PaidAmount}\n" +
                      $"DUE: ₹{bill.DueAmount}\n" +
                      $"STATUS: {bill.Status}\n\n";

            if (!string.IsNullOrWhiteSpace(bill.PaymentMode))
                receipt += $"Payment Mode: {bill.PaymentMode}\n";

            if (!string.IsNullOrWhiteSpace(bill.TransactionId))
                receipt += $"Transaction ID: {bill.TransactionId}\n";

            if (bill.InsuranceCoveredAmount.HasValue && bill.InsuranceCoveredAmount > 0)
                receipt += $"Insurance Covered: ₹{bill.InsuranceCoveredAmount}\n";

            receipt += $"\nThank you for your visit!";

            return receipt;
        }
    }
}