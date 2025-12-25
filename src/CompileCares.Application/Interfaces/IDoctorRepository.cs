using CompileCares.Application.Common.Interfaces;
using CompileCares.Core.Entities.Doctors;

namespace CompileCares.Application.Interfaces
{
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<Doctor?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default);
        Task<Doctor?> GetWithVisitsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Doctor>> SearchDoctorsAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization, CancellationToken cancellationToken = default);
        Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default);
    }
}