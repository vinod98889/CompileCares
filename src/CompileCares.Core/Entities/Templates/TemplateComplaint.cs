using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;

namespace CompileCares.Core.Entities.Templates
{
    public class TemplateComplaint : BaseEntity
    {
        public Guid PrescriptionTemplateId { get; private set; }
        public Guid? ComplaintId { get; private set; }
        public string? CustomText { get; private set; }
        public string? Duration { get; private set; }
        public string? Severity { get; private set; }

        public virtual PrescriptionTemplate? PrescriptionTemplate { get; private set; }
        public virtual Complaint? Complaint { get; private set; }

        // ========== ADD THESE DOMAIN METHODS ==========

        // Private constructor for EF Core
        private TemplateComplaint() { }

        // Main Constructor
        public TemplateComplaint(
            Guid prescriptionTemplateId,
            Guid? complaintId,
            string? customText = null,
            string? duration = null,
            string? severity = null,
            Guid? createdBy = null)
        {
            if (prescriptionTemplateId == Guid.Empty)
                throw new ArgumentException("Prescription template ID is required");

            if (complaintId == null && string.IsNullOrWhiteSpace(customText))
                throw new ArgumentException("Either ComplaintId or CustomText is required");

            PrescriptionTemplateId = prescriptionTemplateId;
            ComplaintId = complaintId;
            CustomText = customText;
            Duration = duration;
            Severity = severity;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Update complaint details
        public void UpdateDetails(
            Guid? complaintId,
            string? customText = null,
            string? duration = null,
            string? severity = null,
            Guid? updatedBy = null)
        {
            if (complaintId == null && string.IsNullOrWhiteSpace(customText))
                throw new ArgumentException("Either ComplaintId or CustomText is required");

            ComplaintId = complaintId;
            CustomText = customText;
            Duration = duration;
            Severity = severity;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Get display text (master complaint name or custom text)
        public string GetDisplayText()
        {
            if (!string.IsNullOrWhiteSpace(CustomText))
                return CustomText;

            if (Complaint != null)
                return Complaint.Name;

            return "Unknown Complaint";
        }

        // Check if this is a custom complaint (not from master)
        public bool IsCustomComplaint()
        {
            return string.IsNullOrWhiteSpace(CustomText) == false;
        }

        // Check if this is from master complaint
        public bool IsFromMaster()
        {
            return ComplaintId.HasValue && Complaint != null;
        }

        // Factory method for testing
        public static TemplateComplaint CreateForTest(
            Guid id,
            Guid prescriptionTemplateId,
            Guid? complaintId = null,
            string? customText = null)
        {
            var templateComplaint = new TemplateComplaint(
                prescriptionTemplateId,
                complaintId,
                customText);

            templateComplaint.SetId(id);
            return templateComplaint;
        }
    }
}