using CompileCares.Application.Common.Interfaces;
using CompileCares.Core.Entities.Pharmacy;

namespace CompileCares.Application.Interfaces
{
    public interface IMedicineRepository : IRepository<Medicine>
    {
        Task<Medicine?> GetWithPrescriptionsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IEnumerable<Medicine>> GetMedicinesByCategoryAsync(string category, CancellationToken cancellationToken = default);
        Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Medicine>> GetExpiredMedicinesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Medicine>> GetActiveMedicinesAsync(CancellationToken cancellationToken = default);
        Task<bool> MedicineNameExistsAsync(string name, CancellationToken cancellationToken = default);
        Task<int> GetTotalStockValueAsync(CancellationToken cancellationToken = default);
    }
}