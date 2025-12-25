using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Patients;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Services
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PatientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PatientDto> CreatePatientAsync(CreatePatientRequest request, Guid createdBy)
        {
            var patient = new Patient(
                request.Name,
                request.Title,
                request.Gender,
                request.Mobile,
                createdBy);

            if (request.DateOfBirth.HasValue)
                patient.SetDateOfBirth(request.DateOfBirth.Value, createdBy);

            if (!string.IsNullOrWhiteSpace(request.Email))
                patient.UpdateContactInfo(
                    request.Mobile,
                    request.Email,
                    request.Address,
                    request.City,
                    request.Pincode,
                    createdBy);

            if (!string.IsNullOrWhiteSpace(request.BloodGroup) ||
                !string.IsNullOrWhiteSpace(request.Allergies) ||
                !string.IsNullOrWhiteSpace(request.MedicalHistory) ||
                !string.IsNullOrWhiteSpace(request.CurrentMedications))
            {
                patient.UpdateMedicalInfo(
                    request.BloodGroup,
                    request.Allergies,
                    request.MedicalHistory,
                    request.CurrentMedications,
                    createdBy);
            }

            if (!string.IsNullOrWhiteSpace(request.EmergencyContactName) &&
                !string.IsNullOrWhiteSpace(request.EmergencyContactPhone))
            {
                patient.SetEmergencyContact(
                    request.EmergencyContactName,
                    request.EmergencyContactPhone,
                    createdBy);
            }

            if (!string.IsNullOrWhiteSpace(request.Occupation))
                patient.UpdateOccupation(request.Occupation, createdBy);

            if (request.MaritalStatus != Shared.Enums.MaritalStatus.Single)
                patient.UpdateMaritalStatus(request.MaritalStatus, createdBy);

            await _unitOfWork.Repository<Patient>().AddAsync(patient);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(patient);
        }

        public async Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientRequest request, Guid updatedBy)
        {
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(id);
            if (patient == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(Patient), id);

            patient.UpdateContactInfo(
                request.Mobile,
                request.Email,
                request.Address,
                request.City,
                request.Pincode,
                updatedBy);

            patient.UpdateMedicalInfo(
                request.BloodGroup,
                request.Allergies,
                request.MedicalHistory,
                request.CurrentMedications,
                updatedBy);

            if (!string.IsNullOrWhiteSpace(request.EmergencyContactName) &&
                !string.IsNullOrWhiteSpace(request.EmergencyContactPhone))
            {
                patient.SetEmergencyContact(
                    request.EmergencyContactName,
                    request.EmergencyContactPhone,
                    updatedBy);
            }

            if (!string.IsNullOrWhiteSpace(request.Occupation))
                patient.UpdateOccupation(request.Occupation, updatedBy);

            if (request.DateOfBirth.HasValue)
                patient.SetDateOfBirth(request.DateOfBirth.Value, updatedBy);

            patient.UpdateMaritalStatus(request.MaritalStatus, updatedBy);
            patient.UpdatePatientStatus(request.Status, updatedBy);

            await _unitOfWork.Repository<Patient>().UpdateAsync(patient);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(patient);
        }

        public async Task<PatientDto> GetPatientAsync(Guid id)
        {
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(id);
            if (patient == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(Patient), id);

            return MapToDto(patient);
        }

        public async Task<PagedResponse<PatientSummaryDto>> SearchPatientsAsync(PatientSearchRequest request)
        {
            var query = _unitOfWork.Repository<Patient>().GetQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(request.SearchTerm) ||
                    p.PatientNumber.Contains(request.SearchTerm) ||
                    p.Mobile.Contains(request.SearchTerm) ||
                    (p.Email != null && p.Email.Contains(request.SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.PatientNumber))
                query = query.Where(p => p.PatientNumber == request.PatientNumber);

            if (!string.IsNullOrWhiteSpace(request.Mobile))
                query = query.Where(p => p.Mobile == request.Mobile);

            if (!string.IsNullOrWhiteSpace(request.Name))
                query = query.Where(p => p.Name.Contains(request.Name));

            if (request.IsActive.HasValue)
                query = query.Where(p => p.IsActive == request.IsActive.Value);

            if (request.CreatedFrom.HasValue)
                query = query.Where(p => p.CreatedAt >= request.CreatedFrom.Value);

            if (request.CreatedTo.HasValue)
                query = query.Where(p => p.CreatedAt <= request.CreatedTo.Value);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "patientnumber" => request.SortDescending ? query.OrderByDescending(p => p.PatientNumber) : query.OrderBy(p => p.PatientNumber),
                "createdat" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                _ => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PatientSummaryDto
                {
                    Id = p.Id,
                    PatientNumber = p.PatientNumber,
                    Name = p.Name,
                    Age = p.Age,
                    Mobile = p.Mobile,
                    BloodGroup = p.BloodGroup,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return new PagedResponse<PatientSummaryDto>(items, request.PageNumber, request.PageSize, totalCount);
        }

        public async Task<bool> DeactivatePatientAsync(Guid id, Guid deactivatedBy)
        {
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(id);
            if (patient == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(Patient), id);

            patient.Deactivate(deactivatedBy);
            await _unitOfWork.Repository<Patient>().UpdateAsync(patient);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<PatientStatisticsDto> GetPatientStatisticsAsync()
        {
            var patients = await _unitOfWork.Repository<Patient>().GetAllAsync();

            var statistics = new PatientStatisticsDto
            {
                TotalPatients = patients.Count(),
                ActivePatients = patients.Count(p => p.IsActive),
                NewPatientsThisMonth = patients.Count(p => p.CreatedAt.Month == DateTime.UtcNow.Month &&
                                                          p.CreatedAt.Year == DateTime.UtcNow.Year),
                NewPatientsThisWeek = patients.Count(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                PatientsWithAllergies = patients.Count(p => !string.IsNullOrWhiteSpace(p.Allergies))
            };

            // Group by gender
            statistics.PatientsByGender = patients
                .GroupBy(p => p.Gender.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by age
            statistics.PatientsByAgeGroup = patients
                .Where(p => p.Age.HasValue)
                .GroupBy(p => GetAgeGroup(p.Age.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by city
            statistics.PatientsByCity = patients
                .Where(p => !string.IsNullOrWhiteSpace(p.City))
                .GroupBy(p => p.City!)
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }

        private static PatientDto MapToDto(Patient patient)
        {
            return new PatientDto
            {
                Id = patient.Id,
                PatientNumber = patient.PatientNumber,
                Name = patient.Name,
                Title = patient.Title,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                Age = patient.Age,
                MaritalStatus = patient.MaritalStatus,
                Status = patient.Status,
                Mobile = patient.Mobile,
                Email = patient.Email,
                Address = patient.Address,
                City = patient.City,
                Pincode = patient.Pincode,
                BloodGroup = patient.BloodGroup,
                Allergies = patient.Allergies,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                Occupation = patient.Occupation,
                CreatedAt = patient.CreatedAt,
                IsActive = patient.IsActive
            };
        }

        private static string GetAgeGroup(int age)
        {
            return age switch
            {
                < 18 => "0-17",
                < 30 => "18-29",
                < 40 => "30-39",
                < 50 => "40-49",
                < 60 => "50-59",
                _ => "60+"
            };
        }
    }
}