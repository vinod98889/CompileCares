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
        private readonly List<OPDBillItem> _billItems = new();
        public IReadOnlyCollection<OPDBillItem> BillItems => _billItems.AsReadOnly();

        // Private constructor for EF Core
        private OPDBill() { }

        // Main Constructor
        public OPDBill(
            Guid opdVisitId,
            Guid patientId,
            Guid doctorId,
            decimal consultationFee,
            Guid consultationItemMasterId, // REQUIRED: Which OPDItemMaster to use for consultation
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

            // Add consultation as a bill item using the provided OPDItemMasterId
            AddConsultationCharge(consultationItemMasterId, consultationFee, createdBy);

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
                throw new ArgumentException("OPD Visit ID is required", nameof(opdVisitId));

            if (patientId == Guid.Empty)
                throw new ArgumentException("Patient ID is required", nameof(patientId));

            if (doctorId == Guid.Empty)
                throw new ArgumentException("Doctor ID is required", nameof(doctorId));

            if (consultationFee < 0)
                throw new ArgumentException("Consultation fee cannot be negative", nameof(consultationFee));
        }

        // Generate bill number
        private static string GenerateBillNumber()
        {
            return $"BILL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        // ========== DOMAIN METHODS ==========

        /// <summary>
        /// Adds a bill item using an OPDItemMaster
        /// </summary>
        public void AddBillItem(
            OPDItemMaster itemMaster,
            int quantity = 1,
            decimal? customPrice = null,
            decimal? discountPercentage = null,
            Guid? updatedBy = null)
        {
            if (itemMaster == null)
                throw new ArgumentNullException(nameof(itemMaster));

            if (!itemMaster.IsActive)
                throw new InvalidOperationException($"Item {itemMaster.ItemName} is not active");

            if (itemMaster.IsConsumable && itemMaster.CurrentStock < quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for {itemMaster.ItemName}. Available: {itemMaster.CurrentStock}, Required: {quantity}");

            var unitPrice = customPrice ?? itemMaster.StandardPrice;

            // Create bill item using your existing constructor
            var billItem = new OPDBillItem(
                opdBillId: Id,
                opdItemMasterId: itemMaster.Id,
                itemName: itemMaster.ItemName,
                itemType: itemMaster.ItemType ?? itemMaster.Category,
                unitPrice: unitPrice,
                quantity: quantity,
                isTaxable: itemMaster.IsTaxable);

            // Apply discount if provided
            if (discountPercentage.HasValue && discountPercentage.Value > 0)
            {
                billItem.ApplyDiscount(discountPercentage.Value, updatedBy);
            }

            // Apply doctor commission from master
            if (itemMaster.DoctorCommission.HasValue)
            {
                billItem.SetDoctorCommission(
                    itemMaster.DoctorCommission.Value,
                    updatedBy);
            }

            _billItems.Add(billItem);

            // Consume stock if item is consumable
            if (itemMaster.IsConsumable)
            {
                itemMaster.ConsumeStock(quantity, updatedBy);
            }

            // Update category totals
            UpdateCategoryTotals(itemMaster.Category, billItem.GetTotalWithTax());

            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        public void AddBillItem(
    OPDItemMaster itemMaster,
    int quantity,
    decimal? customPrice,
    Guid updatedBy)
        {
            // Call the main method with null discount
            AddBillItem(
                itemMaster: itemMaster,
                quantity: quantity,
                customPrice: customPrice,
                discountPercentage: null,
                updatedBy: updatedBy);
        }

        /// <summary>
        /// Adds a bill item using OPDItemMaster ID (when you don't have the object)
        /// </summary>
        public void AddBillItem(
            Guid opdItemMasterId,
            string itemName,
            string itemType,
            decimal unitPrice,
            int quantity = 1,
            decimal discountPercentage = 0,
            bool isTaxable = true,
            Guid? updatedBy = null)
        {
            if (opdItemMasterId == Guid.Empty)
                throw new ArgumentException("OPDItemMaster ID is required", nameof(opdItemMasterId));

            if (string.IsNullOrWhiteSpace(itemName))
                throw new ArgumentException("Item name is required", nameof(itemName));

            if (string.IsNullOrWhiteSpace(itemType))
                throw new ArgumentException("Item type is required", nameof(itemType));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            // Create bill item using your existing constructor
            var billItem = new OPDBillItem(
                opdBillId: Id,
                opdItemMasterId: opdItemMasterId,
                itemName: itemName,
                itemType: itemType,
                unitPrice: unitPrice,
                quantity: quantity,
                isTaxable: isTaxable);

            // Apply discount if provided
            if (discountPercentage > 0)
            {
                billItem.ApplyDiscount(discountPercentage, updatedBy);
            }

            _billItems.Add(billItem);

            // Update category totals (use itemType for category)
            UpdateCategoryTotals(itemType, billItem.GetTotalWithTax());

            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        /// <summary>
        /// Adds a consultation charge using OPDItemMaster ID
        /// </summary>
        public void AddConsultationCharge(
            Guid consultationItemMasterId,
            decimal consultationFee,
            Guid? updatedBy = null)
        {
            AddBillItem(
                opdItemMasterId: consultationItemMasterId,
                itemName: "Consultation Fee",
                itemType: "Consultation",
                unitPrice: consultationFee,
                quantity: 1,
                discountPercentage: 0,
                isTaxable: true,
                updatedBy: updatedBy);
        }

        /// <summary>
        /// Adds a custom bill item (when no OPDItemMaster exists)
        /// </summary>
        public void AddCustomBillItem(
            string itemName,
            string itemType,
            decimal unitPrice,
            int quantity = 1,
            decimal discountPercentage = 0,
            bool isTaxable = true,
            Guid? updatedBy = null)
        {
            AddBillItem(
                opdItemMasterId: Guid.Empty, // Guid.Empty indicates custom item
                itemName: itemName,
                itemType: itemType,
                unitPrice: unitPrice,
                quantity: quantity,
                discountPercentage: discountPercentage,
                isTaxable: isTaxable,
                updatedBy: updatedBy);
        }

        // Helper method to update category totals
        private void UpdateCategoryTotals(string itemType, decimal amount)
        {
            var normalizedType = itemType.ToLower();

            if (normalizedType.Contains("consult"))
                ConsultationFee += amount;
            else if (normalizedType.Contains("procedure") || normalizedType.Contains("dressing") || normalizedType.Contains("injection"))
                ProcedureFee += amount;
            else if (normalizedType.Contains("medicine") || normalizedType.Contains("drug"))
                MedicineFee += amount;
            else if (normalizedType.Contains("lab") || normalizedType.Contains("test") || normalizedType.Contains("xray") || normalizedType.Contains("scan"))
                LabTestFee += amount;
            else
                OtherCharges += amount;
        }

        // Calculate all totals
        private void CalculateTotals()
        {
            // Calculate from bill items
            SubTotal = _billItems.Sum(item => item.GetNetAmount()); // Price after item discounts

            // Item-level discounts
            decimal itemLevelDiscount = _billItems.Sum(item => item.DiscountAmount);

            // Bill-level discount
            decimal billLevelDiscount = SubTotal * (DiscountPercentage / 100);

            DiscountAmount = itemLevelDiscount + billLevelDiscount;

            // Tax from items
            TaxAmount = _billItems.Sum(item => item.TaxAmount);

            // Totals
            TotalAmount = SubTotal + TaxAmount - billLevelDiscount; // SubTotal already has item discounts
            DueAmount = TotalAmount - PaidAmount;
        }

        // Update discount
        public void UpdateDiscount(decimal discountPercentage, Guid? updatedBy = null)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

            DiscountPercentage = discountPercentage;
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update tax
        public void UpdateTax(decimal taxPercentage, Guid? updatedBy = null)
        {
            if (taxPercentage < 0)
                throw new ArgumentException("Tax percentage cannot be negative", nameof(taxPercentage));

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
                throw new ArgumentException("Payment amount must be positive", nameof(amount));

            if (string.IsNullOrWhiteSpace(paymentMode))
                throw new ArgumentException("Payment mode is required", nameof(paymentMode));

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
                throw new ArgumentException("Refund amount must be positive", nameof(refundAmount));

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
            return _billItems
                .Where(item => item.DoctorCommission.HasValue)
                .Sum(item => item.DoctorCommission.Value);
        }

        // Set notes
        public void SetNotes(string notes, Guid? updatedBy = null)
        {
            Notes = notes;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Factory method for testing
        public static OPDBill CreateForTest(
            Guid id,
            Guid opdVisitId,
            Guid patientId,
            Guid doctorId,
            decimal consultationFee,
            Guid consultationItemMasterId)
        {
            var bill = new OPDBill(opdVisitId, patientId, doctorId, consultationFee, consultationItemMasterId);
            bill.SetId(id);
            return bill;
        }
    }
}