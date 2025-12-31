using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Templates.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Templates;
using CompileCares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CompileCares.Infrastructure.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TemplateService> _logger;

        public TemplateService(
            ApplicationDbContext context,
            ILogger<TemplateService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PrescriptionTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, Guid createdBy)
        {
            const string operation = "CreateTemplate";

            try
            {
                _logger.LogInformation("Starting {Operation} for template: {TemplateName}",
                    operation, request.Name);

                ValidateCreateRequest(request);

                // Create template entity
                var template = new PrescriptionTemplate(
                    name: request.Name?.Trim() ?? string.Empty,
                    doctorId: createdBy,
                    description: request.Description?.Trim(),
                    category: request.Category?.Trim(),
                    isPublic: request.IsPublic,
                    createdBy: createdBy
                );

                // Add complaints
                foreach (var complaintReq in request.Complaints)
                {
                    template.AddComplaint(
                        complaintId: complaintReq.ComplaintId,
                        customText: complaintReq.CustomText?.Trim(),
                        duration: complaintReq.Duration?.Trim(),
                        severity: complaintReq.Severity?.Trim(),
                        addedBy: createdBy
                    );
                }

                // Add medicines with validation
                foreach (var medicineReq in request.Medicines)
                {
                    // Validate medicine exists
                    var medicine = await _context.Medicines
                        .FirstOrDefaultAsync(m => m.Id == medicineReq.MedicineId && !m.IsDeleted);

                    if (medicine == null)
                        throw new ValidationException($"Medicine with ID '{medicineReq.MedicineId}' not found.");

                    // Validate dose exists
                    var dose = await _context.Doses
                        .FirstOrDefaultAsync(d => d.Id == medicineReq.DoseId && !d.IsDeleted);

                    if (dose == null)
                        throw new ValidationException($"Dose with ID '{medicineReq.DoseId}' not found.");

                    template.AddMedicine(
                        medicineId: medicineReq.MedicineId,
                        doseId: medicineReq.DoseId,
                        durationDays: medicineReq.DurationDays,
                        quantity: medicineReq.Quantity,
                        instructions: medicineReq.Instructions?.Trim(),
                        addedBy: createdBy
                    );
                }

                // Add advised items
                foreach (var advisedReq in request.AdvisedItems)
                {
                    if (advisedReq.AdvisedId.HasValue)
                    {
                        var advised = await _context.AdvisedItems
                            .FirstOrDefaultAsync(a => a.Id == advisedReq.AdvisedId.Value && !a.IsDeleted);

                        if (advised == null)
                            throw new ValidationException($"Advised item with ID '{advisedReq.AdvisedId}' not found.");
                    }

                    template.AddAdvised(
                        advisedId: advisedReq.AdvisedId,
                        customText: advisedReq.CustomText?.Trim(),
                        addedBy: createdBy
                    );
                }

                // Save to database
                await _context.PrescriptionTemplates.AddAsync(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created template: ID={TemplateId}, Name={TemplateName}",
                    template.Id, template.Name);

                return await MapToTemplateDto(template);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to save template to database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while creating the template.", ex);
            }
        }

        public async Task<PrescriptionTemplateDto> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request, Guid updatedBy)
        {
            const string operation = "UpdateTemplate";

            try
            {
                _logger.LogInformation("Starting {Operation} for template ID: {TemplateId}", operation, id);

                ValidateUpdateRequest(request);

                // Find existing template
                var template = await _context.PrescriptionTemplates
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                if (template == null)
                    throw new NotFoundException($"Template with ID '{id}' not found.");

                // Check permission
                if (template.DoctorId != updatedBy)
                    throw new UnauthorizedAccessException("You don't have permission to update this template.");

                // Update template using domain method
                template.UpdateBasicInfo(
                    name: request.Name?.Trim() ?? string.Empty,
                    description: request.Description?.Trim(),
                    category: request.Category?.Trim(),
                    isPublic: request.IsPublic,
                    updatedBy: updatedBy
                );

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated template: ID={TemplateId}", template.Id);
                return await MapToTemplateDto(template);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to update template in database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while updating the template.", ex);
            }
        }

        public async Task<PrescriptionTemplateDto> GetTemplateAsync(Guid id, Guid doctorId)
        {
            const string operation = "GetTemplate";

            try
            {
                // Get template from database (not deleted)
                var template = await _context.PrescriptionTemplates
                    .AsNoTracking()
                    .Include(t => t.Doctor)
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                if (template == null)
                    throw new NotFoundException($"Template with ID '{id}' not found.");

                // Check permission
                if (!template.CanBeUsedBy(doctorId))
                    throw new UnauthorizedAccessException("You don't have permission to access this template.");

                _logger.LogDebug("Retrieved template {TemplateId} from database", id);
                return await MapToTemplateDto(template);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation} for ID: {TemplateId}", operation, id);
                throw new ApplicationException($"Error retrieving template with ID '{id}'.", ex);
            }
        }

        public async Task<TemplateDetailDto> GetTemplateDetailsAsync(Guid id, Guid doctorId)
        {
            const string operation = "GetTemplateDetails";

            try
            {
                // Get template with all related data
                var template = await _context.PrescriptionTemplates
                    .AsNoTracking()
                    .Include(t => t.Doctor)
                    .Include(t => t.Complaints)
                        .ThenInclude(c => c.Complaint)
                    .Include(t => t.Medicines)
                        .ThenInclude(m => m.Medicine)
                    .Include(t => t.Medicines)
                        .ThenInclude(m => m.Dose)
                    .Include(t => t.AdvisedItems)
                        .ThenInclude(a => a.Advised)
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                if (template == null)
                    throw new NotFoundException($"Template with ID '{id}' not found.");

                // Check permission
                if (!template.CanBeUsedBy(doctorId))
                    throw new UnauthorizedAccessException("You don't have permission to access this template.");

                _logger.LogDebug("Retrieved detailed template {TemplateId} from database", id);
                return await MapToTemplateDetailDto(template);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation} for ID: {TemplateId}", operation, id);
                throw new ApplicationException($"Error retrieving template details for ID '{id}'.", ex);
            }
        }

        public async Task<PagedResponse<PrescriptionTemplateDto>> SearchTemplatesAsync(TemplateSearchRequest request, Guid doctorId)
        {
            const string operation = "SearchTemplates";

            try
            {
                _logger.LogDebug("Starting {Operation} with search term: {SearchTerm}",
                    operation, request.SearchTerm);

                // Validate pagination
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                // Build query (excluding deleted templates)
                var query = _context.PrescriptionTemplates
                    .AsNoTracking()
                    .Include(t => t.Doctor)
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .Where(t => !t.IsDeleted && (t.DoctorId == doctorId || t.IsPublic));

                // Apply filters
                query = ApplyFilters(query, request);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply sorting and pagination
                var templates = await ApplySortingAndPagination(query, request)
                    .ToListAsync();

                // Map to DTOs
                var templateDtos = new List<PrescriptionTemplateDto>();
                foreach (var template in templates)
                {
                    templateDtos.Add(await MapToTemplateDto(template));
                }

                // Create paged response
                var result = new PagedResponse<PrescriptionTemplateDto>(
                    templateDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);

                _logger.LogDebug("Found {Count} templates matching search criteria", totalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error searching templates. Please try again.", ex);
            }
        }

        public async Task<bool> DeleteTemplateAsync(Guid id, Guid deletedBy)
        {
            const string operation = "DeleteTemplate";

            try
            {
                _logger.LogInformation("Starting {Operation} for template ID: {TemplateId}", operation, id);

                var template = await _context.PrescriptionTemplates
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                if (template == null)
                    throw new NotFoundException($"Template with ID '{id}' not found.");

                // Check permission
                if (template.DoctorId != deletedBy)
                    throw new UnauthorizedAccessException("You don't have permission to delete this template.");

                // Soft delete
                template.SoftDelete(deletedBy);
                _context.PrescriptionTemplates.Update(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted template: ID={TemplateId}", id);
                return true;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DbUpdateException("Failed to delete template from database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while deleting the template.", ex);
            }
        }

        public async Task<PrescriptionTemplateDto> CloneTemplateAsync(Guid sourceTemplateId, CloneTemplateRequest request, Guid createdBy)
        {
            const string operation = "CloneTemplate";

            try
            {
                _logger.LogInformation("Starting {Operation} from template ID: {SourceTemplateId}",
                    operation, sourceTemplateId);

                ValidateCloneRequest(request);

                var sourceTemplate = await _context.PrescriptionTemplates
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .FirstOrDefaultAsync(t => t.Id == sourceTemplateId && !t.IsDeleted);

                if (sourceTemplate == null)
                    throw new NotFoundException($"Source template with ID '{sourceTemplateId}' not found.");

                // Check permission to clone
                if (!sourceTemplate.CanBeUsedBy(createdBy))
                    throw new UnauthorizedAccessException("You don't have permission to clone this template.");

                // Use domain clone method
                var clonedTemplate = sourceTemplate.Clone(
                    newName: request.NewName?.Trim() ?? string.Empty,
                    clonedBy: createdBy
                );

                // Use domain methods to set properties
                clonedTemplate.SetDoctorId(request.TargetDoctorId ?? createdBy, createdBy);
                clonedTemplate.SetDescription(request.NewDescription ?? sourceTemplate.Description, createdBy);
                clonedTemplate.SetCategory(request.NewCategory ?? sourceTemplate.Category, createdBy);

                if (request.IsPublic.HasValue)
                {
                    clonedTemplate.UpdateBasicInfo(
                        name: clonedTemplate.Name,
                        description: clonedTemplate.Description,
                        category: clonedTemplate.Category,
                        isPublic: request.IsPublic.Value,
                        updatedBy: createdBy
                    );
                }

                await _context.PrescriptionTemplates.AddAsync(clonedTemplate);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully cloned template: ID={TemplateId}, Name={TemplateName}",
                    clonedTemplate.Id, clonedTemplate.Name);

                return await MapToTemplateDto(clonedTemplate);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
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
                throw new DbUpdateException("Failed to clone template in database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while cloning the template.", ex);
            }
        }

        public async Task<TemplateStatisticsDto> GetTemplateStatisticsAsync(Guid doctorId)
        {
            const string operation = "GetTemplateStatistics";

            try
            {
                // Get all non-deleted templates for the doctor
                var templates = await _context.PrescriptionTemplates
                    .AsNoTracking()
                    .Include(t => t.Doctor)
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .Where(t => !t.IsDeleted && t.DoctorId == doctorId)
                    .ToListAsync();

                // Calculate statistics
                var statistics = new TemplateStatisticsDto
                {
                    TotalTemplates = templates.Count,
                    PublicTemplates = templates.Count(t => t.IsPublic),
                    PrivateTemplates = templates.Count(t => !t.IsPublic),

                    TemplatesWithMedicines = templates.Count(t => t.Medicines.Any()),
                    TemplatesWithComplaints = templates.Count(t => t.Complaints.Any()),
                    TemplatesWithAdvice = templates.Count(t => t.AdvisedItems.Any()),

                    // FIXED: Properly convert double to decimal with rounding
                    AverageMedicinesPerTemplate = templates.Any()
                        ? Math.Round((decimal)templates.Average(t => t.Medicines.Count), 2, MidpointRounding.AwayFromZero)
                        : 0,

                    AverageComplaintsPerTemplate = templates.Any()
                        ? Math.Round((decimal)templates.Average(t => t.Complaints.Count), 2, MidpointRounding.AwayFromZero)
                        : 0,

                    AverageAdvicePerTemplate = templates.Any()
                        ? Math.Round((decimal)templates.Average(t => t.AdvisedItems.Count), 2, MidpointRounding.AwayFromZero)
                        : 0
                };

                // Most used template
                var mostUsedTemplate = templates.OrderByDescending(t => t.UsageCount).FirstOrDefault();
                if (mostUsedTemplate != null)
                {
                    statistics.MostUsedTemplateId = mostUsedTemplate.Id.GetHashCode();
                    statistics.MostUsedTemplateName = mostUsedTemplate.Name;
                    statistics.MostUsedTemplateCount = mostUsedTemplate.UsageCount;
                }

                // Templates by category
                statistics.TemplatesByCategory = templates
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Templates by doctor (if multiple doctors in query)
                statistics.TemplatesByDoctor = templates
                    .GroupBy(t => t.Doctor?.Name ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                _logger.LogDebug("Generated statistics for {Count} templates", templates.Count);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error retrieving template statistics. Please try again.", ex);
            }
        }

        public async Task<int> IncrementUsageCountAsync(Guid templateId, Guid doctorId)
        {
            const string operation = "IncrementUsageCount";

            try
            {
                // Don't use AsNoTracking since we need to update
                var template = await _context.PrescriptionTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);

                if (template == null)
                    throw new NotFoundException($"Template with ID '{templateId}' not found.");

                // Check permission to use
                if (!template.CanBeUsedBy(doctorId))
                    throw new UnauthorizedAccessException("You don't have permission to use this template.");

                // Use domain method
                template.RecordUsage();

                _context.PrescriptionTemplates.Update(template);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Incremented usage count for template {TemplateId} to {Count}",
                    templateId, template.UsageCount);

                return template.UsageCount;
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation} for template: {TemplateId}", operation, templateId);
                throw new ApplicationException($"Error incrementing usage count for template '{templateId}'.", ex);
            }
        }

        public async Task<PrescriptionTemplateDto> UpdateTemplateItemsAsync(Guid templateId, UpdateTemplateItemsRequest request, Guid updatedBy)
        {
            const string operation = "UpdateTemplateItems";

            try
            {
                _logger.LogInformation("Starting {Operation} for template ID: {TemplateId}", operation, templateId);

                ValidateUpdateItemsRequest(request);

                var template = await _context.PrescriptionTemplates
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);

                if (template == null)
                    throw new NotFoundException($"Template with ID '{templateId}' not found.");

                // Check permission
                if (template.DoctorId != updatedBy)
                    throw new UnauthorizedAccessException("You don't have permission to update this template.");

                // Clear existing items
                template.Complaints.Clear();
                template.Medicines.Clear();
                template.AdvisedItems.Clear();

                // Add new complaints if provided
                if (request.Complaints != null)
                {
                    foreach (var complaintReq in request.Complaints)
                    {
                        template.AddComplaint(
                            complaintId: complaintReq.ComplaintId,
                            customText: complaintReq.CustomText?.Trim(),
                            duration: complaintReq.Duration?.Trim(),
                            severity: complaintReq.Severity?.Trim(),
                            addedBy: updatedBy
                        );
                    }
                }

                // Add new medicines if provided
                if (request.Medicines != null)
                {
                    foreach (var medicineReq in request.Medicines)
                    {
                        // Validate medicine exists
                        var medicine = await _context.Medicines
                            .FirstOrDefaultAsync(m => m.Id == medicineReq.MedicineId && !m.IsDeleted);

                        if (medicine == null)
                            throw new ValidationException($"Medicine with ID '{medicineReq.MedicineId}' not found.");

                        // Validate dose exists
                        var dose = await _context.Doses
                            .FirstOrDefaultAsync(d => d.Id == medicineReq.DoseId && !d.IsDeleted);

                        if (dose == null)
                            throw new ValidationException($"Dose with ID '{medicineReq.DoseId}' not found.");

                        template.AddMedicine(
                            medicineId: medicineReq.MedicineId,
                            doseId: medicineReq.DoseId,
                            durationDays: medicineReq.DurationDays,
                            quantity: medicineReq.Quantity,
                            instructions: medicineReq.Instructions?.Trim(),
                            addedBy: updatedBy
                        );
                    }
                }

                // Add new advised items if provided
                if (request.AdvisedItems != null)
                {
                    foreach (var advisedReq in request.AdvisedItems)
                    {
                        if (advisedReq.AdvisedId.HasValue)
                        {
                            var advised = await _context.AdvisedItems
                                .FirstOrDefaultAsync(a => a.Id == advisedReq.AdvisedId.Value && !a.IsDeleted);

                            if (advised == null)
                                throw new ValidationException($"Advised item with ID '{advisedReq.AdvisedId}' not found.");
                        }

                        template.AddAdvised(
                            advisedId: advisedReq.AdvisedId,
                            customText: advisedReq.CustomText?.Trim(),
                            addedBy: updatedBy
                        );
                    }
                }

                template.SetUpdatedBy(updatedBy);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated template items: ID={TemplateId}", templateId);
                return await MapToTemplateDto(template);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found during {Operation}", operation);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {Operation}", operation);
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
                throw new DbUpdateException("Failed to update template items in database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while updating template items.", ex);
            }
        }

        public async Task<List<PrescriptionTemplateDto>> GetTemplatesByCategoryAsync(string category, Guid doctorId)
        {
            const string operation = "GetTemplatesByCategory";

            try
            {
                var templates = await _context.PrescriptionTemplates
                    .AsNoTracking()
                    .Include(t => t.Doctor)
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .Where(t => !t.IsDeleted &&
                           (t.DoctorId == doctorId || t.IsPublic) &&
                           t.Category == category)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                var dtos = new List<PrescriptionTemplateDto>();
                foreach (var template in templates)
                {
                    dtos.Add(await MapToTemplateDto(template));
                }

                _logger.LogDebug("Retrieved {Count} templates for category: {Category}", templates.Count, category);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation} for category: {Category}", operation, category);
                throw new ApplicationException($"Error retrieving templates for category '{category}'.", ex);
            }
        }

        public async Task<List<PrescriptionTemplateDto>> GetPublicTemplatesAsync(Guid? doctorId = null)
        {
            const string operation = "GetPublicTemplates";

            try
            {
                var query = _context.PrescriptionTemplates
                    .AsNoTracking()
                    .Include(t => t.Doctor)
                    .Include(t => t.Complaints)
                    .Include(t => t.Medicines)
                    .Include(t => t.AdvisedItems)
                    .Where(t => !t.IsDeleted && t.IsPublic);

                if (doctorId.HasValue)
                {
                    query = query.Where(t => t.DoctorId == doctorId.Value);
                }

                var templates = await query
                    .OrderByDescending(t => t.UsageCount)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                var dtos = new List<PrescriptionTemplateDto>();
                foreach (var template in templates)
                {
                    dtos.Add(await MapToTemplateDto(template));
                }

                _logger.LogDebug("Retrieved {Count} public templates", templates.Count);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error retrieving public templates.", ex);
            }
        }

        #region Private Helper Methods

        private void ValidateCreateRequest(CreateTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Template name is required.");
        }

        private void ValidateUpdateRequest(UpdateTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Template name is required.");
        }

        private void ValidateCloneRequest(CloneTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
                throw new ValidationException("New template name is required for cloning.");
        }

        private void ValidateUpdateItemsRequest(UpdateTemplateItemsRequest request)
        {
            // Optional validation for each section if they are provided
            if (request.Medicines != null)
            {
                foreach (var medicine in request.Medicines)
                {
                    if (medicine.DurationDays <= 0)
                        throw new ValidationException("Duration days must be positive.");

                    if (medicine.Quantity <= 0)
                        throw new ValidationException("Quantity must be positive.");
                }
            }
        }

        private IQueryable<PrescriptionTemplate> ApplyFilters(IQueryable<PrescriptionTemplate> query, TemplateSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    (t.Category != null && t.Category.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(t => t.Category == request.Category);

            if (request.DoctorId.HasValue)
                query = query.Where(t => t.DoctorId == request.DoctorId.Value);

            if (request.IsPublic.HasValue)
                query = query.Where(t => t.IsPublic == request.IsPublic.Value);

            if (request.HasMedicines.HasValue)
            {
                if (request.HasMedicines.Value)
                    query = query.Where(t => t.Medicines.Any());
                else
                    query = query.Where(t => !t.Medicines.Any());
            }

            if (request.HasComplaints.HasValue)
            {
                if (request.HasComplaints.Value)
                    query = query.Where(t => t.Complaints.Any());
                else
                    query = query.Where(t => !t.Complaints.Any());
            }

            if (request.HasAdvice.HasValue)
            {
                if (request.HasAdvice.Value)
                    query = query.Where(t => t.AdvisedItems.Any());
                else
                    query = query.Where(t => !t.AdvisedItems.Any());
            }

            return query;
        }

        private IQueryable<PrescriptionTemplate> ApplySortingAndPagination(IQueryable<PrescriptionTemplate> query, TemplateSearchRequest request)
        {
            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(t => t.Name)
                    : query.OrderBy(t => t.Name),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt),

                "usagecount" => request.SortDescending
                    ? query.OrderByDescending(t => t.UsageCount)
                    : query.OrderBy(t => t.UsageCount),

                "lastused" => request.SortDescending
                    ? query.OrderByDescending(t => t.LastUsed)
                    : query.OrderBy(t => t.LastUsed),

                _ => query.OrderBy(t => t.Name) // Default sorting
            };

            // Apply pagination
            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private async Task<PrescriptionTemplateDto> MapToTemplateDto(PrescriptionTemplate template)
        {
            // Load Doctor navigation property if not loaded
            if (template.Doctor == null && template.DoctorId.HasValue)
            {
                await _context.Entry(template)
                    .Reference(t => t.Doctor)
                    .LoadAsync();
            }

            return new PrescriptionTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                DoctorId = template.DoctorId,
                DoctorName = template.Doctor?.Name,
                IsPublic = template.IsPublic,
                MedicineCount = template.Medicines?.Count ?? 0,
                ComplaintCount = template.Complaints?.Count ?? 0,
                AdviceCount = template.AdvisedItems?.Count ?? 0,
                UsageCount = template.UsageCount,
                LastUsed = template.LastUsed,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        private async Task<TemplateDetailDto> MapToTemplateDetailDto(PrescriptionTemplate template)
        {
            var baseDto = await MapToTemplateDto(template);
            var detailDto = new TemplateDetailDto
            {
                Id = baseDto.Id,
                Name = baseDto.Name,
                Description = baseDto.Description,
                Category = baseDto.Category,
                DoctorId = baseDto.DoctorId,
                DoctorName = baseDto.DoctorName,
                IsPublic = baseDto.IsPublic,
                MedicineCount = baseDto.MedicineCount,
                ComplaintCount = baseDto.ComplaintCount,
                AdviceCount = baseDto.AdviceCount,
                UsageCount = baseDto.UsageCount,
                LastUsed = baseDto.LastUsed,
                CreatedAt = baseDto.CreatedAt,
                UpdatedAt = baseDto.UpdatedAt,
                Complaints = new List<TemplateComplaintDto>(),
                Medicines = new List<TemplateMedicineDto>(),
                AdvisedItems = new List<TemplateAdvisedDto>()
            };

            // Map complaints
            foreach (var complaint in template.Complaints)
            {
                detailDto.Complaints.Add(new TemplateComplaintDto
                {
                    Id = complaint.Id,
                    ComplaintId = complaint.ComplaintId,
                    ComplaintName = complaint.Complaint?.Name,
                    CustomText = complaint.CustomText,
                    Duration = complaint.Duration,
                    Severity = complaint.Severity
                });
            }

            // Map medicines
            foreach (var medicine in template.Medicines)
            {
                detailDto.Medicines.Add(new TemplateMedicineDto
                {
                    Id = medicine.Id,
                    MedicineId = medicine.MedicineId,
                    MedicineName = medicine.Medicine?.Name ?? string.Empty,
                    GenericName = medicine.Medicine?.GenericName,
                    Strength = medicine.Medicine?.Strength,
                    DoseId = medicine.DoseId,
                    DoseCode = medicine.Dose?.Code ?? string.Empty,
                    DoseName = medicine.Dose?.Name ?? string.Empty,
                    DurationDays = medicine.DurationDays,
                    Quantity = medicine.Quantity,
                    Instructions = medicine.Instructions
                });
            }

            // Map advised items
            foreach (var advised in template.AdvisedItems)
            {
                detailDto.AdvisedItems.Add(new TemplateAdvisedDto
                {
                    Id = advised.Id,
                    AdvisedId = advised.AdvisedId,
                    AdvisedText = advised.Advised?.Text,
                    CustomText = advised.CustomText,
                    Category = advised.Advised?.Category
                });
            }

            return detailDto;
        }

        #endregion
    }
}