using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Interfaces;
using CompileCares.Core.Common;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories.SpecificRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace CompileCares.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Generic Repository Cache
        private readonly Dictionary<Type, object> _repositories = new();

        // Specific Repositories
        private IPatientRepository? _patients;
        private IDoctorRepository? _doctors;
        private IOPDVisitRepository? _opdVisits;
        private IPrescriptionRepository? _prescriptions;
        private IBillingRepository? _bills;
        private IMedicineRepository? _medicines;
        private IMasterRepository? _masterRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Generic Repository Accessor
        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new Repository<T>(_context);
            }
            return (IRepository<T>)_repositories[type];
        }

        // Specific Repositories Properties
        public IPatientRepository Patients =>
            _patients ??= new PatientRepository(_context);

        public IDoctorRepository Doctors =>
            _doctors ??= new DoctorRepository(_context);

        public IOPDVisitRepository OPDVisits =>
            _opdVisits ??= new OPDVisitRepository(_context);

        public IPrescriptionRepository Prescriptions =>
            _prescriptions ??= new PrescriptionRepository(_context);

        public IBillingRepository Bills =>
            _bills ??= new BillingRepository(_context);

        public IMedicineRepository Medicines =>
            _medicines ??= new MedicineRepository(_context);

        public IMasterRepository MasterRepository =>
            _masterRepository ??= new MasterRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            return result > 0;
        }

        // ✅ FIXED: Implement the interface method correctly
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        // ✅ Optional: Overload with Isolation Level
        public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress");
            }

            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
            return _transaction;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            try
            {
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // ✅ Optional: Helper method to get current transaction
        public IDbContextTransaction? GetCurrentTransaction() => _transaction;

        // ✅ Optional: Check if transaction is active
        public bool HasActiveTransaction => _transaction != null;

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}