// File: CompileCares.Infrastructure/Services/MasterService.cs
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Master.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Master;
using CompileCares.Infrastructure.Data;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CompileCares.Infrastructure.Services
{
    public class MasterService : IMasterService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MasterService> _logger;

        public MasterService(
            ApplicationDbContext context,
            ILogger<MasterService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Complaints Implementation

        public async Task<ComplaintDto> CreateComplaintAsync(CreateComplaintRequest request, Guid createdBy)
        {
            const string operation = "CreateComplaint";

            try
            {
                _logger.LogInformation("Starting {Operation} for complaint: {ComplaintName}",
                    operation, request.Name);

                ValidateComplaintRequest(request);

                // Check for duplicate
                var existingComplaint = await _context.Complaints
                    .AsNoTracking()
                    .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower() && !c.IsDeleted);

                if (existingComplaint)
                {
                    throw new ValidationException($"Complaint with name '{request.Name}' already exists.");
                }

                // Create entity
                var complaint = new Complaint(
                    name: request.Name?.Trim() ?? string.Empty,
                    category: request.Category?.Trim(),
                    createdBy: createdBy
                );

                // Set common status
                if (!request.IsCommon)
                {
                    complaint.MarkAsUncommon();
                }

                await _context.Complaints.AddAsync(complaint);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created complaint: ID={ComplaintId}, Name={ComplaintName}",
                    complaint.Id, complaint.Name);

                return MapToComplaintDto(complaint);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to save complaint to database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while creating complaint.", ex);
            }
        }

        public async Task<ComplaintDto> UpdateComplaintAsync(Guid id, CreateComplaintRequest request, Guid updatedBy)
        {
            const string operation = "UpdateComplaint";

            try
            {
                _logger.LogInformation("Starting {Operation} for complaint ID: {ComplaintId}", operation, id);

                ValidateComplaintRequest(request);

                var complaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (complaint == null)
                {
                    throw new NotFoundException($"Complaint with ID '{id}' not found.");
                }

                // Check for duplicate name
                var duplicateName = await _context.Complaints
                    .AsNoTracking()
                    .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower() &&
                                   c.Id != id &&
                                   !c.IsDeleted);

                if (duplicateName)
                {
                    throw new ValidationException($"Another complaint with name '{request.Name}' already exists.");
                }

                // Update complaint
                complaint.Update(
                    name: request.Name?.Trim() ?? string.Empty,
                    category: request.Category?.Trim(),
                    updatedBy: updatedBy
                );

                // Update common status
                if (request.IsCommon && !complaint.IsCommon)
                {
                    complaint.MarkAsCommon();
                }
                else if (!request.IsCommon && complaint.IsCommon)
                {
                    complaint.MarkAsUncommon();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated complaint: ID={ComplaintId}", complaint.Id);
                return MapToComplaintDto(complaint);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Complaint not found during {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw;
            }
        }

        public async Task<ComplaintDto> GetComplaintAsync(Guid id)
        {
            try
            {
                var complaint = await _context.Complaints
                    .AsNoTracking()
                    .Include(c => c.PatientComplaints)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (complaint == null)
                {
                    throw new NotFoundException($"Complaint with ID '{id}' not found.");
                }

                return MapToComplaintDto(complaint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving complaint {ComplaintId}", id);
                throw;
            }
        }

        public async Task<PagedResponse<ComplaintDto>> SearchComplaintsAsync(MasterSearchRequest request)
        {
            try
            {
                // Validate pagination
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                // Build query
                var query = _context.Complaints
                    .AsNoTracking()
                    .Include(c => c.PatientComplaints)
                    .Where(c => !c.IsDeleted);

                // Apply filters
                query = ApplyComplaintFilters(query, request);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply sorting and pagination
                var complaints = await ApplyComplaintSortingAndPagination(query, request)
                    .ToListAsync();

                // Map to DTOs
                var complaintDtos = complaints.Select(MapToComplaintDto).ToList();

                return new PagedResponse<ComplaintDto>(
                    complaintDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching complaints");
                throw new ApplicationException("Error searching complaints.", ex);
            }
        }

        public async Task<bool> DeleteComplaintAsync(Guid id, Guid deletedBy)
        {
            try
            {
                var complaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (complaint == null)
                {
                    throw new NotFoundException($"Complaint with ID '{id}' not found.");
                }

                // Soft delete
                complaint.SoftDelete(deletedBy);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted complaint: ID={ComplaintId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting complaint {ComplaintId}", id);
                throw;
            }
        }

        public async Task<ComplaintDto> ToggleComplaintStatusAsync(Guid id, bool isActive, Guid updatedBy)
        {
            try
            {
                var complaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (complaint == null)
                {
                    throw new NotFoundException($"Complaint with ID '{id}' not found.");
                }

                if (isActive)
                {
                    complaint.Activate();
                }
                else
                {
                    complaint.Deactivate();
                }

                complaint.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();

                return MapToComplaintDto(complaint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling complaint status {ComplaintId}", id);
                throw;
            }
        }

        public async Task<ComplaintDto> ToggleComplaintCommonAsync(Guid id, bool isCommon, Guid updatedBy)
        {
            try
            {
                var complaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (complaint == null)
                {
                    throw new NotFoundException($"Complaint with ID '{id}' not found.");
                }

                if (isCommon)
                {
                    complaint.MarkAsCommon();
                }
                else
                {
                    complaint.MarkAsUncommon();
                }

                complaint.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();

                return MapToComplaintDto(complaint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling complaint common status {ComplaintId}", id);
                throw;
            }
        }

        #endregion

        #region Advised Items Implementation

        public async Task<AdvisedDto> CreateAdvisedAsync(CreateAdvisedRequest request, Guid createdBy)
        {
            const string operation = "CreateAdvised";

            try
            {
                _logger.LogInformation("Starting {Operation} for advised item: {AdvisedText}",
                    operation, request.Text);

                ValidateAdvisedRequest(request);

                var existingAdvised = await _context.AdvisedItems
                    .AsNoTracking()
                    .AnyAsync(a => a.Text.ToLower() == request.Text.ToLower() && !a.IsDeleted);

                if (existingAdvised)
                {
                    throw new ValidationException($"Advised item with text '{request.Text}' already exists.");
                }

                var advised = new Advised(
                    text: request.Text?.Trim() ?? string.Empty,
                    category: request.Category?.Trim(),
                    createdBy: createdBy
                );

                if (!request.IsCommon)
                {
                    advised.MarkAsUncommon();
                }

                await _context.AdvisedItems.AddAsync(advised);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created advised item: ID={AdvisedId}", advised.Id);
                return MapToAdvisedDto(advised);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw;
            }
        }

        public async Task<AdvisedDto> UpdateAdvisedAsync(Guid id, CreateAdvisedRequest request, Guid updatedBy)
        {
            try
            {
                var advised = await _context.AdvisedItems
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advised == null)
                {
                    throw new NotFoundException($"Advised item with ID '{id}' not found.");
                }

                var duplicateText = await _context.AdvisedItems
                    .AsNoTracking()
                    .AnyAsync(a => a.Text.ToLower() == request.Text.ToLower() &&
                                   a.Id != id &&
                                   !a.IsDeleted);

                if (duplicateText)
                {
                    throw new ValidationException($"Another advised item with text '{request.Text}' already exists.");
                }

                advised.Update(
                    text: request.Text?.Trim() ?? string.Empty,
                    category: request.Category?.Trim(),
                    updatedBy: updatedBy
                );

                if (request.IsCommon && !advised.IsCommon)
                {
                    advised.MarkAsCommon();
                }
                else if (!request.IsCommon && advised.IsCommon)
                {
                    advised.MarkAsUncommon();
                }

                await _context.SaveChangesAsync();
                return MapToAdvisedDto(advised);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating advised item {AdvisedId}", id);
                throw;
            }
        }

        public async Task<AdvisedDto> GetAdvisedAsync(Guid id)
        {
            try
            {
                var advised = await _context.AdvisedItems
                    .AsNoTracking()
                    .Include(a => a.PrescriptionAdvisedItems)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advised == null)
                {
                    throw new NotFoundException($"Advised item with ID '{id}' not found.");
                }

                return MapToAdvisedDto(advised);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving advised item {AdvisedId}", id);
                throw;
            }
        }

        public async Task<PagedResponse<AdvisedDto>> SearchAdvisedAsync(MasterSearchRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                var query = _context.AdvisedItems
                    .AsNoTracking()
                    .Include(a => a.PrescriptionAdvisedItems)
                    .Where(a => !a.IsDeleted);

                query = ApplyAdvisedFilters(query, request);

                var totalCount = await query.CountAsync();
                var advisedItems = await ApplyAdvisedSortingAndPagination(query, request)
                    .ToListAsync();

                var advisedDtos = advisedItems.Select(MapToAdvisedDto).ToList();

                return new PagedResponse<AdvisedDto>(
                    advisedDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching advised items");
                throw;
            }
        }

        public async Task<bool> DeleteAdvisedAsync(Guid id, Guid deletedBy)
        {
            try
            {
                var advised = await _context.AdvisedItems
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advised == null)
                {
                    throw new NotFoundException($"Advised item with ID '{id}' not found.");
                }

                advised.SoftDelete(deletedBy);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting advised item {AdvisedId}", id);
                throw;
            }
        }

        public async Task<AdvisedDto> ToggleAdvisedStatusAsync(Guid id, bool isActive, Guid updatedBy)
        {
            try
            {
                var advised = await _context.AdvisedItems
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advised == null)
                {
                    throw new NotFoundException($"Advised item with ID '{id}' not found.");
                }

                if (isActive)
                {
                    advised.Activate();
                }
                else
                {
                    advised.Deactivate();
                }

                advised.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();

                return MapToAdvisedDto(advised);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling advised status {AdvisedId}", id);
                throw;
            }
        }

        public async Task<AdvisedDto> ToggleAdvisedCommonAsync(Guid id, bool isCommon, Guid updatedBy)
        {
            try
            {
                var advised = await _context.AdvisedItems
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advised == null)
                {
                    throw new NotFoundException($"Advised item with ID '{id}' not found.");
                }

                if (isCommon)
                {
                    advised.MarkAsCommon();
                }
                else
                {
                    advised.MarkAsUncommon();
                }

                advised.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();

                return MapToAdvisedDto(advised);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling advised common status {AdvisedId}", id);
                throw;
            }
        }

        #endregion

        #region Doses Implementation

        public async Task<DoseDto> CreateDoseAsync(CreateDoseRequest request, Guid createdBy)
        {
            try
            {
                ValidateDoseRequest(request);

                var existingDose = await _context.Doses
                    .AsNoTracking()
                    .AnyAsync(d => d.Code.ToLower() == request.Code.ToLower() && !d.IsDeleted);

                if (existingDose)
                {
                    throw new ValidationException($"Dose with code '{request.Code}' already exists.");
                }

                var dose = new Dose(
                    code: request.Code?.Trim() ?? string.Empty,
                    name: request.Name?.Trim() ?? string.Empty,
                    createdBy: createdBy
                );

                dose.SetSortOrder(request.SortOrder);

                await _context.Doses.AddAsync(dose);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created dose: {Code} - {Name}", dose.Code, dose.Name);
                return MapToDoseDto(dose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dose");
                throw;
            }
        }

        public async Task<DoseDto> UpdateDoseAsync(Guid id, CreateDoseRequest request, Guid updatedBy)
        {
            try
            {
                var dose = await _context.Doses
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dose == null)
                {
                    throw new NotFoundException($"Dose with ID '{id}' not found.");
                }

                var duplicateCode = await _context.Doses
                    .AsNoTracking()
                    .AnyAsync(d => d.Code.ToLower() == request.Code.ToLower() &&
                                   d.Id != id &&
                                   !d.IsDeleted);

                if (duplicateCode)
                {
                    throw new ValidationException($"Another dose with code '{request.Code}' already exists.");
                }

                dose.Update(
                    code: request.Code?.Trim() ?? string.Empty,
                    name: request.Name?.Trim() ?? string.Empty,
                    updatedBy: updatedBy
                );

                dose.SetSortOrder(request.SortOrder);
                await _context.SaveChangesAsync();

                return MapToDoseDto(dose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dose {DoseId}", id);
                throw;
            }
        }

        public async Task<DoseDto> GetDoseAsync(Guid id)
        {
            try
            {
                var dose = await _context.Doses
                    .AsNoTracking()
                    .Include(d => d.PrescriptionMedicines)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dose == null)
                {
                    throw new NotFoundException($"Dose with ID '{id}' not found.");
                }

                return MapToDoseDto(dose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dose {DoseId}", id);
                throw;
            }
        }

        public async Task<PagedResponse<DoseDto>> SearchDosesAsync(MasterSearchRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                var query = _context.Doses
                    .AsNoTracking()
                    .Include(d => d.PrescriptionMedicines)
                    .Where(d => !d.IsDeleted);

                query = ApplyDoseFilters(query, request);

                var totalCount = await query.CountAsync();
                var doses = await ApplyDoseSortingAndPagination(query, request)
                    .ToListAsync();

                var doseDtos = doses.Select(MapToDoseDto).ToList();

                return new PagedResponse<DoseDto>(
                    doseDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching doses");
                throw;
            }
        }

        public async Task<bool> DeleteDoseAsync(Guid id, Guid deletedBy)
        {
            try
            {
                var dose = await _context.Doses
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dose == null)
                {
                    throw new NotFoundException($"Dose with ID '{id}' not found.");
                }

                // Check if dose is being used
                var isUsed = await _context.PrescriptionMedicines
                    .AnyAsync(pm => pm.DoseId == id);

                if (isUsed)
                {
                    throw new ValidationException($"Cannot delete dose '{dose.Code}' as it is being used in prescriptions.");
                }

                dose.SoftDelete(deletedBy);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dose {DoseId}", id);
                throw;
            }
        }

        public async Task<DoseDto> ToggleDoseStatusAsync(Guid id, bool isActive, Guid updatedBy)
        {
            try
            {
                var dose = await _context.Doses
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dose == null)
                {
                    throw new NotFoundException($"Dose with ID '{id}' not found.");
                }

                if (isActive)
                {
                    dose.Activate();
                }
                else
                {
                    dose.Deactivate();
                }

                dose.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();

                return MapToDoseDto(dose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling dose status {DoseId}", id);
                throw;
            }
        }

        public async Task UpdateDoseSortOrderAsync(Guid id, int sortOrder, Guid updatedBy)
        {
            try
            {
                var dose = await _context.Doses
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (dose == null)
                {
                    throw new NotFoundException($"Dose with ID '{id}' not found.");
                }

                dose.SetSortOrder(sortOrder);
                dose.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dose sort order {DoseId}", id);
                throw;
            }
        }

        public async Task ReorderDosesAsync(List<Guid> doseIds, Guid updatedBy)
        {
            try
            {
                var doses = await _context.Doses
                    .Where(d => doseIds.Contains(d.Id) && !d.IsDeleted)
                    .ToListAsync();

                if (doses.Count != doseIds.Count)
                {
                    throw new NotFoundException("One or more doses not found.");
                }

                for (int i = 0; i < doseIds.Count; i++)
                {
                    var dose = doses.First(d => d.Id == doseIds[i]);
                    dose.SetSortOrder(i + 1);
                    dose.SetUpdatedBy(updatedBy);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering doses");
                throw;
            }
        }

        #endregion

        #region OPD Items Implementation

        public async Task<OPDItemMasterDto> CreateOPDItemAsync(CreateOPDItemRequest request, Guid createdBy)
        {
            const string operation = "CreateOPDItem";

            try
            {
                _logger.LogInformation("Starting {Operation} for OPD item: {ItemCode}",
                    operation, request.ItemCode);

                ValidateOPDItemRequest(request);

                var existingItem = await _context.OPDItemMasters
                    .AsNoTracking()
                    .AnyAsync(i => i.ItemCode.ToLower() == request.ItemCode.ToLower() && !i.IsDeleted);

                if (existingItem)
                {
                    throw new ValidationException($"OPD item with code '{request.ItemCode}' already exists.");
                }

                // Create OPD item
                var opdItem = new OPDItemMaster(
                    itemCode: request.ItemCode?.Trim() ?? string.Empty,
                    itemName: request.ItemName?.Trim() ?? string.Empty,
                    itemType: request.ItemType?.Trim() ?? string.Empty,
                    standardPrice: request.StandardPrice,
                    createdBy: createdBy
                );

                // Update additional properties
                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    opdItem.UpdateBasicInfo(
                        itemName: request.ItemName?.Trim() ?? string.Empty,
                        description: request.Description?.Trim(),
                        itemType: request.ItemType?.Trim() ?? string.Empty,
                        updatedBy: createdBy
                    );
                }

                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    opdItem.UpdateClassification(
                        category: request.Category?.Trim(),
                        subCategory: request.SubCategory?.Trim(),
                        updatedBy: createdBy
                    );
                }

                if (request.DoctorCommission.HasValue)
                {
                    opdItem.UpdatePricing(
                        standardPrice: request.StandardPrice,
                        doctorCommission: request.DoctorCommission.Value,
                        isCommissionPercentage: request.IsCommissionPercentage ?? true,
                        updatedBy: createdBy
                    );
                }

                if (!string.IsNullOrWhiteSpace(request.Instructions))
                {
                    opdItem.UpdateMedicalProperties(
                        instructions: request.Instructions?.Trim(),
                        preparationRequired: request.PreparationRequired?.Trim(),
                        estimatedDuration: request.EstimatedDuration,
                        updatedBy: createdBy
                    );
                }

                if (request.RequiresSpecialist || !string.IsNullOrWhiteSpace(request.RequiredEquipment))
                {
                    opdItem.UpdateRequirements(
                        requiresSpecialist: request.RequiresSpecialist,
                        requiredEquipment: request.RequiredEquipment?.Trim(),
                        requiredConsent: request.RequiredConsent?.Trim(),
                        updatedBy: createdBy
                    );
                }

                // Handle stock if consumable
                if (request.IsConsumable)
                {
                    var initialStock = request.InitialStock ?? 0;
                    opdItem.UpdateStock(
                        currentStock: initialStock,
                        reorderLevel: request.ReorderLevel,
                        updatedBy: createdBy
                    );
                }

                // Update tax info
                if (!string.IsNullOrWhiteSpace(request.HSNCode) || request.GSTPercentage.HasValue)
                {
                    opdItem.UpdateTaxInfo(
                        isTaxable: request.IsTaxable,
                        hsnCode: request.HSNCode?.Trim(),
                        gstPercentage: request.GSTPercentage,
                        isDiscountable: request.IsDiscountable,
                        updatedBy: createdBy
                    );
                }

                await _context.OPDItemMasters.AddAsync(opdItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created OPD item: ID={ItemId}, Code={ItemCode}",
                    opdItem.Id, opdItem.ItemCode);

                return MapToOPDItemDto(opdItem);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while creating OPD item.", ex);
            }
        }

        public async Task<OPDItemMasterDto> UpdateOPDItemAsync(Guid id, CreateOPDItemRequest request, Guid updatedBy)
        {
            try
            {
                var opdItem = await _context.OPDItemMasters
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (opdItem == null)
                {
                    throw new NotFoundException($"OPD item with ID '{id}' not found.");
                }

                ValidateOPDItemRequest(request);

                // Check for duplicate code
                var duplicateCode = await _context.OPDItemMasters
                    .AsNoTracking()
                    .AnyAsync(i => i.ItemCode.ToLower() == request.ItemCode.ToLower() &&
                                   i.Id != id &&
                                   !i.IsDeleted);

                if (duplicateCode)
                {
                    throw new ValidationException($"Another OPD item with code '{request.ItemCode}' already exists.");
                }

                // Update basic info
                opdItem.UpdateBasicInfo(
                    itemName: request.ItemName?.Trim() ?? string.Empty,
                    description: request.Description?.Trim(),
                    itemType: request.ItemType?.Trim() ?? string.Empty,
                    updatedBy: updatedBy
                );

                // Update classification
                opdItem.UpdateClassification(
                    category: request.Category?.Trim(),
                    subCategory: request.SubCategory?.Trim(),
                    updatedBy: updatedBy
                );

                // Update pricing
                opdItem.UpdatePricing(
                    standardPrice: request.StandardPrice,
                    doctorCommission: request.DoctorCommission,
                    isCommissionPercentage: request.IsCommissionPercentage ?? true,
                    updatedBy: updatedBy
                );

                // Update medical properties
                opdItem.UpdateMedicalProperties(
                    instructions: request.Instructions?.Trim(),
                    preparationRequired: request.PreparationRequired?.Trim(),
                    estimatedDuration: request.EstimatedDuration,
                    updatedBy: updatedBy
                );

                // Update requirements
                opdItem.UpdateRequirements(
                    requiresSpecialist: request.RequiresSpecialist,
                    requiredEquipment: request.RequiredEquipment?.Trim(),
                    requiredConsent: request.RequiredConsent?.Trim(),
                    updatedBy: updatedBy
                );

                // Update stock if consumable
                if (opdItem.IsConsumable)
                {
                    opdItem.UpdateStock(
                        currentStock: opdItem.CurrentStock, // Keep current stock
                        reorderLevel: request.ReorderLevel,
                        updatedBy: updatedBy
                    );
                }

                // Update tax info
                opdItem.UpdateTaxInfo(
                    isTaxable: request.IsTaxable,
                    hsnCode: request.HSNCode?.Trim(),
                    gstPercentage: request.GSTPercentage,
                    isDiscountable: request.IsDiscountable,
                    updatedBy: updatedBy
                );

                await _context.SaveChangesAsync();
                return MapToOPDItemDto(opdItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OPD item {ItemId}", id);
                throw;
            }
        }

        public async Task<OPDItemMasterDto> GetOPDItemAsync(Guid id)
        {
            try
            {
                var opdItem = await _context.OPDItemMasters
                    .AsNoTracking()
                    .Include(i => i.OPDBillItems)
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (opdItem == null)
                {
                    throw new NotFoundException($"OPD item with ID '{id}' not found.");
                }

                return MapToOPDItemDto(opdItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OPD item {ItemId}", id);
                throw;
            }
        }

        public async Task<PagedResponse<OPDItemMasterDto>> SearchOPDItemsAsync(MasterSearchRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                var query = _context.OPDItemMasters
                    .AsNoTracking()
                    .Include(i => i.OPDBillItems)
                    .Where(i => !i.IsDeleted);

                query = ApplyOPDItemFilters(query, request);

                var totalCount = await query.CountAsync();
                var items = await ApplyOPDItemSortingAndPagination(query, request)
                    .ToListAsync();

                var itemDtos = items.Select(MapToOPDItemDto).ToList();

                return new PagedResponse<OPDItemMasterDto>(
                    itemDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OPD items");
                throw;
            }
        }

        public async Task<bool> DeleteOPDItemAsync(Guid id, Guid deletedBy)
        {
            try
            {
                var opdItem = await _context.OPDItemMasters
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (opdItem == null)
                {
                    throw new NotFoundException($"OPD item with ID '{id}' not found.");
                }

                // Check if item is being used
                var isUsed = await _context.OPDBillItems
                    .AnyAsync(bi => bi.OPDItemMasterId == id);

                if (isUsed)
                {
                    throw new ValidationException($"Cannot delete OPD item '{opdItem.ItemName}' as it is being used in bills.");
                }

                opdItem.SoftDelete(deletedBy);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting OPD item {ItemId}", id);
                throw;
            }
        }

        public async Task<OPDItemMasterDto> ToggleOPDItemStatusAsync(Guid id, bool isActive, Guid updatedBy)
        {
            try
            {
                var opdItem = await _context.OPDItemMasters
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (opdItem == null)
                {
                    throw new NotFoundException($"OPD item with ID '{id}' not found.");
                }

                if (isActive)
                {
                    opdItem.Activate(updatedBy);
                }
                else
                {
                    opdItem.Deactivate(updatedBy);
                }

                await _context.SaveChangesAsync();
                return MapToOPDItemDto(opdItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling OPD item status {ItemId}", id);
                throw;
            }
        }

        public async Task UpdateOPDItemStockAsync(Guid id, int quantity, StockAction action, Guid updatedBy)
        {
            try
            {
                var opdItem = await _context.OPDItemMasters
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (opdItem == null)
                {
                    throw new NotFoundException($"OPD item with ID '{id}' not found.");
                }

                if (!opdItem.IsConsumable)
                {
                    throw new ValidationException("Cannot update stock for non-consumable item.");
                }

                switch (action)
                {
                    case StockAction.Add:
                        opdItem.AddStock(quantity, updatedBy);
                        break;

                    case StockAction.Remove:
                        opdItem.ConsumeStock(quantity, updatedBy);
                        break;

                    case StockAction.Adjust:
                        opdItem.UpdateStock(quantity, reorderLevel: opdItem.ReorderLevel, updatedBy: updatedBy);
                        break;

                    case StockAction.Return:
                        opdItem.AddStock(quantity, updatedBy);
                        break;

                    default:
                        throw new ValidationException($"Invalid stock action: {action}");
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OPD item stock {ItemId}", id);
                throw;
            }
        }

        public async Task<OPDItemMasterDto> UpdateOPDItemPricingAsync(Guid id, decimal standardPrice,
            decimal? doctorCommission = null, bool? isCommissionPercentage = null, Guid? updatedBy = null)
        {
            try
            {
                var opdItem = await _context.OPDItemMasters
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

                if (opdItem == null)
                {
                    throw new NotFoundException($"OPD item with ID '{id}' not found.");
                }

                opdItem.UpdatePricing(
                    standardPrice: standardPrice,
                    doctorCommission: doctorCommission,
                    isCommissionPercentage: isCommissionPercentage,
                    updatedBy: updatedBy
                );

                await _context.SaveChangesAsync();
                return MapToOPDItemDto(opdItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OPD item pricing {ItemId}", id);
                throw;
            }
        }

        #endregion

        #region Statistics Implementation

        public async Task<MasterStatisticsDto> GetMasterStatisticsAsync()
        {
            try
            {
                var statistics = new MasterStatisticsDto();

                // Complaints statistics
                var complaints = await _context.Complaints
                    .AsNoTracking()
                    .Include(c => c.PatientComplaints)
                    .Where(c => !c.IsDeleted)
                    .ToListAsync();

                statistics.TotalComplaints = complaints.Count;
                statistics.ActiveComplaints = complaints.Count(c => c.IsActive);
                statistics.CommonComplaints = complaints.Count(c => c.IsCommon);

                // Advised items statistics
                var advisedItems = await _context.AdvisedItems
                    .AsNoTracking()
                    .Include(a => a.PrescriptionAdvisedItems)
                    .Where(a => !a.IsDeleted)
                    .ToListAsync();

                statistics.TotalAdvisedItems = advisedItems.Count;
                statistics.ActiveAdvisedItems = advisedItems.Count(a => a.IsActive);
                statistics.CommonAdvisedItems = advisedItems.Count(a => a.IsCommon);

                // Doses statistics
                var doses = await _context.Doses
                    .AsNoTracking()
                    .Include(d => d.PrescriptionMedicines)
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                statistics.TotalDoses = doses.Count;
                statistics.ActiveDoses = doses.Count(d => d.IsActive);

                // OPD items statistics
                var opdItems = await _context.OPDItemMasters
                    .AsNoTracking()
                    .Include(i => i.OPDBillItems)
                    .Where(i => !i.IsDeleted)
                    .ToListAsync();

                statistics.TotalOPDItems = opdItems.Count;
                statistics.ActiveOPDItems = opdItems.Count(i => i.IsActive);
                statistics.ConsumableItems = opdItems.Count(i => i.IsConsumable);
                statistics.LowStockItems = opdItems.Count(i => i.IsStockLow());

                // Usage statistics
                statistics.MostUsedComplaints = complaints
                    .OrderByDescending(c => c.PatientComplaints.Count)
                    .Take(10)
                    .ToDictionary(c => c.Name, c => c.PatientComplaints.Count);

                statistics.MostUsedAdvised = advisedItems
                    .OrderByDescending(a => a.PrescriptionAdvisedItems.Count)
                    .Take(10)
                    .ToDictionary(a => a.Text, a => a.PrescriptionAdvisedItems.Count);

                statistics.MostUsedDoses = doses
                    .OrderByDescending(d => d.PrescriptionMedicines.Count)
                    .Take(10)
                    .ToDictionary(d => d.Code, d => d.PrescriptionMedicines.Count);

                statistics.MostUsedOPDItems = opdItems
                    .OrderByDescending(i => i.OPDBillItems.Count)
                    .Take(10)
                    .ToDictionary(i => i.ItemName, i => i.OPDBillItems.Count);

                // Category statistics
                statistics.ItemsByType = opdItems
                    .Where(i => !string.IsNullOrEmpty(i.ItemType))
                    .GroupBy(i => i.ItemType)
                    .ToDictionary(g => g.Key, g => g.Count());

                statistics.ItemsByCategory = opdItems
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .GroupBy(i => i.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Financial statistics
                statistics.TotalOPDItemValue = opdItems.Sum(i => i.StandardPrice);
                statistics.TotalConsumableStockValue = opdItems
                    .Where(i => i.IsConsumable)
                    .Sum(i => i.CurrentStock * i.StandardPrice);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting master statistics");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetUsageStatisticsAsync(string type, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                fromDate ??= DateTime.UtcNow.AddDays(-30);
                toDate ??= DateTime.UtcNow;

                var result = new Dictionary<string, int>();

                switch (type.ToLower())
                {
                    case "complaints":
                        var complaints = await _context.PatientComplaints
                            .Where(pc => pc.CreatedAt >= fromDate && pc.CreatedAt <= toDate)
                            .GroupBy(pc => pc.Complaint.Name)
                            .Select(g => new { Name = g.Key, Count = g.Count() })
                            .ToListAsync();

                        result = complaints.ToDictionary(x => x.Name, x => x.Count);
                        break;

                    case "advised":
                        var advised = await _context.PrescriptionAdvisedItems
                            .Where(pa => pa.CreatedAt >= fromDate && pa.CreatedAt <= toDate)
                            .GroupBy(pa => pa.Advised.Text)
                            .Select(g => new { Text = g.Key, Count = g.Count() })
                            .ToListAsync();

                        result = advised.ToDictionary(x => x.Text, x => x.Count);
                        break;

                    case "doses":
                        var doses = await _context.PrescriptionMedicines
                            .Where(pm => pm.CreatedAt >= fromDate && pm.CreatedAt <= toDate)
                            .GroupBy(pm => pm.Dose.Code)
                            .Select(g => new { Code = g.Key, Count = g.Count() })
                            .ToListAsync();

                        result = doses.ToDictionary(x => x.Code, x => x.Count);
                        break;

                    case "opditems":
                        var opdItems = await _context.OPDBillItems
                            .Where(bi => bi.CreatedAt >= fromDate && bi.CreatedAt <= toDate)
                            .GroupBy(bi => bi.OPDItemMaster.ItemName)
                            .Select(g => new { Name = g.Key, Count = g.Count() })
                            .ToListAsync();

                        result = opdItems.ToDictionary(x => x.Name, x => x.Count);
                        break;

                    default:
                        throw new ValidationException($"Invalid type: {type}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage statistics for type: {Type}", type);
                throw;
            }
        }

        public async Task<List<ComplaintDto>> GetMostUsedComplaintsAsync(int count = 10)
        {
            try
            {
                var complaints = await _context.Complaints
                    .AsNoTracking()
                    .Include(c => c.PatientComplaints)
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .OrderByDescending(c => c.PatientComplaints.Count)
                    .Take(count)
                    .ToListAsync();

                return complaints.Select(MapToComplaintDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used complaints");
                throw;
            }
        }

        public async Task<List<AdvisedDto>> GetMostUsedAdvisedAsync(int count = 10)
        {
            try
            {
                var advisedItems = await _context.AdvisedItems
                    .AsNoTracking()
                    .Include(a => a.PrescriptionAdvisedItems)
                    .Where(a => !a.IsDeleted && a.IsActive)
                    .OrderByDescending(a => a.PrescriptionAdvisedItems.Count)
                    .Take(count)
                    .ToListAsync();

                return advisedItems.Select(MapToAdvisedDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used advised items");
                throw;
            }
        }

        public async Task<List<DoseDto>> GetMostUsedDosesAsync(int count = 10)
        {
            try
            {
                var doses = await _context.Doses
                    .AsNoTracking()
                    .Include(d => d.PrescriptionMedicines)
                    .Where(d => !d.IsDeleted && d.IsActive)
                    .OrderByDescending(d => d.PrescriptionMedicines.Count)
                    .Take(count)
                    .ToListAsync();

                return doses.Select(MapToDoseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used doses");
                throw;
            }
        }

        public async Task<List<OPDItemMasterDto>> GetMostUsedOPDItemsAsync(int count = 10)
        {
            try
            {
                var opdItems = await _context.OPDItemMasters
                    .AsNoTracking()
                    .Include(i => i.OPDBillItems)
                    .Where(i => !i.IsDeleted && i.IsActive)
                    .OrderByDescending(i => i.OPDBillItems.Count)
                    .Take(count)
                    .ToListAsync();

                return opdItems.Select(MapToOPDItemDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used OPD items");
                throw;
            }
        }

        #endregion

        #region Bulk Operations

        public async Task<List<ComplaintDto>> BulkCreateComplaintsAsync(List<CreateComplaintRequest> requests, Guid createdBy)
        {
            try
            {
                var complaints = new List<Complaint>();
                var existingNames = await _context.Complaints
                    .Where(c => !c.IsDeleted)
                    .Select(c => c.Name.ToLower())
                    .ToListAsync();

                foreach (var request in requests)
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                        continue;

                    if (existingNames.Contains(request.Name.ToLower()))
                        continue;

                    var complaint = new Complaint(
                        name: request.Name.Trim(),
                        category: request.Category?.Trim(),
                        createdBy: createdBy
                    );

                    if (!request.IsCommon)
                    {
                        complaint.MarkAsUncommon();
                    }

                    complaints.Add(complaint);
                    existingNames.Add(request.Name.ToLower());
                }

                if (complaints.Any())
                {
                    await _context.Complaints.AddRangeAsync(complaints);
                    await _context.SaveChangesAsync();
                }

                return complaints.Select(MapToComplaintDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating complaints");
                throw;
            }
        }

        public async Task<List<AdvisedDto>> BulkCreateAdvisedAsync(List<CreateAdvisedRequest> requests, Guid createdBy)
        {
            try
            {
                var advisedItems = new List<Advised>();
                var existingTexts = await _context.AdvisedItems
                    .Where(a => !a.IsDeleted)
                    .Select(a => a.Text.ToLower())
                    .ToListAsync();

                foreach (var request in requests)
                {
                    if (string.IsNullOrWhiteSpace(request.Text))
                        continue;

                    if (existingTexts.Contains(request.Text.ToLower()))
                        continue;

                    var advised = new Advised(
                        text: request.Text.Trim(),
                        category: request.Category?.Trim(),
                        createdBy: createdBy
                    );

                    if (!request.IsCommon)
                    {
                        advised.MarkAsUncommon();
                    }

                    advisedItems.Add(advised);
                    existingTexts.Add(request.Text.ToLower());
                }

                if (advisedItems.Any())
                {
                    await _context.AdvisedItems.AddRangeAsync(advisedItems);
                    await _context.SaveChangesAsync();
                }

                return advisedItems.Select(MapToAdvisedDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating advised items");
                throw;
            }
        }

        public async Task<List<DoseDto>> BulkCreateDosesAsync(List<CreateDoseRequest> requests, Guid createdBy)
        {
            try
            {
                var doses = new List<Dose>();
                var existingCodes = await _context.Doses
                    .Where(d => !d.IsDeleted)
                    .Select(d => d.Code.ToLower())
                    .ToListAsync();

                foreach (var request in requests)
                {
                    if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                        continue;

                    if (existingCodes.Contains(request.Code.ToLower()))
                        continue;

                    var dose = new Dose(
                        code: request.Code.Trim(),
                        name: request.Name.Trim(),
                        createdBy: createdBy
                    );

                    dose.SetSortOrder(request.SortOrder);
                    doses.Add(dose);
                    existingCodes.Add(request.Code.ToLower());
                }

                if (doses.Any())
                {
                    await _context.Doses.AddRangeAsync(doses);
                    await _context.SaveChangesAsync();
                }

                return doses.Select(MapToDoseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating doses");
                throw;
            }
        }

        public async Task<List<OPDItemMasterDto>> BulkCreateOPDItemsAsync(List<CreateOPDItemRequest> requests, Guid createdBy)
        {
            try
            {
                var opdItems = new List<OPDItemMaster>();
                var existingCodes = await _context.OPDItemMasters
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.ItemCode.ToLower())
                    .ToListAsync();

                foreach (var request in requests)
                {
                    if (string.IsNullOrWhiteSpace(request.ItemCode) ||
                        string.IsNullOrWhiteSpace(request.ItemName) ||
                        string.IsNullOrWhiteSpace(request.ItemType))
                        continue;

                    if (existingCodes.Contains(request.ItemCode.ToLower()))
                        continue;

                    var opdItem = new OPDItemMaster(
                        itemCode: request.ItemCode.Trim(),
                        itemName: request.ItemName.Trim(),
                        itemType: request.ItemType.Trim(),
                        standardPrice: request.StandardPrice,
                        createdBy: createdBy
                    );

                    opdItems.Add(opdItem);
                    existingCodes.Add(request.ItemCode.ToLower());
                }

                if (opdItems.Any())
                {
                    await _context.OPDItemMasters.AddRangeAsync(opdItems);
                    await _context.SaveChangesAsync();
                }

                return opdItems.Select(MapToOPDItemDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating OPD items");
                throw;
            }
        }

        #endregion

        #region Categories

        public async Task<List<string>> GetComplaintCategoriesAsync()
        {
            try
            {
                return await _context.Complaints
                    .Where(c => !c.IsDeleted && !string.IsNullOrEmpty(c.Category))
                    .Select(c => c.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting complaint categories");
                throw;
            }
        }

        public async Task<List<string>> GetAdvisedCategoriesAsync()
        {
            try
            {
                return await _context.AdvisedItems
                    .Where(a => !a.IsDeleted && !string.IsNullOrEmpty(a.Category))
                    .Select(a => a.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting advised categories");
                throw;
            }
        }

        public async Task<List<string>> GetOPDItemTypesAsync()
        {
            try
            {
                return await _context.OPDItemMasters
                    .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.ItemType))
                    .Select(i => i.ItemType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item types");
                throw;
            }
        }

        public async Task<List<string>> GetOPDItemCategoriesAsync()
        {
            try
            {
                return await _context.OPDItemMasters
                    .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item categories");
                throw;
            }
        }

        public async Task<List<string>> GetOPDItemSubCategoriesAsync(string category)
        {
            try
            {
                return await _context.OPDItemMasters
                    .Where(i => !i.IsDeleted &&
                           i.Category == category &&
                           !string.IsNullOrEmpty(i.SubCategory))
                    .Select(i => i.SubCategory!)
                    .Distinct()
                    .OrderBy(sc => sc)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item subcategories for category: {Category}", category);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private void ValidateComplaintRequest(CreateComplaintRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Complaint name is required.");

            if (request.Name.Length > 200)
                throw new ValidationException("Complaint name cannot exceed 200 characters.");
        }

        private void ValidateAdvisedRequest(CreateAdvisedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                throw new ValidationException("Advised text is required.");

            if (request.Text.Length > 500)
                throw new ValidationException("Advised text cannot exceed 500 characters.");
        }

        private void ValidateDoseRequest(CreateDoseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ValidationException("Dose code is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Dose name is required.");

            if (request.Code.Length > 10)
                throw new ValidationException("Dose code cannot exceed 10 characters.");

            if (request.Name.Length > 100)
                throw new ValidationException("Dose name cannot exceed 100 characters.");
        }

        private void ValidateOPDItemRequest(CreateOPDItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemCode))
                throw new ValidationException("Item code is required.");

            if (string.IsNullOrWhiteSpace(request.ItemName))
                throw new ValidationException("Item name is required.");

            if (string.IsNullOrWhiteSpace(request.ItemType))
                throw new ValidationException("Item type is required.");

            if (request.StandardPrice < 0)
                throw new ValidationException("Standard price cannot be negative.");

            if (request.DoctorCommission.HasValue && request.DoctorCommission.Value < 0)
                throw new ValidationException("Doctor commission cannot be negative.");

            if (request.GSTPercentage.HasValue && (request.GSTPercentage < 0 || request.GSTPercentage > 100))
                throw new ValidationException("GST percentage must be between 0 and 100.");
        }

        private IQueryable<Complaint> ApplyComplaintFilters(IQueryable<Complaint> query, MasterSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Category != null && c.Category.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(c => c.Category == request.Category);

            if (request.IsActive.HasValue)
                query = query.Where(c => c.IsActive == request.IsActive.Value);

            if (request.IsCommon.HasValue)
                query = query.Where(c => c.IsCommon == request.IsCommon.Value);

            return query;
        }

        private IQueryable<Complaint> ApplyComplaintSortingAndPagination(IQueryable<Complaint> query, MasterSearchRequest request)
        {
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),

                "category" => request.SortDescending
                    ? query.OrderByDescending(c => c.Category)
                    : query.OrderBy(c => c.Category),

                "usagecount" => request.SortDescending
                    ? query.OrderByDescending(c => c.PatientComplaints.Count)
                    : query.OrderBy(c => c.PatientComplaints.Count),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),

                _ => query.OrderBy(c => c.Name)
            };

            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private IQueryable<Advised> ApplyAdvisedFilters(IQueryable<Advised> query, MasterSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(a =>
                    a.Text.ToLower().Contains(searchTerm) ||
                    (a.Category != null && a.Category.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(a => a.Category == request.Category);

            if (request.IsActive.HasValue)
                query = query.Where(a => a.IsActive == request.IsActive.Value);

            if (request.IsCommon.HasValue)
                query = query.Where(a => a.IsCommon == request.IsCommon.Value);

            return query;
        }

        private IQueryable<Advised> ApplyAdvisedSortingAndPagination(IQueryable<Advised> query, MasterSearchRequest request)
        {
            query = request.SortBy?.ToLower() switch
            {
                "text" => request.SortDescending
                    ? query.OrderByDescending(a => a.Text)
                    : query.OrderBy(a => a.Text),

                "category" => request.SortDescending
                    ? query.OrderByDescending(a => a.Category)
                    : query.OrderBy(a => a.Category),

                "usagecount" => request.SortDescending
                    ? query.OrderByDescending(a => a.PrescriptionAdvisedItems.Count)
                    : query.OrderBy(a => a.PrescriptionAdvisedItems.Count),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(a => a.CreatedAt)
                    : query.OrderBy(a => a.CreatedAt),

                _ => query.OrderBy(a => a.Text)
            };

            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private IQueryable<Dose> ApplyDoseFilters(IQueryable<Dose> query, MasterSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(d =>
                    d.Code.ToLower().Contains(searchTerm) ||
                    d.Name.ToLower().Contains(searchTerm));
            }

            if (request.IsActive.HasValue)
                query = query.Where(d => d.IsActive == request.IsActive.Value);

            return query;
        }

        private IQueryable<Dose> ApplyDoseSortingAndPagination(IQueryable<Dose> query, MasterSearchRequest request)
        {
            query = request.SortBy?.ToLower() switch
            {
                "code" => request.SortDescending
                    ? query.OrderByDescending(d => d.Code)
                    : query.OrderBy(d => d.Code),

                "name" => request.SortDescending
                    ? query.OrderByDescending(d => d.Name)
                    : query.OrderBy(d => d.Name),

                "sortorder" => request.SortDescending
                    ? query.OrderByDescending(d => d.SortOrder)
                    : query.OrderBy(d => d.SortOrder),

                "usagecount" => request.SortDescending
                    ? query.OrderByDescending(d => d.PrescriptionMedicines.Count)
                    : query.OrderBy(d => d.PrescriptionMedicines.Count),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(d => d.CreatedAt)
                    : query.OrderBy(d => d.CreatedAt),

                _ => query.OrderBy(d => d.SortOrder).ThenBy(d => d.Code)
            };

            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private IQueryable<OPDItemMaster> ApplyOPDItemFilters(IQueryable<OPDItemMaster> query, MasterSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(i =>
                    i.ItemCode.ToLower().Contains(searchTerm) ||
                    i.ItemName.ToLower().Contains(searchTerm) ||
                    (i.Description != null && i.Description.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(i => i.Category == request.Category);

            if (!string.IsNullOrWhiteSpace(request.Type))
                query = query.Where(i => i.ItemType == request.Type);

            if (request.IsActive.HasValue)
                query = query.Where(i => i.IsActive == request.IsActive.Value);

            if (request.IsConsumable.HasValue)
                query = query.Where(i => i.IsConsumable == request.IsConsumable.Value);

            if (request.IsLowStock.HasValue)
                query = query.Where(i => i.IsStockLow() == request.IsLowStock.Value);

            if (request.MinPrice.HasValue)
                query = query.Where(i => i.StandardPrice >= request.MinPrice.Value);

            if (request.MaxPrice.HasValue)
                query = query.Where(i => i.StandardPrice <= request.MaxPrice.Value);

            return query;
        }

        private IQueryable<OPDItemMaster> ApplyOPDItemSortingAndPagination(IQueryable<OPDItemMaster> query, MasterSearchRequest request)
        {
            query = request.SortBy?.ToLower() switch
            {
                "itemcode" => request.SortDescending
                    ? query.OrderByDescending(i => i.ItemCode)
                    : query.OrderBy(i => i.ItemCode),

                "itemname" => request.SortDescending
                    ? query.OrderByDescending(i => i.ItemName)
                    : query.OrderBy(i => i.ItemName),

                "price" => request.SortDescending
                    ? query.OrderByDescending(i => i.StandardPrice)
                    : query.OrderBy(i => i.StandardPrice),

                "stock" => request.SortDescending
                    ? query.OrderByDescending(i => i.CurrentStock)
                    : query.OrderBy(i => i.CurrentStock),

                "usagecount" => request.SortDescending
                    ? query.OrderByDescending(i => i.OPDBillItems.Count)
                    : query.OrderBy(i => i.OPDBillItems.Count),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(i => i.CreatedAt)
                    : query.OrderBy(i => i.CreatedAt),

                _ => query.OrderBy(i => i.ItemName)
            };

            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private ComplaintDto MapToComplaintDto(Complaint complaint)
        {
            return new ComplaintDto
            {
                Id = complaint.Id,
                Name = complaint.Name,
                Category = complaint.Category,
                IsActive = complaint.IsActive,
                IsCommon = complaint.IsCommon,
                UsageCount = complaint.PatientComplaints?.Count ?? 0,
                LastUsed = complaint.PatientComplaints?.Any() == true
                    ? complaint.PatientComplaints.Max(pc => pc.CreatedAt)
                    : complaint.CreatedAt,
                CreatedAt = complaint.CreatedAt,
                UpdatedAt = complaint.UpdatedAt
            };
        }

        private AdvisedDto MapToAdvisedDto(Advised advised)
        {
            return new AdvisedDto
            {
                Id = advised.Id,
                Text = advised.Text,
                Category = advised.Category,
                IsActive = advised.IsActive,
                IsCommon = advised.IsCommon,
                UsageCount = advised.PrescriptionAdvisedItems?.Count ?? 0,
                LastUsed = advised.PrescriptionAdvisedItems?.Any() == true
                    ? advised.PrescriptionAdvisedItems.Max(pa => pa.CreatedAt)
                    : advised.CreatedAt,
                CreatedAt = advised.CreatedAt,
                UpdatedAt = advised.UpdatedAt
            };
        }

        private DoseDto MapToDoseDto(Dose dose)
        {
            return new DoseDto
            {
                Id = dose.Id,
                Code = dose.Code,
                Name = dose.Name,
                IsActive = dose.IsActive,
                SortOrder = dose.SortOrder,
                UsageCount = dose.PrescriptionMedicines?.Count ?? 0,
                CreatedAt = dose.CreatedAt,
                UpdatedAt = dose.UpdatedAt
            };
        }

        private OPDItemMasterDto MapToOPDItemDto(OPDItemMaster opdItem)
        {
            return new OPDItemMasterDto
            {
                Id = opdItem.Id,
                ItemCode = opdItem.ItemCode,
                ItemName = opdItem.ItemName,
                Description = opdItem.Description,
                ItemType = opdItem.ItemType,
                Category = opdItem.Category,
                SubCategory = opdItem.SubCategory,
                StandardPrice = opdItem.StandardPrice,
                DoctorCommission = opdItem.DoctorCommission,
                IsCommissionPercentage = opdItem.IsCommissionPercentage,
                Instructions = opdItem.Instructions,
                PreparationRequired = opdItem.PreparationRequired,
                EstimatedDuration = opdItem.EstimatedDuration,
                RequiresSpecialist = opdItem.RequiresSpecialist,
                RequiredEquipment = opdItem.RequiredEquipment,
                RequiredConsent = opdItem.RequiredConsent,
                IsConsumable = opdItem.IsConsumable,
                CurrentStock = opdItem.CurrentStock,
                ReorderLevel = opdItem.ReorderLevel,
                IsStockLow = opdItem.IsStockLow(),
                IsActive = opdItem.IsActive,
                IsTaxable = opdItem.IsTaxable,
                IsDiscountable = opdItem.IsDiscountable,
                HSNCode = opdItem.HSNCode,
                GSTPercentage = opdItem.GSTPercentage,
                UsageCount = opdItem.OPDBillItems?.Count ?? 0,
                TotalRevenue = opdItem.OPDBillItems?.Sum(bi => bi.TotalPrice) ?? 0,
                LastUsed = opdItem.OPDBillItems?.Any() == true
                    ? opdItem.OPDBillItems.Max(bi => bi.CreatedAt)
                    : opdItem.CreatedAt,
                CreatedAt = opdItem.CreatedAt,
                UpdatedAt = opdItem.UpdatedAt
            };
        }

        #endregion
    }
}