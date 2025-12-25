using CompileCares.Application.Common.Interfaces;
using CompileCares.Core.Entities.Billing;
using CompileCares.Shared.Enums;

namespace CompileCares.Application.Interfaces
{
    public interface IBillingRepository : IRepository<OPDBill>
    {
        Task<OPDBill?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OPDBill?> GetByBillNumberAsync(string billNumber, CancellationToken cancellationToken = default);
        Task<OPDBill?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDBill>> GetBillsByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDBill>> GetBillsByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDBill>> GetBillsByStatusAsync(BillStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDBill>> GetBillsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDBill>> GetUnpaidBillsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDBill>> GetPaidBillsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalRevenueByDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<bool> BillNumberExistsAsync(string billNumber, CancellationToken cancellationToken = default);
    }
}