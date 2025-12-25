using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Core.Entities.Templates
{
    public class TemplateAdvised : BaseEntity
    {
        public Guid PrescriptionTemplateId { get; private set; }
        public Guid? AdvisedId { get; private set; }
        public string? CustomText { get; private set; }

        public virtual PrescriptionTemplate? PrescriptionTemplate { get; private set; }
        public virtual Advised? Advised { get; private set; }

        // Add to TemplateAdvised.cs
        private TemplateAdvised() { }

        public TemplateAdvised(
            Guid prescriptionTemplateId,
            Guid? advisedId,
            string? customText = null,
            Guid? createdBy = null)
        {
            if (prescriptionTemplateId == Guid.Empty)
                throw new ArgumentException("Prescription template ID is required");

            if (advisedId == null && string.IsNullOrWhiteSpace(customText))
                throw new ArgumentException("Either AdvisedId or CustomText is required");

            PrescriptionTemplateId = prescriptionTemplateId;
            AdvisedId = advisedId;
            CustomText = customText;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }
    }
}
