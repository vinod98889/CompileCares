using CompileCares.Core.Common;
using CompileCares.Core.Entities.ClinicalMaster;

namespace CompileCares.Core.Entities.Clinical
{
    public class PatientComplaint : BaseEntity
    {
        public Guid PrescriptionId { get; private set; }
        public Guid? ComplaintId { get; private set; }
        public string? CustomComplaint { get; private set; }
        public string? Duration { get; private set; }
        public string? Severity { get; private set; }

        public virtual Prescription? Prescription { get; private set; }
        public virtual Complaint? Complaint { get; private set; }

        // Private constructor for EF Core
        private PatientComplaint() { }

        // Public constructor
        public PatientComplaint(
            Guid prescriptionId,
            Guid? complaintId,
            string? customComplaint = null,
            string? duration = null,
            string? severity = null)
        {
            PrescriptionId = prescriptionId;
            ComplaintId = complaintId;
            CustomComplaint = customComplaint;
            Duration = duration;
            Severity = severity;
        }
    }
}