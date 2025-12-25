using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;
using System;

namespace CompileCares.Core.Entities.Patients
{
    public class Patient : BaseEntity
    {
        // Basic Info
        public string PatientNumber { get; private set; }
        public string Name { get; private set; }
        public Title Title { get; private set; }
        public Gender Gender { get; private set; }
        public DateTime? DateOfBirth { get; private set; }
        public MaritalStatus MaritalStatus { get; private set; }
        public PatientStatus Status { get; private set; } = PatientStatus.Active;

        // Contact Info
        public string Mobile { get; private set; }
        public string? Email { get; private set; }
        public string? Address { get; private set; }
        public string? City { get; private set; }
        public string? Pincode { get; private set; }

        // Medical Info
        public string? BloodGroup { get; private set; }
        public string? Allergies { get; private set; }
        public string? MedicalHistory { get; private set; }
        public string? CurrentMedications { get; private set; }

        // Emergency Contact
        public string? EmergencyContactName { get; private set; }
        public string? EmergencyContactPhone { get; private set; }

        // Additional
        public string? Occupation { get; private set; }
        public bool IsActive { get; private set; }

        public virtual ICollection<OPDVisit> OPDVisits { get; private set; } = new List<OPDVisit>();

        // Computed Property
        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return null;

                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;

                if (DateOfBirth.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        // Private constructor for EF Core
        private Patient() { }

        // Main Constructor with validation
        public Patient(
            string patientNumber,
            string name,
            Title title,
            Gender gender,
            string mobile,
            Guid? createdBy = null)
        {
            ValidateInput(patientNumber, name, mobile);

            PatientNumber = patientNumber;
            Name = name;
            Title = title;
            Gender = gender;
            Mobile = mobile;
            IsActive = true;
            Status = PatientStatus.Active;

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Constructor overload with auto-generated patient number
        public Patient(
            string name,
            Title title,
            Gender gender,
            string mobile,
            Guid? createdBy = null)
            : this(GeneratePatientNumber(), name, title, gender, mobile, createdBy)
        {
        }

        // Validation helper
        private static void ValidateInput(string patientNumber, string name, string mobile)
        {
            if (string.IsNullOrWhiteSpace(patientNumber))
                throw new ArgumentException("Patient number is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Patient name is required");

            if (string.IsNullOrWhiteSpace(mobile))
                throw new ArgumentException("Mobile number is required");

            if (mobile.Length < 10)
                throw new ArgumentException("Mobile number must be at least 10 digits");
        }

        // Patient number generation
        private static string GeneratePatientNumber()
        {
            var prefix = "PAT";
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = new Random().Next(1000, 9999);
            return $"{prefix}-{datePart}-{randomPart}";
        }

        // ========== DOMAIN METHODS ==========

        // Update contact information
        public void UpdateContactInfo(
            string mobile,
            string? email = null,
            string? address = null,
            string? city = null,
            string? pincode = null,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                throw new ArgumentException("Mobile number is required");

            Mobile = mobile;
            Email = email;
            Address = address;
            City = city;
            Pincode = pincode;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update medical information
        public void UpdateMedicalInfo(
            string? bloodGroup,
            string? allergies,
            string? medicalHistory,
            string? currentMedications,
            Guid? updatedBy = null)
        {
            BloodGroup = bloodGroup;
            Allergies = allergies;
            MedicalHistory = medicalHistory;
            CurrentMedications = currentMedications;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set date of birth
        public void SetDateOfBirth(DateTime dateOfBirth, Guid? updatedBy = null)
        {
            if (dateOfBirth > DateTime.UtcNow)
                throw new ArgumentException("Date of birth cannot be in the future");

            DateOfBirth = dateOfBirth;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update marital status
        public void UpdateMaritalStatus(MaritalStatus status, Guid? updatedBy = null)
        {
            MaritalStatus = status;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update patient status
        public void UpdatePatientStatus(PatientStatus status, Guid? updatedBy = null)
        {
            Status = status;

            // Sync with IsActive
            if (status == PatientStatus.Inactive ||
                status == PatientStatus.Deceased ||
                status == PatientStatus.Transferred)
                IsActive = false;
            else if (status == PatientStatus.Active ||
                     status == PatientStatus.Discharged)
                IsActive = true;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set emergency contact
        public void SetEmergencyContact(string name, string phone, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Emergency contact name is required");

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Emergency contact phone is required");

            EmergencyContactName = name;
            EmergencyContactPhone = phone;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update occupation
        public void UpdateOccupation(string occupation, Guid? updatedBy = null)
        {
            Occupation = occupation;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Deactivate patient (soft disable, not delete)
        public void Deactivate(Guid? deactivatedBy = null)
        {
            if (!IsActive) return;

            IsActive = false;
            Status = PatientStatus.Inactive;

            if (deactivatedBy.HasValue)
                SetUpdatedBy(deactivatedBy.Value);
        }

        // Activate patient
        public void Activate(Guid? activatedBy = null)
        {
            if (IsActive) return;

            IsActive = true;
            Status = PatientStatus.Active;

            if (activatedBy.HasValue)
                SetUpdatedBy(activatedBy.Value);
        }

        // Soft delete override (combine with deactivation)
        public override void SoftDelete(Guid? deletedBy = null)
        {
            base.SoftDelete(deletedBy);
            Deactivate(deletedBy); // Also deactivate when deleted
        }

        // Restore override
        public override void Restore()
        {
            base.Restore();
            Activate(); // Also activate when restored
        }

        // Factory method for testing
        public static Patient CreateForTest(
            Guid id,
            string patientNumber,
            string name,
            Title title,
            Gender gender,
            string mobile)
        {
            var patient = new Patient(patientNumber, name, title, gender, mobile);
            patient.SetId(id); // ✅ Requires SetId method in BaseEntity
            return patient;
        }
    }
}