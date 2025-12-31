using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Visits.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CompileCares.API.Controllers.V1
{    
    [ApiController]
    [Route("api/v1/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;
        private readonly ILogger<VisitsController> _logger;

        public VisitsController(
            IVisitService visitService,
            ILogger<VisitsController> logger)
        {
            _visitService = visitService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new OPD visit
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateVisit([FromBody] CreateVisitRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Invalid request data"));
                }

                var createdBy = GetCurrentUserId();
                _logger.LogInformation("Creating new visit for patient {PatientId}", request.PatientId);

                var visit = await _visitService.CreateVisitAsync(request, createdBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(visit.Id);

                return CreatedAtAction(nameof(GetVisit), new { id = visit.Id },
                    ApiResponse<OPDVisitDto>.SuccessResponse(
                        visitDetail,
                        "Visit created successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found while creating visit");
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating visit");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating visit for patient {PatientId}", request.PatientId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while creating visit"));
            }
        }

        /// <summary>
        /// Get visit by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetVisit(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting visit with ID: {VisitId}", id);

                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Visit retrieved successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting visit with ID: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving visit"));
            }
        }

        /// <summary>
        /// Update visit details
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitRequest request)
        {
            try
            {
                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Updating visit: {VisitId}", id);

                var visit = await _visitService.UpdateVisitAsync(id, request, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Visit updated successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating visit: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while updating visit"));
            }
        }

        /// <summary>
        /// Start consultation for a visit
        /// </summary>
        [HttpPost("{id}/start-consultation")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> StartConsultation(Guid id)
        {
            try
            {
                var startedBy = GetCurrentUserId();
                _logger.LogInformation("Starting consultation for visit: {VisitId}", id);

                var visit = await _visitService.StartConsultationAsync(id, startedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Consultation started successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot start consultation for visit: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting consultation for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while starting consultation"));
            }
        }

        /// <summary>
        /// Complete consultation for a visit
        /// </summary>
        [HttpPost("{id}/complete-consultation")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CompleteConsultation(Guid id, [FromQuery] int? consultationDurationMinutes = null)
        {
            try
            {
                var completedBy = GetCurrentUserId();
                TimeSpan? consultationDuration = consultationDurationMinutes.HasValue
                    ? TimeSpan.FromMinutes(consultationDurationMinutes.Value)
                    : null;

                _logger.LogInformation("Completing consultation for visit: {VisitId}", id);

                var visit = await _visitService.CompleteConsultationAsync(id, consultationDuration, completedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Consultation completed successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error completing consultation: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot complete consultation for visit: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing consultation for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while completing consultation"));
            }
        }

        /// <summary>
        /// Cancel a visit
        /// </summary>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CancelVisit(Guid id, [FromQuery] string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Cancellation reason is required"));
                }

                var cancelledBy = GetCurrentUserId();
                _logger.LogInformation("Cancelling visit: {VisitId}", id);

                var visit = await _visitService.CancelVisitAsync(id, reason, cancelledBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Visit cancelled successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error cancelling visit: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while cancelling visit"));
            }
        }

        /// <summary>
        /// Mark visit as no-show
        /// </summary>
        [HttpPost("{id}/mark-no-show")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> MarkAsNoShow(Guid id)
        {
            try
            {
                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Marking visit as no-show: {VisitId}", id);

                var visit = await _visitService.MarkAsNoShowAsync(id, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Visit marked as no-show"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error marking visit as no-show: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking visit as no-show: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while marking visit as no-show"));
            }
        }

        /// <summary>
        /// Update vitals for a visit
        /// </summary>
        [HttpPut("{id}/vitals")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateVitals(Guid id, [FromBody] VisitVitalsDto vitals)
        {
            try
            {
                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Updating vitals for visit: {VisitId}", id);

                var visit = await _visitService.UpdateVitalsAsync(id, vitals, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Vitals updated successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating vitals: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vitals for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while updating vitals"));
            }
        }

        /// <summary>
        /// Get visit vitals
        /// </summary>
        [HttpGet("{id}/vitals")]
        [ProducesResponseType(typeof(ApiResponse<VisitVitalsDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetVitals(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting vitals for visit: {VisitId}", id);

                var vitals = await _visitService.GetVisitVitalsAsync(id);

                return Ok(ApiResponse<VisitVitalsDto>.SuccessResponse(
                    vitals,
                    "Vitals retrieved successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vitals for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while getting vitals"));
            }
        }

        /// <summary>
        /// Set diagnosis for a visit
        /// </summary>
        [HttpPut("{id}/diagnosis")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetDiagnosis(Guid id, [FromBody] string diagnosis)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(diagnosis))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Diagnosis is required"));
                }

                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Setting diagnosis for visit: {VisitId}", id);

                var visit = await _visitService.SetDiagnosisAsync(id, diagnosis, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Diagnosis set successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error setting diagnosis: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting diagnosis for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while setting diagnosis"));
            }
        }

        /// <summary>
        /// Set treatment plan for a visit
        /// </summary>
        [HttpPut("{id}/treatment-plan")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetTreatmentPlan(Guid id, [FromBody] string treatmentPlan)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(treatmentPlan))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Treatment plan is required"));
                }

                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Setting treatment plan for visit: {VisitId}", id);

                var visit = await _visitService.SetTreatmentPlanAsync(id, treatmentPlan, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Treatment plan set successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error setting treatment plan: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting treatment plan for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while setting treatment plan"));
            }
        }

        /// <summary>
        /// Add clinical notes to a visit
        /// </summary>
        [HttpPost("{id}/clinical-notes")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AddClinicalNotes(Guid id, [FromBody] string notes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(notes))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Notes are required"));
                }

                var addedBy = GetCurrentUserId();
                _logger.LogInformation("Adding clinical notes to visit: {VisitId}", id);

                var visit = await _visitService.AddClinicalNotesAsync(id, notes, addedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Clinical notes added successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding clinical notes to visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while adding clinical notes"));
            }
        }

        /// <summary>
        /// Set follow-up for a visit
        /// </summary>
        [HttpPut("{id}/follow-up")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetFollowUp(Guid id, [FromQuery] DateTime followUpDate, [FromQuery] string? instructions = null)
        {
            try
            {
                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Setting follow-up for visit: {VisitId}", id);

                var visit = await _visitService.SetFollowUpAsync(id, followUpDate, instructions, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Follow-up set successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error setting follow-up: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting follow-up for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while setting follow-up"));
            }
        }

        /// <summary>
        /// Refer patient to another doctor
        /// </summary>
        [HttpPost("{id}/refer")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ReferToDoctor(Guid id, [FromQuery] Guid referredToDoctorId, [FromQuery] string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Referral reason is required"));
                }

                var updatedBy = GetCurrentUserId();
                _logger.LogInformation("Referring visit {VisitId} to doctor {DoctorId}", id, referredToDoctorId);

                var visit = await _visitService.ReferToDoctorAsync(id, referredToDoctorId, reason, updatedBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Patient referred successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit or doctor not found");
                return NotFound(ApiResponse.ErrorResponse("Visit or doctor not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error referring patient: {VisitId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error referring patient for visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while referring patient"));
            }
        }

        /// <summary>
        /// Search visits with filters
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<VisitSummaryDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchVisits([FromQuery] VisitSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Searching visits with filters");

                var visits = await _visitService.SearchVisitsAsync(request);
                var visitSummaries = visits.ToList();

                return Ok(ApiResponse<List<VisitSummaryDto>>.SuccessResponse(
                    visitSummaries,
                    "Visits retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching visits");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while searching visits"));
            }
        }

        /// <summary>
        /// Get today's visits
        /// </summary>
        [HttpGet("today")]
        [ProducesResponseType(typeof(ApiResponse<List<VisitSummaryDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTodaysVisits()
        {
            try
            {
                _logger.LogInformation("Getting today's visits");

                var visits = await _visitService.GetTodaysVisitsAsync();
                var visitSummaries = visits.ToList();

                return Ok(ApiResponse<List<VisitSummaryDto>>.SuccessResponse(
                    visitSummaries,
                    "Today's visits retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's visits");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while getting today's visits"));
            }
        }

        /// <summary>
        /// Get visit statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<VisitStatisticsDto>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetVisitStatistics()
        {
            try
            {
                _logger.LogInformation("Getting visit statistics");

                var statistics = await _visitService.GetVisitStatisticsAsync();

                return Ok(ApiResponse<VisitStatisticsDto>.SuccessResponse(
                    statistics,
                    "Visit statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting visit statistics");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while getting visit statistics"));
            }
        }

        /// <summary>
        /// Get doctor schedules for today
        /// </summary>
        [HttpGet("doctor-schedules")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorScheduleDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDoctorSchedules([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow.Date;
                _logger.LogInformation("Getting doctor schedules for {Date}", targetDate);

                var schedules = await _visitService.GetDoctorSchedulesAsync(targetDate);
                var scheduleList = schedules.ToList();

                return Ok(ApiResponse<List<DoctorScheduleDto>>.SuccessResponse(
                    scheduleList,
                    "Doctor schedules retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor schedules");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while getting doctor schedules"));
            }
        }

        /// <summary>
        /// Validate if a visit is ready for prescription
        /// </summary>
        [HttpGet("{id}/validate-for-prescription")]
        [ProducesResponseType(typeof(ApiResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ValidateForPrescription(Guid id)
        {
            try
            {
                _logger.LogInformation("Validating visit for prescription: {VisitId}", id);

                var isValid = await _visitService.ValidateVisitForPrescriptionAsync(id);

                return Ok(ApiResponse<bool>.SuccessResponse(
                    isValid,
                    isValid ? "Visit is valid for prescription" : "Visit is not valid for prescription"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating visit for prescription: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while validating visit"));
            }
        }
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<OPDVisitDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> RestoreVisit(Guid id)
        {
            try
            {
                var restoredBy = GetCurrentUserId();
                _logger.LogInformation("Restoring visit: {VisitId}", id);

                var visit = await _visitService.RestoreVisitAsync(id, restoredBy);
                var visitDetail = await _visitService.GetVisitDetailAsync(id);

                return Ok(ApiResponse<OPDVisitDto>.SuccessResponse(
                    visitDetail,
                    "Visit restored successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found with ID: {VisitId}", id);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring visit: {VisitId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while restoring visit"));
            }
        }
        // Helper Methods
        private Guid GetCurrentUserId()
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            // Fallback to header for development
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var headerUserId))
            {
                return headerUserId;
            }

            throw new UnauthorizedAccessException("User not authenticated");
        }
    }
}