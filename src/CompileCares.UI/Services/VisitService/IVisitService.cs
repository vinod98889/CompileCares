using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Visits.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.VisitService
{
    public interface IVisitService
    {
        Task<ApiResponse<OPDVisitDto>> CreateVisitAsync(CreateVisitRequest request);
        Task<ApiResponse<OPDVisitDto>> GetVisitAsync(Guid id);
        Task<ApiResponse<OPDVisitDto>> UpdateVisitAsync(Guid id, UpdateVisitRequest request);
        Task<ApiResponse<OPDVisitDto>> StartConsultationAsync(Guid id);
        Task<ApiResponse<OPDVisitDto>> CompleteConsultationAsync(Guid id, int? consultationDurationMinutes = null);
        Task<ApiResponse<OPDVisitDto>> CancelVisitAsync(Guid id, string reason);
        Task<ApiResponse<OPDVisitDto>> MarkAsNoShowAsync(Guid id);
        Task<ApiResponse<OPDVisitDto>> UpdateVitalsAsync(Guid id, VisitVitalsDto vitals);
        Task<ApiResponse<VisitVitalsDto>> GetVitalsAsync(Guid id);
        Task<ApiResponse<OPDVisitDto>> SetDiagnosisAsync(Guid id, string diagnosis);
        Task<ApiResponse<OPDVisitDto>> SetTreatmentPlanAsync(Guid id, string treatmentPlan);
        Task<ApiResponse<OPDVisitDto>> AddClinicalNotesAsync(Guid id, string notes);
        Task<ApiResponse<OPDVisitDto>> SetFollowUpAsync(Guid id, DateTime followUpDate, string? instructions = null);
        Task<ApiResponse<OPDVisitDto>> ReferToDoctorAsync(Guid id, Guid referredToDoctorId, string reason);
        Task<ApiResponse<List<VisitSummaryDto>>> SearchVisitsAsync(VisitSearchRequest request);
        Task<ApiResponse<List<VisitSummaryDto>>> GetTodaysVisitsAsync();
        Task<ApiResponse<VisitStatisticsDto>> GetVisitStatisticsAsync();
        Task<ApiResponse<List<DoctorScheduleDto>>> GetDoctorSchedulesAsync(DateTime? date = null);
        Task<ApiResponse<bool>> ValidateForPrescriptionAsync(Guid id);
        Task<ApiResponse<OPDVisitDto>> RestoreVisitAsync(Guid id);
    }
}
