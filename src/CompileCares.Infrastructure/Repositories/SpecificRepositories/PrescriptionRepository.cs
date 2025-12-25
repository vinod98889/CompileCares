using CompileCares.Application.Interfaces;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Repositories.SpecificRepositories
{
    public class PrescriptionRepository : Repository<Prescription>, IPrescriptionRepository
    {
        public PrescriptionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Prescription?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.OPDVisit)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Dose)
                .Include(p => p.Complaints)
                    .ThenInclude(c => c.Complaint)
                .Include(p => p.AdvisedItems)
                    .ThenInclude(a => a.Advised)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Prescription?> GetWithMedicinesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Dose)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Prescription?> GetByPrescriptionNumberAsync(string prescriptionNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.PrescriptionNumber == prescriptionNumber, cancellationToken);
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.PatientId == patientId)
                .Include(p => p.Doctor)
                .Include(p => p.Medicines)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.DoctorId == doctorId)
                .Include(p => p.Patient)
                .Include(p => p.Medicines)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Prescription>> GetActivePrescriptionsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(p => p.Status == Shared.Enums.PrescriptionStatus.Active &&
                           (!p.ValidUntil.HasValue || p.ValidUntil.Value > now))
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Prescription>> GetExpiredPrescriptionsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(p => p.ValidUntil.HasValue && p.ValidUntil.Value < now)
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> PrescriptionNumberExistsAsync(string prescriptionNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(p => p.PrescriptionNumber == prescriptionNumber, cancellationToken);
        }
    }
}