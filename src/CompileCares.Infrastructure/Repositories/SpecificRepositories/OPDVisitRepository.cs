using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class OPDVisitRepository : Repository<OPDVisit>, IOPDVisitRepository
    {
        public OPDVisitRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<OPDVisit?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Prescription)
                .Include(v => v.Bill)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<OPDVisit?> GetWithBillAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Bill)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<OPDVisit?> GetWithPrescriptionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Prescription)
                .ThenInclude(p => p!.Medicines)
                .ThenInclude(m => m.Medicine)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<OPDVisit>> GetVisitsByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => v.PatientId == patientId)
                .Include(v => v.Doctor)
                .Include(v => v.Bill)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDVisit>> GetVisitsByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => v.DoctorId == doctorId)
                .Include(v => v.Patient)
                .Include(v => v.Bill)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDVisit>> GetVisitsByStatusAsync(VisitStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => v.Status == status)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDVisit>> GetTodaysVisitsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet
                .Where(v => v.VisitDate.Date == today)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .OrderBy(v => v.VisitDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDVisit>> GetVisitsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => v.VisitDate.Date >= startDate.Date && v.VisitDate.Date <= endDate.Date)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Bill)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetVisitCountByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(v => v.PatientId == patientId, cancellationToken);
        }
    }
}