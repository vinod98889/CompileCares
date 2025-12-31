// File: CompileCares.API/Controllers/MasterController.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Master.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CompileCares.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MasterController : ControllerBase
    {
        private readonly IMasterService _masterService;
        private readonly ILogger<MasterController> _logger;

        public MasterController(
            IMasterService masterService,
            ILogger<MasterController> logger)
        {
            _masterService = masterService ?? throw new ArgumentNullException(nameof(masterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }

        #region Complaints Endpoints

        [HttpPost("complaints")]
        public async Task<IActionResult> CreateComplaint([FromBody] CreateComplaintRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.CreateComplaintAsync(request, userId);
                return Ok(ApiResponse<ComplaintDto>.SuccessResponse(result, "Complaint created successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating complaint");
                return StatusCode(500, ApiResponse<ComplaintDto>.ErrorResponse("An error occurred while creating complaint"));
            }
        }

        [HttpPut("complaints/{id}")]
        public async Task<IActionResult> UpdateComplaint(Guid id, [FromBody] CreateComplaintRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.UpdateComplaintAsync(id, request, userId);
                return Ok(ApiResponse<ComplaintDto>.SuccessResponse(result, "Complaint updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating complaint {ComplaintId}", id);
                return StatusCode(500, ApiResponse<ComplaintDto>.ErrorResponse("An error occurred while updating complaint"));
            }
        }

        [HttpGet("complaints/{id}")]
        public async Task<IActionResult> GetComplaint(Guid id)
        {
            try
            {
                var result = await _masterService.GetComplaintAsync(id);
                return Ok(ApiResponse<ComplaintDto>.SuccessResponse(result));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting complaint {ComplaintId}", id);
                return StatusCode(500, ApiResponse<ComplaintDto>.ErrorResponse("An error occurred while retrieving complaint"));
            }
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> SearchComplaints([FromQuery] MasterSearchRequest request)
        {
            try
            {
                var result = await _masterService.SearchComplaintsAsync(request);
                return Ok(ApiResponse<PagedResponse<ComplaintDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching complaints");
                return StatusCode(500, ApiResponse<PagedResponse<ComplaintDto>>.ErrorResponse("An error occurred while searching complaints"));
            }
        }

        [HttpDelete("complaints/{id}")]
        public async Task<IActionResult> DeleteComplaint(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.DeleteComplaintAsync(id, userId);
                return Ok(ApiResponse.SuccessResponse("Complaint deleted successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting complaint {ComplaintId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting complaint"));
            }
        }

        [HttpPatch("complaints/{id}/toggle-status")]
        public async Task<IActionResult> ToggleComplaintStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.ToggleComplaintStatusAsync(id, isActive, userId);
                return Ok(ApiResponse<ComplaintDto>.SuccessResponse(result, $"Complaint {(isActive ? "activated" : "deactivated")} successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling complaint status {ComplaintId}", id);
                return StatusCode(500, ApiResponse<ComplaintDto>.ErrorResponse("An error occurred while toggling complaint status"));
            }
        }

        [HttpPatch("complaints/{id}/toggle-common")]
        public async Task<IActionResult> ToggleComplaintCommon(Guid id, [FromBody] bool isCommon)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.ToggleComplaintCommonAsync(id, isCommon, userId);
                return Ok(ApiResponse<ComplaintDto>.SuccessResponse(result, $"Complaint marked as {(isCommon ? "common" : "uncommon")}"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<ComplaintDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling complaint common status {ComplaintId}", id);
                return StatusCode(500, ApiResponse<ComplaintDto>.ErrorResponse("An error occurred while toggling complaint common status"));
            }
        }

        #endregion

        #region Advised Items Endpoints

        [HttpPost("advised")]
        public async Task<IActionResult> CreateAdvised([FromBody] CreateAdvisedRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.CreateAdvisedAsync(request, userId);
                return Ok(ApiResponse<AdvisedDto>.SuccessResponse(result, "Advised item created successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating advised item");
                return StatusCode(500, ApiResponse<AdvisedDto>.ErrorResponse("An error occurred while creating advised item"));
            }
        }

        [HttpPut("advised/{id}")]
        public async Task<IActionResult> UpdateAdvised(Guid id, [FromBody] CreateAdvisedRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.UpdateAdvisedAsync(id, request, userId);
                return Ok(ApiResponse<AdvisedDto>.SuccessResponse(result, "Advised item updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating advised item {AdvisedId}", id);
                return StatusCode(500, ApiResponse<AdvisedDto>.ErrorResponse("An error occurred while updating advised item"));
            }
        }

        [HttpGet("advised/{id}")]
        public async Task<IActionResult> GetAdvised(Guid id)
        {
            try
            {
                var result = await _masterService.GetAdvisedAsync(id);
                return Ok(ApiResponse<AdvisedDto>.SuccessResponse(result));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting advised item {AdvisedId}", id);
                return StatusCode(500, ApiResponse<AdvisedDto>.ErrorResponse("An error occurred while retrieving advised item"));
            }
        }

        [HttpGet("advised")]
        public async Task<IActionResult> SearchAdvised([FromQuery] MasterSearchRequest request)
        {
            try
            {
                var result = await _masterService.SearchAdvisedAsync(request);
                return Ok(ApiResponse<PagedResponse<AdvisedDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching advised items");
                return StatusCode(500, ApiResponse<PagedResponse<AdvisedDto>>.ErrorResponse("An error occurred while searching advised items"));
            }
        }

        [HttpDelete("advised/{id}")]
        public async Task<IActionResult> DeleteAdvised(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.DeleteAdvisedAsync(id, userId);
                return Ok(ApiResponse.SuccessResponse("Advised item deleted successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting advised item {AdvisedId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting advised item"));
            }
        }

        [HttpPatch("advised/{id}/toggle-status")]
        public async Task<IActionResult> ToggleAdvisedStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.ToggleAdvisedStatusAsync(id, isActive, userId);
                return Ok(ApiResponse<AdvisedDto>.SuccessResponse(result, $"Advised item {(isActive ? "activated" : "deactivated")} successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling advised status {AdvisedId}", id);
                return StatusCode(500, ApiResponse<AdvisedDto>.ErrorResponse("An error occurred while toggling advised status"));
            }
        }

        [HttpPatch("advised/{id}/toggle-common")]
        public async Task<IActionResult> ToggleAdvisedCommon(Guid id, [FromBody] bool isCommon)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.ToggleAdvisedCommonAsync(id, isCommon, userId);
                return Ok(ApiResponse<AdvisedDto>.SuccessResponse(result, $"Advised item marked as {(isCommon ? "common" : "uncommon")}"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<AdvisedDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling advised common status {AdvisedId}", id);
                return StatusCode(500, ApiResponse<AdvisedDto>.ErrorResponse("An error occurred while toggling advised common status"));
            }
        }

        #endregion

        #region Doses Endpoints

        [HttpPost("doses")]
        public async Task<IActionResult> CreateDose([FromBody] CreateDoseRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.CreateDoseAsync(request, userId);
                return Ok(ApiResponse<DoseDto>.SuccessResponse(result, "Dose created successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dose");
                return StatusCode(500, ApiResponse<DoseDto>.ErrorResponse("An error occurred while creating dose"));
            }
        }

        [HttpPut("doses/{id}")]
        public async Task<IActionResult> UpdateDose(Guid id, [FromBody] CreateDoseRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.UpdateDoseAsync(id, request, userId);
                return Ok(ApiResponse<DoseDto>.SuccessResponse(result, "Dose updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dose {DoseId}", id);
                return StatusCode(500, ApiResponse<DoseDto>.ErrorResponse("An error occurred while updating dose"));
            }
        }

        [HttpGet("doses/{id}")]
        public async Task<IActionResult> GetDose(Guid id)
        {
            try
            {
                var result = await _masterService.GetDoseAsync(id);
                return Ok(ApiResponse<DoseDto>.SuccessResponse(result));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dose {DoseId}", id);
                return StatusCode(500, ApiResponse<DoseDto>.ErrorResponse("An error occurred while retrieving dose"));
            }
        }

        [HttpGet("doses")]
        public async Task<IActionResult> SearchDoses([FromQuery] MasterSearchRequest request)
        {
            try
            {
                var result = await _masterService.SearchDosesAsync(request);
                return Ok(ApiResponse<PagedResponse<DoseDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching doses");
                return StatusCode(500, ApiResponse<PagedResponse<DoseDto>>.ErrorResponse("An error occurred while searching doses"));
            }
        }

        [HttpDelete("doses/{id}")]
        public async Task<IActionResult> DeleteDose(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.DeleteDoseAsync(id, userId);
                return Ok(ApiResponse.SuccessResponse("Dose deleted successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dose {DoseId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting dose"));
            }
        }

        [HttpPatch("doses/{id}/toggle-status")]
        public async Task<IActionResult> ToggleDoseStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.ToggleDoseStatusAsync(id, isActive, userId);
                return Ok(ApiResponse<DoseDto>.SuccessResponse(result, $"Dose {(isActive ? "activated" : "deactivated")} successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<DoseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling dose status {DoseId}", id);
                return StatusCode(500, ApiResponse<DoseDto>.ErrorResponse("An error occurred while toggling dose status"));
            }
        }

        [HttpPatch("doses/{id}/sort-order")]
        public async Task<IActionResult> UpdateDoseSortOrder(Guid id, [FromBody] int sortOrder)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.UpdateDoseSortOrderAsync(id, sortOrder, userId);
                return Ok(ApiResponse.SuccessResponse("Dose sort order updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dose sort order {DoseId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while updating dose sort order"));
            }
        }

        [HttpPost("doses/reorder")]
        public async Task<IActionResult> ReorderDoses([FromBody] List<Guid> doseIds)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.ReorderDosesAsync(doseIds, userId);
                return Ok(ApiResponse.SuccessResponse("Doses reordered successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering doses");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while reordering doses"));
            }
        }

        #endregion

        #region OPD Items Endpoints

        [HttpPost("opd-items")]
        public async Task<IActionResult> CreateOPDItem([FromBody] CreateOPDItemRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.CreateOPDItemAsync(request, userId);
                return Ok(ApiResponse<OPDItemMasterDto>.SuccessResponse(result, "OPD item created successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OPD item");
                return StatusCode(500, ApiResponse<OPDItemMasterDto>.ErrorResponse("An error occurred while creating OPD item"));
            }
        }

        [HttpPut("opd-items/{id}")]
        public async Task<IActionResult> UpdateOPDItem(Guid id, [FromBody] CreateOPDItemRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.UpdateOPDItemAsync(id, request, userId);
                return Ok(ApiResponse<OPDItemMasterDto>.SuccessResponse(result, "OPD item updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OPD item {ItemId}", id);
                return StatusCode(500, ApiResponse<OPDItemMasterDto>.ErrorResponse("An error occurred while updating OPD item"));
            }
        }

        [HttpGet("opd-items/{id}")]
        public async Task<IActionResult> GetOPDItem(Guid id)
        {
            try
            {
                var result = await _masterService.GetOPDItemAsync(id);
                return Ok(ApiResponse<OPDItemMasterDto>.SuccessResponse(result));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item {ItemId}", id);
                return StatusCode(500, ApiResponse<OPDItemMasterDto>.ErrorResponse("An error occurred while retrieving OPD item"));
            }
        }

        [HttpGet("opd-items")]
        public async Task<IActionResult> SearchOPDItems([FromQuery] MasterSearchRequest request)
        {
            try
            {
                var result = await _masterService.SearchOPDItemsAsync(request);
                return Ok(ApiResponse<PagedResponse<OPDItemMasterDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OPD items");
                return StatusCode(500, ApiResponse<PagedResponse<OPDItemMasterDto>>.ErrorResponse("An error occurred while searching OPD items"));
            }
        }

        [HttpDelete("opd-items/{id}")]
        public async Task<IActionResult> DeleteOPDItem(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.DeleteOPDItemAsync(id, userId);
                return Ok(ApiResponse.SuccessResponse("OPD item deleted successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting OPD item {ItemId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting OPD item"));
            }
        }

        [HttpPatch("opd-items/{id}/toggle-status")]
        public async Task<IActionResult> ToggleOPDItemStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.ToggleOPDItemStatusAsync(id, isActive, userId);
                return Ok(ApiResponse<OPDItemMasterDto>.SuccessResponse(result, $"OPD item {(isActive ? "activated" : "deactivated")} successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling OPD item status {ItemId}", id);
                return StatusCode(500, ApiResponse<OPDItemMasterDto>.ErrorResponse("An error occurred while toggling OPD item status"));
            }
        }

        [HttpPatch("opd-items/{id}/stock")]
        public async Task<IActionResult> UpdateOPDItemStock(Guid id, [FromBody] UpdateStockRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _masterService.UpdateOPDItemStockAsync(id, request.Quantity, request.Action, userId);
                return Ok(ApiResponse.SuccessResponse("OPD item stock updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OPD item stock {ItemId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while updating OPD item stock"));
            }
        }

        [HttpPatch("opd-items/{id}/pricing")]
        public async Task<IActionResult> UpdateOPDItemPricing(Guid id, [FromBody] UpdatePricingRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.UpdateOPDItemPricingAsync(
                    id,
                    request.StandardPrice,
                    request.DoctorCommission,
                    request.IsCommissionPercentage,
                    userId);

                return Ok(ApiResponse<OPDItemMasterDto>.SuccessResponse(result, "OPD item pricing updated successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<OPDItemMasterDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OPD item pricing {ItemId}", id);
                return StatusCode(500, ApiResponse<OPDItemMasterDto>.ErrorResponse("An error occurred while updating OPD item pricing"));
            }
        }

        #endregion

        #region Statistics Endpoints

        [HttpGet("statistics")]
        public async Task<IActionResult> GetMasterStatistics()
        {
            try
            {
                var result = await _masterService.GetMasterStatisticsAsync();
                return Ok(ApiResponse<MasterStatisticsDto>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting master statistics");
                return StatusCode(500, ApiResponse<MasterStatisticsDto>.ErrorResponse("An error occurred while retrieving statistics"));
            }
        }

        [HttpGet("usage-statistics/{type}")]
        public async Task<IActionResult> GetUsageStatistics(string type, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await _masterService.GetUsageStatisticsAsync(type, fromDate, toDate);
                return Ok(ApiResponse<Dictionary<string, int>>.SuccessResponse(result));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<Dictionary<string, int>>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage statistics for type: {Type}", type);
                return StatusCode(500, ApiResponse<Dictionary<string, int>>.ErrorResponse("An error occurred while retrieving usage statistics"));
            }
        }

        [HttpGet("most-used/complaints")]
        public async Task<IActionResult> GetMostUsedComplaints([FromQuery] int count = 10)
        {
            try
            {
                var result = await _masterService.GetMostUsedComplaintsAsync(count);
                return Ok(ApiResponse<List<ComplaintDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used complaints");
                return StatusCode(500, ApiResponse<List<ComplaintDto>>.ErrorResponse("An error occurred while retrieving most used complaints"));
            }
        }

        [HttpGet("most-used/advised")]
        public async Task<IActionResult> GetMostUsedAdvised([FromQuery] int count = 10)
        {
            try
            {
                var result = await _masterService.GetMostUsedAdvisedAsync(count);
                return Ok(ApiResponse<List<AdvisedDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used advised items");
                return StatusCode(500, ApiResponse<List<AdvisedDto>>.ErrorResponse("An error occurred while retrieving most used advised items"));
            }
        }

        [HttpGet("most-used/doses")]
        public async Task<IActionResult> GetMostUsedDoses([FromQuery] int count = 10)
        {
            try
            {
                var result = await _masterService.GetMostUsedDosesAsync(count);
                return Ok(ApiResponse<List<DoseDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used doses");
                return StatusCode(500, ApiResponse<List<DoseDto>>.ErrorResponse("An error occurred while retrieving most used doses"));
            }
        }

        [HttpGet("most-used/opd-items")]
        public async Task<IActionResult> GetMostUsedOPDItems([FromQuery] int count = 10)
        {
            try
            {
                var result = await _masterService.GetMostUsedOPDItemsAsync(count);
                return Ok(ApiResponse<List<OPDItemMasterDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most used OPD items");
                return StatusCode(500, ApiResponse<List<OPDItemMasterDto>>.ErrorResponse("An error occurred while retrieving most used OPD items"));
            }
        }

        #endregion

        #region Bulk Operations Endpoints

        [HttpPost("bulk/complaints")]
        public async Task<IActionResult> BulkCreateComplaints([FromBody] List<CreateComplaintRequest> requests)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.BulkCreateComplaintsAsync(requests, userId);
                return Ok(ApiResponse<List<ComplaintDto>>.SuccessResponse(result, $"{result.Count} complaints created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<List<ComplaintDto>>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating complaints");
                return StatusCode(500, ApiResponse<List<ComplaintDto>>.ErrorResponse("An error occurred while bulk creating complaints"));
            }
        }

        [HttpPost("bulk/advised")]
        public async Task<IActionResult> BulkCreateAdvised([FromBody] List<CreateAdvisedRequest> requests)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.BulkCreateAdvisedAsync(requests, userId);
                return Ok(ApiResponse<List<AdvisedDto>>.SuccessResponse(result, $"{result.Count} advised items created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<List<AdvisedDto>>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating advised items");
                return StatusCode(500, ApiResponse<List<AdvisedDto>>.ErrorResponse("An error occurred while bulk creating advised items"));
            }
        }

        [HttpPost("bulk/doses")]
        public async Task<IActionResult> BulkCreateDoses([FromBody] List<CreateDoseRequest> requests)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.BulkCreateDosesAsync(requests, userId);
                return Ok(ApiResponse<List<DoseDto>>.SuccessResponse(result, $"{result.Count} doses created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<List<DoseDto>>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating doses");
                return StatusCode(500, ApiResponse<List<DoseDto>>.ErrorResponse("An error occurred while bulk creating doses"));
            }
        }

        [HttpPost("bulk/opd-items")]
        public async Task<IActionResult> BulkCreateOPDItems([FromBody] List<CreateOPDItemRequest> requests)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _masterService.BulkCreateOPDItemsAsync(requests, userId);
                return Ok(ApiResponse<List<OPDItemMasterDto>>.SuccessResponse(result, $"{result.Count} OPD items created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<List<OPDItemMasterDto>>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating OPD items");
                return StatusCode(500, ApiResponse<List<OPDItemMasterDto>>.ErrorResponse("An error occurred while bulk creating OPD items"));
            }
        }

        #endregion

        #region Categories Endpoints

        [HttpGet("categories/complaints")]
        public async Task<IActionResult> GetComplaintCategories()
        {
            try
            {
                var result = await _masterService.GetComplaintCategoriesAsync();
                return Ok(ApiResponse<List<string>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting complaint categories");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving complaint categories"));
            }
        }

        [HttpGet("categories/advised")]
        public async Task<IActionResult> GetAdvisedCategories()
        {
            try
            {
                var result = await _masterService.GetAdvisedCategoriesAsync();
                return Ok(ApiResponse<List<string>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting advised categories");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving advised categories"));
            }
        }

        [HttpGet("categories/opd-items/types")]
        public async Task<IActionResult> GetOPDItemTypes()
        {
            try
            {
                var result = await _masterService.GetOPDItemTypesAsync();
                return Ok(ApiResponse<List<string>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item types");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving OPD item types"));
            }
        }

        [HttpGet("categories/opd-items")]
        public async Task<IActionResult> GetOPDItemCategories()
        {
            try
            {
                var result = await _masterService.GetOPDItemCategoriesAsync();
                return Ok(ApiResponse<List<string>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item categories");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving OPD item categories"));
            }
        }

        [HttpGet("categories/opd-items/{category}/subcategories")]
        public async Task<IActionResult> GetOPDItemSubCategories(string category)
        {
            try
            {
                var result = await _masterService.GetOPDItemSubCategoriesAsync(category);
                return Ok(ApiResponse<List<string>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OPD item subcategories for category: {Category}", category);
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving OPD item subcategories"));
            }
        }
        #endregion
    }  
}