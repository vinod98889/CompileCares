using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Consultations.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Doctors;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using static Azure.Core.HttpHeader;

namespace CompileCares.API.Controllers
{

    //POST    /api/consultations/complete                       # Full consultation
    //POST    /api/consultations/quick                          # Quick consultation  
    //POST    /api/consultations/ultra-quick                    # Ultra-quick consultation
    //POST    /api/consultations/template                       # Apply template

    //GET     /api/consultations/{id/}/summary                  # Get summary
    //GET     /api/consultations/{id}/print                     # Print slip

    //GET     /api/consultations/doctor/{id}/today/stats        # Today's stats
    //GET     /api/consultations/doctor/{id}/common - diagnoses # Common diagnoses (from start date)
    //GET     /api/consultations/dashboard                      # Full dashboard

    [ApiController]
    [Route("api/[controller]")]
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;
        private readonly ILogger<ConsultationsController> _logger;

        public ConsultationsController(
            IConsultationService consultationService,
            ILogger<ConsultationsController> logger)
        {
            _consultationService = consultationService;
            _logger = logger;
        }

        // ========== CONSULTATION CREATION ENDPOINTS ==========

        /// <summary>
        /// Complete consultation with full details
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteConsultation(
            [FromBody] CompleteConsultationRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            return await HandleConsultationCreation(async () =>
            {
                var performedBy = GetAndValidateUserId(userId);
                _logger.LogInformation("Starting complete consultation for doctor: {DoctorId}", request.DoctorId);

                return await _consultationService.CompleteConsultationAsync(request, performedBy);
            }, "Consultation completed successfully");
        }

        /// <summary>
        /// Quick consultation for existing patients
        /// </summary>
        [HttpPost("quick")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> QuickConsultation(
            [FromBody] QuickConsultationRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Validation failed", GetModelStateErrors()));

            return await HandleConsultationCreation(async () =>
            {
                var performedBy = GetAndValidateUserId(userId);
                _logger.LogInformation("Starting quick consultation for patient: {PatientId}", request.PatientId);

                var completeRequest = ConvertQuickToCompleteRequest(request);
                return await _consultationService.CompleteConsultationAsync(completeRequest, performedBy);
            }, "Quick consultation completed successfully");
        }

        /// <summary>
        /// Ultra-quick consultation for small clinics (supports new/existing patients)
        /// </summary>
        [HttpPost("ultra-quick")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UltraQuickConsultation(
            [FromBody] UltraQuickConsultationRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Validation failed", GetModelStateErrors()));

            return await HandleConsultationCreation(async () =>
            {
                var performedBy = GetAndValidateUserId(userId);
                _logger.LogInformation("ULTRA-QUICK consultation for patient: {PatientName}",
                    request.IsNewPatient ? request.NewPatient?.Name : "Existing");

                var completeRequest = ConvertUltraQuickToCompleteRequest(request);
                return await _consultationService.CompleteConsultationAsync(completeRequest, performedBy);
            }, "Ultra-quick consultation completed successfully");
        }

        /// <summary>
        /// Apply a saved consultation template
        /// </summary>
        [HttpPost("template")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApplyTemplateConsultation(
            [FromBody] TemplateConsultationRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Validation failed", GetModelStateErrors()));

            var performedBy = GetAndValidateUserId(userId);
            if (performedBy == Guid.Empty)
                return Unauthorized(ApiResponse.ErrorResponse("Doctor not authenticated"));

            try
            {
                _logger.LogInformation("Applying template consultation. Template: {TemplateId}", request.TemplateId);

                var result = await _consultationService.ApplyTemplateConsultationAsync(
                    request.PatientId,
                    request.DoctorId,
                    request.TemplateId,
                    performedBy);

                _logger.LogInformation("Template consultation applied. Visit: {VisitId}", result.ConsultationId);

                return CreatedAtAction(
                    nameof(GetConsultationSummary),
                    new { visitId = result.ConsultationId },
                    ApiResponse<ConsultationResult>.SuccessResponse(
                        result,
                        "Template consultation applied successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Template not found: {TemplateId}", request.TemplateId);
                return NotFound(ApiResponse.ErrorResponse("Template not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying template consultation");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while applying template consultation"));
            }
        }

        // ========== CONSULTATION RETRIEVAL ENDPOINTS ==========

        /// <summary>
        /// Get consultation summary by visit ID
        /// </summary>
        [HttpGet("{visitId}/summary")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetConsultationSummary(Guid visitId)
        {
            try
            {
                _logger.LogInformation("Getting consultation summary for visit: {VisitId}", visitId);
                var summary = await _consultationService.GenerateConsultationSummaryAsync(visitId);


                return Ok(ApiResponse<string>.SuccessResponse(
                    summary,
                    "Consultation summary retrieved successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found: {VisitId}", visitId);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting consultation summary for visit: {VisitId}", visitId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving consultation summary"));
            }
        }

        /// <summary>
        /// Generate printable consultation slip
        /// </summary>
        [HttpGet("{visitId}/print")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PrintConsultationSlip(Guid visitId)
        {
            try
            {
                _logger.LogInformation("Generating print slip for visit: {VisitId}", visitId);
                var slip = await _consultationService.PrintCombinedSlipAsync(visitId);

                return Ok(ApiResponse<string>.SuccessResponse(
                    slip,
                    "Consultation slip generated successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Visit not found: {VisitId}", visitId);
                return NotFound(ApiResponse.ErrorResponse("Visit not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating slip for visit: {VisitId}", visitId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while generating consultation slip"));
            }
        }

        // ========== STATISTICS ENDPOINTS ==========

        /// <summary>
        /// Get today's consultation statistics for a doctor
        /// </summary>
        [HttpGet("doctor/{doctorId}/today/stats")]
        [ProducesResponseType(typeof(ApiResponse<TodaysStatsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodaysStats(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("Getting today's stats for doctor: {DoctorId}", doctorId);

                var count = await _consultationService.GetTodaysConsultationsCountAsync(doctorId);
                var revenue = await _consultationService.GetTodaysRevenueAsync(doctorId);

                var stats = new TodaysStatsDto
                {
                    Date = DateTime.UtcNow.Date,
                    ConsultationCount = count,
                    TotalRevenue = count
                };

                return Ok(ApiResponse<TodaysStatsDto>.SuccessResponse(
                    stats,
                    $"Today's stats retrieved - {stats.ConsultationCount} consultations, ₹{stats.TotalRevenue:N2} revenue"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's stats for doctor: {DoctorId}", doctorId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving today's stats"));
            }
        }

        /// <summary>
        /// Get common diagnoses for a doctor from a specific start date
        /// </summary>
        [HttpGet("doctor/{doctorId}/common-diagnoses")]
        [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCommonDiagnoses(
            Guid doctorId,
            [FromQuery] DateTime? fromDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);

                _logger.LogInformation("Getting common diagnoses for doctor: {DoctorId} from {FromDate}",
                    doctorId, startDate);

                var diagnoses = await _consultationService.GetCommonDiagnosesAsync(doctorId, startDate);

                return Ok(ApiResponse<Dictionary<string, int>>.SuccessResponse(
                    diagnoses,
                    "Common diagnoses retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting common diagnoses for doctor: {DoctorId}", doctorId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving common diagnoses"));
            }
        }

        /// <summary>
        /// Get comprehensive consultation dashboard
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConsultationDashboard(
            [FromQuery] Guid doctorId,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var dashboardDate = date ?? DateTime.UtcNow.Date;

                _logger.LogInformation("Getting dashboard for doctor: {DoctorId} on {Date}",
                    doctorId, dashboardDate);

                var countTask = _consultationService.GetTodaysConsultationsCountAsync(doctorId);
                var revenueTask = _consultationService.GetTodaysRevenueAsync(doctorId);
                var diagnosesTask = _consultationService.GetCommonDiagnosesAsync(doctorId, dashboardDate.AddDays(-7));

                await Task.WhenAll(countTask, revenueTask, diagnosesTask);

                var dashboard = new ConsultationDashboardDto
                {
                    Date = dashboardDate,
                    DoctorId = doctorId,
                    TodaysConsultationCount = await countTask,
                    TodaysRevenue = await revenueTask,
                    CommonDiagnoses = await diagnosesTask,
                    AverageConsultationTime = "15 min",
                    PatientSatisfaction = "95%",
                    FollowUpAppointments = 3
                };

                return Ok(ApiResponse<ConsultationDashboardDto>.SuccessResponse(
                    dashboard,
                    "Dashboard data retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard for doctor: {DoctorId}", doctorId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving dashboard"));
            }
        }

        // ========== HELPER METHODS ==========

        private async Task<IActionResult> HandleConsultationCreation(
            Func<Task<ConsultationResult>> consultationAction,
            string successMessage)
        {
            try
            {
                var result = await consultationAction();

                _logger.LogInformation("Consultation completed. Visit: {VisitId}", result.ConsultationId);

                return CreatedAtAction(
                    nameof(GetConsultationSummary),
                    new { visitId = result.ConsultationId },
                    ApiResponse<ConsultationResult>.SuccessResponse(result, successMessage));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments in consultation");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found in consultation");
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Doctor not authenticated");
                return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing consultation");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while completing consultation"));
            }
        }

        private Guid GetAndValidateUserId(Guid? userId)
        {
            var performedBy = userId ?? GetCurrentUserId();
            if (performedBy == Guid.Empty)
                throw new UnauthorizedAccessException("Doctor not authenticated");

            return performedBy;
        }

        private List<string> GetModelStateErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
        }

        private CompleteConsultationRequest ConvertQuickToCompleteRequest(QuickConsultationRequest request)
        {
            return new CompleteConsultationRequest
            {
                IsNewPatient = false,
                ExistingPatientId = request.PatientId,
                DoctorId = request.DoctorId,
                ConsultationDetails = new Application.Features.Visits.DTOs.UpdateVisitRequest
                {
                    ChiefComplaint = request.ChiefComplaint,
                    Diagnosis = request.Diagnosis
                },
                ConsultationFee = request.Fee,
                Payment = new Application.Features.Billing.DTOs.AddPaymentRequest
                {
                    Amount = request.Fee,
                    PaymentMode = request.PaymentMode,
                    TransactionId = $"QC_{DateTime.UtcNow:yyyyMMddHHmmss}"
                },
                SetFollowUp = request.FollowUpDays.HasValue,
                FollowUpDays = request.FollowUpDays
            };
        }

        private CompleteConsultationRequest ConvertUltraQuickToCompleteRequest(UltraQuickConsultationRequest request)
        {
            return new CompleteConsultationRequest
            {
                IsNewPatient = request.IsNewPatient,
                NewPatient = request.NewPatient != null ? new PatientQuickCreateRequest
                {
                    Name = request.NewPatient.Name,
                    Mobile = request.NewPatient.Mobile,
                    Gender = request.NewPatient.Gender
                } : null,
                ExistingPatientId = request.ExistingPatientId,
                DoctorId = request.DoctorId,
                ConsultationDetails = new Application.Features.Visits.DTOs.UpdateVisitRequest
                {
                    ChiefComplaint = request.ChiefComplaint,
                    Diagnosis = request.Diagnosis,
                    BloodPressure = request.BloodPressure,
                    Temperature = request.Temperature,
                    Pulse = request.Pulse
                },
                Medicines = request.Medicines.Select(m => new Application.Features.Prescriptions.DTOs.AddMedicineRequest
                {
                    MedicineId = m.MedicineId,
                    DoseId = m.DoseId,
                    DurationDays = m.DurationDays,
                    Quantity = m.Quantity,
                    Instructions = m.Instructions
                }).ToList(),
                ConsultationFee = request.ConsultationFee,
                Payment = new Application.Features.Billing.DTOs.AddPaymentRequest
                {
                    Amount = request.ConsultationFee,
                    PaymentMode = "Cash",
                    TransactionId = $"UQ_{DateTime.UtcNow:yyyyMMddHHmmss}"
                },
                ConsultationNotes = "Ultra-quick consultation",
                SetFollowUp = request.SetFollowUp,
                FollowUpDays = request.FollowUpDays,
                FollowUpInstructions = request.FollowUpInstructions
            };
        }

        private Guid GetCurrentUserId()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var userId))
            {
                return userId;
            }

            return Guid.Empty; // Return empty for unauthenticated users
        }
    }
}