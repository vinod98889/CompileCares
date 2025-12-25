using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Patients.DTOs;

public interface IPatientService
{
    Task<PatientDto> CreatePatientAsync(CreatePatientRequest request, Guid createdBy);
    Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientRequest request, Guid updatedBy);
    Task<PatientDto> GetPatientAsync(Guid id);
    Task<PagedResponse<PatientSummaryDto>> SearchPatientsAsync(PatientSearchRequest request);
    Task<bool> DeactivatePatientAsync(Guid id, Guid deactivatedBy);
    Task<PatientStatisticsDto> GetPatientStatisticsAsync();
}