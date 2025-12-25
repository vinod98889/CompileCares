using CompileCares.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompileCares.Core.Entities.Templates
{
    public class PrescriptionTemplate : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public string? Category { get; private set; }
        public Guid? DoctorId { get; private set; }
        public bool IsPublic { get; private set; }

        public virtual ICollection<TemplateComplaint> Complaints { get; private set; } = new List<TemplateComplaint>();
        public virtual ICollection<TemplateMedicine> Medicines { get; private set; } = new List<TemplateMedicine>();
        public virtual ICollection<TemplateAdvised> AdvisedItems { get; private set; } = new List<TemplateAdvised>();

        // ========== ADD THESE DOMAIN METHODS ==========

        // Private constructor for EF Core
        private PrescriptionTemplate() { }

        // Main Constructor
        public PrescriptionTemplate(
            string name,
            Guid? doctorId = null,
            string? description = null,
            string? category = null,
            bool isPublic = false,
            Guid? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name is required");

            Name = name;
            Description = description;
            Category = category;
            DoctorId = doctorId;
            IsPublic = isPublic;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Update basic info
        public void UpdateBasicInfo(
            string name,
            string? description = null,
            string? category = null,
            bool? isPublic = null,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name is required");

            Name = name;
            Description = description;
            Category = category;

            if (isPublic.HasValue)
                IsPublic = isPublic.Value;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Add complaint to template
        public void AddComplaint(
            Guid? complaintId,
            string? customText = null,
            string? duration = null,
            string? severity = null,
            Guid? addedBy = null)
        {
            if (complaintId == null && string.IsNullOrWhiteSpace(customText))
                throw new ArgumentException("Either ComplaintId or CustomText is required");

            var templateComplaint = new TemplateComplaint(
                Id,                    // prescriptionTemplateId
                complaintId,           // complaintId
                customText,            // customText
                duration,              // duration
                severity,              // severity
                addedBy);              // createdBy

            Complaints.Add(templateComplaint);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Add medicine to template
        public void AddMedicine(
            Guid medicineId,
            Guid doseId,
            int durationDays,
            int quantity = 1,
            string? instructions = null,
            Guid? addedBy = null)
        {
            if (medicineId == Guid.Empty)
                throw new ArgumentException("Medicine ID is required");

            if (doseId == Guid.Empty)
                throw new ArgumentException("Dose ID is required");

            if (durationDays <= 0)
                throw new ArgumentException("Duration days must be positive");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            var templateMedicine = new TemplateMedicine(
               Id,                    // prescriptionTemplateId
               medicineId,            // medicineId
               doseId,                // doseId
               durationDays,          // durationDays
               quantity,              // quantity
               instructions,          // instructions
               addedBy);              // createdBy

            Medicines.Add(templateMedicine);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Add advised item to template
        public void AddAdvised(
            Guid? advisedId,
            string? customText = null,
            Guid? addedBy = null)
        {
            if (advisedId == null && string.IsNullOrWhiteSpace(customText))
                throw new ArgumentException("Either AdvisedId or CustomText is required");

            var templateAdvised = new TemplateAdvised(
                Id,                    // prescriptionTemplateId
                advisedId,             // advisedId
                customText,            // customText
                addedBy);              // createdBy

            AdvisedItems.Add(templateAdvised);

            if (addedBy.HasValue)
                SetUpdatedBy(addedBy.Value);
        }

        // Remove complaint from template
        public void RemoveComplaint(Guid templateComplaintId, Guid? removedBy = null)
        {
            var complaint = Complaints.FirstOrDefault(c => c.Id == templateComplaintId);
            if (complaint != null)
            {
                Complaints.Remove(complaint);

                if (removedBy.HasValue)
                    SetUpdatedBy(removedBy.Value);
            }
        }

        // Remove medicine from template
        public void RemoveMedicine(Guid templateMedicineId, Guid? removedBy = null)
        {
            var medicine = Medicines.FirstOrDefault(m => m.Id == templateMedicineId);
            if (medicine != null)
            {
                Medicines.Remove(medicine);

                if (removedBy.HasValue)
                    SetUpdatedBy(removedBy.Value);
            }
        }

        // Remove advised item from template
        public void RemoveAdvised(Guid templateAdvisedId, Guid? removedBy = null)
        {
            var advised = AdvisedItems.FirstOrDefault(a => a.Id == templateAdvisedId);
            if (advised != null)
            {
                AdvisedItems.Remove(advised);

                if (removedBy.HasValue)
                    SetUpdatedBy(removedBy.Value);
            }
        }

        // Clone template (create a copy)
        public PrescriptionTemplate Clone(string newName, Guid? clonedBy = null)
        {
            var clonedTemplate = new PrescriptionTemplate(
                newName,
                DoctorId,
                Description,
                Category,
                IsPublic,
                clonedBy);

            // Clone complaints
            foreach (var complaint in Complaints)
            {
                clonedTemplate.AddComplaint(
                    complaint.ComplaintId,
                    complaint.CustomText,
                    complaint.Duration,
                    complaint.Severity);
            }

            // Clone medicines
            foreach (var medicine in Medicines)
            {
                clonedTemplate.AddMedicine(
                    medicine.MedicineId,
                    medicine.DoseId,
                    medicine.DurationDays,
                    medicine.Quantity,
                    medicine.Instructions);
            }

            // Clone advised items
            foreach (var advised in AdvisedItems)
            {
                clonedTemplate.AddAdvised(
                    advised.AdvisedId,
                    advised.CustomText);
            }

            return clonedTemplate;
        }

        // Check if template belongs to a doctor
        public bool BelongsToDoctor(Guid doctorId)
        {
            return DoctorId.HasValue && DoctorId.Value == doctorId;
        }

        // Check if template can be used by doctor
        public bool CanBeUsedBy(Guid doctorId)
        {
            return IsPublic || BelongsToDoctor(doctorId);
        }

        // Get template summary
        public string GetSummary()
        {
            return $"{Name}: {Medicines.Count} medicines, {Complaints.Count} complaints, {AdvisedItems.Count} advice items";
        }

        // Factory method for testing
        public static PrescriptionTemplate CreateForTest(
            Guid id,
            string name,
            Guid? doctorId = null)
        {
            var template = new PrescriptionTemplate(name, doctorId);
            template.SetId(id);
            return template;
        }
    }
}