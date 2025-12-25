using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Master;
using CompileCares.Core.Entities.Templates;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class MasterRepository : IMasterRepository
    {
        private readonly ApplicationDbContext _context;

        public MasterRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // OPD Item Master
        public async Task<IEnumerable<OPDItemMaster>> GetOPDItemsByTypeAsync(string itemType, CancellationToken cancellationToken = default)
        {
            return await _context.OPDItemMasters
                .Where(i => i.ItemType == itemType && i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDItemMaster>> GetActiveOPDItemsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.OPDItemMasters
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemType)
                .ThenBy(i => i.ItemName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OPDItemMaster>> GetLowStockOPDItemsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.OPDItemMasters
                .Where(i => i.IsConsumable && i.IsActive && i.CurrentStock <= (i.ReorderLevel ?? 0))
                .OrderBy(i => i.CurrentStock)
                .ToListAsync(cancellationToken);
        }

        // Complaints
        public async Task<IEnumerable<Complaint>> GetComplaintsByCategoryAsync(string? category, CancellationToken cancellationToken = default)
        {
            var query = _context.Complaints.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(c => c.Category == category);
            }

            return await query
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Complaint>> GetCommonComplaintsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Complaints
                .Where(c => c.IsActive && c.IsCommon)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        // Advised Items
        public async Task<IEnumerable<Advised>> GetAdvisedItemsByCategoryAsync(string? category, CancellationToken cancellationToken = default)
        {
            var query = _context.AdvisedItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(a => a.Category == category);
            }

            return await query
                .Where(a => a.IsActive)
                .OrderBy(a => a.Text)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Advised>> GetCommonAdvisedItemsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.AdvisedItems
                .Where(a => a.IsActive && a.IsCommon)
                .OrderBy(a => a.Text)
                .ToListAsync(cancellationToken);
        }

        // Doses
        public async Task<IEnumerable<Dose>> GetAllDosesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Doses
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Dose>> GetActiveDosesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Doses
                .Where(d => d.IsActive)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        // Prescription Templates
        public async Task<IEnumerable<PrescriptionTemplate>> GetTemplatesByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            return await _context.PrescriptionTemplates
                .Where(t => (t.DoctorId == doctorId || t.IsPublic) && !t.IsDeleted)
                .Include(t => t.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(t => t.Complaints)
                    .ThenInclude(c => c.Complaint)
                .Include(t => t.AdvisedItems)
                    .ThenInclude(a => a.Advised)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<PrescriptionTemplate>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PrescriptionTemplates
                .Where(t => t.IsPublic && !t.IsDeleted)
                .Include(t => t.Medicines)
                    .ThenInclude(m => m.Medicine)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
    }
}