using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;
using System;

namespace CompileCares.Core.Entities.Pharmacy
{
    public class Medicine : BaseEntity
    {
        // Basic Information
        public string Name { get; private set; }
        public string? GenericName { get; private set; }
        public string? BrandName { get; private set; }
        public MedicineType MedicineType { get; private set; }

        // Classification
        public string? Category { get; private set; } // e.g., Antibiotic, Analgesic
        public string? SubCategory { get; private set; }
        public string? TherapeuticClass { get; private set; }

        // Form & Strength
        public string? Form { get; private set; } // Tablet, Capsule, Syrup
        public string? Strength { get; private set; } // e.g., "500mg", "10mg/ml"
        public string? PackSize { get; private set; } // e.g., "10 Tablets", "100ml"

        // Manufacturer
        public string? Manufacturer { get; private set; }
        public string? ManufacturerCode { get; private set; }

        // Composition
        public string? Composition { get; private set; }

        // Stock Management
        public int CurrentStock { get; private set; }
        public int MinimumStockLevel { get; private set; }
        public int ReorderLevel { get; private set; }
        public string? StockUnit { get; private set; } // e.g., "Tablets", "Bottles"

        // Pricing
        public decimal PurchasePrice { get; private set; }
        public decimal SellingPrice { get; private set; }
        public decimal? MRP { get; private set; }
        public decimal? GSTPercentage { get; private set; } = 5.0m; // Default 5%

        // Regulatory
        public string? HSNCode { get; private set; }
        public string? Schedule { get; private set; } // e.g., "H1", "X"
        public bool RequiresPrescription { get; private set; }

        // Safety Information
        public string? SideEffects { get; private set; }
        public string? Contraindications { get; private set; }
        public string? Precautions { get; private set; }
        public string? StorageInstructions { get; private set; }

        // Status
        public bool IsActive { get; private set; }
        public DateTime? ExpiryDate { get; private set; }

        // Navigation
        public virtual ICollection<PrescriptionMedicine> PrescriptionMedicines { get; private set; } = new List<PrescriptionMedicine>();

        // Private constructor for EF Core
        private Medicine() { }

        // Main Constructor
        public Medicine(
            string name,
            MedicineType medicineType,
            decimal sellingPrice,
            Guid? createdBy = null)
        {
            ValidateInput(name, sellingPrice);

            Name = name;
            MedicineType = medicineType;
            SellingPrice = sellingPrice;
            PurchasePrice = sellingPrice * 0.7m; // Default 70% of selling price
            MRP = sellingPrice * 1.1m; // Default 10% above selling price

            CurrentStock = 0;
            MinimumStockLevel = 10;
            ReorderLevel = 20;
            RequiresPrescription = true; // Default requires prescription
            IsActive = true;
            StockUnit = GetDefaultStockUnit(medicineType);

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Validation
        private static void ValidateInput(string name, decimal sellingPrice)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Medicine name is required");

            if (sellingPrice <= 0)
                throw new ArgumentException("Selling price must be positive");
        }

        // Get default stock unit based on medicine type
        private static string GetDefaultStockUnit(MedicineType medicineType)
        {
            return medicineType switch
            {
                MedicineType.Tablet => "Tablets",
                MedicineType.Capsule => "Capsules",
                MedicineType.Syrup => "Bottles",
                MedicineType.Injection => "Vials",
                MedicineType.Ointment => "Tubes",
                MedicineType.Cream => "Tubes",
                MedicineType.Drops => "Bottles",
                MedicineType.Inhaler => "Pieces",
                MedicineType.Powder => "Packets",
                _ => "Units"
            };
        }

        // ========== DOMAIN METHODS ==========

        // Update basic information
        public void UpdateBasicInfo(
            string name,
            string? genericName,
            string? brandName,
            MedicineType medicineType,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Medicine name is required");

            Name = name;
            GenericName = genericName;
            BrandName = brandName;
            MedicineType = medicineType;
            StockUnit = GetDefaultStockUnit(medicineType);

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update classification
        public void UpdateClassification(
            string? category,
            string? subCategory,
            string? therapeuticClass,
            Guid? updatedBy = null)
        {
            Category = category;
            SubCategory = subCategory;
            TherapeuticClass = therapeuticClass;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update form and strength
        public void UpdateFormAndStrength(
            string? form,
            string? strength,
            string? packSize,
            string? composition,
            Guid? updatedBy = null)
        {
            Form = form;
            Strength = strength;
            PackSize = packSize;
            Composition = composition;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update manufacturer
        public void UpdateManufacturer(
            string? manufacturer,
            string? manufacturerCode,
            Guid? updatedBy = null)
        {
            Manufacturer = manufacturer;
            ManufacturerCode = manufacturerCode;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update pricing
        public void UpdatePricing(
            decimal purchasePrice,
            decimal sellingPrice,
            decimal? mrp = null,
            decimal? gstPercentage = null,
            Guid? updatedBy = null)
        {
            if (purchasePrice <= 0)
                throw new ArgumentException("Purchase price must be positive");

            if (sellingPrice <= 0)
                throw new ArgumentException("Selling price must be positive");

            PurchasePrice = purchasePrice;
            SellingPrice = sellingPrice;
            MRP = mrp ?? sellingPrice * 1.1m;
            GSTPercentage = gstPercentage ?? 5.0m;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update stock
        public void UpdateStock(
            int currentStock,
            int? minimumStockLevel = null,
            int? reorderLevel = null,
            string? stockUnit = null,
            Guid? updatedBy = null)
        {
            if (currentStock < 0)
                throw new ArgumentException("Stock cannot be negative");

            CurrentStock = currentStock;
            MinimumStockLevel = minimumStockLevel ?? MinimumStockLevel;
            ReorderLevel = reorderLevel ?? ReorderLevel;
            StockUnit = stockUnit ?? StockUnit;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add stock (increment)
        public void AddStock(int quantity, Guid? updatedBy = null)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            CurrentStock += quantity;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Remove stock (decrement - for dispensing)
        public void RemoveStock(int quantity, Guid? updatedBy = null)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            if (CurrentStock < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {CurrentStock}, Requested: {quantity}");

            CurrentStock -= quantity;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update regulatory info
        public void UpdateRegulatoryInfo(
            string? hsnCode,
            string? schedule,
            bool? requiresPrescription = null,
            Guid? updatedBy = null)
        {
            HSNCode = hsnCode;
            Schedule = schedule;

            if (requiresPrescription.HasValue)
                RequiresPrescription = requiresPrescription.Value;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update safety info
        public void UpdateSafetyInfo(
            string? sideEffects,
            string? contraindications,
            string? precautions,
            string? storageInstructions,
            Guid? updatedBy = null)
        {
            SideEffects = sideEffects;
            Contraindications = contraindications;
            Precautions = precautions;
            StorageInstructions = storageInstructions;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set expiry date
        public void SetExpiryDate(DateTime? expiryDate, Guid? updatedBy = null)
        {
            ExpiryDate = expiryDate;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Check if medicine is expired
        public bool IsExpired()
        {
            return ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
        }

        // Check if stock is low
        public bool IsStockLow()
        {
            return CurrentStock <= ReorderLevel;
        }

        // Check if stock is critical
        public bool IsStockCritical()
        {
            return CurrentStock <= MinimumStockLevel;
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
        public static Medicine CreateForTest(
            Guid id,
            string name,
            MedicineType medicineType,
            decimal sellingPrice)
        {
            var medicine = new Medicine(name, medicineType, sellingPrice);
            medicine.SetId(id);
            return medicine;
        }
    }
}