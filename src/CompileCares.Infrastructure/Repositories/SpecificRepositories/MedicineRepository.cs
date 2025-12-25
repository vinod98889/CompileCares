using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.Pharmacy;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class MedicineRepository : Repository<Medicine>, IMedicineRepository
    {
        public MedicineRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Medicine?> GetWithPrescriptionsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.PrescriptionMedicines)
                    .ThenInclude(pm => pm.Prescription)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m =>
                    m.Name.Contains(searchTerm) ||
                    (m.GenericName != null && m.GenericName.Contains(searchTerm)) ||
                    (m.BrandName != null && m.BrandName.Contains(searchTerm)) ||
                    (m.Composition != null && m.Composition.Contains(searchTerm)));
            }

            return await query
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Medicine>> GetMedicinesByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.Category == category && m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.IsActive && m.CurrentStock <= m.ReorderLevel)
                .OrderBy(m => m.CurrentStock)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Medicine>> GetExpiredMedicinesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value < now)
                .OrderBy(m => m.ExpiryDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Medicine>> GetActiveMedicinesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> MedicineNameExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(m => m.Name == name, cancellationToken);
        }

        public async Task<int> GetTotalStockValueAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.IsActive)
                .SumAsync(m => m.CurrentStock, cancellationToken);
        }
    }
}