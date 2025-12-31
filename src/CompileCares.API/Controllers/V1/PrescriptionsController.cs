using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Clinical;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

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
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreatePrescription(
            [FromBody] CreatePrescriptionRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Invalid request data"));
                }

                var createdBy = GetCurrentUserId(userId);

                _logger.LogInformation("Creating prescription for patient: {PatientId}", request.PatientId);

                // Create prescription
                var prescription = await _prescriptionService.CreatePrescriptionAsync(
                    request.PatientId,
                    request.DoctorId,
                    request.OPDVisitId,
                    createdBy);

                // Update diagnosis if provided
                if (!string.IsNullOrWhiteSpace(request.Diagnosis))
                {
                    await _prescriptionService.UpdatePrescriptionDiagnosisAsync(
                        prescription.Id, request.Diagnosis, createdBy);
                }

                // Update instructions if provided
                if (!string.IsNullOrWhiteSpace(request.Instructions))
                {
                    await _prescriptionService.UpdatePrescriptionInstructionsAsync(
                        prescription.Id, request.Instructions, createdBy);
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
                        medicineRequest.CustomDosage,
                        createdBy);
                }

                // Add complaints
                foreach (var complaintRequest in request.Complaints)
                {
                    await _prescriptionService.AddComplaintToPrescriptionAsync(
                        prescription.Id,
                        complaintRequest.ComplaintId,
                        complaintRequest.CustomComplaint,
                        complaintRequest.Duration,
                        complaintRequest.Severity,
                        createdBy);
                }

                // Add advised items
                foreach (var advisedRequest in request.AdvisedItems)
                {
                    await _prescriptionService.AddAdviceToPrescriptionAsync(
                        prescription.Id,
                        advisedRequest.AdvisedId,
                        advisedRequest.CustomAdvice,
                        createdBy);
                }

                // Set validity if different from default
                if (request.ValidityDays != 7)
                {
                    // Get prescription and set validity
                    var prescriptionToUpdate = await _prescriptionService.GetPrescriptionWithDetailsAsync(prescription.Id);
                    prescriptionToUpdate.SetValidity(request.ValidityDays, createdBy);
                }

                // Complete the prescription
                await _prescriptionService.CompletePrescriptionAsync(prescription.Id, createdBy);

                // Get complete prescription with details
                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(prescription.Id);

                return CreatedAtAction(
                    nameof(GetPrescription),
                    new { id = prescription.Id },
                    ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                        prescriptionDetail,
                        "Prescription created successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found while creating prescription");
                return NotFound(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating prescription");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Business validation error creating prescription");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription for patient: {PatientId}", request.PatientId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while creating prescription"));
            }
        }

        // GET: api/prescriptions/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPrescription(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting prescription with ID: {PrescriptionId}", id);

                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(id);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    prescriptionDetail,
                    "Prescription retrieved successfully"));
            }
            catch (NotFoundException ex)
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
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AddMedicine(
            Guid id,
            [FromBody] AddMedicineRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Invalid request data"));
                }

                var updatedBy = GetCurrentUserId(userId);

                _logger.LogInformation("Adding medicine to prescription: {PrescriptionId}", id);

                var prescription = await _prescriptionService.AddMedicineToPrescriptionAsync(
                    id,
                    request.MedicineId,
                    request.DoseId,
                    request.DurationDays,
                    request.Quantity,
                    request.Instructions,
                    request.CustomDosage,
                    updatedBy);

                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(id);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    prescriptionDetail,
                    "Medicine added successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error adding medicine to prescription");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medicine to prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while adding medicine"));
            }
        }

        // POST: api/prescriptions/{id}/apply-template
        [HttpPost("{id}/apply-template")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ApplyTemplate(
            Guid id,
            [FromBody] ApplyTemplateRequest request,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Invalid request data"));
                }

                var appliedBy = GetCurrentUserId(userId);

                _logger.LogInformation("Applying template to prescription: {PrescriptionId}", id);

                var prescription = await _prescriptionService.ApplyTemplateToPrescriptionAsync(
                    id,
                    request.TemplateId,
                    appliedBy);

                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(id);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    prescriptionDetail,
                    "Template applied successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription or template not found");
                return NotFound(ApiResponse.ErrorResponse("Prescription or template not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error applying template");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying template to prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while applying template"));
            }
        }

        // POST: api/prescriptions/{id}/complete
        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CompletePrescription(
            Guid id,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                var completedBy = GetCurrentUserId(userId);

                _logger.LogInformation("Completing prescription: {PrescriptionId}", id);

                var prescription = await _prescriptionService.CompletePrescriptionAsync(id, completedBy);
                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(id);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    prescriptionDetail,
                    "Prescription completed successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error completing prescription: {PrescriptionId}", id);
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot complete prescription from current state");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while completing prescription"));
            }
        }

        // GET: api/prescriptions/patient/{patientId}
        [HttpGet("patient/{patientId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientPrescriptions(Guid patientId)
        {
            try
            {
                _logger.LogInformation("Getting prescriptions for patient: {PatientId}", patientId);

                var prescriptions = await _prescriptionService.GetPatientPrescriptionsAsync(patientId);
                var prescriptionDtos = new List<PrescriptionDto>();

                foreach (var prescription in prescriptions)
                {
                    var dto = await _prescriptionService.GetPrescriptionDetailAsync(prescription.Id);
                    prescriptionDtos.Add(new PrescriptionDto
                    {
                        Id = dto.Id,
                        PrescriptionNumber = dto.PrescriptionNumber,
                        PrescriptionDate = dto.PrescriptionDate,
                        PatientId = dto.PatientId,
                        PatientName = dto.PatientName,
                        PatientNumber = dto.PatientNumber,
                        DoctorId = dto.DoctorId,
                        DoctorName = dto.DoctorName,
                        DoctorRegistrationNumber = dto.DoctorRegistrationNumber,
                        OPDVisitId = dto.OPDVisitId,
                        VisitNumber = dto.VisitNumber,
                        Diagnosis = dto.Diagnosis,
                        Instructions = dto.Instructions,
                        FollowUpInstructions = dto.FollowUpInstructions,
                        Status = dto.Status,
                        ValidityDays = dto.ValidityDays,
                        ValidUntil = dto.ValidUntil,
                        IsValid = dto.IsValid,
                        MedicineCount = dto.Medicines.Count,
                        ComplaintCount = dto.Complaints.Count,
                        AdviceCount = dto.AdvisedItems.Count,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt
                    });
                }

                return Ok(ApiResponse<List<PrescriptionDto>>.SuccessResponse(
                    prescriptionDtos,
                    "Patient prescriptions retrieved successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Patient not found: {PatientId}", patientId);
                return NotFound(ApiResponse.ErrorResponse("Patient not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prescriptions for patient: {PatientId}", patientId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while retrieving prescriptions"));
            }
        }

        // GET: api/prescriptions/{id}/validate
        [HttpGet("{id}/validate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
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
            catch (NotFoundException ex)
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
        [ProducesResponseType(typeof(ApiResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
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
            catch (NotFoundException ex)
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

        // DELETE: api/prescriptions/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
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

                var cancelledBy = GetCurrentUserId(userId);

                _logger.LogInformation("Cancelling prescription: {PrescriptionId}", id);

                var success = await _prescriptionService.CancelPrescriptionAsync(id, reason, cancelledBy);

                return Ok(ApiResponse.SuccessResponse("Prescription cancelled successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription not found with ID: {PrescriptionId}", id);
                return NotFound(ApiResponse.ErrorResponse("Prescription not found"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error cancelling prescription");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling prescription: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while cancelling prescription"));
            }
        }

        // GET: api/prescriptions/search
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchPrescriptions([FromQuery] PrescriptionSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Searching prescriptions with filters");

                var prescriptionDtos = await _prescriptionService.SearchPrescriptionsAsync(request);

                // prescriptionDtos is already a list of PrescriptionDto, no need to map again
                var prescriptionsList = prescriptionDtos.ToList();

                return Ok(ApiResponse<List<PrescriptionDto>>.SuccessResponse(
                    prescriptionsList,
                    "Prescriptions retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching prescriptions");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while searching prescriptions"));
            }
        }

        // POST: api/prescriptions/{prescriptionId}/medicines/{medicineId}/dispense
        [HttpPost("{prescriptionId}/medicines/{medicineId}/dispense")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
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

                var updatedBy = GetCurrentUserId(userId);

                _logger.LogInformation("Dispensing medicine {MedicineId} from prescription {PrescriptionId}",
                    medicineId, prescriptionId);

                var prescription = await _prescriptionService.MarkMedicineAsDispensedAsync(
                    prescriptionId, medicineId, dispensedBy, updatedBy);

                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(prescriptionId);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    prescriptionDetail,
                    "Medicine dispensed successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription or medicine not found");
                return NotFound(ApiResponse.ErrorResponse("Prescription or medicine not found"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot dispense medicine");
                return BadRequest(ApiResponse.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispensing medicine {MedicineId} from prescription {PrescriptionId}",
                    medicineId, prescriptionId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while dispensing medicine"));
            }
        }

        // DELETE: api/prescriptions/{prescriptionId}/medicines/{medicineId}
        [HttpDelete("{prescriptionId}/medicines/{medicineId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> RemoveMedicine(
            Guid prescriptionId,
            Guid medicineId,
            [FromHeader(Name = "X-User-Id")] Guid? userId = null)
        {
            try
            {
                var removedBy = GetCurrentUserId(userId);

                _logger.LogInformation("Removing medicine {MedicineId} from prescription {PrescriptionId}",
                    medicineId, prescriptionId);

                // Get prescription with details
                var prescription = await _prescriptionService.GetPrescriptionWithDetailsAsync(prescriptionId);

                // Remove medicine
                prescription.RemoveMedicine(medicineId, removedBy);

                // Update prescription in database
                // Note: You'll need to implement this update method in your service or repository
                await _prescriptionService.UpdatePrescriptionAsync(prescription);

                var prescriptionDetail = await _prescriptionService.GetPrescriptionDetailAsync(prescriptionId);

                return Ok(ApiResponse<PrescriptionDetailDto>.SuccessResponse(
                    prescriptionDetail,
                    "Medicine removed successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Prescription or medicine not found");
                return NotFound(ApiResponse.ErrorResponse("Prescription or medicine not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing medicine {MedicineId} from prescription {PrescriptionId}",
                    medicineId, prescriptionId);
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while removing medicine"));
            }
        }

        // Helper Methods
        private Guid GetCurrentUserId(Guid? userId = null)
        {
            // Use provided userId or get from header
            if (userId.HasValue && userId.Value != Guid.Empty)
                return userId.Value;

            // Try to get from header
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var parsedUserId))
            {
                return parsedUserId;
            }

            // TODO: In production, use proper authentication
            // For now, use a default or throw exception
            throw new UnauthorizedAccessException("User not authenticated. Please provide X-User-Id header.");
        }
    }
}