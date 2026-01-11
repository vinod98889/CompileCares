using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Templates.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CompileCares.Api.Controllers
{
    [ApiController]
    [Route("api/v1/templates")]
    [Authorize] // Require authentication for all endpoints
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplateService _templateService;
        private readonly ILogger<TemplatesController> _logger;

        public TemplatesController(
            ITemplateService templateService,
            ILogger<TemplatesController> logger)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                            ?? User.FindFirst("UserId")?.Value
                            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // This matches your claim

            if (string.IsNullOrEmpty(userIdClaim))
            {
                // For debugging, log all available claims
                var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                _logger.LogError("User ID not found in token. Available claims: {@Claims}", allClaims);
                throw new UnauthorizedAccessException("User ID not found in token.");
            }

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            throw new UnauthorizedAccessException($"Invalid User ID format in token: {userIdClaim}");
        }

        private Guid? GetCurrentDoctorId()
        {
            // Check if user is admin first
            var isAdmin = IsAdmin();
            if (isAdmin)
                return null; // Admin doesn't need a DoctorId

            var doctorIdClaim = User.FindFirst("DoctorId")?.Value;
            if (Guid.TryParse(doctorIdClaim, out var doctorId))
                return doctorId;

            return null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        }

        /// <summary>
        /// Create a new prescription template
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PrescriptionTemplateDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
        {
            try
            {
                Guid? doctorId = null;
                var isAdmin = IsAdmin();

                // First, try to get DoctorId from the request (for Admins)
                if (request.DoctorId.HasValue)
                {
                    // Admins can assign templates to any doctor
                    if (isAdmin)
                    {
                        doctorId = request.DoctorId.Value;
                    }
                    else
                    {
                        // Regular doctors can only create templates for themselves
                        var currentDoctorId = GetCurrentDoctorId();
                        if (currentDoctorId.HasValue && request.DoctorId.Value != currentDoctorId.Value)
                        {
                            return Forbid("You can only create templates for yourself.");
                        }
                        doctorId = request.DoctorId.Value;
                    }
                }
                else
                {
                    // If no DoctorId in request
                    if (isAdmin)
                    {
                        // Admin must specify a DoctorId when creating templates
                        return BadRequest("DoctorId is required when creating templates as an admin.");
                    }

                    // Get DoctorId from claims (for regular doctors)
                    doctorId = GetCurrentDoctorId();
                }

                if (!doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to create templates.");

                var result = await _templateService.CreateTemplateAsync(request, doctorId.Value);
                return CreatedAtAction(nameof(GetTemplate), new { id = result.Id }, result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the template.");
            }
        }

        /// <summary>
        /// Get template by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PrescriptionTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTemplate(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to access templates.");

                var result = await _templateService.GetTemplateAsync(id, doctorId, isAdmin);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the template.");
            }
        }

        /// <summary>
        /// Get template details with all items
        /// </summary>
        [HttpGet("{id}/details")]
        [ProducesResponseType(typeof(TemplateDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTemplateDetails(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to access templates.");

                var result = await _templateService.GetTemplateDetailsAsync(id, doctorId, isAdmin);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template details {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving template details.");
            }
        }

        /// <summary>
        /// Update template metadata
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PrescriptionTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to update templates.");

                var updatedBy = doctorId ?? GetCurrentUserId();
                var result = await _templateService.UpdateTemplateAsync(id, request, updatedBy, isAdmin);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the template.");
            }
        }

        /// <summary>
        /// Delete template
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to delete templates.");

                var success = await _templateService.DeleteTemplateAsync(id, doctorId, isAdmin);
                if (success)
                    return Ok(ApiResponse.SuccessResponse("Template deleted successfully"));

                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete template.");
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the template.");
            }
        }

        /// <summary>
        /// Clone template
        /// </summary>
        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(PrescriptionTemplateDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CloneTemplate(Guid id, [FromBody] CloneTemplateRequest request)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to clone templates.");

                var createdBy = doctorId ?? GetCurrentUserId();
                var result = await _templateService.CloneTemplateAsync(id, request, createdBy, isAdmin);
                return CreatedAtAction(nameof(GetTemplate), new { id = result.Id }, result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning template {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cloning the template.");
            }
        }

        /// <summary>
        /// Search templates
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResponse<PrescriptionTemplateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SearchTemplates([FromQuery] TemplateSearchRequest request)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to search templates.");

                var result = await _templateService.SearchTemplatesAsync(request, doctorId, isAdmin);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching templates");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching templates.");
            }
        }

        /// <summary>
        /// Get templates by category
        /// </summary>
        [HttpGet("category/{category}")]
        [ProducesResponseType(typeof(List<PrescriptionTemplateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTemplatesByCategory(string category)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to access templates.");

                var result = await _templateService.GetTemplatesByCategoryAsync(category, doctorId, isAdmin);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates for category {Category}", category);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving templates.");
            }
        }

        /// <summary>
        /// Get public templates
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous] // This endpoint remains public
        [ProducesResponseType(typeof(List<PrescriptionTemplateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicTemplates([FromQuery] Guid? doctorId = null)
        {
            try
            {
                var result = await _templateService.GetPublicTemplatesAsync(doctorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public templates");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving public templates.");
            }
        }

        /// <summary>
        /// Get template statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(TemplateStatisticsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTemplateStatistics()
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to access statistics.");

                var result = await _templateService.GetTemplateStatisticsAsync(doctorId, isAdmin);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template statistics");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving template statistics.");
            }
        }

        /// <summary>
        /// Increment template usage count
        /// </summary>
        [HttpPost("{id}/usage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> IncrementUsage(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to use templates.");

                var result = await _templateService.IncrementUsageCountAsync(id, doctorId, isAdmin);
                return Ok(new { UsageCount = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing usage for template {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while incrementing usage count.");
            }
        }

        /// <summary>
        /// Update template items (complaints, medicines, advised items)
        /// </summary>
        [HttpPut("{id}/items")]
        [ProducesResponseType(typeof(PrescriptionTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateTemplateItems(Guid id, [FromBody] UpdateTemplateItemsRequest request)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to update templates.");

                var updatedBy = doctorId ?? GetCurrentUserId();
                var result = await _templateService.UpdateTemplateItemsAsync(id, request, updatedBy, isAdmin);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template items {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating template items.");
            }
        }

        /// <summary>
        /// Get template for prescription generation
        /// </summary>
        [HttpGet("{id}/prescription")]
        [ProducesResponseType(typeof(TemplateDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTemplateForPrescription(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                var isAdmin = IsAdmin();

                // For non-admin users, doctorId must be present
                if (!isAdmin && !doctorId.HasValue)
                    return Unauthorized("Doctor ID is required to use templates.");

                // Increment usage count
                await _templateService.IncrementUsageCountAsync(id, doctorId, isAdmin);

                // Get template details
                var result = await _templateService.GetTemplateDetailsAsync(id, doctorId, isAdmin);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template for prescription {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while loading template for prescription.");
            }
        }
    }
}