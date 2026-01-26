// IPrescriptionService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Prescriptions.DTOs;

namespace CompileCares.UI.Services.PrescriptionService
{
    public interface IPrescriptionService
    {
        // Prescription CRUD
        Task<ApiResponse<PrescriptionDetailDto>> CreatePrescriptionAsync(CreatePrescriptionRequest request);
        Task<ApiResponse<PrescriptionDetailDto>> GetPrescriptionAsync(Guid id);
        Task<ApiResponse<PrescriptionDetailDto>> AddMedicineAsync(Guid prescriptionId, AddMedicineRequest request);
        Task<ApiResponse<PrescriptionDetailDto>> ApplyTemplateAsync(Guid prescriptionId, ApplyTemplateRequest request);
        Task<ApiResponse<PrescriptionDetailDto>> CompletePrescriptionAsync(Guid prescriptionId);
        Task<ApiResponse<string>> CancelPrescriptionAsync(Guid prescriptionId, string reason);

        // Patient Prescriptions
        Task<ApiResponse<List<PrescriptionDto>>> GetPatientPrescriptionsAsync(Guid patientId);

        // Search & Lists
        Task<ApiResponse<List<PrescriptionDto>>> SearchPrescriptionsAsync(PrescriptionSearchRequest request);

        // Validation & Printing
        Task<ApiResponse<bool>> ValidatePrescriptionAsync(Guid id);
        Task<ApiResponse<string>> PrintPrescriptionAsync(Guid id);

        // Dispensing
        Task<ApiResponse<PrescriptionDetailDto>> DispenseMedicineAsync(Guid prescriptionId, Guid medicineId, string dispensedBy);

        // Medicine Operations
        Task<ApiResponse<PrescriptionDetailDto>> RemoveMedicineAsync(Guid prescriptionId, Guid medicineId);
    }
}