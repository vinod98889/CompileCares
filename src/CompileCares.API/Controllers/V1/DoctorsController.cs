// File: DoctorsController.cs
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Doctors.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace CompileCares.API.Controllers
{
    /// <summary>
    /// Controller for managing doctors
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Produces("application/json")]
    [Authorize]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(
            IDoctorService doctorService,
            ILogger<DoctorsController> logger)
        {
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new doctor
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DoctorDto>> CreateDoctor([FromBody] CreateDoctorRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var doctor = await _doctorService.CreateDoctorAsync(request, userId);
                return CreatedAtAction(nameof(GetDoctor), new { id = doctor.Id }, doctor);
            }
            catch (ValidationException ex)
            {
                return Problem(
                    title: "Validation Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating doctor");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing doctor
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDto>> UpdateDoctor(
            [FromRoute] Guid id,
            [FromBody] UpdateDoctorRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var doctor = await _doctorService.UpdateDoctorAsync(id, request, userId);
                return Ok(doctor);
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (ValidationException ex)
            {
                return Problem(
                    title: "Validation Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets a doctor by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDto>> GetDoctor([FromRoute] Guid id)
        {
            try
            {
                var doctor = await _doctorService.GetDoctorAsync(id);
                return Ok(doctor);
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Searches doctors with pagination and filtering
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResponse<DoctorSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<DoctorSummaryDto>>> SearchDoctors(
            [FromQuery] DoctorSearchRequest request)
        {
            try
            {
                var result = await _doctorService.SearchDoctorsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching doctors");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets all available doctors
        /// </summary>
        [HttpGet("available")]
        [ProducesResponseType(typeof(IEnumerable<DoctorSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DoctorSummaryDto>>> GetAvailableDoctors()
        {
            try
            {
                var doctors = await _doctorService.GetAvailableDoctorsAsync();
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available doctors");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets doctor statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(DoctorStatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<DoctorStatisticsDto>> GetStatistics()
        {
            try
            {
                var statistics = await _doctorService.GetDoctorStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor statistics");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Verifies a doctor
        /// </summary>
        [HttpPost("{id:guid}/verify")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyDoctor([FromRoute] Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _doctorService.VerifyDoctorAsync(id, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Sets doctor availability
        /// </summary>
        [HttpPut("{id:guid}/availability")]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetAvailability(
            [FromRoute] Guid id,
            [FromBody] SetAvailabilityRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _doctorService.SetDoctorAvailabilityAsync(id, request.IsAvailable, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting availability for doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Activates a doctor
        /// </summary>
        [HttpPost("{id:guid}/activate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateDoctor([FromRoute] Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _doctorService.ActivateDoctorAsync(id, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deactivates a doctor
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateDoctor([FromRoute] Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _doctorService.DeactivateDoctorAsync(id, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates doctor signature
        /// </summary>
        [HttpPut("{id:guid}/signature")]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSignature(
            [FromRoute] Guid id,
            [FromBody] UpdateSignatureRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _doctorService.UpdateDoctorSignatureAsync(id, request.SignaturePath, request.DigitalSignature, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return Problem(
                    title: "Not Found",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating signature for doctor {DoctorId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        #region DTOs for Additional Operations

        public class SetAvailabilityRequest
        {
            public bool IsAvailable { get; set; }
        }

        public class UpdateSignatureRequest
        {
            public string SignaturePath { get; set; } = string.Empty;
            public string? DigitalSignature { get; set; }
        }

        #endregion

        private Guid GetCurrentUserId()
        {
            // Implementation depends on your authentication setup
            // This is a placeholder - adjust based on your auth system
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Unable to determine current user ID");
        }
    }
}