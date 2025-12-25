using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Master;
using CompileCares.Core.Entities.Patients;
using CompileCares.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompileCares.Core.Entities.Billing
{
    public class OPDBill : BaseEntity
    {
        // References
        public Guid OPDVisitId { get; private set; }
        public Guid PatientId { get; private set; }
        public Guid DoctorId { get; private set; }

        // Bill Information
        public string BillNumber { get; private set; }
        public DateTime BillDate { get; private set; }
        public BillStatus Status { get; private set; }

        // Charges
        public decimal ConsultationFee { get; private set; }
        public decimal ProcedureFee { get; private set; }
        public decimal MedicineFee { get; private set; }
        public decimal LabTestFee { get; private set; }
        public decimal OtherCharges { get; private set; }

        // Calculations
        public decimal SubTotal { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal TotalAmount { get; private set; }
        public decimal PaidAmount { get; private set; }
        public decimal DueAmount { get; private set; }

        // Payment Information
        public string? PaymentMode { get; private set; } // Cash, Card, UPI, Insurance
        public string? TransactionId { get; private set; }
        public string? InsuranceProvider { get; private set; }
        public string? InsurancePolicyNumber { get; private set; }
        public decimal? InsuranceCoveredAmount { get; private set; }

        // Discount & Tax
        public decimal DiscountPercentage { get; private set; }
        public decimal TaxPercentage { get; private set; }

        // Notes
        public string? Notes { get; private set; }

        // Navigation Properties
        public virtual OPDVisit? OPDVisit { get; private set; }
        public virtual Patient? Patient { get; private set; }
        public virtual Doctor? Doctor { get; private set; }
        public virtual ICollection<OPDBillItem> BillItems { get; private set; } = new List<OPDBillItem>();

        // Private constructor for EF Core
        private OPDBill() { }

        // Main Constructor
        public OPDBill(
            Guid opdVisitId,
            Guid patientId,
            Guid doctorId,
            decimal consultationFee,
            Guid? createdBy = null)
        {
            ValidateInput(opdVisitId, patientId, doctorId, consultationFee);

            OPDVisitId = opdVisitId;
            PatientId = patientId;
            DoctorId = doctorId;
            ConsultationFee = consultationFee;

            BillDate = DateTime.UtcNow;
            Status = BillStatus.Draft;
            BillNumber = GenerateBillNumber();

            // Default values
            DiscountPercentage = 0;
            TaxPercentage = 5.0m; // Default 5% GST

            // Add consultation as first bill item
            AddConsultationCharge("Consultation", consultationFee);

            CalculateTotals();

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Validation
        private static void ValidateInput(
            Guid opdVisitId,
            Guid patientId,
            Guid doctorId,
            decimal consultationFee)
        {
            if (opdVisitId == Guid.Empty)
                throw new ArgumentException("OPD Visit ID is required");

            if (patientId == Guid.Empty)
                throw new ArgumentException("Patient ID is required");

            if (doctorId == Guid.Empty)
                throw new ArgumentException("Doctor ID is required");

            if (consultationFee < 0)
                throw new ArgumentException("Consultation fee cannot be negative");
        }

        // Generate bill number
        private static string GenerateBillNumber()
        {
            return $"BILL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        // ========== DOMAIN METHODS ==========

        // Add bill item from OPDItemMaster
        public void AddBillItem(
            OPDItemMaster itemMaster,
            int quantity = 1,
            decimal? customPrice = null,
            Guid? updatedBy = null)
        {
            if (!itemMaster.IsActive)
                throw new InvalidOperationException($"Item {itemMaster.ItemName} is not active");

            if (itemMaster.IsConsumable && itemMaster.CurrentStock < quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for {itemMaster.ItemName}. Available: {itemMaster.CurrentStock}, Required: {quantity}");

            var unitPrice = customPrice ?? itemMaster.StandardPrice;

            // Create bill item using the OPDItemMaster
            var billItem = OPDBillItem.CreateFromItemMaster(
                Id,
                itemMaster,
                quantity,
                unitPrice);

            // Apply default commission from master
            if (itemMaster.DoctorCommission.HasValue)
            {
                billItem.SetDoctorCommission(
                    itemMaster.CalculateCommission(billItem.GetNetAmount()));
            }

            BillItems.Add(billItem);

            // Consume stock if item is consumable
            if (itemMaster.IsConsumable)
            {
                itemMaster.ConsumeStock(quantity, updatedBy);
            }

            // Update category totals
            UpdateCategoryTotals(itemMaster.ItemType, billItem.GetTotalWithTax());

            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add consultation charge (special method since it's not from OPDItemMaster)
        public void AddConsultationCharge(string description, decimal amount, Guid? updatedBy = null)
        {
            var billItem = new OPDBillItem(
                Id,
                Guid.Empty, // No OPDItemMaster for consultation
                description,
                "Consultation",
                amount,
                1,
                true);

            BillItems.Add(billItem);
            UpdateCategoryTotals("consultation", amount);
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add custom bill item (when no OPDItemMaster exists)
        public void AddCustomBillItem(
            string itemName,
            string itemType,
            decimal unitPrice,
            int quantity = 1,
            bool isTaxable = true,
            Guid? updatedBy = null)
        {
            var billItem = new OPDBillItem(
                Id,
                Guid.Empty, // No OPDItemMaster ID
                itemName,
                itemType,
                unitPrice,
                quantity,
                isTaxable);

            BillItems.Add(billItem);
            UpdateCategoryTotals(itemType.ToLower(), billItem.GetTotalWithTax());
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Helper method to update category totals
        private void UpdateCategoryTotals(string itemType, decimal amount)
        {
            switch (itemType.ToLower())
            {
                case "consultation":
                    ConsultationFee += amount;
                    break;
                case "procedure":
                    ProcedureFee += amount;
                    break;
                case "medicine":
                    MedicineFee += amount;
                    break;
                case "labtest":
                    LabTestFee += amount;
                    break;
                default:
                    OtherCharges += amount;
                    break;
            }
        }

        // Calculate all totals
        private void CalculateTotals()
        {
            // Each item calculates: TotalWithTax = (UnitPrice × Quantity - ItemDiscount) × (1 + TaxRate)
            decimal itemsTotal = BillItems.Sum(item => item.GetTotalWithTax());

            SubTotal = itemsTotal;

            // Simple bill-level discount
            DiscountAmount = SubTotal * (DiscountPercentage / 100);

            // Tax already included in itemsTotal, so no need to recalculate
            TaxAmount = BillItems
                .Where(item => item.IsTaxable)
                .Sum(item => item.TaxAmount);

            TotalAmount = SubTotal - DiscountAmount;
            DueAmount = TotalAmount - PaidAmount;
        }

        // Update discount
        public void UpdateDiscount(decimal discountPercentage, Guid? updatedBy = null)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentException("Discount percentage must be between 0 and 100");

            DiscountPercentage = discountPercentage;
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update tax
        public void UpdateTax(decimal taxPercentage, Guid? updatedBy = null)
        {
            if (taxPercentage < 0)
                throw new ArgumentException("Tax percentage cannot be negative");

            TaxPercentage = taxPercentage;
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add payment
        public void AddPayment(
            decimal amount,
            string paymentMode,
            string? transactionId = null,
            Guid? updatedBy = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Payment amount must be positive");

            if (string.IsNullOrWhiteSpace(paymentMode))
                throw new ArgumentException("Payment mode is required");

            PaidAmount += amount;
            PaymentMode = paymentMode;
            TransactionId = transactionId;

            // Update status based on payment
            if (PaidAmount >= TotalAmount)
            {
                Status = BillStatus.Paid;
                DueAmount = 0;
            }
            else if (PaidAmount > 0)
            {
                Status = BillStatus.PartiallyPaid;
                DueAmount = TotalAmount - PaidAmount;
            }
            else
            {
                Status = BillStatus.Generated;
                DueAmount = TotalAmount;
            }

            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update insurance info
        public void UpdateInsuranceInfo(
            string? provider,
            string? policyNumber,
            decimal? coveredAmount,
            Guid? updatedBy = null)
        {
            InsuranceProvider = provider;
            InsurancePolicyNumber = policyNumber;
            InsuranceCoveredAmount = coveredAmount;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Generate bill (move from draft to generated)
        public void GenerateBill(Guid? updatedBy = null)
        {
            if (Status != BillStatus.Draft)
                throw new InvalidOperationException($"Cannot generate bill from {Status} status");

            Status = BillStatus.Generated;
            DueAmount = TotalAmount;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Cancel bill
        public void CancelBill(string reason, Guid? updatedBy = null)
        {
            if (Status == BillStatus.Paid || Status == BillStatus.PartiallyPaid)
                throw new InvalidOperationException("Cannot cancel a paid bill");

            Status = BillStatus.Cancelled;
            Notes = $"[CANCELLED: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {reason}\n{Notes}";

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Refund bill
        public void RefundBill(decimal refundAmount, string reason, Guid? updatedBy = null)
        {
            if (Status != BillStatus.Paid && Status != BillStatus.PartiallyPaid)
                throw new InvalidOperationException("Can only refund paid or partially paid bills");

            if (refundAmount <= 0)
                throw new ArgumentException("Refund amount must be positive");

            if (refundAmount > PaidAmount)
                throw new InvalidOperationException($"Refund amount cannot exceed paid amount ({PaidAmount})");

            PaidAmount -= refundAmount;
            Status = BillStatus.Refunded;
            Notes = $"[REFUNDED: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] Amount: {refundAmount}, Reason: {reason}\n{Notes}";

            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Check if bill is fully paid
        public bool IsFullyPaid()
        {
            return Status == BillStatus.Paid && DueAmount == 0;
        }

        // Get total doctor commission from all items
        public decimal GetTotalDoctorCommission()
        {
            return BillItems
                .Where(item => item.DoctorCommission.HasValue)
                .Sum(item => item.DoctorCommission.Value);
        }

        // Factory method for testing
        public static OPDBill CreateForTest(
            Guid id,
            Guid opdVisitId,
            Guid patientId,
            Guid doctorId,
            decimal consultationFee)
        {
            var bill = new OPDBill(opdVisitId, patientId, doctorId, consultationFee);
            bill.SetId(id);
            return bill;
        }
    }
}