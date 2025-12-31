using CompileCares.Application.Features.Visits.DTOs;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompileCares.Application.Services
{
    public interface IVisitService
    {
        // Core CRUD Operations
        Task<OPDVisit> CreateVisitAsync(CreateVisitRequest request, Guid createdBy);
        Task<OPDVisit> GetVisitByIdAsync(Guid visitId);
        Task<OPDVisitDto> GetVisitDetailAsync(Guid visitId);
        Task<IEnumerable<OPDVisit>> GetPatientVisitsAsync(Guid patientId);
        Task<OPDVisit> UpdateVisitAsync(Guid visitId, UpdateVisitRequest request, Guid updatedBy);
        Task<bool> DeleteVisitAsync(Guid visitId, string reason, Guid deletedBy);

        // Visit Status Operations
        Task<OPDVisit> StartConsultationAsync(Guid visitId, Guid startedBy);
        Task<OPDVisit> CompleteConsultationAsync(Guid visitId, TimeSpan? consultationDuration, Guid completedBy);
        Task<OPDVisit> CancelVisitAsync(Guid visitId, string reason, Guid cancelledBy);
        Task<OPDVisit> MarkAsNoShowAsync(Guid visitId, Guid updatedBy);

        // Clinical Data Operations
        Task<OPDVisit> UpdateChiefComplaintAsync(Guid visitId, string chiefComplaint, Guid updatedBy);
        Task<OPDVisit> UpdateVitalsAsync(Guid visitId, VisitVitalsDto vitals, Guid updatedBy);
        Task<OPDVisit> SetDiagnosisAsync(Guid visitId, string diagnosis, Guid updatedBy);
        Task<OPDVisit> SetTreatmentPlanAsync(Guid visitId, string treatmentPlan, Guid updatedBy);
        Task<OPDVisit> AddClinicalNotesAsync(Guid visitId, string notes, Guid addedBy);
        Task<OPDVisit> OrderInvestigationsAsync(Guid visitId, string investigations, Guid orderedBy);
        Task<OPDVisit> SetFollowUpAsync(Guid visitId, DateTime followUpDate, string? instructions, Guid updatedBy);
        Task<OPDVisit> ReferToDoctorAsync(Guid visitId, Guid referredToDoctorId, string reason, Guid updatedBy);

        // Search and Reporting
        Task<IEnumerable<VisitSummaryDto>> SearchVisitsAsync(VisitSearchRequest request);
        Task<IEnumerable<VisitSummaryDto>> GetDoctorVisitsAsync(Guid doctorId, DateTime date);
        Task<IEnumerable<VisitSummaryDto>> GetTodaysVisitsAsync();
        Task<VisitStatisticsDto> GetVisitStatisticsAsync();

        // Doctor Schedule
        Task<IEnumerable<DoctorScheduleDto>> GetDoctorSchedulesAsync(DateTime date);

        // Billing Integration
        Task<OPDVisit> LinkBillAsync(Guid visitId, Guid billId, Guid updatedBy);

        // Validation
        Task<bool> ValidateVisitForPrescriptionAsync(Guid visitId);

        // Get visit vitals
        Task<VisitVitalsDto> GetVisitVitalsAsync(Guid visitId);

        Task<OPDVisit> RestoreVisitAsync(Guid visitId, Guid restoredBy);
    }
}