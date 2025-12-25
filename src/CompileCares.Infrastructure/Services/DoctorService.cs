using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Features.Doctors.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.Doctors;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorRequest request, Guid createdBy)
        {
            var doctor = new Doctor(
                request.RegistrationNumber,
                request.Name,
                request.Title,
                request.Gender,
                request.Mobile,
                request.DoctorType,
                request.Specialization,
                request.ConsultationFee,
                createdBy);

            if (request.DateOfBirth.HasValue)
                doctor.SetDateOfBirth(request.DateOfBirth.Value, createdBy);

            doctor.UpdateProfessionalInfo(
                request.DoctorType,
                request.Specialization,
                request.Qualification,
                request.Department,
                request.YearsOfExperience,
                createdBy);

            doctor.UpdateContactInfo(
                request.Mobile,
                request.Email,
                request.Address,
                request.City,
                request.Pincode,
                createdBy);

            if (!string.IsNullOrWhiteSpace(request.HospitalName) ||
                !string.IsNullOrWhiteSpace(request.HospitalAddress) ||
                !string.IsNullOrWhiteSpace(request.ConsultationHours) ||
                !string.IsNullOrWhiteSpace(request.AvailableDays))
            {
                doctor.UpdateWorkDetails(
                    request.HospitalName,
                    request.HospitalAddress,
                    request.ConsultationHours,
                    request.AvailableDays,
                    createdBy);
            }

            doctor.UpdateFees(
                request.ConsultationFee,
                request.FollowUpFee,
                request.EmergencyFee,
                createdBy);

            await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request, Guid updatedBy)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(id);
            if (doctor == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(Doctor), id);

            doctor.UpdateBasicInfo(
                request.Name,
                request.Title,
                request.Gender,
                updatedBy);

            if (request.DateOfBirth.HasValue)
                doctor.SetDateOfBirth(request.DateOfBirth.Value, updatedBy);

            doctor.UpdateProfessionalInfo(
                request.DoctorType,
                request.Specialization,
                request.Qualification,
                request.Department,
                request.YearsOfExperience,
                updatedBy);

            doctor.UpdateContactInfo(
                request.Mobile,
                request.Email,
                request.Address,
                request.City,
                request.Pincode,
                updatedBy);

            doctor.UpdateWorkDetails(
                request.HospitalName,
                request.HospitalAddress,
                request.ConsultationHours,
                request.AvailableDays,
                updatedBy);

            doctor.UpdateFees(
                request.ConsultationFee,
                request.FollowUpFee,
                request.EmergencyFee,
                updatedBy);

            doctor.UpdateStatus(
                request.IsActive,
                request.IsAvailable,
                request.IsVerified
                );

            await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> GetDoctorAsync(Guid id)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(id);
            if (doctor == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(Doctor), id);

            return MapToDto(doctor);
        }

        public async Task<PagedResponse<DoctorSummaryDto>> SearchDoctorsAsync(DoctorSearchRequest request)
        {
            var query = _unitOfWork.Repository<Doctor>().GetQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(d =>
                    d.Name.Contains(request.SearchTerm) ||
                    d.RegistrationNumber.Contains(request.SearchTerm) ||
                    d.Specialization.Contains(request.SearchTerm) ||
                    d.Mobile.Contains(request.SearchTerm) ||
                    (d.Email != null && d.Email.Contains(request.SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
                query = query.Where(d => d.RegistrationNumber == request.RegistrationNumber);

            if (!string.IsNullOrWhiteSpace(request.Specialization))
                query = query.Where(d => d.Specialization == request.Specialization);

            if (!string.IsNullOrWhiteSpace(request.Department))
                query = query.Where(d => d.Department == request.Department);

            if (request.IsAvailable.HasValue)
                query = query.Where(d => d.IsAvailable == request.IsAvailable.Value);

            if (request.IsActive.HasValue)
                query = query.Where(d => d.IsActive == request.IsActive.Value);

            if (request.IsVerified.HasValue)
                query = query.Where(d => d.IsVerified == request.IsVerified.Value);

            if (request.MinConsultationFee.HasValue)
                query = query.Where(d => d.ConsultationFee >= request.MinConsultationFee.Value);

            if (request.MaxConsultationFee.HasValue)
                query = query.Where(d => d.ConsultationFee <= request.MaxConsultationFee.Value);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
                "specialization" => request.SortDescending ? query.OrderByDescending(d => d.Specialization) : query.OrderBy(d => d.Specialization),
                "consultationfee" => request.SortDescending ? query.OrderByDescending(d => d.ConsultationFee) : query.OrderBy(d => d.ConsultationFee),
                _ => request.SortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and select
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DoctorSummaryDto
                {
                    Id = d.Id,
                    RegistrationNumber = d.RegistrationNumber,
                    Name = d.Name,
                    Specialization = d.Specialization,
                    Department = d.Department,
                    Mobile = d.Mobile,
                    ConsultationFee = d.ConsultationFee,
                    IsAvailable = d.IsAvailable,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            return new PagedResponse<DoctorSummaryDto>(items, request.PageNumber, request.PageSize, totalCount);
        }

        public async Task<IEnumerable<DoctorSummaryDto>> GetAvailableDoctorsAsync()
        {
            var doctors = await _unitOfWork.Repository<Doctor>()
                .FindAsync(d => d.IsActive && d.IsAvailable);

            return doctors.Select(d => new DoctorSummaryDto
            {
                Id = d.Id,
                RegistrationNumber = d.RegistrationNumber,
                Name = d.Name,
                Specialization = d.Specialization,
                Department = d.Department,
                Mobile = d.Mobile,
                ConsultationFee = d.ConsultationFee,
                IsAvailable = d.IsAvailable,
                IsActive = d.IsActive
            }).ToList();
        }

        public async Task<DoctorStatisticsDto> GetDoctorStatisticsAsync()
        {
            var doctors = await _unitOfWork.Repository<Doctor>().GetAllAsync();
            var visits = await _unitOfWork.Repository<OPDVisit>().GetAllAsync();

            var statistics = new DoctorStatisticsDto
            {
                TotalDoctors = doctors.Count(),
                ActiveDoctors = doctors.Count(d => d.IsActive),
                AvailableDoctors = doctors.Count(d => d.IsAvailable),
                VerifiedDoctors = doctors.Count(d => d.IsVerified),
                AverageConsultationFee = doctors.Any() ? doctors.Average(d => d.ConsultationFee) : 0
            };

            // Group by specialization
            statistics.DoctorsBySpecialization = doctors
                .GroupBy(d => d.Specialization)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by department
            statistics.DoctorsByDepartment = doctors
                .Where(d => !string.IsNullOrWhiteSpace(d.Department))
                .GroupBy(d => d.Department!)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate top performing doctors (simplified)
            var doctorVisits = visits
                .GroupBy(v => v.DoctorId)
                .Select(g => new
                {
                    DoctorId = g.Key,
                    TotalVisits = g.Count()
                })
                .OrderByDescending(x => x.TotalVisits)
                .Take(5)
                .ToList();

            statistics.TopPerformingDoctors = doctorVisits
                .Join(doctors,
                    dv => dv.DoctorId,
                    d => d.Id,
                    (dv, d) => new TopPerformingDoctorDto
                    {
                        DoctorId = d.Id,
                        DoctorName = d.Name,
                        Specialization = d.Specialization,
                        TotalVisits = dv.TotalVisits
                    })
                .ToList();

            return statistics;
        }

        private static DoctorDto MapToDto(Doctor doctor)
        {
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
                Specialization = doctor.Specialization,
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
    }
}