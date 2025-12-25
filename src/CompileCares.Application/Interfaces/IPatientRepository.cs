using CompileCares.Application.Common.Interfaces;
using CompileCares.Core.Entities.Patients;

namespace CompileCares.Application.Interfaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task<Patient?> GetByPatientNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
        Task<Patient?> GetWithVisitsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IEnumerable<Patient>> GetActivePatientsAsync(CancellationToken cancellationToken = default);
        Task<bool> PatientNumberExistsAsync(string patientNumber, CancellationToken cancellationToken = default);
    }
}