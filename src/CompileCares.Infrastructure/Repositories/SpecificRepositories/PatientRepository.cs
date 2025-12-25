using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.Patients;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Patient?> GetByPatientNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.PatientNumber == patientNumber, cancellationToken);
        }

        public async Task<Patient?> GetWithVisitsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.OPDVisits)
                .ThenInclude(v => v.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    p.PatientNumber.Contains(searchTerm) ||
                    p.Mobile.Contains(searchTerm) ||
                    (p.Email != null && p.Email.Contains(searchTerm)));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Patient>> GetActivePatientsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> PatientNumberExistsAsync(string patientNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(p => p.PatientNumber == patientNumber, cancellationToken);
        }
    }
}