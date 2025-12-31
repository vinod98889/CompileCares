// File: DoctorMappingExtensions.cs
using System;
using CompileCares.Application.Features.Doctors.DTOs;
using CompileCares.Core.Entities.Doctors;

namespace CompileCares.Application.Features.Doctors.Mappings
{
    public static class DoctorMappingExtensions
    {
        public static Doctor ToEntity(this CreateDoctorRequest request, Guid createdBy)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Create doctor using the domain constructor
            var doctor = new Doctor(
                registrationNumber: request.RegistrationNumber?.Trim() ?? string.Empty,
                name: request.Name?.Trim() ?? string.Empty,
                title: request.Title,
                gender: request.Gender,
                mobile: request.Mobile?.Trim() ?? string.Empty,
                doctorType: request.DoctorType,
                specialization: request.Specialization?.Trim() ?? string.Empty,
                consultationFee: request.ConsultationFee,
                createdBy: createdBy
            );

            // Set optional properties using domain methods
            if (request.DateOfBirth.HasValue)
            {
                doctor.SetDateOfBirth(request.DateOfBirth.Value, createdBy);
            }

            if (!string.IsNullOrWhiteSpace(request.Qualification))
            {
                doctor.UpdateProfessionalInfo(
                    doctorType: request.DoctorType,
                    specialization: request.Specialization?.Trim() ?? string.Empty,
                    qualification: request.Qualification?.Trim(),
                    department: request.Department?.Trim(),
                    yearsOfExperience: request.YearsOfExperience,
                    updatedBy: createdBy
                );
            }

            if (!string.IsNullOrWhiteSpace(request.Email) ||
                !string.IsNullOrWhiteSpace(request.Address))
            {
                doctor.UpdateContactInfo(
                    mobile: request.Mobile?.Trim() ?? string.Empty,
                    email: request.Email?.Trim(),
                    address: request.Address?.Trim(),
                    city: request.City?.Trim(),
                    pincode: request.Pincode?.Trim(),
                    updatedBy: createdBy
                );
            }

            if (!string.IsNullOrWhiteSpace(request.HospitalName) ||
                !string.IsNullOrWhiteSpace(request.HospitalAddress))
            {
                doctor.UpdateWorkDetails(
                    hospitalName: request.HospitalName?.Trim(),
                    hospitalAddress: request.HospitalAddress?.Trim(),
                    consultationHours: request.ConsultationHours?.Trim(),
                    availableDays: request.AvailableDays?.Trim(),
                    updatedBy: createdBy
                );
            }

            // Set custom fees if provided
            if (request.FollowUpFee.HasValue || request.EmergencyFee.HasValue)
            {
                doctor.UpdateFees(
                    consultationFee: request.ConsultationFee,
                    followUpFee: request.FollowUpFee,
                    emergencyFee: request.EmergencyFee,
                    updatedBy: createdBy
                );
            }

            return doctor;
        }

        public static void UpdateEntityFromRequest(this Doctor doctor, UpdateDoctorRequest request, Guid updatedBy)
        {
            if (doctor == null)
                throw new ArgumentNullException(nameof(doctor));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Update basic info
            doctor.UpdateBasicInfo(
                name: request.Name?.Trim() ?? string.Empty,
                title: request.Title,
                gender: request.Gender,
                updatedBy: updatedBy
            );

            // Update professional info
            doctor.UpdateProfessionalInfo(
                doctorType: request.DoctorType,
                specialization: request.Specialization?.Trim() ?? string.Empty,
                qualification: request.Qualification?.Trim(),
                department: request.Department?.Trim(),
                yearsOfExperience: request.YearsOfExperience,
                updatedBy: updatedBy
            );

            // Update contact info
            doctor.UpdateContactInfo(
                mobile: request.Mobile?.Trim() ?? string.Empty,
                email: request.Email?.Trim(),
                address: request.Address?.Trim(),
                city: request.City?.Trim(),
                pincode: request.Pincode?.Trim(),
                updatedBy: updatedBy
            );

            // Update work details
            doctor.UpdateWorkDetails(
                hospitalName: request.HospitalName?.Trim(),
                hospitalAddress: request.HospitalAddress?.Trim(),
                consultationHours: request.ConsultationHours?.Trim(),
                availableDays: request.AvailableDays?.Trim(),
                updatedBy: updatedBy
            );

            // Update fees
            doctor.UpdateFees(
                consultationFee: request.ConsultationFee,
                followUpFee: request.FollowUpFee,
                emergencyFee: request.EmergencyFee,
                updatedBy: updatedBy
            );

            // Update status
            doctor.UpdateStatus(
                isActive: request.IsActive,
                isAvailable: request.IsAvailable,
                isVerified: request.IsVerified,
                updatedBy: updatedBy
            );

            // Set date of birth if provided
            if (request.DateOfBirth.HasValue)
            {
                doctor.SetDateOfBirth(request.DateOfBirth.Value, updatedBy);
            }
        }

        public static DoctorDto ToDto(this Doctor doctor)
        {
            if (doctor == null)
                return null;

            return new DoctorDto
            {
                Id = doctor.Id,
                RegistrationNumber = doctor.RegistrationNumber,
                Name = doctor.Name,
                Title = doctor.Title,
                Gender = doctor.Gender,
                DateOfBirth = doctor.DateOfBirth,
                Age = doctor.Age,
                DoctorType = doctor.DoctorType,
                Qualification = doctor.Qualification,
                Specialization = doctor.Specialization ?? string.Empty,
                Department = doctor.Department,
                YearsOfExperience = doctor.YearsOfExperience,
                Mobile = doctor.Mobile,
                Email = doctor.Email,
                Address = doctor.Address,
                City = doctor.City,
                Pincode = doctor.Pincode,
                HospitalName = doctor.HospitalName,
                HospitalAddress = doctor.HospitalAddress,
                ConsultationHours = doctor.ConsultationHours,
                AvailableDays = doctor.AvailableDays,
                ConsultationFee = doctor.ConsultationFee,
                FollowUpFee = doctor.FollowUpFee,
                EmergencyFee = doctor.EmergencyFee,
                IsActive = doctor.IsActive,
                IsAvailable = doctor.IsAvailable,
                IsVerified = doctor.IsVerified,
                SignaturePath = doctor.SignaturePath,
                CreatedAt = doctor.CreatedAt,
                UpdatedAt = doctor.UpdatedAt
            };
        }

        public static DoctorSummaryDto ToSummaryDto(this Doctor doctor)
        {
            if (doctor == null)
                return null;

            return new DoctorSummaryDto
            {
                Id = doctor.Id,
                RegistrationNumber = doctor.RegistrationNumber,
                Name = doctor.Name,
                Specialization = doctor.Specialization ?? string.Empty,
                Department = doctor.Department,
                Mobile = doctor.Mobile,
                ConsultationFee = doctor.ConsultationFee,
                IsAvailable = doctor.IsAvailable,
                IsActive = doctor.IsActive,
                TodaysVisits = GetTodaysVisitsCount(doctor), // This would need actual implementation
                TotalVisits = GetTotalVisitsCount(doctor)    // This would need actual implementation
            };
        }

        private static int GetTodaysVisitsCount(Doctor doctor)
        {
            // This should query your visits/OPD visits for today
            // For now, return 0 - you'll need to implement this based on your data model
            return 0;
        }

        private static int GetTotalVisitsCount(Doctor doctor)
        {
            // This should query your visits/OPD visits
            // For now, return 0 - you'll need to implement this based on your data model
            return 0;
        }
    }
}