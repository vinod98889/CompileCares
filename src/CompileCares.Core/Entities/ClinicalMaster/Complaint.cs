using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;

namespace CompileCares.Core.Entities.ClinicalMaster
{
    public class Complaint : BaseEntity
    {
        // Core
        public string Name { get; private set; }        // "Fever", "Cough"
        public string? Category { get; private set; }   // "General", "Respiratory"

        // Status
        public bool IsActive { get; private set; }
        public bool IsCommon { get; private set; } = true;

        // Navigation
        public virtual ICollection<PatientComplaint> PatientComplaints { get; private set; } = new List<PatientComplaint>();

        private Complaint() { }

        public Complaint(string name, string? category = null, Guid? createdBy = null)
        {
            Name = name;
            Category = category;
            IsActive = true;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        public void Update(string name, string? category, Guid? updatedBy = null)
        {
            Name = name;
            Category = category;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        public void MarkAsCommon() => IsCommon = true;
        public void MarkAsUncommon() => IsCommon = false;
        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;
    }
}