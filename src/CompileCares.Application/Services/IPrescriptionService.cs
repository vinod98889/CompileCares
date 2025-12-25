using CompileCares.Core.Entities.Clinical;

namespace CompileCares.Application.Services
{
    public interface IPrescriptionService
    {
        Task<Prescription> CreatePrescriptionAsync(Guid patientId, Guid doctorId, Guid visitId, Guid createdBy);
        Task<Prescription> AddMedicineToPrescriptionAsync(Guid prescriptionId, Guid medicineId, Guid doseId, int durationDays, int quantity, string? instructions, Guid updatedBy);
        Task<Prescription> ApplyTemplateToPrescriptionAsync(Guid prescriptionId, Guid templateId, Guid appliedBy);
        Task<Prescription> CompletePrescriptionAsync(Guid prescriptionId, Guid completedBy);
        Task<IEnumerable<Prescription>> GetPatientPrescriptionsAsync(Guid patientId);
        Task<bool> ValidatePrescriptionAsync(Guid prescriptionId);
        Task<string> GeneratePrescriptionPrintAsync(Guid prescriptionId);
    }
}