using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompileCares.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<PrescriptionsController> _logger;

        public PrescriptionsController(
            IPrescriptionService prescriptionService,
            ILogger<PrescriptionsController> logger)
        {
            _prescriptionService = prescriptionService;
            _logger = logger;
        }

        // POST: api/prescriptions
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePrescription(
            [FromBody] CreatePrescriptionRequest request,
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

                var createdBy = userId ?? GetCurrentUserId();
                if (createdBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Creating prescription for patient: {PatientId}", request.PatientId);

                // First create the prescription
                var prescription = await _prescriptionService.CreatePrescriptionAsync(
                    request.PatientId,
                    request.DoctorId,
                    request.OPDVisitId,
                    createdBy);

                // Update diagnosis and instructions if provided
                if (!string.IsNullOrWhiteSpace(request.Diagnosis))
                {
                    prescription.UpdateDiagnosis(request.Diagnosis, createdBy);
                }

                if (!string.IsNullOrWhiteSpace(request.Instructions))
                {
                    prescription.UpdateInstructions(request.Instructions, createdBy);
                }

                if (!string.IsNullOrWhiteSpace(request.FollowUpInstructions))
                {
                    // Note: Need to add SetFollowUpInstructions method to Prescription entity
                    // prescription.SetFollowUpInstructions(request.FollowUpInstructions, createdBy);
                }

                // Add medicines
                foreach (var medicineRequest in request.Medicines)
                {
                    await _prescriptionService.AddMedicineToPrescriptionAsync(
                        prescription.Id,
                        medicineRequest.MedicineId,
                        medicineRequest.DoseId,
                        medicineRequest.DurationDays,
                        medicineRequest.Quantity,
                        medicineRequest.Instructions,
                        createdBy);
                }

                // Add complaints
                foreach (var complaintRequest in request.Complaints)
                {
                    // Note: Need to add AddComplaintToPrescriptionAsync method to service
                    // await _prescriptionService.AddComplaintToPrescriptionAsync(
                    //     prescription.Id,
                    //     complaintRequest.ComplaintId,
                    //     complaintRequest.CustomComplaint,
                    //     complaintRequest.Duration,
                    //     complaintRequest.Severity,
                    //     createdBy);
                }

                // Add advised items
                foreach (var advisedRequest in request.AdvisedItems)
                {
                    // Note: Need to add AddAdviceToPrescriptionAsync method to service
                    // await _prescriptionService.AddAdviceToPrescriptionAsync(
                    //     prescription.Id,
                    //     advisedRequest.AdvisedId,
                    //     advisedRequest.CustomAdvice,
                    //     createdBy);
                }

                // Set validity if different from default
                if (request.ValidityDays != 7)
                {
                    prescription.SetValidity(request.ValidityDays, createdBy);
                }

                // Get updated prescription with all details
                var updatedPrescription = await GetPrescriptionWithDetails(prescription.Id);

                return CreatedAtAction(
                    nameof(GetPrescription),
                    new { id = prescription.Id },
                    ApiResponse<PrescriptionDto>.SuccessResponse(
                        MapToPrescriptionDto(updatedPrescription),
                        "Prescription created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription for patient: {PatientId}", request.PatientId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while creating prescription"));
            }
        }

        // GET: api/prescriptions/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPrescription(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting prescription with ID: {PrescriptionId}", id);

                var prescription = await GetPrescriptionWithDetails(id);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    MapToPrescriptionDetailDto(prescription),
                    "Prescription retrieved successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prescription with ID: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving prescription"));
            }
        }

        // POST: api/prescriptions/{id}/medicines
        [HttpPost("{id}/medicines")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddMedicine(
            Guid id,
            [FromBody] AddMedicineRequest request,
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

                _logger.LogInformation("Adding medicine to prescription: {PrescriptionId}", id);

                var prescription = await _prescriptionService.AddMedicineToPrescriptionAsync(
                    id,
                    request.MedicineId,
                    request.DoseId,
                    request.DurationDays,
                    request.Quantity,
                    request.Instructions,
                    updatedBy);

                return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(
                    MapToPrescriptionDto(prescription),
                    "Medicine added successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medicine to prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while adding medicine"));
            }
        }

        // POST: api/prescriptions/{id}/apply-template
        [HttpPost("{id}/apply-template")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApplyTemplate(
            Guid id,
            [FromBody] ApplyTemplateRequest request,
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

                var appliedBy = userId ?? GetCurrentUserId();
                if (appliedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Applying template to prescription: {PrescriptionId}", id);

                var prescription = await _prescriptionService.ApplyTemplateToPrescriptionAsync(
                    id,
                    request.TemplateId,
                    appliedBy);

                return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(
                    MapToPrescriptionDto(prescription),
                    "Template applied successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription or template not found");
                return NotFound(ApiResponse.ErrorResponse("Prescription or template not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying template to prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while applying template"));
            }
        }

        // POST: api/prescriptions/{id}/complete
        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompletePrescription(
            Guid id,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                var completedBy = userId ?? GetCurrentUserId();
                if (completedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Completing prescription: {PrescriptionId}", id);

                var prescription = await _prescriptionService.CompletePrescriptionAsync(id, completedBy);

                return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(
                    MapToPrescriptionDto(prescription),
                    "Prescription completed successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            //catch (Application.Common.Exceptions.ValidationException ex)
            //{
            //    _logger.LogWarning(ex, "Validation failed for completing prescription: {PrescriptionId}", id);
            //    return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            //}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while completing prescription"));
            }
        }

        // GET: api/prescriptions/patient/{patientId}
        [HttpGet("patient/{patientId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPatientPrescriptions(Guid patientId)
        {
            try
            {
                _logger.LogInformation("Getting prescriptions for patient: {PatientId}", patientId);

                var prescriptions = await _prescriptionService.GetPatientPrescriptionsAsync(patientId);

                return Ok(ApiResponse<List<PrescriptionDto>>.SuccessResponse(
                    prescriptions.Select(MapToPrescriptionDto).ToList(),
                    "Patient prescriptions retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prescriptions for patient: {PatientId}", patientId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving prescriptions"));
            }
        }

        // GET: api/prescriptions/{id}/validate
        [HttpGet("{id}/validate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ValidatePrescription(Guid id)
        {
            try
            {
                _logger.LogInformation("Validating prescription: {PrescriptionId}", id);

                var isValid = await _prescriptionService.ValidatePrescriptionAsync(id);

                return Ok(ApiResponse<bool>.SuccessResponse(
                    isValid,
                    isValid ? "Prescription is valid" : "Prescription is invalid"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while validating prescription"));
            }
        }

        // GET: api/prescriptions/{id}/print
        [HttpGet("{id}/print")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PrintPrescription(Guid id)
        {
            try
            {
                _logger.LogInformation("Generating print for prescription: {PrescriptionId}", id);

                var printContent = await _prescriptionService.GeneratePrescriptionPrintAsync(id);

                return Ok(ApiResponse<string>.SuccessResponse(
                    printContent,
                    "Prescription print generated successfully"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating print for prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while generating print"));
            }
        }

        // DELETE: api/prescriptions/{id}/cancel
        [HttpDelete("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelPrescription(
            Guid id,
            [FromQuery] string reason,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Cancellation reason is required"));
                }

                var cancelledBy = userId ?? GetCurrentUserId();
                if (cancelledBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Cancelling prescription: {PrescriptionId}", id);

                // Note: Need to add CancelPrescriptionAsync method to service
                // var success = await _prescriptionService.CancelPrescriptionAsync(id, reason, cancelledBy);

                // return Ok(ApiResponse.SuccessResponse("Prescription cancelled successfully"));

                return Ok(ApiResponse.SuccessResponse("Cancellation endpoint ready - implementation pending"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while cancelling prescription"));
            }
        }

        // GET: api/prescriptions/search
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchPrescriptions([FromQuery] PrescriptionSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Searching prescriptions with filters");

                // Note: Need to add SearchPrescriptionsAsync method to service
                // var prescriptions = await _prescriptionService.SearchPrescriptionsAsync(request);

                // return Ok(ApiResponse<List<PrescriptionDto>>.SuccessResponse(
                //     prescriptions.Select(MapToPrescriptionDto).ToList(),
                //     "Prescriptions retrieved successfully"));

                return Ok(ApiResponse<List<PrescriptionDto>>.SuccessResponse(
                    new List<PrescriptionDto>(),
                    "Search endpoint ready - implementation pending"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching prescriptions");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while searching prescriptions"));
            }
        }

        // POST: api/prescriptions/{prescriptionId}/medicines/{medicineId}/dispense
        [HttpPost("{prescriptionId}/medicines/{medicineId}/dispense")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DispenseMedicine(
            Guid prescriptionId,
            Guid medicineId,
            [FromQuery] string dispensedBy,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dispensedBy))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Dispenser name is required"));
                }

                var updatedBy = userId ?? GetCurrentUserId();
                if (updatedBy == Guid.Empty)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("User not authenticated"));
                }

                _logger.LogInformation("Dispensing medicine {MedicineId} from prescription {PrescriptionId}",
                    medicineId, prescriptionId);

                // Note: Need to add MarkMedicineAsDispensedAsync method to service
                // var prescription = await _prescriptionService.MarkMedicineAsDispensedAsync(
                //     prescriptionId, medicineId, dispensedBy, updatedBy);

                // return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(
                //     MapToPrescriptionDto(prescription),
                //     "Medicine dispensed successfully"));

                return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(
                    new PrescriptionDto(),
                    "Dispense endpoint ready - implementation pending"));
            }
            catch (Application.Common.Exceptions.NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription or medicine not found");
                return NotFound(ApiResponse.ErrorResponse("Prescription or medicine not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispensing medicine {MedicineId} from prescription {PrescriptionId}",
                    medicineId, prescriptionId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while dispensing medicine"));
            }
        }

        // Helper Methods
        private async Task<Core.Entities.Clinical.Prescription> GetPrescriptionWithDetails(Guid id)
        {
            // This is a placeholder - you need to implement this in your service layer
            // or create a repository method to get prescription with all includes
            throw new NotImplementedException("Implement GetPrescriptionWithDetails in service layer");
        }

        private PrescriptionDto MapToPrescriptionDto(Core.Entities.Clinical.Prescription prescription)
        {
            // Map entity to DTO - you should use AutoMapper for this
            return new PrescriptionDto
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                PrescriptionDate = prescription.PrescriptionDate,
                PatientId = prescription.PatientId,
                DoctorId = prescription.DoctorId,
                OPDVisitId = prescription.OPDVisitId,
                Diagnosis = prescription.Diagnosis,
                Instructions = prescription.Instructions,
                FollowUpInstructions = prescription.FollowUpInstructions,
                Status = prescription.Status,
                ValidityDays = prescription.ValidityDays,
                ValidUntil = prescription.ValidUntil,
                IsValid = prescription.IsValid(),
                CreatedAt = prescription.CreatedAt,
                UpdatedAt = prescription.UpdatedAt
            };
        }

        private PrescriptionDetailDto MapToPrescriptionDetailDto(Core.Entities.Clinical.Prescription prescription)
        {
            // Map entity to detail DTO - implement this properly
            var dto = new PrescriptionDetailDto
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                PrescriptionDate = prescription.PrescriptionDate,
                PatientId = prescription.PatientId,
                DoctorId = prescription.DoctorId,
                OPDVisitId = prescription.OPDVisitId,
                Diagnosis = prescription.Diagnosis,
                Instructions = prescription.Instructions,
                FollowUpInstructions = prescription.FollowUpInstructions,
                Status = prescription.Status,
                ValidityDays = prescription.ValidityDays,
                ValidUntil = prescription.ValidUntil,
                IsValid = prescription.IsValid(),
                CreatedAt = prescription.CreatedAt,
                UpdatedAt = prescription.UpdatedAt
            };

            // Add mapping for medicines, complaints, advised items here
            return dto;
        }

        private Guid GetCurrentUserId()
        {
            // TODO: Replace with actual authentication
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var userId))
            {
                return userId;
            }

            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
    }
}