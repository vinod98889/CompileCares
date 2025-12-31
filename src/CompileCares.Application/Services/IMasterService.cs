using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Master.DTOs;
using CompileCares.Application.Features.Pharmacy.DTOs;
using CompileCares.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Services
{
    public interface IMasterService
    {
        #region Complaints
        Task<ComplaintDto> CreateComplaintAsync(CreateComplaintRequest request, Guid createdBy);
        Task<ComplaintDto> UpdateComplaintAsync(Guid id, CreateComplaintRequest request, Guid updatedBy);
        Task<ComplaintDto> GetComplaintAsync(Guid id);
        Task<PagedResponse<ComplaintDto>> SearchComplaintsAsync(MasterSearchRequest request);
        Task<bool> DeleteComplaintAsync(Guid id, Guid deletedBy);
        Task<ComplaintDto> ToggleComplaintStatusAsync(Guid id, bool isActive, Guid updatedBy);
        Task<ComplaintDto> ToggleComplaintCommonAsync(Guid id, bool isCommon, Guid updatedBy);
        #endregion

        #region Advised Items
        Task<AdvisedDto> CreateAdvisedAsync(CreateAdvisedRequest request, Guid createdBy);
        Task<AdvisedDto> UpdateAdvisedAsync(Guid id, CreateAdvisedRequest request, Guid updatedBy);
        Task<AdvisedDto> GetAdvisedAsync(Guid id);
        Task<PagedResponse<AdvisedDto>> SearchAdvisedAsync(MasterSearchRequest request);
        Task<bool> DeleteAdvisedAsync(Guid id, Guid deletedBy);
        Task<AdvisedDto> ToggleAdvisedStatusAsync(Guid id, bool isActive, Guid updatedBy);
        Task<AdvisedDto> ToggleAdvisedCommonAsync(Guid id, bool isCommon, Guid updatedBy);
        #endregion

        #region Doses
        Task<DoseDto> CreateDoseAsync(CreateDoseRequest request, Guid createdBy);
        Task<DoseDto> UpdateDoseAsync(Guid id, CreateDoseRequest request, Guid updatedBy);
        Task<DoseDto> GetDoseAsync(Guid id);
        Task<PagedResponse<DoseDto>> SearchDosesAsync(MasterSearchRequest request);
        Task<bool> DeleteDoseAsync(Guid id, Guid deletedBy);
        Task<DoseDto> ToggleDoseStatusAsync(Guid id, bool isActive, Guid updatedBy);
        Task UpdateDoseSortOrderAsync(Guid id, int sortOrder, Guid updatedBy);
        Task ReorderDosesAsync(List<Guid> doseIds, Guid updatedBy);
        #endregion

        #region OPD Items
        Task<OPDItemMasterDto> CreateOPDItemAsync(CreateOPDItemRequest request, Guid createdBy);
        Task<OPDItemMasterDto> UpdateOPDItemAsync(Guid id, CreateOPDItemRequest request, Guid updatedBy);
        Task<OPDItemMasterDto> GetOPDItemAsync(Guid id);
        Task<PagedResponse<OPDItemMasterDto>> SearchOPDItemsAsync(MasterSearchRequest request);
        Task<bool> DeleteOPDItemAsync(Guid id, Guid deletedBy);
        Task<OPDItemMasterDto> ToggleOPDItemStatusAsync(Guid id, bool isActive, Guid updatedBy);
        Task UpdateOPDItemStockAsync(Guid id, int quantity, StockAction action, Guid updatedBy);
        Task<OPDItemMasterDto> UpdateOPDItemPricingAsync(Guid id, decimal standardPrice,
            decimal? doctorCommission = null, bool? isCommissionPercentage = null, Guid? updatedBy = null);
        #endregion

        #region Statistics
        Task<MasterStatisticsDto> GetMasterStatisticsAsync();
        Task<Dictionary<string, int>> GetUsageStatisticsAsync(string type, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<ComplaintDto>> GetMostUsedComplaintsAsync(int count = 10);
        Task<List<AdvisedDto>> GetMostUsedAdvisedAsync(int count = 10);
        Task<List<DoseDto>> GetMostUsedDosesAsync(int count = 10);
        Task<List<OPDItemMasterDto>> GetMostUsedOPDItemsAsync(int count = 10);
        #endregion

        #region Bulk Operations
        Task<List<ComplaintDto>> BulkCreateComplaintsAsync(List<CreateComplaintRequest> requests, Guid createdBy);
        Task<List<AdvisedDto>> BulkCreateAdvisedAsync(List<CreateAdvisedRequest> requests, Guid createdBy);
        Task<List<DoseDto>> BulkCreateDosesAsync(List<CreateDoseRequest> requests, Guid createdBy);
        Task<List<OPDItemMasterDto>> BulkCreateOPDItemsAsync(List<CreateOPDItemRequest> requests, Guid createdBy);
        #endregion

        #region Categories
        Task<List<string>> GetComplaintCategoriesAsync();
        Task<List<string>> GetAdvisedCategoriesAsync();
        Task<List<string>> GetOPDItemTypesAsync();
        Task<List<string>> GetOPDItemCategoriesAsync();
        Task<List<string>> GetOPDItemSubCategoriesAsync(string category);
        #endregion
    }    
}
