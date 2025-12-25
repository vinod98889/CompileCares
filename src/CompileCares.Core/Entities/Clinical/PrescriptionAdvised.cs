using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;

namespace CompileCares.Core.Entities.Clinical
{
    public class PrescriptionAdvised : BaseEntity
    {
        public Guid PrescriptionId { get; private set; }
        public Guid? AdvisedId { get; private set; }
        public string? CustomAdvice { get; private set; }

        public virtual Prescription? Prescription { get; private set; }
        public virtual Advised? Advised { get; private set; }

        // Private constructor for EF Core
        private PrescriptionAdvised() { }

        // Public constructor
        public PrescriptionAdvised(
            Guid prescriptionId,
            Guid? advisedId,
            string? customAdvice = null)
        {
            PrescriptionId = prescriptionId;
            AdvisedId = advisedId;
            CustomAdvice = customAdvice;
        }
    }
}