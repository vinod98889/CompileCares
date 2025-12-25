using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;

namespace CompileCares.Core.Entities.ClinicalMaster
{
    public class Dose : BaseEntity
    {
        // Core
        public string Code { get; private set; }        // "OD", "BD", "TDS", "1-0-1"
        public string Name { get; private set; }        // "Once Daily", "Twice Daily"

        // Status
        public bool IsActive { get; private set; }
        public int SortOrder { get; private set; }

        // Navigation
        public virtual ICollection<PrescriptionMedicine> PrescriptionMedicines { get; private set; } = new List<PrescriptionMedicine>();

        private Dose() { }

        public Dose(string code, string name, Guid? createdBy = null)
        {
            Code = code;
            Name = name;
            IsActive = true;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        public void Update(string code, string name, Guid? updatedBy = null)
        {
            Code = code;
            Name = name;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;
    }
}