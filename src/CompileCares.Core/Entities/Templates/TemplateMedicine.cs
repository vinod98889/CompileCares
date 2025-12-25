using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Pharmacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Core.Entities.Templates
{
    public class TemplateMedicine : BaseEntity
    {
        public Guid PrescriptionTemplateId { get; private set; }
        public Guid MedicineId { get; private set; }
        public Guid DoseId { get; private set; }
        public int DurationDays { get; private set; }
        public int Quantity { get; private set; }
        public string? Instructions { get; private set; }

        public virtual PrescriptionTemplate? PrescriptionTemplate { get; private set; }
        public virtual Medicine? Medicine { get; private set; }
        public virtual Dose? Dose { get; private set; }
        // Add to TemplateMedicine.cs
        private TemplateMedicine() { }

        public TemplateMedicine(
            Guid prescriptionTemplateId,
            Guid medicineId,
            Guid doseId,
            int durationDays,
            int quantity = 1,
            string? instructions = null,
            Guid? createdBy = null)
        {
            if (prescriptionTemplateId == Guid.Empty)
                throw new ArgumentException("Prescription template ID is required");

            if (medicineId == Guid.Empty)
                throw new ArgumentException("Medicine ID is required");

            if (doseId == Guid.Empty)
                throw new ArgumentException("Dose ID is required");

            if (durationDays <= 0)
                throw new ArgumentException("Duration days must be positive");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            PrescriptionTemplateId = prescriptionTemplateId;
            MedicineId = medicineId;
            DoseId = doseId;
            DurationDays = durationDays;
            Quantity = quantity;
            Instructions = instructions;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }
    }
}
