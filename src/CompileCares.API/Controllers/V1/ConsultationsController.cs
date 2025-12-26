using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Consultations.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompileCares.API.Controllers
{
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

        // POST: api/consultations/complete
        [HttpPost("complete")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteConsultation(
            [FromBody] CompleteConsultationRequest request,
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

                var performedBy = userId ?? GetCurrentUserId();
                if (performedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Doctor not authenticated"));
                }

                _logger.LogInformation("Starting complete consultation for doctor: {DoctorId}", request.DoctorId);

                var result = await _consultationService.CompleteConsultationAsync(
                    request,
                    performedBy);

                _logger.LogInformation("Consultation completed. Visit: {VisitId}", result.ConsultationId);

                return CreatedAtAction(
                    nameof(GetConsultationSummary),
                    new { visitId = result.ConsultationId },
                    ApiResponse<ConsultationResult>.SuccessResponse(
                        result,
                        "Consultation completed successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments in complete consultation");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found in complete consultation");
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing consultation");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while completing consultation"));
            }
        }

        // POST: api/consultations/test-ultra-quick (TEST ENDPOINT)
        [HttpPost("test-ultra-quick")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        public async Task<IActionResult> TestUltraQuickConsultation()
        {
            try
            {
                var performedBy = Guid.Parse("22222222-2222-2222-2222-222222222222");

                _logger.LogInformation("TEST ULTRA-QUICK consultation");

                // Use hardcoded GUIDs for testing
                var completeRequest = new CompleteConsultationRequest
                {
                    IsNewPatient = true,
                    NewPatient = new PatientQuickCreateRequest
                    {
                        Name = "Test Patient",
                        Mobile = "9876543210",
                        Gender = Shared.Enums.Gender.Male
                    },
                    DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ConsultationDetails = new Application.Features.Visits.DTOs.UpdateVisitRequest
                    {
                        ChiefComplaint = "Fever and cold",
                        Diagnosis = "Viral Infection"
                    },
                    Medicines = new List<Application.Features.Prescriptions.DTOs.AddMedicineRequest>
            {
                new Application.Features.Prescriptions.DTOs.AddMedicineRequest
                {
                    MedicineId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), // Test medicine
                    DoseId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),     // Test dose
                    DurationDays = 5,
                    Quantity = 1,
                    Instructions = "After food"
                }
            },
                    ConsultationFee = 300,
                    Payment = new Application.Features.Billing.DTOs.AddPaymentRequest
                    {
                        Amount = 300,
                        PaymentMode = "Cash",
                        TransactionId = $"TEST_{DateTime.UtcNow:yyyyMMddHHmmss}"
                    },
                    ConsultationNotes = "Test consultation",
                    SetFollowUp = true,
                    FollowUpDays = 3,
                    FollowUpInstructions = "Return if fever persists"
                };

                var result = await _consultationService.CompleteConsultationAsync(
                    completeRequest,
                    performedBy);

                _logger.LogInformation("TEST completed! Visit: {VisitId}", result.ConsultationId);

                return CreatedAtAction(
                    nameof(GetConsultationSummary),
                    new { visitId = result.ConsultationId },
                    ApiResponse<ConsultationResult>.SuccessResponse(
                        result,
                        "Test consultation completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test consultation");
                return StatusCode(500, ApiResponse.ErrorResponse($"Test error: {ex.Message}"));
            }
        }
        // POST: api/consultations/quick
        [HttpPost("quick")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> QuickConsultation(
            [FromBody] QuickConsultationRequest request,
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

                var performedBy = userId ?? GetCurrentUserId();
                if (performedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Doctor not authenticated"));
                }

                _logger.LogInformation("Starting quick consultation");

                // Convert QuickConsultationRequest to CompleteConsultationRequest
                var completeRequest = ConvertToCompleteConsultationRequest(request);

                var result = await _consultationService.CompleteConsultationAsync(
                    completeRequest,
                    performedBy);

                _logger.LogInformation("Quick consultation completed. Visit: {VisitId}", result.ConsultationId);

                return CreatedAtAction(
                    nameof(GetConsultationSummary),
                    new { visitId = result.ConsultationId },
                    ApiResponse<ConsultationResult>.SuccessResponse(
                        result,
                        "Quick consultation completed successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments in quick consultation");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick consultation");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while processing quick consultation"));
            }
        }

        // POST: api/consultations/template
        [HttpPost("template")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApplyTemplateConsultation(
            [FromBody] TemplateConsultationRequest request,
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

                var performedBy = userId ?? GetCurrentUserId();
                if (performedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Doctor not authenticated"));
                }

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

        // POST: api/consultations/ultra-quick (SPECIAL FOR SMALL CLINIC)
        [HttpPost("ultra-quick")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UltraQuickConsultation(
            [FromBody] UltraQuickConsultationRequest request,
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

                var performedBy = userId ?? GetCurrentUserId();
                if (performedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Doctor not authenticated"));
                }

                _logger.LogInformation("ULTRA-QUICK consultation for patient: {PatientName}",
                    request.IsNewPatient ? request.NewPatient?.Name : "Existing");

                // Convert to CompleteConsultationRequest
                var completeRequest = new CompleteConsultationRequest
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

                var result = await _consultationService.CompleteConsultationAsync(
                    completeRequest,
                    performedBy);

                _logger.LogInformation("ULTRA-QUICK completed! Visit: {VisitId}", result.ConsultationId);

                return CreatedAtAction(
                    nameof(GetConsultationSummary),
                    new { visitId = result.ConsultationId },
                    ApiResponse<ConsultationResult>.SuccessResponse(
                        result,
                        "Ultra-quick consultation completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ultra-quick consultation");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred in ultra-quick consultation"));
            }
        }

        // GET: api/consultations/{visitId}/summary
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

        // GET: api/consultations/{visitId}/print
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

        // GET: api/consultations/doctor/{doctorId}/today/count
        [HttpGet("doctor/{doctorId}/today/count")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodaysConsultationsCount(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("Getting today's consultation count for doctor: {DoctorId}", doctorId);

                var count = await _consultationService.GetTodaysConsultationsCountAsync(doctorId);

                return Ok(ApiResponse<int>.SuccessResponse(
                    count,
                    $"Today's consultations: {count}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's consultation count for doctor: {DoctorId}", doctorId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving consultation count"));
            }
        }

        // GET: api/consultations/doctor/{doctorId}/today/revenue
        [HttpGet("doctor/{doctorId}/today/revenue")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodaysRevenue(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("Getting today's revenue for doctor: {DoctorId}", doctorId);

                var revenue = await _consultationService.GetTodaysRevenueAsync(doctorId);

                return Ok(ApiResponse<decimal>.SuccessResponse(
                    revenue,
                    $"Today's revenue: ₹{revenue:N2}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's revenue for doctor: {DoctorId}", doctorId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving today's revenue"));
            }
        }

        // GET: api/consultations/doctor/{doctorId}/common-diagnoses
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

        // GET: api/consultations/dashboard
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

                var todayCount = await _consultationService.GetTodaysConsultationsCountAsync(doctorId);
                var todayRevenue = await _consultationService.GetTodaysRevenueAsync(doctorId);
                var commonDiagnoses = await _consultationService.GetCommonDiagnosesAsync(doctorId, dashboardDate.AddDays(-7));

                var dashboard = new ConsultationDashboardDto
                {
                    Date = dashboardDate,
                    DoctorId = doctorId,
                    TodaysConsultationCount = todayCount,
                    TodaysRevenue = todayRevenue,
                    CommonDiagnoses = commonDiagnoses,
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

        // Helper Methods
        private CompleteConsultationRequest ConvertToCompleteConsultationRequest(QuickConsultationRequest request)
        {
            // This is a placeholder - you need to implement this based on your logic
            // For now, return a basic CompleteConsultationRequest
            return new CompleteConsultationRequest
            {
                IsNewPatient = false, // Assuming existing patient for quick consultation
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

        private Guid GetCurrentUserId()
        {
            // TODO: Replace with actual authentication
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var userId))
            {
                return userId;
            }

            return Guid.Parse("22222222-2222-2222-2222-222222222222"); // Clinic Doctor ID
        }
    }
}