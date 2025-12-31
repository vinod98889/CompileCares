// File: IDoctorService.cs (Updated)
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Doctors.DTOs;

public interface IDoctorService
{
    Task<DoctorDto> CreateDoctorAsync(CreateDoctorRequest request, Guid createdBy);
    Task<DoctorDto> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request, Guid updatedBy);
    Task<DoctorDto> GetDoctorAsync(Guid id);
    Task<PagedResponse<DoctorSummaryDto>> SearchDoctorsAsync(DoctorSearchRequest request);
    Task<IEnumerable<DoctorSummaryDto>> GetAvailableDoctorsAsync();
    Task<DoctorStatisticsDto> GetDoctorStatisticsAsync();

    // Additional domain operations
    Task VerifyDoctorAsync(Guid doctorId, Guid verifiedBy);
    Task SetDoctorAvailabilityAsync(Guid doctorId, bool isAvailable, Guid updatedBy);
    Task ActivateDoctorAsync(Guid doctorId, Guid activatedBy);
    Task DeactivateDoctorAsync(Guid doctorId, Guid deactivatedBy);
    Task UpdateDoctorSignatureAsync(Guid doctorId, string signaturePath, string? digitalSignature, Guid updatedBy);
}