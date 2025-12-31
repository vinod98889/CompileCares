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
        Task<PrescriptionTemplateDto> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request, Guid updatedBy);
        Task<PrescriptionTemplateDto> GetTemplateAsync(Guid id, Guid doctorId);
        Task<TemplateDetailDto> GetTemplateDetailsAsync(Guid id, Guid doctorId);
        Task<PagedResponse<PrescriptionTemplateDto>> SearchTemplatesAsync(TemplateSearchRequest request, Guid doctorId);
        Task<bool> DeleteTemplateAsync(Guid id, Guid deletedBy);
        Task<PrescriptionTemplateDto> CloneTemplateAsync(Guid sourceTemplateId, CloneTemplateRequest request, Guid createdBy);
        Task<TemplateStatisticsDto> GetTemplateStatisticsAsync(Guid doctorId);
        Task<int> IncrementUsageCountAsync(Guid templateId, Guid doctorId);
        Task<PrescriptionTemplateDto> UpdateTemplateItemsAsync(Guid templateId, UpdateTemplateItemsRequest request, Guid updatedBy);
        Task<List<PrescriptionTemplateDto>> GetTemplatesByCategoryAsync(string category, Guid doctorId);
        Task<List<PrescriptionTemplateDto>> GetPublicTemplatesAsync(Guid? doctorId = null);
    }    
}