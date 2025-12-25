using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;

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

        // GET: api/patients/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatient(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting patient with ID: {PatientId}", id);

                var patient = await _patientService.GetPatientAsync(id);

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

        // POST: api/patients
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePatient(
            [FromBody] CreatePatientRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse.ErrorResponse("Validation failed", errors));
                }

                // Use provided userId or get from authentication context
                var createdBy = userId ?? GetCurrentUserId();

                if (createdBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Creating patient: {PatientName}", request.Name);

                var patient = await _patientService.CreatePatientAsync(request, createdBy);

                return CreatedAtAction(
                    nameof(GetPatient),
                    new { id = patient.Id },
                    ApiResponse<PatientDto>.SuccessResponse(patient, "Patient created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient: {PatientName}", request.Name);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while creating patient"));
            }
        }

        // PUT: api/patients/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePatient(
            Guid id,
            [FromBody] UpdatePatientRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse.ErrorResponse("Validation failed", errors));
                }

                var updatedBy = userId ?? GetCurrentUserId();

                if (updatedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Updating patient with ID: {PatientId}", id);

                var patient = await _patientService.UpdatePatientAsync(id, request, updatedBy);

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

        // GET: api/patients/search
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<PatientSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchPatients([FromQuery] PatientSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Searching patients with term: {SearchTerm}", request.SearchTerm);

                var result = await _patientService.SearchPatientsAsync(request);

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

        // DELETE: api/patients/{id}/deactivate
        [HttpDelete("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivatePatient(
            Guid id,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                var deactivatedBy = userId ?? GetCurrentUserId();

                if (deactivatedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Deactivating patient with ID: {PatientId}", id);

                var success = await _patientService.DeactivatePatientAsync(id, deactivatedBy);

                return Ok(ApiResponse.SuccessResponse("Patient deactivated successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Patient not found with ID: {PatientId}", id);
                return NotFound(ApiResponse.ErrorResponse("Patient not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating patient with ID: {PatientId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deactivating patient"));
            }
        }

        // GET: api/patients/statistics
        [HttpGet("statistics")]
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

        // Helper method to get current user ID (placeholder - implement authentication later)
        private Guid GetCurrentUserId()
        {
            // TODO: Replace with actual authentication
            // For now, return a dummy user ID or get from JWT token

            // Check if we have a user ID in the header (for testing)
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var userId))
            {
                return userId;
            }

            // Return a default user ID for development
            // In production, this should throw if no user is authenticated
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
    }
}