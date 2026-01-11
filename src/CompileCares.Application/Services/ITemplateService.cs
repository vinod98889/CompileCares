using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Templates.DTOs;

namespace CompileCares.Application.Services
{
    public interface ITemplateService
    {
        Task<PrescriptionTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, Guid createdBy);

        // Updated methods with admin support
        Task<PrescriptionTemplateDto> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request, Guid updatedBy, bool isAdmin = false);
        Task<PrescriptionTemplateDto> GetTemplateAsync(Guid id, Guid? doctorId, bool isAdmin = false);
        Task<TemplateDetailDto> GetTemplateDetailsAsync(Guid id, Guid? doctorId, bool isAdmin = false);
        Task<PagedResponse<PrescriptionTemplateDto>> SearchTemplatesAsync(TemplateSearchRequest request, Guid? doctorId, bool isAdmin = false);
        Task<bool> DeleteTemplateAsync(Guid id, Guid? deletedBy, bool isAdmin = false);
        Task<PrescriptionTemplateDto> CloneTemplateAsync(Guid sourceTemplateId, CloneTemplateRequest request, Guid createdBy, bool isAdmin = false);
        Task<TemplateStatisticsDto> GetTemplateStatisticsAsync(Guid? doctorId, bool isAdmin = false);
        Task<int> IncrementUsageCountAsync(Guid templateId, Guid? doctorId, bool isAdmin = false);
        Task<PrescriptionTemplateDto> UpdateTemplateItemsAsync(Guid templateId, UpdateTemplateItemsRequest request, Guid updatedBy, bool isAdmin = false);
        Task<List<PrescriptionTemplateDto>> GetTemplatesByCategoryAsync(string category, Guid? doctorId, bool isAdmin = false);

        // Keep existing signatures for backward compatibility
        Task<PrescriptionTemplateDto> GetTemplateAsync(Guid id, Guid doctorId);
        Task<TemplateDetailDto> GetTemplateDetailsAsync(Guid id, Guid doctorId);
        Task<PagedResponse<PrescriptionTemplateDto>> SearchTemplatesAsync(TemplateSearchRequest request, Guid doctorId);
        Task<bool> DeleteTemplateAsync(Guid id, Guid deletedBy);
        Task<PrescriptionTemplateDto> CloneTemplateAsync(Guid sourceTemplateId, CloneTemplateRequest request, Guid createdBy);
        Task<TemplateStatisticsDto> GetTemplateStatisticsAsync(Guid doctorId);
        Task<int> IncrementUsageCountAsync(Guid templateId, Guid doctorId);
        Task<PrescriptionTemplateDto> UpdateTemplateItemsAsync(Guid templateId, UpdateTemplateItemsRequest request, Guid updatedBy);
        Task<List<PrescriptionTemplateDto>> GetTemplatesByCategoryAsync(string category, Guid doctorId);

        // Public templates remain unchanged
        Task<List<PrescriptionTemplateDto>> GetPublicTemplatesAsync(Guid? doctorId = null);
    }
}