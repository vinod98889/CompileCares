using CompileCares.Application.Interfaces;
using CompileCares.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace CompileCares.Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Generic Repository
        IRepository<T> Repository<T>() where T : BaseEntity;
        DbContext GetDbContext();
        // Specific Repositories
        IPatientRepository Patients { get; }
        IDoctorRepository Doctors { get; }
        IOPDVisitRepository OPDVisits { get; }
        IPrescriptionRepository Prescriptions { get; }
        IBillingRepository Bills { get; }
        IMedicineRepository Medicines { get; }
        IMasterRepository MasterRepository { get; }  // Add this

        // Transaction methods
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
        Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
    }
}