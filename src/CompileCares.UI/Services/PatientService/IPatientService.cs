using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.PatientService
{
    public interface IPatientService
    {
        // Patient CRUD Operations
        Task<ApiResponse<PatientDto>> CreatePatientAsync(CreatePatientRequest request);
        Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientRequest request);
        Task<ApiResponse<PatientDto>> GetPatientAsync(Guid id);
        Task<ApiResponse<bool>> DeactivatePatientAsync(Guid id);

        // Patient Search & Lists
        Task<ApiResponse<PagedResponse<PatientSummaryDto>>> SearchPatientsAsync(PatientSearchRequest request);
        Task<ApiResponse<List<PatientSummaryDto>>> GetActivePatientsAsync();

        // Quick Operations
        Task<ApiResponse<PatientDto>> QuickCreatePatientAsync(PatientQuickCreateRequest request);
        Task<ApiResponse<PatientDto>> GetPatientByMobileAsync(string mobile);
        Task<ApiResponse<bool>> CheckPatientExistsAsync(string mobile);

        // Statistics
        Task<ApiResponse<PatientStatisticsDto>> GetPatientStatisticsAsync();
    }
}
