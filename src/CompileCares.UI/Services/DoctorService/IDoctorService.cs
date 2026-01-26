// IDoctorService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Doctors.DTOs;
using static CompileCares.API.Controllers.DoctorsController;

namespace CompileCares.UI.Services.DoctorService
{
    public interface IDoctorService
    {
        // Doctor CRUD Operations
        Task<ApiResponse<DoctorDto>> CreateDoctorAsync(CreateDoctorRequest request);
        Task<ApiResponse<DoctorDto>> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request);
        Task<ApiResponse<DoctorDto>> GetDoctorAsync(Guid id);

        // Doctor Lists & Search
        Task<ApiResponse<PagedResponse<DoctorSummaryDto>>> SearchDoctorsAsync(DoctorSearchRequest request);
        Task<ApiResponse<List<DoctorSummaryDto>>> GetAvailableDoctorsAsync();

        // Doctor Operations
        Task<ApiResponse<string>> VerifyDoctorAsync(Guid id);
        Task<ApiResponse<string>> SetAvailabilityAsync(Guid id, bool isAvailable);
        Task<ApiResponse<string>> ActivateDoctorAsync(Guid id);
        Task<ApiResponse<string>> DeactivateDoctorAsync(Guid id);
        Task<ApiResponse<string>> UpdateSignatureAsync(Guid id, UpdateSignatureRequest request);

        // Statistics
        Task<ApiResponse<DoctorStatisticsDto>> GetDoctorStatisticsAsync();
    }
}