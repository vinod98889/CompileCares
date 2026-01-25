using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Consultations.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.ConsultationService
{
    public interface IConsultationService
    {
        // Consultation Creation
        Task<ApiResponse<ConsultationResult>> CompleteConsultationAsync(CompleteConsultationRequest request);
        Task<ApiResponse<ConsultationResult>> QuickConsultationAsync(QuickConsultationRequest request);
        Task<ApiResponse<ConsultationResult>> UltraQuickConsultationAsync(UltraQuickConsultationRequest request);
        Task<ApiResponse<ConsultationResult>> ApplyTemplateConsultationAsync(TemplateConsultationRequest request);

        // Consultation Retrieval
        Task<ApiResponse<string>> GetConsultationSummaryAsync(Guid visitId);
        Task<ApiResponse<string>> PrintConsultationSlipAsync(Guid visitId);

        // Statistics & Dashboard
        Task<ApiResponse<TodaysStatsDto>> GetTodaysStatsAsync(Guid doctorId);
        Task<ApiResponse<Dictionary<string, int>>> GetCommonDiagnosesAsync(Guid doctorId, DateTime? fromDate = null);
        Task<ApiResponse<ConsultationDashboardDto>> GetConsultationDashboardAsync(Guid doctorId, DateTime? date = null);

        // Utility Methods
        Task<string> GeneratePrescriptionPrintUrl(Guid consultationId);
        Task<string> GenerateBillReceiptUrl(Guid consultationId);
    }
}
