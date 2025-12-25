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
}