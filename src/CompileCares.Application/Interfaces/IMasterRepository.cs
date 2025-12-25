using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Master;
using CompileCares.Core.Entities.Templates;

namespace CompileCares.Application.Interfaces
{
    public interface IMasterRepository
    {
        // OPD Item Master
        Task<IEnumerable<OPDItemMaster>> GetOPDItemsByTypeAsync(string itemType, CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDItemMaster>> GetActiveOPDItemsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OPDItemMaster>> GetLowStockOPDItemsAsync(CancellationToken cancellationToken = default);

        // Complaints
        Task<IEnumerable<Complaint>> GetComplaintsByCategoryAsync(string? category, CancellationToken cancellationToken = default);
        Task<IEnumerable<Complaint>> GetCommonComplaintsAsync(CancellationToken cancellationToken = default);

        // Advised Items
        Task<IEnumerable<Advised>> GetAdvisedItemsByCategoryAsync(string? category, CancellationToken cancellationToken = default);
        Task<IEnumerable<Advised>> GetCommonAdvisedItemsAsync(CancellationToken cancellationToken = default);

        // Doses
        Task<IEnumerable<Dose>> GetAllDosesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Dose>> GetActiveDosesAsync(CancellationToken cancellationToken = default);

        // Prescription Templates
        Task<IEnumerable<PrescriptionTemplate>> GetTemplatesByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PrescriptionTemplate>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default);
    }
}