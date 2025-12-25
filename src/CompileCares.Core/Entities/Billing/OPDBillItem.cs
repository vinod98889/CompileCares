using CompileCares.Core.Common;
using CompileCares.Core.Entities.Master;

namespace CompileCares.Core.Entities.Billing
{
    public class OPDBillItem : BaseEntity
    {
        // References
        public Guid OPDBillId { get; private set; }
        public Guid OPDItemMasterId { get; private set; }

        // Snapshot of item details at billing time
        public string ItemName { get; private set; }
        public string ItemType { get; private set; }
        public decimal UnitPrice { get; private set; } // Price at time of billing
        public int Quantity { get; private set; }
        public decimal TotalPrice { get; private set; }

        // Discount & Commission
        public decimal DiscountAmount { get; private set; }
        public decimal DiscountPercentage { get; private set; }
        public decimal? DoctorCommission { get; private set; }

        // Tax
        public bool IsTaxable { get; private set; }
        public decimal TaxAmount { get; private set; }

        // Status
        public bool IsAdministered { get; private set; }
        public DateTime? AdministeredDate { get; private set; }
        public string? AdministeredBy { get; private set; }
        public string? AdministrationNotes { get; private set; }

        // Navigation Properties
        public virtual OPDBill OPDBill { get; private set; } = null!;
        public virtual OPDItemMaster? OPDItemMaster { get; private set; }

        // Private constructor for EF Core
        private OPDBillItem() { }

        // Main Constructor
        public OPDBillItem(
            Guid opdBillId,
            Guid opdItemMasterId,
            string itemName,
            string itemType,
            decimal unitPrice,
            int quantity = 1,
            bool isTaxable = true)
        {
            ValidateInput(opdBillId, itemName, itemType, unitPrice, quantity);

            OPDBillId = opdBillId;
            OPDItemMasterId = opdItemMasterId;
            ItemName = itemName;
            ItemType = itemType;
            UnitPrice = unitPrice;
            Quantity = quantity;
            IsTaxable = isTaxable;
            TotalPrice = unitPrice * quantity;

            CalculateTotals();
        }

        // Factory method to create from OPDItemMaster
        public static OPDBillItem CreateFromItemMaster(
            Guid opdBillId,
            OPDItemMaster itemMaster,
            int quantity = 1,
            decimal? customPrice = null)
        {
            return new OPDBillItem(
                opdBillId,
                itemMaster.Id,
                itemMaster.ItemName,
                itemMaster.ItemType,
                customPrice ?? itemMaster.StandardPrice,
                quantity,
                itemMaster.IsTaxable);
        }

        // Validation
        private static void ValidateInput(
            Guid opdBillId,
            string itemName,
            string itemType,
            decimal unitPrice,
            int quantity)
        {
            if (opdBillId == Guid.Empty)
                throw new ArgumentException("Bill ID is required");

            if (string.IsNullOrWhiteSpace(itemName))
                throw new ArgumentException("Item name is required");

            if (string.IsNullOrWhiteSpace(itemType))
                throw new ArgumentException("Item type is required");

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");
        }

        // Calculate totals
        private void CalculateTotals()
        {
            TotalPrice = UnitPrice * Quantity;

            // Calculate discounted price
            var discountedPrice = TotalPrice - DiscountAmount;

            // Calculate tax (only if taxable) - default 5% GST
            TaxAmount = IsTaxable ? discountedPrice * 0.05m : 0;
        }

        // ========== DOMAIN METHODS ==========

        // Apply discount
        public void ApplyDiscount(decimal discountPercentage, Guid? updatedBy = null)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentException("Discount percentage must be between 0 and 100");

            DiscountPercentage = discountPercentage;
            DiscountAmount = TotalPrice * (discountPercentage / 100);
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Apply fixed discount
        public void ApplyFixedDiscount(decimal discountAmount, Guid? updatedBy = null)
        {
            if (discountAmount < 0)
                throw new ArgumentException("Discount amount cannot be negative");

            if (discountAmount > TotalPrice)
                throw new ArgumentException("Discount amount cannot exceed total price");

            DiscountAmount = discountAmount;
            DiscountPercentage = (discountAmount / TotalPrice) * 100;
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update quantity
        public void UpdateQuantity(int quantity, Guid? updatedBy = null)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            Quantity = quantity;
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update unit price
        public void UpdateUnitPrice(decimal unitPrice, Guid? updatedBy = null)
        {
            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative");

            UnitPrice = unitPrice;
            CalculateTotals();

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set doctor commission
        public void SetDoctorCommission(decimal commission, Guid? updatedBy = null)
        {
            if (commission < 0)
                throw new ArgumentException("Commission cannot be negative");

            DoctorCommission = commission;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Mark as administered (for procedures, injections, etc.)
        public void MarkAsAdministered(string administeredBy, string? notes = null, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(administeredBy))
                throw new ArgumentException("Administered by name is required");

            if (IsAdministered)
                throw new InvalidOperationException("Item is already administered");

            IsAdministered = true;
            AdministeredDate = DateTime.UtcNow;
            AdministeredBy = administeredBy;
            AdministrationNotes = notes;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Get net amount (after discount)
        public decimal GetNetAmount()
        {
            return TotalPrice - DiscountAmount;
        }

        // Get taxable amount
        public decimal GetTaxableAmount()
        {
            return IsTaxable ? GetNetAmount() : 0;
        }

        // Get total with tax
        public decimal GetTotalWithTax()
        {
            return GetNetAmount() + TaxAmount;
        }

        // Factory method for testing
        public static OPDBillItem CreateForTest(
            Guid id,
            Guid opdBillId,
            Guid opdItemMasterId,
            string itemName,
            string itemType,
            decimal unitPrice,
            int quantity = 1)
        {
            var billItem = new OPDBillItem(
                opdBillId, opdItemMasterId, itemName, itemType, unitPrice, quantity);

            billItem.SetId(id);
            return billItem;
        }
    }
}