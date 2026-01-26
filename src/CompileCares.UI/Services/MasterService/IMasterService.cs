// IMasterService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Master.DTOs;
using CompileCares.Shared.Enums;

namespace CompileCares.UI.Services.MasterService
{
    public interface IMasterService
    {
        #region Complaints Operations
        Task<ApiResponse<ComplaintDto>> CreateComplaintAsync(CreateComplaintRequest request);
        Task<ApiResponse<ComplaintDto>> UpdateComplaintAsync(Guid id, CreateComplaintRequest request);
        Task<ApiResponse<ComplaintDto>> GetComplaintAsync(Guid id);
        Task<ApiResponse<PagedResponse<ComplaintDto>>> SearchComplaintsAsync(MasterSearchRequest request);
        Task<ApiResponse<string>> DeleteComplaintAsync(Guid id);
        Task<ApiResponse<ComplaintDto>> ToggleComplaintStatusAsync(Guid id, bool isActive);
        Task<ApiResponse<ComplaintDto>> ToggleComplaintCommonAsync(Guid id, bool isCommon);
        #endregion

        #region Advised Items Operations
        Task<ApiResponse<AdvisedDto>> CreateAdvisedAsync(CreateAdvisedRequest request);
        Task<ApiResponse<AdvisedDto>> UpdateAdvisedAsync(Guid id, CreateAdvisedRequest request);
        Task<ApiResponse<AdvisedDto>> GetAdvisedAsync(Guid id);
        Task<ApiResponse<PagedResponse<AdvisedDto>>> SearchAdvisedAsync(MasterSearchRequest request);
        Task<ApiResponse<string>> DeleteAdvisedAsync(Guid id);
        Task<ApiResponse<AdvisedDto>> ToggleAdvisedStatusAsync(Guid id, bool isActive);
        Task<ApiResponse<AdvisedDto>> ToggleAdvisedCommonAsync(Guid id, bool isCommon);
        #endregion

        #region Doses Operations
        Task<ApiResponse<DoseDto>> CreateDoseAsync(CreateDoseRequest request);
        Task<ApiResponse<DoseDto>> UpdateDoseAsync(Guid id, CreateDoseRequest request);
        Task<ApiResponse<DoseDto>> GetDoseAsync(Guid id);
        Task<ApiResponse<PagedResponse<DoseDto>>> SearchDosesAsync(MasterSearchRequest request);
        Task<ApiResponse<string>> DeleteDoseAsync(Guid id);
        Task<ApiResponse<DoseDto>> ToggleDoseStatusAsync(Guid id, bool isActive);
        Task<ApiResponse<string>> UpdateDoseSortOrderAsync(Guid id, int sortOrder);
        Task<ApiResponse<string>> ReorderDosesAsync(List<Guid> doseIds);
        #endregion

        #region OPD Items Operations
        Task<ApiResponse<OPDItemMasterDto>> CreateOPDItemAsync(CreateOPDItemRequest request);
        Task<ApiResponse<OPDItemMasterDto>> UpdateOPDItemAsync(Guid id, CreateOPDItemRequest request);
        Task<ApiResponse<OPDItemMasterDto>> GetOPDItemAsync(Guid id);
        Task<ApiResponse<PagedResponse<OPDItemMasterDto>>> SearchOPDItemsAsync(MasterSearchRequest request);
        Task<ApiResponse<string>> DeleteOPDItemAsync(Guid id);
        Task<ApiResponse<OPDItemMasterDto>> ToggleOPDItemStatusAsync(Guid id, bool isActive);        
        // In IMasterService interface
        Task<ApiResponse<string>> UpdateOPDItemStockAsync(
            Guid id,
            int quantity,
            StockAction action,
            decimal? purchasePrice = null,
            DateTime? expiryDate = null,
            string? notes = null,
            string? referenceNumber = null);
        Task<ApiResponse<OPDItemMasterDto>> UpdateOPDItemPricingAsync(Guid id, decimal standardPrice, decimal doctorCommission, bool isCommissionPercentage);
        #endregion

        #region Statistics
        Task<ApiResponse<MasterStatisticsDto>> GetMasterStatisticsAsync();
        Task<ApiResponse<Dictionary<string, int>>> GetUsageStatisticsAsync(string type, DateTime? fromDate = null, DateTime? toDate = null);
        Task<ApiResponse<List<ComplaintDto>>> GetMostUsedComplaintsAsync(int count = 10);
        Task<ApiResponse<List<AdvisedDto>>> GetMostUsedAdvisedAsync(int count = 10);
        Task<ApiResponse<List<DoseDto>>> GetMostUsedDosesAsync(int count = 10);
        Task<ApiResponse<List<OPDItemMasterDto>>> GetMostUsedOPDItemsAsync(int count = 10);
        #endregion

        #region Bulk Operations
        Task<ApiResponse<List<ComplaintDto>>> BulkCreateComplaintsAsync(List<CreateComplaintRequest> requests);
        Task<ApiResponse<List<AdvisedDto>>> BulkCreateAdvisedAsync(List<CreateAdvisedRequest> requests);
        Task<ApiResponse<List<DoseDto>>> BulkCreateDosesAsync(List<CreateDoseRequest> requests);
        Task<ApiResponse<List<OPDItemMasterDto>>> BulkCreateOPDItemsAsync(List<CreateOPDItemRequest> requests);
        #endregion

        #region Categories
        Task<ApiResponse<List<string>>> GetComplaintCategoriesAsync();
        Task<ApiResponse<List<string>>> GetAdvisedCategoriesAsync();
        Task<ApiResponse<List<string>>> GetOPDItemTypesAsync();
        Task<ApiResponse<List<string>>> GetOPDItemCategoriesAsync();
        Task<ApiResponse<List<string>>> GetOPDItemSubCategoriesAsync(string category);
        #endregion
    }
}