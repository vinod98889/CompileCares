using CompileCares.Application.Features.Consultations.DTOs;

namespace CompileCares.Application.Services
{
    public interface IConsultationService
    {
        Task<ConsultationResult> CompleteConsultationAsync(
            CompleteConsultationRequest request,
            Guid performedBy,
            CancellationToken cancellationToken = default);

        Task<ConsultationResult> QuickConsultationAsync(
            Guid patientId,
            Guid doctorId,
            string chiefComplaint,
            string diagnosis,
            List<Guid> commonMedicineIds, // Predefined common medicines
            Guid performedBy,
            CancellationToken cancellationToken = default);

        Task<ConsultationResult> ApplyTemplateConsultationAsync(
            Guid patientId,
            Guid doctorId,
            Guid templateId,
            Guid performedBy,
            CancellationToken cancellationToken = default);

        Task<string> GenerateConsultationSummaryAsync(Guid visitId, CancellationToken cancellationToken = default);
        Task<string> PrintCombinedSlipAsync(Guid visitId, CancellationToken cancellationToken = default);

        // Statistics
        Task<int> GetTodaysConsultationsCountAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<decimal> GetTodaysRevenueAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, int>> GetCommonDiagnosesAsync(Guid doctorId, DateTime fromDate, CancellationToken cancellationToken = default);
    }
}