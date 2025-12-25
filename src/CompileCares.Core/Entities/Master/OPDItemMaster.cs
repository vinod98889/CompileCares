using CompileCares.Core.Common;
using CompileCares.Core.Entities.Billing;
using CompileCares.Shared.Enums;
using System;

namespace CompileCares.Core.Entities.Master
{
    public class OPDItemMaster : BaseEntity
    {
        // Basic Information
        public string ItemCode { get; private set; }
        public string ItemName { get; private set; }
        public string? Description { get; private set; }

        // Classification
        public string ItemType { get; private set; } // Consultation, Procedure, Injection, Dressing, Medicine, LabTest
        public string? Category { get; private set; } // e.g., "Minor Procedure", "Major Procedure"
        public string? SubCategory { get; private set; }

        // Pricing
        public decimal StandardPrice { get; private set; }
        public decimal? DoctorCommission { get; private set; } // Commission percentage or fixed amount
        public bool IsCommissionPercentage { get; private set; } = true; // True = %, False = Fixed

        // Medical Properties
        public string? Instructions { get; private set; }
        public string? PreparationRequired { get; private set; }
        public TimeSpan? EstimatedDuration { get; private set; }

        // Requirements
        public bool RequiresSpecialist { get; private set; }
        public string? RequiredEquipment { get; private set; }
        public string? RequiredConsent { get; private set; }

        // Stock (if applicable - for consumables like injections)
        public bool IsConsumable { get; private set; }
        public int CurrentStock { get; private set; }
        public int? ReorderLevel { get; private set; }

        // Status
        public bool IsActive { get; private set; }
        public bool IsTaxable { get; private set; } = true;
        public bool IsDiscountable { get; private set; } = true;

        // HSN Code for GST
        public string? HSNCode { get; private set; }
        public decimal? GSTPercentage { get; private set; }

        // Navigation
        public virtual ICollection<OPDBillItem> OPDBillItems { get; private set; } = new List<OPDBillItem>();

        // Private constructor for EF Core
        private OPDItemMaster() { }

        // Main Constructor
        public OPDItemMaster(
            string itemCode,
            string itemName,
            string itemType,
            decimal standardPrice,
            Guid? createdBy = null)
        {
            ValidateInput(itemCode, itemName, itemType, standardPrice);

            ItemCode = itemCode;
            ItemName = itemName;
            ItemType = itemType;
            StandardPrice = standardPrice;
            IsActive = true;
            IsConsumable = IsConsumableItemType(itemType);

            // Set default commission based on item type
            SetDefaultCommission(itemType);

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Validation
        private static void ValidateInput(
            string itemCode,
            string itemName,
            string itemType,
            decimal standardPrice)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
                throw new ArgumentException("Item code is required");

            if (string.IsNullOrWhiteSpace(itemName))
                throw new ArgumentException("Item name is required");

            if (string.IsNullOrWhiteSpace(itemType))
                throw new ArgumentException("Item type is required");

            if (standardPrice < 0)
                throw new ArgumentException("Standard price cannot be negative");
        }

        // Check if item type is consumable
        private static bool IsConsumableItemType(string itemType)
        {
            return itemType.ToLower() switch
            {
                "injection" => true,
                "dressing" => true,
                "medicine" => true,
                "consumable" => true,
                _ => false
            };
        }

        // Set default commission based on item type
        private void SetDefaultCommission(string itemType)
        {
            DoctorCommission = itemType.ToLower() switch
            {
                "consultation" => 70.0m, // 70% for consultation
                "procedure" => 30.0m,    // 30% for procedures
                "injection" => 10.0m,    // 10% for injections
                _ => 0.0m                // No commission for others
            };
        }

        // ========== DOMAIN METHODS ==========

        // Update basic information
        public void UpdateBasicInfo(
            string itemName,
            string? description,
            string itemType,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                throw new ArgumentException("Item name is required");

            if (string.IsNullOrWhiteSpace(itemType))
                throw new ArgumentException("Item type is required");

            ItemName = itemName;
            Description = description;
            ItemType = itemType;
            IsConsumable = IsConsumableItemType(itemType);

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update classification
        public void UpdateClassification(
            string? category,
            string? subCategory,
            Guid? updatedBy = null)
        {
            Category = category;
            SubCategory = subCategory;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update pricing
        public void UpdatePricing(
            decimal standardPrice,
            decimal? doctorCommission = null,
            bool? isCommissionPercentage = null,
            Guid? updatedBy = null)
        {
            if (standardPrice < 0)
                throw new ArgumentException("Standard price cannot be negative");

            if (doctorCommission.HasValue && doctorCommission.Value < 0)
                throw new ArgumentException("Doctor commission cannot be negative");

            StandardPrice = standardPrice;

            if (doctorCommission.HasValue)
                DoctorCommission = doctorCommission.Value;

            if (isCommissionPercentage.HasValue)
                IsCommissionPercentage = isCommissionPercentage.Value;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update medical properties
        public void UpdateMedicalProperties(
            string? instructions,
            string? preparationRequired,
            TimeSpan? estimatedDuration,
            Guid? updatedBy = null)
        {
            Instructions = instructions;
            PreparationRequired = preparationRequired;
            EstimatedDuration = estimatedDuration;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update requirements
        public void UpdateRequirements(
            bool requiresSpecialist,
            string? requiredEquipment,
            string? requiredConsent,
            Guid? updatedBy = null)
        {
            RequiresSpecialist = requiresSpecialist;
            RequiredEquipment = requiredEquipment;
            RequiredConsent = requiredConsent;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update stock (for consumables)
        public void UpdateStock(
            int currentStock,
            int? reorderLevel = null,
            Guid? updatedBy = null)
        {
            if (!IsConsumable)
                throw new InvalidOperationException("Cannot update stock for non-consumable item");

            if (currentStock < 0)
                throw new ArgumentException("Stock cannot be negative");

            CurrentStock = currentStock;
            ReorderLevel = reorderLevel ?? ReorderLevel;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add stock
        public void AddStock(int quantity, Guid? updatedBy = null)
        {
            if (!IsConsumable)
                throw new InvalidOperationException("Cannot add stock for non-consumable item");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            CurrentStock += quantity;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Consume stock (for procedures/consumables)
        public void ConsumeStock(int quantity, Guid? updatedBy = null)
        {
            if (!IsConsumable)
                throw new InvalidOperationException("Cannot consume stock for non-consumable item");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            if (CurrentStock < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {CurrentStock}, Required: {quantity}");

            CurrentStock -= quantity;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update tax information
        public void UpdateTaxInfo(
            bool isTaxable,
            string? hsnCode,
            decimal? gstPercentage,
            bool isDiscountable,
            Guid? updatedBy = null)
        {
            IsTaxable = isTaxable;
            HSNCode = hsnCode;
            GSTPercentage = gstPercentage;
            IsDiscountable = isDiscountable;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Check if stock is low
        public bool IsStockLow()
        {
            if (!IsConsumable || !ReorderLevel.HasValue)
                return false;

            return CurrentStock <= ReorderLevel.Value;
        }

        // Calculate commission amount
        public decimal CalculateCommission(decimal billedAmount)
        {
            if (!DoctorCommission.HasValue || DoctorCommission.Value == 0)
                return 0;

            if (IsCommissionPercentage)
                return billedAmount * (DoctorCommission.Value / 100);
            else
                return DoctorCommission.Value; // Fixed commission
        }

        // Activate/Deactivate
        public void Activate(Guid? activatedBy = null)
        {
            IsActive = true;

            if (activatedBy.HasValue)
                SetUpdatedBy(activatedBy.Value);
        }

        public void Deactivate(Guid? deactivatedBy = null)
        {
            IsActive = false;

            if (deactivatedBy.HasValue)
                SetUpdatedBy(deactivatedBy.Value);
        }

        // Factory method for testing
        public static OPDItemMaster CreateForTest(
            Guid id,
            string itemCode,
            string itemName,
            string itemType,
            decimal standardPrice)
        {
            var item = new OPDItemMaster(itemCode, itemName, itemType, standardPrice);
            item.SetId(id);
            return item;
        }
    }
}