using CompileCares.Application.Common.Interfaces;
using CompileCares.Core.Entities.Clinical;

namespace CompileCares.Application.Interfaces
{
    public interface IPrescriptionRepository : IRepository<Prescription>
    {
        Task<Prescription?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Prescription?> GetWithMedicinesAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Prescription?> GetByPrescriptionNumberAsync(string prescriptionNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<Prescription>> GetPrescriptionsByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Prescription>> GetPrescriptionsByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Prescription>> GetActivePrescriptionsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Prescription>> GetExpiredPrescriptionsAsync(CancellationToken cancellationToken = default);
        Task<bool> PrescriptionNumberExistsAsync(string prescriptionNumber, CancellationToken cancellationToken = default);
    }
}