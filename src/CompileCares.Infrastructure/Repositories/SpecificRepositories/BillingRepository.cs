using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.Billing;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class BillingRepository : Repository<OPDBill>, IBillingRepository
    {
        public BillingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<OPDBill?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(b => b.BillItems)
                    .ThenInclude(i => i.OPDItemMaster)
                .Include(b => b.Patient)
                .Include(b => b.Doctor)
                .Include(b => b.OPDVisit)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<OPDBill?> GetByBillNumberAsync(string billNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(b => b.BillNumber == billNumber, cancellationToken);
        }

        public async Task<OPDBill?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(b => b.OPDVisitId == visitId, cancellationToken);
        }

        public async Task<IEnumerable<OPDBill>> GetBillsByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.PatientId == patientId)
                .Include(b => b.Doctor)
                .Include(b => b.OPDVisit)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDBill>> GetBillsByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.DoctorId == doctorId)
                .Include(b => b.Patient)
                .Include(b => b.OPDVisit)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDBill>> GetBillsByStatusAsync(BillStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.Status == status)
                .Include(b => b.Patient)
                .Include(b => b.Doctor)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDBill>> GetBillsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.BillDate.Date >= startDate.Date && b.BillDate.Date <= endDate.Date)
                .Include(b => b.Patient)
                .Include(b => b.Doctor)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDBill>> GetUnpaidBillsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.Status == BillStatus.Generated || b.Status == BillStatus.PartiallyPaid)
                .Include(b => b.Patient)
                .Include(b => b.Doctor)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDBill>> GetPaidBillsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.Status == BillStatus.Paid && b.BillDate.Date == date.Date)
                .Include(b => b.Patient)
                .Include(b => b.Doctor)
                .OrderBy(b => b.BillDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetTotalRevenueByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.Status == BillStatus.Paid && b.BillDate.Date == date.Date)
                .SumAsync(b => b.TotalAmount, cancellationToken);
        }

        public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(b => b.Status == BillStatus.Paid &&
                           b.BillDate.Date >= startDate.Date && b.BillDate.Date <= endDate.Date)
                .SumAsync(b => b.TotalAmount, cancellationToken);
        }

        public async Task<bool> BillNumberExistsAsync(string billNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(b => b.BillNumber == billNumber, cancellationToken);
        }
    }
}