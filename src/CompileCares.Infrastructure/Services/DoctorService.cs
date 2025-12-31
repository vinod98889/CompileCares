// File: DoctorService.cs (Updated with OPDBill support)
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Features.Doctors.DTOs;
using CompileCares.Application.Features.Doctors.Mappings;
using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Infrastructure.Data;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CompileCares.Infrastructure.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(
            ApplicationDbContext context,
            ILogger<DoctorService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorRequest request, Guid createdBy)
        {
            const string operation = "CreateDoctor";

            try
            {
                _logger.LogInformation("Starting {Operation} for registration number: {RegNumber}",
                    operation, request.RegistrationNumber);

                // Validate request
                ValidateCreateRequest(request);

                // Check for duplicate registration number
                var existingRegNumber = await _context.Doctors
                    .AsNoTracking()
                    .AnyAsync(d => d.RegistrationNumber == request.RegistrationNumber && !d.IsDeleted);

                if (existingRegNumber)
                {
                    throw new ValidationException($"Doctor with registration number '{request.RegistrationNumber}' already exists.");
                }

                // Check for duplicate mobile number
                var existingMobile = await _context.Doctors
                    .AsNoTracking()
                    .AnyAsync(d => d.Mobile == request.Mobile && !d.IsDeleted);

                if (existingMobile)
                {
                    throw new ValidationException($"Doctor with mobile number '{request.Mobile}' already exists.");
                }

                // Check for duplicate email if provided
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var existingEmail = await _context.Doctors
                        .AsNoTracking()
                        .AnyAsync(d => d.Email == request.Email && !d.IsDeleted);

                    if (existingEmail)
                    {
                        throw new ValidationException($"Doctor with email '{request.Email}' already exists.");
                    }
                }

                // Create entity using extension method
                var doctor = request.ToEntity(createdBy);

                // Save to database
                await _context.Doctors.AddAsync(doctor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created doctor: ID={DoctorId}, Name={DoctorName}",
                    doctor.Id, doctor.Name);

                // Return DTO
                return doctor.ToDto();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DatabaseException("Failed to save doctor to database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while creating the doctor.", ex);
            }
        }

        public async Task<DoctorDto> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request, Guid updatedBy)
        {
            const string operation = "UpdateDoctor";

            try
            {
                _logger.LogInformation("Starting {Operation} for doctor ID: {DoctorId}", operation, id);

                // Validate request
                ValidateUpdateRequest(request);

                // Find existing doctor (not deleted)
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (doctor == null)
                {
                    throw new NotFoundException($"Doctor with ID '{id}' not found.");
                }

                // Check for duplicate mobile number (excluding current doctor)
                var duplicateMobile = await _context.Doctors
                    .AsNoTracking()
                    .AnyAsync(d => d.Mobile == request.Mobile && d.Id != id && !d.IsDeleted);

                if (duplicateMobile)
                {
                    throw new ValidationException($"Another doctor with mobile number '{request.Mobile}' already exists.");
                }

                // Check for duplicate email (excluding current doctor)
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var duplicateEmail = await _context.Doctors
                        .AsNoTracking()
                        .AnyAsync(d => d.Email == request.Email && d.Id != id && !d.IsDeleted);

                    if (duplicateEmail)
                    {
                        throw new ValidationException($"Another doctor with email '{request.Email}' already exists.");
                    }
                }

                // Update entity using extension method
                doctor.UpdateEntityFromRequest(request, updatedBy);

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated doctor: ID={DoctorId}", doctor.Id);

                // Return updated DTO
                return doctor.ToDto();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Doctor not found during {Operation}", operation);
                throw;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for {Operation}", operation);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during {Operation}", operation);
                throw new DatabaseException("Failed to update doctor in database. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operation);
                throw new ApplicationException("An unexpected error occurred while updating the doctor.", ex);
            }
        }

        public async Task<DoctorDto> GetDoctorAsync(Guid id)
        {
            const string operation = "GetDoctor";

            try
            {
                // Get from database (not deleted)
                var doctor = await _context.Doctors
                    .AsNoTracking()
                    .Include(d => d.OPDVisits)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (doctor == null)
                {
                    throw new NotFoundException($"Doctor with ID '{id}' not found.");
                }

                _logger.LogDebug("Retrieved doctor {DoctorId} from database", id);
                return doctor.ToDto();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Doctor not found during {Operation}", operation);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation} for ID: {DoctorId}", operation, id);
                throw new ApplicationException($"Error retrieving doctor with ID '{id}'.", ex);
            }
        }

        public async Task<PagedResponse<DoctorSummaryDto>> SearchDoctorsAsync(DoctorSearchRequest request)
        {
            const string operation = "SearchDoctors";

            try
            {
                _logger.LogDebug("Starting {Operation} with search term: {SearchTerm}",
                    operation, request.SearchTerm);

                // Validate pagination
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                // Build query (excluding deleted doctors)
                var query = _context.Doctors
                    .AsNoTracking()
                    .Include(d => d.OPDVisits)
                    .Where(d => !d.IsDeleted);

                // Apply filters
                query = ApplyFilters(query, request);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply sorting and pagination
                var doctors = await ApplySortingAndPagination(query, request)
                    .ToListAsync();

                // Map to summary DTOs
                var doctorDtos = doctors.Select(d => d.ToSummaryDto()).ToList();

                // Create paged response
                var result = new PagedResponse<DoctorSummaryDto>(
                    doctorDtos,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);

                _logger.LogDebug("Found {Count} doctors matching search criteria", totalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error searching doctors. Please try again.", ex);
            }
        }

        public async Task<IEnumerable<DoctorSummaryDto>> GetAvailableDoctorsAsync()
        {
            const string operation = "GetAvailableDoctors";

            try
            {
                // Get from database (not deleted, active and available)
                var doctors = await _context.Doctors
                    .AsNoTracking()
                    .Include(d => d.OPDVisits)
                    .Where(d => !d.IsDeleted && d.IsActive && d.IsAvailable)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                // Map to DTOs
                var doctorDtos = doctors.Select(d => d.ToSummaryDto()).ToList();

                _logger.LogDebug("Found {Count} available doctors", doctorDtos.Count);
                return doctorDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error retrieving available doctors. Please try again.", ex);
            }
        }

        public async Task<DoctorStatisticsDto> GetDoctorStatisticsAsync()
        {
            const string operation = "GetDoctorStatistics";

            try
            {
                // Get all non-deleted doctors
                var doctors = await _context.Doctors
                    .AsNoTracking()
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                // Calculate statistics
                var statistics = new DoctorStatisticsDto
                {
                    TotalDoctors = doctors.Count,
                    ActiveDoctors = doctors.Count(d => d.IsActive),
                    AvailableDoctors = doctors.Count(d => d.IsAvailable),
                    VerifiedDoctors = doctors.Count(d => d.IsVerified),
                    DoctorsBySpecialization = doctors
                        .Where(d => !string.IsNullOrEmpty(d.Specialization))
                        .GroupBy(d => d.Specialization!)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    DoctorsByDepartment = doctors
                        .Where(d => !string.IsNullOrWhiteSpace(d.Department))
                        .GroupBy(d => d.Department!)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    AverageConsultationFee = doctors.Any()
                        ? Math.Round(doctors.Average(d => d.ConsultationFee), 2)
                        : 0,
                    TotalMonthlyRevenue = await CalculateTotalMonthlyRevenueAsync(),
                    TotalCommissionPaid = await CalculateTotalCommissionPaidAsync(),
                    TopPerformingDoctors = await GetTopPerformingDoctorsAsync()
                };

                _logger.LogDebug("Generated statistics for {Count} doctors", doctors.Count);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Operation}", operation);
                throw new ApplicationException("Error retrieving doctor statistics. Please try again.", ex);
            }
        }

        #region Additional Domain Methods

        public async Task VerifyDoctorAsync(Guid doctorId, Guid verifiedBy)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new NotFoundException($"Doctor with ID '{doctorId}' not found.");

            doctor.Verify(verifiedBy);
            await _context.SaveChangesAsync();
        }

        public async Task SetDoctorAvailabilityAsync(Guid doctorId, bool isAvailable, Guid updatedBy)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new NotFoundException($"Doctor with ID '{doctorId}' not found.");

            doctor.SetAvailable(isAvailable, updatedBy);
            await _context.SaveChangesAsync();
        }

        public async Task ActivateDoctorAsync(Guid doctorId, Guid activatedBy)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new NotFoundException($"Doctor with ID '{doctorId}' not found.");

            doctor.Activate(activatedBy);
            await _context.SaveChangesAsync();
        }

        public async Task DeactivateDoctorAsync(Guid doctorId, Guid deactivatedBy)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new NotFoundException($"Doctor with ID '{doctorId}' not found.");

            doctor.Deactivate(deactivatedBy);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDoctorSignatureAsync(Guid doctorId, string signaturePath, string? digitalSignature, Guid updatedBy)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new NotFoundException($"Doctor with ID '{doctorId}' not found.");

            doctor.UpdateSignature(signaturePath, digitalSignature, updatedBy);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Private Helper Methods

        private void ValidateCreateRequest(CreateDoctorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RegistrationNumber))
                throw new ValidationException("Registration number is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Doctor name is required.");

            if (string.IsNullOrWhiteSpace(request.Mobile))
                throw new ValidationException("Mobile number is required.");

            if (request.Mobile.Length < 10)
                throw new ValidationException("Mobile number must be at least 10 digits.");

            if (string.IsNullOrWhiteSpace(request.Specialization))
                throw new ValidationException("Specialization is required.");

            if (request.ConsultationFee <= 0)
                throw new ValidationException("Consultation fee must be positive.");

            if (request.YearsOfExperience < 0)
                throw new ValidationException("Years of experience cannot be negative.");
        }

        private void ValidateUpdateRequest(UpdateDoctorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Doctor name is required.");

            if (string.IsNullOrWhiteSpace(request.Mobile))
                throw new ValidationException("Mobile number is required.");

            if (request.Mobile.Length < 10)
                throw new ValidationException("Mobile number must be at least 10 digits.");

            if (string.IsNullOrWhiteSpace(request.Specialization))
                throw new ValidationException("Specialization is required.");

            if (request.ConsultationFee <= 0)
                throw new ValidationException("Consultation fee must be positive.");

            if (request.YearsOfExperience < 0)
                throw new ValidationException("Years of experience cannot be negative.");
        }

        private IQueryable<Doctor> ApplyFilters(IQueryable<Doctor> query, DoctorSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(searchTerm) ||
                    d.RegistrationNumber.ToLower().Contains(searchTerm) ||
                    (d.Specialization != null && d.Specialization.ToLower().Contains(searchTerm)) ||
                    (d.Email != null && d.Email.ToLower().Contains(searchTerm)) ||
                    d.Mobile.Contains(searchTerm));
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

            return query;
        }

        private IQueryable<Doctor> ApplySortingAndPagination(IQueryable<Doctor> query, DoctorSearchRequest request)
        {
            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(d => d.Name)
                    : query.OrderBy(d => d.Name),

                "consultationfee" => request.SortDescending
                    ? query.OrderByDescending(d => d.ConsultationFee)
                    : query.OrderBy(d => d.ConsultationFee),

                "yearsofexperience" => request.SortDescending
                    ? query.OrderByDescending(d => d.YearsOfExperience)
                    : query.OrderBy(d => d.YearsOfExperience),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(d => d.CreatedAt)
                    : query.OrderBy(d => d.CreatedAt),

                _ => query.OrderBy(d => d.Name) // Default sorting
            };

            // Apply pagination
            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        private async Task<decimal> CalculateTotalMonthlyRevenueAsync()
        {
            try
            {
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                // Calculate total revenue from OPDBills for current month
                var monthlyRevenue = await _context.OPDBills
                    .Where(b => b.BillDate.Month == currentMonth &&
                           b.BillDate.Year == currentYear &&
                           (b.Status == BillStatus.Paid || b.Status == BillStatus.PartiallyPaid))
                    .SumAsync(b => b.TotalAmount);

                return monthlyRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating monthly revenue");
                return 0;
            }
        }

        private async Task<decimal> CalculateTotalCommissionPaidAsync()
        {
            try
            {
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                // Calculate total doctor commission from bill items for current month
                // First, get all bills for current month
                var bills = await _context.OPDBills
                    .Include(b => b.BillItems)
                    .Where(b => b.BillDate.Month == currentMonth &&
                           b.BillDate.Year == currentYear &&
                           (b.Status == BillStatus.Paid || b.Status == BillStatus.PartiallyPaid))
                    .ToListAsync();

                // Sum doctor commission from all bill items
                var totalCommission = bills
                    .SelectMany(b => b.BillItems)
                    .Where(item => item.DoctorCommission.HasValue)
                    .Sum(item => item.DoctorCommission.Value);

                return totalCommission;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating total commission");
                return 0;
            }
        }

        private async Task<List<TopPerformingDoctorDto>> GetTopPerformingDoctorsAsync()
        {
            try
            {
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                // Get doctor performance data for current month
                var doctorPerformance = await _context.Doctors
                    .Where(d => !d.IsDeleted)
                    .Select(d => new
                    {
                        Doctor = d,
                        MonthlyVisits = d.OPDVisits
                            .Count(v => v.VisitDate.Month == currentMonth &&
                                   v.VisitDate.Year == currentYear),
                        MonthlyRevenue = _context.OPDBills
                            .Where(b => b.DoctorId == d.Id &&
                                   b.BillDate.Month == currentMonth &&
                                   b.BillDate.Year == currentYear &&
                                   (b.Status == BillStatus.Paid || b.Status == BillStatus.PartiallyPaid))
                            .Sum(b => (decimal?)b.TotalAmount) ?? 0,
                        MonthlyCommission = _context.OPDBills
                            .Where(b => b.DoctorId == d.Id &&
                                   b.BillDate.Month == currentMonth &&
                                   b.BillDate.Year == currentYear)
                            .SelectMany(b => b.BillItems)
                            .Where(item => item.DoctorCommission.HasValue)
                            .Sum(item => (decimal?)item.DoctorCommission.Value) ?? 0
                    })
                    .Where(x => x.MonthlyVisits > 0 || x.MonthlyRevenue > 0) // Only include doctors with activity
                    .OrderByDescending(x => x.MonthlyRevenue)
                    .Take(10)
                    .ToListAsync();

                // Map to DTO
                var topDoctors = doctorPerformance.Select(x => new TopPerformingDoctorDto
                {
                    DoctorId = x.Doctor.Id,
                    DoctorName = x.Doctor.Name,
                    Specialization = x.Doctor.Specialization ?? "Unknown",
                    TotalVisits = x.MonthlyVisits,
                    TotalRevenue = x.MonthlyRevenue,
                    TotalCommission = x.MonthlyCommission,
                    PatientSatisfactionScore = CalculatePatientSatisfactionScore(x.Doctor.Id) // Implement this separately
                }).ToList();

                return topDoctors;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting top performing doctors");
                return new List<TopPerformingDoctorDto>();
            }
        }

        private decimal CalculatePatientSatisfactionScore(Guid doctorId)
        {
            // This should be implemented based on your patient feedback/rating system
            // For now, return a placeholder value
            return 4.5m;
        }

        #endregion
    }
}