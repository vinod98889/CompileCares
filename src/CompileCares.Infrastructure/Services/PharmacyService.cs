// File: PharmacyService.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Pharmacy.DTOs;
using CompileCares.Application.Features.Pharmacy.Services;
using CompileCares.Core.Entities.Pharmacy;
using CompileCares.Infrastructure.Data;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompileCares.Infrastructure.Services
{
    public class PharmacyService : IPharmacyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PharmacyService> _logger;

        public PharmacyService(
            ApplicationDbContext context,
            ILogger<PharmacyService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MedicineDto> CreateMedicineAsync(CreateMedicineRequest request, Guid createdBy)
        {
            const string operation = "CreateMedicine";

            try
            {
                _logger.LogInformation("Starting {Operation} for medicine: {MedicineName}",
                    operation, request.Name);

                ValidateCreateRequest(request);

                // Check for duplicate medicine name (case-insensitive)
                var existingMedicine = await _context.Medicines
                    .AsNoTracking()
                    .AnyAsync(m => m.Name.ToLower() == request.Name.ToLower() && !m.IsDeleted);

                if (existingMedicine)
                {
                    throw new ValidationException($"Medicine with name '{request.Name}' already exists.");
                }

                // Create entity
                var medicine = new Medicine(
                    name: request.Name?.Trim() ?? string.Empty,
                    medicineType: request.MedicineType,
                    sellingPrice: request.SellingPrice,
                    createdBy: createdBy
                );

                // Set optional properties using domain methods
                if (!string.IsNullOrWhiteSpace(request.GenericName))
                {
                    medicine.UpdateBasicInfo(
                        name: request.Name?.Trim() ?? string.Empty,
                        genericName: request.GenericName?.Trim(),
                        brandName: request.BrandName?.Trim(),
                        medicineType: request.MedicineType,
                        updatedBy: createdBy
                    );
                }

                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    medicine.UpdateClassification(
                        category: request.Category?.Trim(),
                        subCategory: request.SubCategory?.Trim(),
                        therapeuticClass: request.TherapeuticClass?.Trim(),
                        updatedBy: createdBy
                    );
                }

                if (!string.IsNullOrWhiteSpace(request.Form))
                {
                    medicine.UpdateFormAndStrength(
                        form: request.Form?.Trim(),
                        strength: request.Strength?.Trim(),
                        packSize: request.PackSize?.Trim(),
                        composition: request.Composition?.Trim(),
                        updatedBy: createdBy
                    );
                }

                if (!string.IsNullOrWhiteSpace(request.Manufacturer))
                {
                    medicine.UpdateManufacturer(
                        manufacturer: request.Manufacturer?.Trim(),
                        manufacturerCode: request.ManufacturerCode?.Trim(),
                        updatedBy: createdBy
                    );
                }

                // Set pricing if provided
                if (request.PurchasePrice.HasValue)
                {
                    medicine.UpdatePricing(
                        purchasePrice: request.PurchasePrice.Value,
                        sellingPrice: request.SellingPrice,
                        mrp: request.MRP,
                        gstPercentage: request.GSTPercentage,
                        updatedBy: createdBy
                    );
                }

                // Set stock levels
                medicine.UpdateStock(
                    currentStock: request.InitialStock,
                    minimumStockLevel: request.MinimumStockLevel,
                    reorderLevel: request.ReorderLevel,
                    stockUnit: request.StockUnit?.Trim(),
                    updatedBy: createdBy
                );

                // Set regulatory info
                if (!string.IsNullOrWhiteSpace(request.HSNCode) || request.RequiresPrescription.HasValue)
                {
                    medicine.UpdateRegulatoryInfo(
                        hsnCode: request.HSNCode?.Trim(),
                        schedule: request.Schedule?.Trim(),
                        requiresPrescription: request.RequiresPrescription,
                        updatedBy: createdBy
                    );
                }

                // Set safety info
                if (!string.IsNullOrWhiteSpace(request.SideEffects) ||
                    !string.IsNullOrWhiteSpace(request.StorageInstructions))
                {
                    medicine.UpdateSafetyInfo(
                        sideEffects: request.SideEffects?.Trim(),
                        contraindications: request.Contraindications?.Trim(),
                        precautions: request.Precautions?.Trim(),
                        storageInstructions: request.StorageInstructions?.Trim(),
                        updatedBy: createdBy
                    );
                }

                // Set expiry date if provided
                if (request.ExpiryDate.HasValue)
                {
                    medicine.SetExpiryDate(request.ExpiryDate.Value, createdBy);
                }

                // Save to database
                await _context.Medicines.AddAsync(medicine);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created medicine: ID={MedicineId}, Name={MedicineName}",
                    medicine.Id, medicine.Name);

                return MapToDto(medicine);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to save medicine to database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while creating the medicine.", ex);
            }
        }

        public async Task<MedicineDto> UpdateMedicineAsync(Guid id, UpdateMedicineRequest request, Guid updatedBy)
        {
            const string operation = "UpdateMedicine";

            try
            {
                _logger.LogInformation("Starting {Operation} for medicine ID: {MedicineId}", operation, id);

                ValidateUpdateRequest(request);

                // Find existing medicine (not deleted)
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

                if (medicine == null)
                {
                    throw new NotFoundException($"Medicine with ID '{id}' not found.");
                }

                // Check for duplicate name (excluding current medicine)
                var duplicateName = await _context.Medicines
                    .AsNoTracking()
                    .AnyAsync(m => m.Name.ToLower() == request.Name.ToLower() &&
                                   m.Id != id &&
                                   !m.IsDeleted);

                if (duplicateName)
                {
                    throw new ValidationException($"Another medicine with name '{request.Name}' already exists.");
                }

                // Update medicine using domain methods
                medicine.UpdateBasicInfo(
                    name: request.Name?.Trim() ?? string.Empty,
                    genericName: request.GenericName?.Trim(),
                    brandName: request.BrandName?.Trim(),
                    medicineType: medicine.MedicineType, // Keep existing type
                    updatedBy: updatedBy
                );

                medicine.UpdateClassification(
                    category: request.Category?.Trim(),
                    subCategory: request.SubCategory?.Trim(),
                    therapeuticClass: request.TherapeuticClass?.Trim(),
                    updatedBy: updatedBy
                );

                medicine.UpdateFormAndStrength(
                    form: request.Form?.Trim(),
                    strength: request.Strength?.Trim(),
                    packSize: request.PackSize?.Trim(),
                    composition: request.Composition?.Trim(),
                    updatedBy: updatedBy
                );

                medicine.UpdateManufacturer(
                    manufacturer: request.Manufacturer?.Trim(),
                    manufacturerCode: request.ManufacturerCode?.Trim(),
                    updatedBy: updatedBy
                );

                medicine.UpdatePricing(
                    purchasePrice: request.PurchasePrice,
                    sellingPrice: request.SellingPrice,
                    mrp: request.MRP,
                    gstPercentage: request.GSTPercentage,
                    updatedBy: updatedBy
                );

                medicine.UpdateStock(
                    currentStock: medicine.CurrentStock, // Keep current stock
                    minimumStockLevel: request.MinimumStockLevel,
                    reorderLevel: request.ReorderLevel,
                    stockUnit: request.StockUnit?.Trim(),
                    updatedBy: updatedBy
                );

                medicine.UpdateRegulatoryInfo(
                    hsnCode: request.HSNCode?.Trim(),
                    schedule: request.Schedule?.Trim(),
                    requiresPrescription: request.RequiresPrescription,
                    updatedBy: updatedBy
                );

                medicine.UpdateSafetyInfo(
                    sideEffects: request.SideEffects?.Trim(),
                    contraindications: request.Contraindications?.Trim(),
                    precautions: request.Precautions?.Trim(),
                    storageInstructions: request.StorageInstructions?.Trim(),
                    updatedBy: updatedBy
                );

                medicine.SetExpiryDate(request.ExpiryDate, updatedBy);

                // Update active status
                if (request.IsActive && !medicine.IsActive)
                {
                    medicine.Activate(updatedBy);
                }
                else if (!request.IsActive && medicine.IsActive)
                {
                    medicine.Deactivate(updatedBy);
                }

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated medicine: ID={MedicineId}", medicine.Id);
                return MapToDto(medicine);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Medicine not found during {Operation}", operation);
                throw;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to update medicine in database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while updating the medicine.", ex);
            }
        }

        public async Task<MedicineDto> GetMedicineAsync(Guid id)
        {
            const string operation = "GetMedicine";

            try
            {
                // Get from database (not deleted)
                var medicine = await _context.Medicines
                    .AsNoTracking()
                    .Include(m => m.PrescriptionMedicines)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

                if (medicine == null)
                {
                    throw new NotFoundException($"Medicine with ID '{id}' not found.");
                }

                _logger.LogDebug("Retrieved medicine {MedicineId} from database", id);
                return MapToDto(medicine);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Medicine not found during {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation} for ID: {MedicineId}", operation, id);
                throw new ApplicationException($"Error retrieving medicine with ID '{id}'.", ex);
            }
        }

        public async Task<PagedResponse<MedicineDto>> SearchMedicinesAsync(MedicineSearchRequest request)
        {
            const string operation = "SearchMedicines";

            try
            {
                _logger.LogDebug("Starting {Operation} with search term: {SearchTerm}",
                    operation, request.SearchTerm);

                // Validate pagination
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                // Build query (excluding deleted medicines)
                var query = _context.Medicines
                    .AsNoTracking()
                    .Include(m => m.PrescriptionMedicines)
                    .Where(m => !m.IsDeleted);

                // Apply filters
                query = ApplyFilters(query, request);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply sorting and pagination
                var medicines = await ApplySortingAndPagination(query, request)
                    .ToListAsync();

                // Map to DTOs
                var medicineDtos = medicines.Select(MapToDto).ToList();

                // Create paged response
                var result = new PagedResponse<MedicineDto>(
                    medicineDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);

                _logger.LogDebug("Found {Count} medicines matching search criteria", totalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error searching medicines. Please try again.", ex);
            }
        }

        public async Task<MedicineStatisticsDto> GetMedicineStatisticsAsync()
        {
            const string operation = "GetMedicineStatistics";

            try
            {
                // Get all non-deleted medicines
                var medicines = await _context.Medicines
                    .AsNoTracking()
                    .Include(m => m.PrescriptionMedicines)
                    .Where(m => !m.IsDeleted)
                    .ToListAsync();

                // Calculate statistics
                var statistics = new MedicineStatisticsDto
                {
                    TotalMedicines = medicines.Count,
                    ActiveMedicines = medicines.Count(m => m.IsActive),
                    ExpiredMedicines = medicines.Count(m => m.IsExpired()),
                    LowStockMedicines = medicines.Count(m => m.IsStockLow()),
                    OutOfStockMedicines = medicines.Count(m => m.CurrentStock <= 0),
                    CriticalStockMedicines = medicines.Count(m => m.IsStockCritical()),
                    TotalStockValue = medicines.Sum(m => m.CurrentStock * m.PurchasePrice),
                    TotalPurchaseValue = medicines.Sum(m => m.CurrentStock * m.PurchasePrice),
                    TotalPotentialRevenue = medicines.Sum(m => m.CurrentStock * m.SellingPrice),
                    MedicinesByCategory = medicines
                        .Where(m => !string.IsNullOrEmpty(m.Category))
                        .GroupBy(m => m.Category!)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    MedicinesByType = medicines
                        .GroupBy(m => m.MedicineType.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TopPrescribedMedicines = medicines
                        .OrderByDescending(m => m.PrescriptionMedicines.Count)
                        .Take(10)
                        .ToDictionary(m => m.Name, m => m.PrescriptionMedicines.Count),
                    MonthlySales = await CalculateMonthlySalesAsync(),
                    MonthlyProfit = await CalculateMonthlyProfitAsync(),
                    AverageProfitMargin = medicines.Any()
                        ? Math.Round(medicines.Average(m => m.SellingPrice > 0 ?
                            ((m.SellingPrice - m.PurchasePrice) / m.SellingPrice) * 100 : 0), 2)
                        : 0,
                    ExpiringSoon = await GetExpiringSoonMedicinesAsync(),
                    LowStockList = await GetLowStockMedicinesAsync()
                };

                _logger.LogDebug("Generated statistics for {Count} medicines", medicines.Count);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error retrieving medicine statistics. Please try again.", ex);
            }
        }

        public async Task<MedicineDto> UpdateStockAsync(Guid medicineId, StockUpdateRequest request, Guid updatedBy)
        {
            const string operation = "UpdateStock";

            try
            {
                _logger.LogInformation("Starting {Operation} for medicine ID: {MedicineId}", operation, medicineId);

                ValidateStockUpdateRequest(request);

                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.Id == medicineId && !m.IsDeleted);

                if (medicine == null)
                {
                    throw new NotFoundException($"Medicine with ID '{medicineId}' not found.");
                }

                // Update stock based on action
                switch (request.Action)
                {
                    case StockAction.Add:
                        medicine.AddStock(request.Quantity, updatedBy);
                        break;

                    case StockAction.Remove:
                        medicine.RemoveStock(request.Quantity, updatedBy);
                        break;

                    case StockAction.Adjust:
                        medicine.UpdateStock(request.Quantity, updatedBy: updatedBy);
                        break;

                    case StockAction.Return:
                        medicine.AddStock(request.Quantity, updatedBy);
                        break;

                    default:
                        throw new ValidationException($"Invalid stock action: {request.Action}");
                }

                // Update purchase price if provided
                if (request.PurchasePrice.HasValue)
                {
                    medicine.UpdatePricing(
                        purchasePrice: request.PurchasePrice.Value,
                        sellingPrice: medicine.SellingPrice,
                        mrp: medicine.MRP,
                        gstPercentage: medicine.GSTPercentage,
                        updatedBy: updatedBy
                    );
                }

                // Update expiry date if provided
                if (request.ExpiryDate.HasValue)
                {
                    medicine.SetExpiryDate(request.ExpiryDate.Value, updatedBy);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated stock for medicine: ID={MedicineId}", medicineId);
                return MapToDto(medicine);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Medicine not found during {Operation}", operation);
                throw;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Stock operation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to update medicine stock in database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while updating medicine stock.", ex);
            }
        }

        public async Task<bool> CheckAvailabilityAsync(Guid medicineId, int quantity)
        {
            try
            {
                var medicine = await _context.Medicines
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == medicineId && !m.IsDeleted && m.IsActive);

                if (medicine == null)
                {
                    return false;
                }

                return medicine.CurrentStock >= quantity && !medicine.IsExpired();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for medicine {MedicineId}", medicineId);
                return false;
            }
        }

        #region Private Helper Methods

        private void ValidateCreateRequest(CreateMedicineRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Medicine name is required.");

            if (request.SellingPrice <= 0)
                throw new ValidationException("Selling price must be positive.");

            if (request.InitialStock < 0)
                throw new ValidationException("Initial stock cannot be negative.");

            if (request.MinimumStockLevel.HasValue && request.MinimumStockLevel < 0)
                throw new ValidationException("Minimum stock level cannot be negative.");

            if (request.ReorderLevel.HasValue && request.ReorderLevel < 0)
                throw new ValidationException("Reorder level cannot be negative.");

            if (request.PurchasePrice.HasValue && request.PurchasePrice <= 0)
                throw new ValidationException("Purchase price must be positive.");

            if (request.GSTPercentage.HasValue && (request.GSTPercentage < 0 || request.GSTPercentage > 100))
                throw new ValidationException("GST percentage must be between 0 and 100.");
        }

        private void ValidateUpdateRequest(UpdateMedicineRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Medicine name is required.");

            if (request.PurchasePrice <= 0)
                throw new ValidationException("Purchase price must be positive.");

            if (request.SellingPrice <= 0)
                throw new ValidationException("Selling price must be positive.");

            if (request.MinimumStockLevel.HasValue && request.MinimumStockLevel < 0)
                throw new ValidationException("Minimum stock level cannot be negative.");

            if (request.ReorderLevel.HasValue && request.ReorderLevel < 0)
                throw new ValidationException("Reorder level cannot be negative.");

            if (request.GSTPercentage.HasValue && (request.GSTPercentage < 0 || request.GSTPercentage > 100))
                throw new ValidationException("GST percentage must be between 0 and 100.");
        }

        private void ValidateStockUpdateRequest(StockUpdateRequest request)
        {
            if (request.Quantity <= 0)
                throw new ValidationException("Quantity must be positive.");

            if (request.PurchasePrice.HasValue && request.PurchasePrice <= 0)
                throw new ValidationException("Purchase price must be positive.");
        }

        private IQueryable<Medicine> ApplyFilters(IQueryable<Medicine> query, MedicineSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(searchTerm) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(searchTerm)) ||
                    (m.BrandName != null && m.BrandName.ToLower().Contains(searchTerm)) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(m => m.Category == request.Category);

            if (!string.IsNullOrWhiteSpace(request.TherapeuticClass))
                query = query.Where(m => m.TherapeuticClass == request.TherapeuticClass);

            if (!string.IsNullOrWhiteSpace(request.Manufacturer))
                query = query.Where(m => m.Manufacturer == request.Manufacturer);

            if (request.RequiresPrescription.HasValue)
                query = query.Where(m => m.RequiresPrescription == request.RequiresPrescription.Value);

            if (request.IsActive.HasValue)
                query = query.Where(m => m.IsActive == request.IsActive.Value);

            if (request.IsExpired.HasValue)
                query = query.Where(m => m.IsExpired() == request.IsExpired.Value);

            if (request.IsLowStock.HasValue)
                query = query.Where(m => m.IsStockLow() == request.IsLowStock.Value);

            if (request.IsOutOfStock.HasValue)
                query = query.Where(m => (request.IsOutOfStock.Value && m.CurrentStock == 0) ||
                                        (!request.IsOutOfStock.Value && m.CurrentStock > 0));

            if (request.MinPrice.HasValue)
                query = query.Where(m => m.SellingPrice >= request.MinPrice.Value);

            if (request.MaxPrice.HasValue)
                query = query.Where(m => m.SellingPrice <= request.MaxPrice.Value);

            return query;
        }

        private IQueryable<Medicine> ApplySortingAndPagination(IQueryable<Medicine> query, MedicineSearchRequest request)
        {
            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(m => m.Name)
                    : query.OrderBy(m => m.Name),

                "stock" => request.SortDescending
                    ? query.OrderByDescending(m => m.CurrentStock)
                    : query.OrderBy(m => m.CurrentStock),

                "price" => request.SortDescending
                    ? query.OrderByDescending(m => m.SellingPrice)
                    : query.OrderBy(m => m.SellingPrice),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(m => m.CreatedAt)
                    : query.OrderBy(m => m.CreatedAt),

                "expirydate" => request.SortDescending
                    ? query.OrderByDescending(m => m.ExpiryDate)
                    : query.OrderBy(m => m.ExpiryDate),

                _ => query.OrderBy(m => m.Name) // Default sorting
            };

            // Apply pagination
            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private MedicineDto MapToDto(Medicine medicine)
        {
            return new MedicineDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                GenericName = medicine.GenericName,
                BrandName = medicine.BrandName,
                MedicineType = medicine.MedicineType,
                Category = medicine.Category,
                SubCategory = medicine.SubCategory,
                TherapeuticClass = medicine.TherapeuticClass,
                Form = medicine.Form,
                Strength = medicine.Strength,
                PackSize = medicine.PackSize,
                Composition = medicine.Composition,
                Manufacturer = medicine.Manufacturer,
                ManufacturerCode = medicine.ManufacturerCode,
                CurrentStock = medicine.CurrentStock,
                MinimumStockLevel = medicine.MinimumStockLevel,
                ReorderLevel = medicine.ReorderLevel,
                StockUnit = medicine.StockUnit,
                StockStatus = GetStockStatus(medicine),
                PurchasePrice = medicine.PurchasePrice,
                SellingPrice = medicine.SellingPrice,
                MRP = medicine.MRP,
                GSTPercentage = medicine.GSTPercentage,
                HSNCode = medicine.HSNCode,
                Schedule = medicine.Schedule,
                RequiresPrescription = medicine.RequiresPrescription,
                SideEffects = medicine.SideEffects,
                Contraindications = medicine.Contraindications,
                Precautions = medicine.Precautions,
                StorageInstructions = medicine.StorageInstructions,
                IsActive = medicine.IsActive,
                ExpiryDate = medicine.ExpiryDate,
                IsExpired = medicine.IsExpired(),
                IsStockLow = medicine.IsStockLow(),
                IsStockCritical = medicine.IsStockCritical(),
                TotalPrescribed = medicine.PrescriptionMedicines?.Count ?? 0,
                TotalDispensed = CalculateTotalDispensed(medicine),
                CreatedAt = medicine.CreatedAt,
                UpdatedAt = medicine.UpdatedAt
            };
        }

        private string GetStockStatus(Medicine medicine)
        {
            if (medicine.CurrentStock <= 0)
                return "Out of Stock";

            if (medicine.IsStockCritical())
                return "Critical";

            if (medicine.IsStockLow())
                return "Low";

            return "In Stock";
        }

        private int CalculateTotalDispensed(Medicine medicine)
        {
            // This would need to query your dispensing records
            // For now, return a placeholder
            return 0;
        }

        private async Task<decimal> CalculateMonthlySalesAsync()
        {
            try
            {
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                // This would query your dispensing/sales records
                // For now, return a placeholder
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<decimal> CalculateMonthlyProfitAsync()
        {
            try
            {
                var monthlySales = await CalculateMonthlySalesAsync();
                // This would calculate actual profit from sales records
                // For now, return a placeholder
                return monthlySales * 0.3m; // 30% profit margin
            }
            catch
            {
                return 0;
            }
        }

        private async Task<List<ExpiringSoonMedicineDto>> GetExpiringSoonMedicinesAsync()
        {
            try
            {
                var medicines = await _context.Medicines
                    .Where(m => !m.IsDeleted &&
                           m.IsActive &&
                           m.ExpiryDate.HasValue &&
                           m.ExpiryDate.Value > DateTime.UtcNow &&
                           m.ExpiryDate.Value <= DateTime.UtcNow.AddDays(90))
                    .OrderBy(m => m.ExpiryDate)
                    .Take(20)
                    .ToListAsync();

                return medicines.Select(m => new ExpiringSoonMedicineDto
                {
                    MedicineId = m.Id,
                    MedicineName = m.Name,
                    ExpiryDate = m.ExpiryDate.Value,
                    DaysUntilExpiry = (int)(m.ExpiryDate.Value - DateTime.UtcNow).TotalDays,
                    CurrentStock = m.CurrentStock,
                    StockValue = m.CurrentStock * m.PurchasePrice
                }).ToList();
            }
            catch
            {
                return new List<ExpiringSoonMedicineDto>();
            }
        }

        private async Task<List<LowStockMedicineDto>> GetLowStockMedicinesAsync()
        {
            try
            {
                var medicines = await _context.Medicines
                    .Where(m => !m.IsDeleted &&
                           m.IsActive &&
                           m.CurrentStock <= m.ReorderLevel)
                    .OrderBy(m => m.CurrentStock)
                    .Take(20)
                    .ToListAsync();

                return medicines.Select(m => new LowStockMedicineDto
                {
                    MedicineId = m.Id,
                    MedicineName = m.Name,
                    CurrentStock = m.CurrentStock,
                    MinimumStockLevel = m.MinimumStockLevel,
                    ReorderLevel = m.ReorderLevel,
                    Deficiency = Math.Max(0, m.ReorderLevel - m.CurrentStock),
                    IsCritical = m.IsStockCritical()
                }).ToList();
            }
            catch
            {
                return new List<LowStockMedicineDto>();
            }
        }

        #endregion
    }
}