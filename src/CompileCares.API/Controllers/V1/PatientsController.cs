using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Security.Claims;

namespace CompileCares.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            IPatientService patientService,
            ILogger<PatientsController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        // GET: api/v1/patients/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ResponseCache(Duration = 30)] // Cache for 30 seconds
        public async Task<IActionResult> GetPatient(Guid id)
        {
            var stopwatch = Stopwatch.StartNew();

            if (id == Guid.Empty)
                return BadRequest(ApiResponse.ErrorResponse("Invalid patient ID"));

            try
            {
                _logger.LogInformation("Getting patient with ID: {PatientId}", id);

                var patient = await _patientService.GetPatientAsync(id);

                _logger.LogInformation("Patient retrieved successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return Ok(ApiResponse<PatientDto>.SuccessResponse(
                    patient,
                    "Patient retrieved successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Patient not found with ID: {PatientId}", id);
                return NotFound(ApiResponse.ErrorResponse("Patient not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient with ID: {PatientId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving patient"));
            }
        }

        // POST: api/v1/patients
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreatePatient(
            [FromBody] CreatePatientRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(ModelState)
                {
                    Title = "Validation failed",
                    Detail = "One or more validation errors occurred"
                });
            }

            try
            {
                _logger.LogInformation("Creating patient: {PatientName} by User: {UserId}",
                    request.Name, userId);

                var patient = await _patientService.CreatePatientAsync(request, userId);

                _logger.LogInformation("Patient created successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return CreatedAtAction(
                    nameof(GetPatient),
                    new { id = patient.Id, version = "1.0" },
                    ApiResponse<PatientDto>.SuccessResponse(patient, "Patient created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient: {PatientName} by User: {UserId}",
                    request.Name, userId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while creating patient"));
            }
        }

        // PUT: api/v1/patients/{id}
        [HttpPut("{id}")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePatient(
            Guid id,
            [FromBody] UpdatePatientRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = GetCurrentUserId();

            if (id == Guid.Empty)
                return BadRequest(ApiResponse.ErrorResponse("Invalid patient ID"));

            if (!ModelState.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(ModelState));
            }

            try
            {
                _logger.LogInformation("Updating patient with ID: {PatientId} by User: {UserId}",
                    id, userId);

                var patient = await _patientService.UpdatePatientAsync(id, request, userId);

                _logger.LogInformation("Patient updated successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return Ok(ApiResponse<PatientDto>.SuccessResponse(
                    patient,
                    "Patient updated successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Patient not found with ID: {PatientId}", id);
                return NotFound(ApiResponse.ErrorResponse("Patient not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {PatientId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while updating patient"));
            }
        }

        // GET: api/v1/patients/search
        [HttpGet("search")]
        [AllowAnonymous]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<PatientSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchPatients([FromQuery] PatientSearchRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Searching patients with term: {SearchTerm}", request.SearchTerm);

                var result = await _patientService.SearchPatientsAsync(request);

                // Log both current page count and total records
                _logger.LogInformation(
                    "Patient search completed in {ElapsedMs}ms. " +
                    "Returned {CurrentCount} results (page {PageNumber}/{TotalPages}), " +
                    "Total records: {TotalRecords}",
                    stopwatch.ElapsedMilliseconds,
                    result.Data.Count(),           // Records in current page
                    result.PageNumber,
                    result.TotalPages,
                    result.TotalRecords            // Total records in database
                );

                return Ok(ApiResponse<PagedResponse<PatientSummaryDto>>.SuccessResponse(
                    result,
                    "Patients retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while searching patients"));
            }
        }

        // DELETE: api/v1/patients/{id}/deactivate
        [HttpDelete("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivatePatient(Guid id)
        {
            var userId = GetCurrentUserId();

            if (id == Guid.Empty)
                return BadRequest(ApiResponse.ErrorResponse("Invalid patient ID"));

            try
            {
                _logger.LogWarning("Patient deactivation requested for ID: {PatientId} by User: {UserId}",
                    id, userId);

                var success = await _patientService.DeactivatePatientAsync(id, userId);

                _logger.LogWarning("Patient deactivated successfully for ID: {PatientId} by User: {UserId}",
                    id, userId);

                return Ok(ApiResponse.SuccessResponse("Patient deactivated successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Patient not found with ID: {PatientId}", id);
                return NotFound(ApiResponse.ErrorResponse("Patient not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating patient with ID: {PatientId} by User: {UserId}",
                    id, userId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deactivating patient"));
            }
        }

        // GET: api/v1/patients/statistics
        [HttpGet("statistics")]
        [ResponseCache(Duration = 60)] // Cache for 1 minute
        [ProducesResponseType(typeof(ApiResponse<PatientStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPatientStatistics()
        {
            try
            {
                _logger.LogInformation("Getting patient statistics");

                var statistics = await _patientService.GetPatientStatisticsAsync();

                return Ok(ApiResponse<PatientStatisticsDto>.SuccessResponse(
                    statistics,
                    "Patient statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient statistics");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving statistics"));
            }
        }

        // GET: api/v1/patients/health
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "Patients API",
                timestamp = DateTime.UtcNow,
                version = "1.0"
            });
        }

        // Secure method to get current user ID
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId;
        }
    }
}