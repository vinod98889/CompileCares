// File: PharmacyController.cs
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Pharmacy.DTOs;
using CompileCares.Application.Features.Pharmacy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CompileCares.API.Controllers
{
    [ApiController]
    [Route("api/v1/pharmacy/medicines")]
    [Produces("application/json")]
    [Authorize]
    public class PharmacyController : ControllerBase
    {
        private readonly IPharmacyService _pharmacyService;
        private readonly ILogger<PharmacyController> _logger;

        public PharmacyController(
            IPharmacyService pharmacyService,
            ILogger<PharmacyController> logger)
        {
            _pharmacyService = pharmacyService ?? throw new ArgumentNullException(nameof(pharmacyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pharmacist")]
        [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MedicineDto>> CreateMedicine([FromBody] CreateMedicineRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var medicine = await _pharmacyService.CreateMedicineAsync(request, userId);
                return CreatedAtAction(nameof(GetMedicine), new { id = medicine.Id }, medicine);
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
                _logger.LogError(ex, "Error creating medicine");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Pharmacist")]
        [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicineDto>> UpdateMedicine(
            [FromRoute] Guid id,
            [FromBody] UpdateMedicineRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var medicine = await _pharmacyService.UpdateMedicineAsync(id, request, userId);
                return Ok(medicine);
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
                _logger.LogError(ex, "Error updating medicine {MedicineId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicineDto>> GetMedicine([FromRoute] Guid id)
        {
            try
            {
                var medicine = await _pharmacyService.GetMedicineAsync(id);
                return Ok(medicine);
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
                _logger.LogError(ex, "Error getting medicine {MedicineId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResponse<MedicineDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<MedicineDto>>> SearchMedicines(
            [FromQuery] MedicineSearchRequest request)
        {
            try
            {
                var result = await _pharmacyService.SearchMedicinesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching medicines");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,Pharmacist")]
        [ProducesResponseType(typeof(MedicineStatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<MedicineStatisticsDto>> GetStatistics()
        {
            try
            {
                var statistics = await _pharmacyService.GetMedicineStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting medicine statistics");
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{id:guid}/stock")]
        [Authorize(Roles = "Admin,Pharmacist")]
        [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicineDto>> UpdateStock(
            [FromRoute] Guid id,
            [FromBody] StockUpdateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var medicine = await _pharmacyService.UpdateStockAsync(id, request, userId);
                return Ok(medicine);
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
                _logger.LogError(ex, "Error updating stock for medicine {MedicineId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id:guid}/availability")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> CheckAvailability(
            [FromRoute] Guid id,
            [FromQuery] int quantity = 1)
        {
            try
            {
                var isAvailable = await _pharmacyService.CheckAvailabilityAsync(id, quantity);
                return Ok(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for medicine {MedicineId}", id);
                return Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Unable to determine current user ID");
        }
    }
}