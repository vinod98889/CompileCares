using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Core.Entities.Clinical;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompileCares.Application.Services
{
    public interface IPrescriptionService
    {
        Task<Prescription> CreatePrescriptionAsync(Guid patientId, Guid doctorId, Guid visitId, Guid createdBy);

        Task<Prescription> AddMedicineToPrescriptionAsync(
            Guid prescriptionId,
            Guid medicineId,
            Guid doseId,
            int durationDays,
            int quantity,
            string? instructions,
            string? customDosage,
            Guid updatedBy);

        Task<Prescription> ApplyTemplateToPrescriptionAsync(Guid prescriptionId, Guid templateId, Guid appliedBy);
        Task<Prescription> CompletePrescriptionAsync(Guid prescriptionId, Guid completedBy);
        Task<IEnumerable<Prescription>> GetPatientPrescriptionsAsync(Guid patientId);
        Task<bool> ValidatePrescriptionAsync(Guid prescriptionId);
        Task<string> GeneratePrescriptionPrintAsync(Guid prescriptionId);

        Task<Prescription> GetPrescriptionWithDetailsAsync(Guid prescriptionId);
        Task<PrescriptionDetailDto> GetPrescriptionDetailAsync(Guid prescriptionId);
        Task<IEnumerable<PrescriptionDto>> SearchPrescriptionsAsync(PrescriptionSearchRequest request);

        Task<Prescription> UpdatePrescriptionDiagnosisAsync(Guid prescriptionId, string diagnosis, Guid updatedBy);
        Task<Prescription> UpdatePrescriptionInstructionsAsync(Guid prescriptionId, string instructions, Guid updatedBy);

        Task<Prescription> AddComplaintToPrescriptionAsync(
            Guid prescriptionId,
            Guid? complaintId,
            string? customComplaint,
            string? duration,
            string? severity,
            Guid updatedBy);

        Task<Prescription> AddAdviceToPrescriptionAsync(
            Guid prescriptionId,
            Guid? advisedId,
            string? customAdvice,
            Guid updatedBy);

        Task<Prescription> MarkMedicineAsDispensedAsync(
            Guid prescriptionId,
            Guid medicineId,
            string dispensedBy,
            Guid updatedBy);

        Task<bool> CancelPrescriptionAsync(Guid prescriptionId, string reason, Guid cancelledBy);
        Task<Prescription> UpdatePrescriptionAsync(Prescription prescription);
    }
}