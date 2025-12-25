using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class DoctorRepository : Repository<Doctor>, IDoctorRepository
    {
        public DoctorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Doctor?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.RegistrationNumber == registrationNumber, cancellationToken);
        }

        public async Task<Doctor?> GetWithVisitsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.OPDVisits)
                .ThenInclude(v => v.Patient)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(d =>
                    d.Name.Contains(searchTerm) ||
                    d.RegistrationNumber.Contains(searchTerm) ||
                    d.Specialization.Contains(searchTerm) ||
                    d.Mobile.Contains(searchTerm) ||
                    (d.Email != null && d.Email.Contains(searchTerm)));
            }

            return await query
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.IsActive && d.IsAvailable)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.Specialization == specialization && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(d => d.RegistrationNumber == registrationNumber, cancellationToken);
        }
    }
}