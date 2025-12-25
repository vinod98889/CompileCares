using CompileCares.Application.Common.Interfaces;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;

namespace CompileCares.Application.Interfaces
{
    public interface IOPDVisitRepository : IRepository<OPDVisit>
    {
        Task<OPDVisit?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OPDVisit?> GetWithBillAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OPDVisit?> GetWithPrescriptionAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDVisit>> GetVisitsByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDVisit>> GetVisitsByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDVisit>> GetVisitsByStatusAsync(VisitStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDVisit>> GetTodaysVisitsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDVisit>> GetVisitsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<int> GetVisitCountByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}