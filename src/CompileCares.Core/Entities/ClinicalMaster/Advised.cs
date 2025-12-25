using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;

namespace CompileCares.Core.Entities.ClinicalMaster
{
    public class Advised : BaseEntity
    {
        // Core
        public string Text { get; private set; }        // "Drink warm water"
        public string? Category { get; private set; }   // "General", "Dietary"

        // Status
        public bool IsActive { get; private set; }
        public bool IsCommon { get; private set; } = true;

        // Navigation
        public virtual ICollection<PrescriptionAdvised> PrescriptionAdvisedItems { get; private set; } = new List<PrescriptionAdvised>();

        private Advised() { }

        public Advised(string text, string? category = null, Guid? createdBy = null)
        {
            Text = text;
            Category = category;
            IsActive = true;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        public void Update(string text, string? category, Guid? updatedBy = null)
        {
            Text = text;
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