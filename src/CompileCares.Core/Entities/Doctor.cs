using CompileCares.Core.Common;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;
using System;

namespace CompileCares.Core.Entities.Doctors
{
    public class Doctor : BaseEntity
    {
        // Basic Info
        public string RegistrationNumber { get; private set; }
        public string Name { get; private set; }
        public Title Title { get; private set; }
        public Gender Gender { get; private set; }
        public DateTime? DateOfBirth { get; private set; }
        public DoctorType DoctorType { get; private set; }

        // Professional Info
        public string? Qualification { get; private set; }
        public string? Specialization { get; private set; }
        public string? Department { get; private set; }
        public int YearsOfExperience { get; private set; }

        // Contact Info
        public string Mobile { get; private set; }
        public string? Email { get; private set; }
        public string? Address { get; private set; }
        public string? City { get; private set; }
        public string? Pincode { get; private set; }

        // Work Details
        public string? HospitalName { get; private set; }
        public string? HospitalAddress { get; private set; }
        public string? ConsultationHours { get; private set; }
        public string? AvailableDays { get; private set; }

        // Fees
        public decimal ConsultationFee { get; private set; }
        public decimal FollowUpFee { get; private set; }
        public decimal EmergencyFee { get; private set; }

        // Status
        public bool IsActive { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsVerified { get; private set; }

        // Additional
        public string? SignaturePath { get; private set; } // Path to signature image
        public string? DigitalSignature { get; private set; } // Encrypted signature

        // Add to Doctor.cs in Core layer:
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
        private Doctor() { }

        // Main Constructor
        public Doctor(
            string registrationNumber,
            string name,
            Title title,
            Gender gender,
            string mobile,
            DoctorType doctorType,
            string specialization,
            decimal consultationFee,
            Guid? createdBy = null)
        {
            ValidateInput(registrationNumber, name, mobile, consultationFee);

            RegistrationNumber = registrationNumber;
            Name = name;
            Title = title;
            Gender = gender;
            Mobile = mobile;
            DoctorType = doctorType;
            Specialization = specialization;
            ConsultationFee = consultationFee;
            FollowUpFee = consultationFee * 0.7m; // 70% of consultation fee
            EmergencyFee = consultationFee * 1.5m; // 150% of consultation fee
            IsActive = true;
            IsAvailable = true;
            IsVerified = false; // Needs admin verification

            if (createdBy.HasValue)
                SetCreatedBy(createdBy.Value);
        }

        // Validation helper
        private static void ValidateInput(
            string registrationNumber,
            string name,
            string mobile,
            decimal consultationFee)
        {
            if (string.IsNullOrWhiteSpace(registrationNumber))
                throw new ArgumentException("Registration number is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Doctor name is required");

            if (string.IsNullOrWhiteSpace(mobile))
                throw new ArgumentException("Mobile number is required");

            if (mobile.Length < 10)
                throw new ArgumentException("Mobile number must be at least 10 digits");

            if (consultationFee <= 0)
                throw new ArgumentException("Consultation fee must be positive");
        }

        // ========== DOMAIN METHODS ==========

        // Update basic info
        public void UpdateBasicInfo(
            string name,
            Title title,
            Gender gender,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Doctor name is required");

            Name = name;
            Title = title;
            Gender = gender;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update professional info
        public void UpdateProfessionalInfo(
            DoctorType doctorType,
            string specialization,
            string? qualification,
            string? department,
            int yearsOfExperience,
            Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(specialization))
                throw new ArgumentException("Specialization is required");

            DoctorType = doctorType;
            Specialization = specialization;
            Qualification = qualification;
            Department = department;
            YearsOfExperience = yearsOfExperience;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update contact info
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

        // Update work details
        public void UpdateWorkDetails(
            string? hospitalName,
            string? hospitalAddress,
            string? consultationHours,
            string? availableDays,
            Guid? updatedBy = null)
        {
            HospitalName = hospitalName;
            HospitalAddress = hospitalAddress;
            ConsultationHours = consultationHours;
            AvailableDays = availableDays;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update fees
        public void UpdateFees(
            decimal consultationFee,
            decimal? followUpFee = null,
            decimal? emergencyFee = null,
            Guid? updatedBy = null)
        {
            if (consultationFee <= 0)
                throw new ArgumentException("Consultation fee must be positive");

            ConsultationFee = consultationFee;
            FollowUpFee = followUpFee ?? consultationFee * 0.7m;
            EmergencyFee = emergencyFee ?? consultationFee * 1.5m;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Set date of birth
        public void SetDateOfBirth(DateTime dateOfBirth, Guid? updatedBy = null)
        {
            if (dateOfBirth > DateTime.UtcNow.AddYears(-25)) // Doctors must be at least 25
                throw new ArgumentException("Doctor must be at least 25 years old");

            DateOfBirth = dateOfBirth;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Update signature
        public void UpdateSignature(string? signaturePath, string? digitalSignature, Guid? updatedBy = null)
        {
            SignaturePath = signaturePath;
            DigitalSignature = digitalSignature;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Verification
        public void Verify(Guid? verifiedBy = null)
        {
            IsVerified = true;

            if (verifiedBy.HasValue)
                SetUpdatedBy(verifiedBy.Value);
        }

        public void Unverify(Guid? updatedBy = null)
        {
            IsVerified = false;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Availability
        public void SetAvailable(bool isAvailable, Guid? updatedBy = null)
        {
            // Business rule: Can't be available if not active
            if (isAvailable && !IsActive)
                throw new InvalidOperationException("Cannot set available for inactive doctor");

            IsAvailable = isAvailable;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        // Activate/Deactivate
        public void Activate(Guid? activatedBy = null)
        {
            IsActive = true;

            if (activatedBy.HasValue)
                SetUpdatedBy(activatedBy.Value);
        }

        public void Deactivate(Guid? deactivatedBy = null)
        {
            IsActive = false;
            IsAvailable = false; // Can't be available if deactivated

            if (deactivatedBy.HasValue)
                SetUpdatedBy(deactivatedBy.Value);
        }

        // Soft delete override
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

        // In Doctor.cs - Add these domain methods
        public void UpdateStatus(
            bool isActive,
            bool isAvailable,
            bool isVerified,
            Guid? updatedBy = null)
        {
            IsActive = isActive;
            IsAvailable = isAvailable;
            IsVerified = isVerified;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }

        public void SetActive(bool isActive, Guid? updatedBy = null)
        {
            IsActive = isActive;

            // Business rule: If deactivating, also mark as unavailable
            if (!isActive)
                IsAvailable = false;

            if (updatedBy.HasValue)
                SetUpdatedBy(updatedBy.Value);
        }
        // Factory method for testing
        public static Doctor CreateForTest(
            Guid id,
            string registrationNumber,
            string name,
            Title title,
            Gender gender,
            string mobile,
            DoctorType doctorType,
            string specialization,
            decimal consultationFee)
        {
            var doctor = new Doctor(
                registrationNumber,
                name,
                title,
                gender,
                mobile,
                doctorType,
                specialization,
                consultationFee);

            doctor.SetId(id);
            return doctor;
        }
    }
}