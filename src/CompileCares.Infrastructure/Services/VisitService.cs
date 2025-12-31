using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Features.Visits.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Patients;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CompileCares.Infrastructure.Services
{
    public class VisitService : IVisitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VisitService> _logger;

        public VisitService(
            IUnitOfWork unitOfWork,
            ILogger<VisitService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OPDVisit> CreateVisitAsync(CreateVisitRequest request, Guid createdBy)
        {
            try
            {
                _logger.LogInformation("Creating new visit for patient {PatientId} with doctor {DoctorId}",
                    request.PatientId, request.DoctorId);

                // Validate patient exists
                var patient = await _unitOfWork.Repository<Patient>()
                    .GetByIdAsync(request.PatientId);
                if (patient == null)
                    throw new NotFoundException(nameof(Patient), request.PatientId);

                // Validate doctor exists and is active
                var doctor = await _unitOfWork.Repository<Doctor>()
                    .GetByIdAsync(request.DoctorId);
                if (doctor == null)
                    throw new NotFoundException(nameof(Doctor), request.DoctorId);
                if (!doctor.IsActive)
                    throw new ValidationException($"Doctor {doctor.Name} is not active");

                // Check for existing active visit for same patient today
                var today = DateTime.UtcNow.Date;
                var existingVisit = await _unitOfWork.Repository<OPDVisit>()
                    .GetQueryable()
                    .Where(v => v.PatientId == request.PatientId &&
                                v.VisitDate.Date == today &&
                                (v.Status == VisitStatus.CheckedIn || v.Status == VisitStatus.InProgress))
                    .FirstOrDefaultAsync();

                if (existingVisit != null)
                    throw new ValidationException($"Patient already has an active visit today (Visit #: {existingVisit.VisitNumber})");

                // Create visit
                var visit = new OPDVisit(request.PatientId, request.DoctorId, request.VisitType, createdBy);

                // Set chief complaint if provided
                if (!string.IsNullOrWhiteSpace(request.ChiefComplaint))
                {
                    visit.UpdateChiefComplaint(request.ChiefComplaint, null, null, null, createdBy);
                }

                await _unitOfWork.Repository<OPDVisit>().AddAsync(visit);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Visit created successfully: {VisitNumber}", visit.VisitNumber);
                return visit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating visit for patient {PatientId}", request.PatientId);
                throw;
            }
        }

        public async Task<OPDVisit> GetVisitByIdAsync(Guid visitId)
        {
            var visit = await _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => v.Id == visitId && !v.IsDeleted)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.ReferredToDoctor)
                .Include(v => v.Prescription)
                .Include(v => v.Bill)
                .FirstOrDefaultAsync();

            if (visit == null)
                throw new NotFoundException(nameof(OPDVisit), visitId);

            return visit;
        }

        public async Task<OPDVisitDto> GetVisitDetailAsync(Guid visitId)
        {
            var visit = await GetVisitByIdAsync(visitId);
            return MapToOPDVisitDto(visit);
        }

        public async Task<IEnumerable<OPDVisit>> GetPatientVisitsAsync(Guid patientId)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), patientId);

            var visits = await _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => v.PatientId == patientId && !v.IsDeleted)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Prescription)
                .Include(v => v.Bill)
                .OrderByDescending(v => v.VisitDate)
                .Take(100)
                .ToListAsync();

            return visits;
        }

        public async Task<OPDVisit> UpdateVisitAsync(Guid visitId, UpdateVisitRequest request, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Check if visit can be updated
            if (visit.Status == VisitStatus.Completed || visit.Status == VisitStatus.Cancelled)
                throw new ValidationException($"Cannot update visit with status: {visit.Status}");

            // Update clinical information
            if (!string.IsNullOrWhiteSpace(request.ChiefComplaint))
                visit.UpdateChiefComplaint(
                    request.ChiefComplaint,
                    request.HistoryOfPresentIllness,
                    request.PastMedicalHistory,
                    request.FamilyHistory,
                    updatedBy);

            if (!string.IsNullOrWhiteSpace(request.ClinicalNotes))
                visit.AddClinicalNotes(request.ClinicalNotes, updatedBy);

            if (!string.IsNullOrWhiteSpace(request.Diagnosis))
                visit.SetDiagnosis(request.Diagnosis, updatedBy);

            if (!string.IsNullOrWhiteSpace(request.TreatmentPlan))
                visit.SetTreatmentPlan(request.TreatmentPlan, updatedBy);

            // Update vitals if provided
            if (!string.IsNullOrWhiteSpace(request.BloodPressure) ||
                request.Temperature.HasValue ||
                request.Pulse.HasValue ||
                request.RespiratoryRate.HasValue ||
                request.SPO2.HasValue ||
                request.Weight.HasValue ||
                request.Height.HasValue)
            {
                visit.UpdateVitals(
                    request.BloodPressure,
                    request.Temperature,
                    request.Pulse,
                    request.RespiratoryRate,
                    request.SPO2,
                    request.Weight,
                    request.Height,
                    updatedBy);
            }

            // Update examination findings
            if (!string.IsNullOrWhiteSpace(request.GeneralExamination) ||
                !string.IsNullOrWhiteSpace(request.SystemicExamination) ||
                !string.IsNullOrWhiteSpace(request.LocalExamination))
            {
                visit.UpdateExaminationFindings(
                    request.GeneralExamination,
                    request.SystemicExamination,
                    request.LocalExamination,
                    updatedBy);
            }

            // Update investigations
            if (!string.IsNullOrWhiteSpace(request.InvestigationsOrdered))
                visit.OrderInvestigations(request.InvestigationsOrdered, updatedBy);

            // Update advice
            if (!string.IsNullOrWhiteSpace(request.Advice))
            {
                // Note: Your OPDVisit entity has Advice property, but you might need to add a method to update it
                // For now, we'll add it to clinical notes
                visit.AddClinicalNotes($"Advice: {request.Advice}", updatedBy);
            }

            // Update follow-up
            if (request.FollowUpDate.HasValue)
            {
                visit.SetFollowUp(
                    request.FollowUpDate.Value,
                    request.FollowUpInstructions,
                    request.FollowUpDays,
                    updatedBy);
            }

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visit {VisitNumber} updated successfully", visit.VisitNumber);
            return visit;
        }

        public async Task<bool> DeleteVisitAsync(Guid visitId, string reason, Guid deletedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Check if visit can be deleted
            if (visit.Status == VisitStatus.Completed && visit.PrescriptionId.HasValue)
                throw new ValidationException("Cannot delete completed visit with prescription");

            if (visit.Status == VisitStatus.Completed && visit.OPDBillId.HasValue)
                throw new ValidationException("Cannot delete completed visit with bill");

            // Use the SoftDelete method from BaseEntity
            visit.SoftDelete(deletedBy);

            // Add deletion note to clinical notes
            visit.AddClinicalNotes($"[DELETED: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] Reason: {reason}", deletedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visit {VisitNumber} deleted", visit.VisitNumber);
            return true;
        }

        public async Task<OPDVisit> StartConsultationAsync(Guid visitId, Guid startedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);
            visit.StartConsultation(startedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Consultation started for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> CompleteConsultationAsync(Guid visitId, TimeSpan? consultationDuration, Guid completedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Validate visit has at least chief complaint
            if (string.IsNullOrWhiteSpace(visit.ChiefComplaint))
                throw new ValidationException("Cannot complete consultation without chief complaint");

            visit.CompleteConsultation(consultationDuration, completedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Consultation completed for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> CancelVisitAsync(Guid visitId, string reason, Guid cancelledBy)
        {
            var visit = await GetVisitByIdAsync(visitId);
            visit.CancelVisit(reason, cancelledBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visit {VisitNumber} cancelled", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> MarkAsNoShowAsync(Guid visitId, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Only checked-in visits can be marked as no-show
            if (visit.Status != VisitStatus.CheckedIn)
                throw new ValidationException($"Cannot mark visit as no-show from status: {visit.Status}");

            visit.MarkAsNoShow(updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visit {VisitNumber} marked as no-show", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> UpdateChiefComplaintAsync(Guid visitId, string chiefComplaint, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Only allow updating chief complaint if consultation hasn't been completed
            if (visit.Status == VisitStatus.Completed)
                throw new ValidationException("Cannot update chief complaint for completed visit");

            visit.UpdateChiefComplaint(chiefComplaint, null, null, null, updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Chief complaint updated for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> UpdateVitalsAsync(Guid visitId, VisitVitalsDto vitals, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Validate consultation is in progress
            if (visit.Status != VisitStatus.InProgress && visit.Status != VisitStatus.CheckedIn)
                throw new ValidationException($"Cannot update vitals from status: {visit.Status}");

            visit.UpdateVitals(
                vitals.BloodPressure,
                vitals.Temperature,
                vitals.Pulse,
                vitals.RespiratoryRate,
                vitals.SPO2,
                vitals.Weight,
                vitals.Height,
                updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Vitals updated for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> SetDiagnosisAsync(Guid visitId, string diagnosis, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            if (visit.Status != VisitStatus.InProgress)
                throw new ValidationException($"Cannot set diagnosis from status: {visit.Status}");

            visit.SetDiagnosis(diagnosis, updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Diagnosis set for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> SetTreatmentPlanAsync(Guid visitId, string treatmentPlan, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            if (visit.Status != VisitStatus.InProgress)
                throw new ValidationException($"Cannot set treatment plan from status: {visit.Status}");

            visit.SetTreatmentPlan(treatmentPlan, updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Treatment plan set for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> AddClinicalNotesAsync(Guid visitId, string notes, Guid addedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);
            visit.AddClinicalNotes(notes, addedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Clinical notes added to visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> OrderInvestigationsAsync(Guid visitId, string investigations, Guid orderedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            if (visit.Status != VisitStatus.InProgress && visit.Status != VisitStatus.Completed)
                throw new ValidationException($"Cannot order investigations from status: {visit.Status}");

            visit.OrderInvestigations(investigations, orderedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Investigations ordered for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> SetFollowUpAsync(Guid visitId, DateTime followUpDate, string? instructions, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            if (visit.Status != VisitStatus.InProgress && visit.Status != VisitStatus.Completed)
                throw new ValidationException($"Cannot set follow-up from status: {visit.Status}");

            // Validate follow-up date is in future
            if (followUpDate <= DateTime.UtcNow)
                throw new ValidationException("Follow-up date must be in the future");

            visit.SetFollowUp(followUpDate, instructions, null, updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Follow-up set for visit {VisitNumber}", visit.VisitNumber);
            return visit;
        }

        public async Task<OPDVisit> ReferToDoctorAsync(Guid visitId, Guid referredToDoctorId, string reason, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Validate referred doctor exists
            var referredDoctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(referredToDoctorId);
            if (referredDoctor == null)
                throw new NotFoundException(nameof(Doctor), referredToDoctorId);

            // Cannot refer to same doctor
            if (referredToDoctorId == visit.DoctorId)
                throw new ValidationException("Cannot refer to the same doctor");

            visit.ReferToDoctor(referredToDoctorId, reason, updatedBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visit {VisitNumber} referred to doctor {DoctorId}",
                visit.VisitNumber, referredToDoctorId);
            return visit;
        }

        public async Task<IEnumerable<VisitSummaryDto>> SearchVisitsAsync(VisitSearchRequest request)
        {
            var query = _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => !v.IsDeleted);

            // Apply filters
            if (request.PatientId.HasValue)
                query = query.Where(v => v.PatientId == request.PatientId.Value);

            if (request.DoctorId.HasValue)
                query = query.Where(v => v.DoctorId == request.DoctorId.Value);

            if (!string.IsNullOrWhiteSpace(request.VisitNumber))
                query = query.Where(v => v.VisitNumber.Contains(request.VisitNumber));

            if (request.VisitType.HasValue)
                query = query.Where(v => v.VisitType == request.VisitType.Value);

            if (request.Status.HasValue)
                query = query.Where(v => v.Status == request.Status.Value);

            if (request.VisitDateFrom.HasValue)
                query = query.Where(v => v.VisitDate >= request.VisitDateFrom.Value);

            if (request.VisitDateTo.HasValue)
            {
                var endDate = request.VisitDateTo.Value.Date.AddDays(1);
                query = query.Where(v => v.VisitDate < endDate);
            }

            if (request.HasFollowUp.HasValue)
            {
                query = request.HasFollowUp.Value
                    ? query.Where(v => v.FollowUpDate.HasValue)
                    : query.Where(v => !v.FollowUpDate.HasValue);
            }

            if (request.HasPrescription.HasValue)
            {
                query = request.HasPrescription.Value
                    ? query.Where(v => v.PrescriptionId.HasValue)
                    : query.Where(v => !v.PrescriptionId.HasValue);
            }

            if (request.HasBill.HasValue)
            {
                query = request.HasBill.Value
                    ? query.Where(v => v.OPDBillId.HasValue)
                    : query.Where(v => !v.OPDBillId.HasValue);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "visitdate" => request.SortDescending
                    ? query.OrderByDescending(v => v.VisitDate)
                    : query.OrderBy(v => v.VisitDate),
                "patientname" => request.SortDescending
                    ? query.OrderByDescending(v => v.Patient != null ? v.Patient.Name : "")
                    : query.OrderBy(v => v.Patient != null ? v.Patient.Name : ""),
                "doctorname" => request.SortDescending
                    ? query.OrderByDescending(v => v.Doctor != null ? v.Doctor.Name : "")
                    : query.OrderBy(v => v.Doctor != null ? v.Doctor.Name : ""),
                "status" => request.SortDescending
                    ? query.OrderByDescending(v => v.Status)
                    : query.OrderBy(v => v.Status),
                _ => request.SortDescending
                    ? query.OrderByDescending(v => v.VisitDate)
                    : query.OrderBy(v => v.VisitDate)
            };

            // Apply pagination with includes
            var visits = await query
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Prescription)
                .Include(v => v.Bill)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return visits.Select(MapToVisitSummaryDto);
        }

        public async Task<IEnumerable<VisitSummaryDto>> GetDoctorVisitsAsync(Guid doctorId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var visits = await _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => v.DoctorId == doctorId &&
                           v.VisitDate >= startDate &&
                           v.VisitDate < endDate &&
                           !v.IsDeleted)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Prescription)
                .Include(v => v.Bill)
                .OrderBy(v => v.VisitDate)
                .ToListAsync();

            return visits.Select(MapToVisitSummaryDto);
        }

        public async Task<IEnumerable<VisitSummaryDto>> GetTodaysVisitsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var visits = await _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => v.VisitDate >= today &&
                           v.VisitDate < tomorrow &&
                           !v.IsDeleted)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Prescription)
                .Include(v => v.Bill)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();

            return visits.Select(MapToVisitSummaryDto);
        }

        public async Task<VisitStatisticsDto> GetVisitStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var allVisits = await _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => !v.IsDeleted)
                .Include(v => v.Doctor)
                .ToListAsync();

            var todaysVisits = allVisits.Where(v => v.VisitDate.Date == today);
            var thisWeekVisits = allVisits.Where(v => v.VisitDate >= weekStart);
            var thisMonthVisits = allVisits.Where(v => v.VisitDate >= monthStart);

            var statistics = new VisitStatisticsDto
            {
                TotalVisits = allVisits.Count,
                TodaysVisits = todaysVisits.Count(),
                ThisWeekVisits = thisWeekVisits.Count(),
                ThisMonthVisits = thisMonthVisits.Count(),
                CompletedVisits = allVisits.Count(v => v.Status == VisitStatus.Completed),
                CancelledVisits = allVisits.Count(v => v.Status == VisitStatus.Cancelled),
                NoShowVisits = allVisits.Count(v => v.Status == VisitStatus.NoShow),
                VisitsWithFollowUp = allVisits.Count(v => v.FollowUpDate.HasValue),
                VisitsWithPrescription = allVisits.Count(v => v.PrescriptionId.HasValue),
                AverageConsultationDuration = allVisits
                    .Where(v => v.ConsultationDuration.HasValue)
                    .Average(v => (decimal)v.ConsultationDuration.Value.TotalMinutes)
            };

            // Visits by type
            foreach (VisitType type in Enum.GetValues(typeof(VisitType)))
            {
                statistics.VisitsByType[type] = allVisits.Count(v => v.VisitType == type);
            }

            // Visits by status
            foreach (VisitStatus status in Enum.GetValues(typeof(VisitStatus)))
            {
                statistics.VisitsByStatus[status] = allVisits.Count(v => v.Status == status);
            }

            // Visits by doctor
            var visitsByDoctor = allVisits
                .Where(v => v.Doctor != null)
                .GroupBy(v => v.Doctor!.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            statistics.VisitsByDoctor = visitsByDoctor;

            // Daily visits for last 7 days
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dailyCount = allVisits.Count(v => v.VisitDate.Date == date);
                statistics.DailyVisits[date] = dailyCount;
            }

            return statistics;
        }

        public async Task<IEnumerable<DoctorScheduleDto>> GetDoctorSchedulesAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            // Get all doctors with visits for the day
            var doctors = await _unitOfWork.Repository<Doctor>()
                .GetQueryable()
                .Where(d => d.IsActive)
                .ToListAsync();

            var schedules = new List<DoctorScheduleDto>();

            foreach (var doctor in doctors)
            {
                var doctorVisits = await _unitOfWork.Repository<OPDVisit>()
                    .GetQueryable()
                    .Where(v => v.DoctorId == doctor.Id &&
                               v.VisitDate >= startDate &&
                               v.VisitDate < endDate &&
                               !v.IsDeleted)
                    .Include(v => v.Patient)
                    .OrderBy(v => v.VisitDate)
                    .ToListAsync();

                var schedule = new DoctorScheduleDto
                {
                    DoctorId = doctor.Id,
                    DoctorName = doctor.Name,
                    Specialization = doctor.Specialization ?? "General",
                    TotalAppointments = doctorVisits.Count,
                    CompletedAppointments = doctorVisits.Count(v => v.Status == VisitStatus.Completed),
                    PendingAppointments = doctorVisits.Count(v => v.Status == VisitStatus.CheckedIn || v.Status == VisitStatus.InProgress),
                    TodaysSchedule = doctorVisits.Select(v => new VisitSlotDto
                    {
                        VisitId = v.Id,
                        VisitNumber = v.VisitNumber,
                        ScheduledTime = v.VisitDate,
                        PatientName = v.Patient?.Name ?? "Unknown",
                        PatientNumber = v.Patient?.PatientNumber ?? "",
                        Status = v.Status,
                        Duration = v.ConsultationDuration,
                        IsUrgent = v.VisitType == VisitType.Emergency
                    }).ToList()
                };

                schedules.Add(schedule);
            }

            return schedules;
        }

        public async Task<OPDVisit> LinkBillAsync(Guid visitId, Guid billId, Guid updatedBy)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Validate bill exists
            var bill = await _unitOfWork.Repository<OPDBill>().GetByIdAsync(billId);
            if (bill == null)
                throw new NotFoundException("OPDBill", billId);

            visit.LinkBill(bill);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Bill {BillId} linked to visit {VisitNumber}", billId, visit.VisitNumber);
            return visit;
        }

        public async Task<bool> ValidateVisitForPrescriptionAsync(Guid visitId)
        {
            var visit = await GetVisitByIdAsync(visitId);

            // Check if visit is completed
            if (visit.Status != VisitStatus.Completed)
                return false;

            // Check if visit already has prescription
            if (visit.PrescriptionId.HasValue)
                return false;

            // Check if visit has chief complaint
            if (string.IsNullOrWhiteSpace(visit.ChiefComplaint))
                return false;

            return true;
        }

        public async Task<VisitVitalsDto> GetVisitVitalsAsync(Guid visitId)
        {
            var visit = await GetVisitByIdAsync(visitId);

            return new VisitVitalsDto
            {
                BloodPressure = visit.BloodPressure,
                Temperature = visit.Temperature,
                Pulse = visit.Pulse,
                RespiratoryRate = visit.RespiratoryRate,
                SPO2 = visit.SPO2,
                Weight = visit.Weight,
                Height = visit.Height,
                BMI = visit.BMI,
                RecordedAt = visit.UpdatedAt ?? visit.CreatedAt,
                RecordedBy = "System" // You might want to get actual user name
            };
        }

        // Helper Methods
        private OPDVisitDto MapToOPDVisitDto(OPDVisit visit)
        {
            return new OPDVisitDto
            {
                Id = visit.Id,
                VisitNumber = visit.VisitNumber,
                VisitDate = visit.VisitDate,
                VisitType = visit.VisitType,
                Status = visit.Status,
                PatientId = visit.PatientId,
                PatientName = visit.Patient?.Name ?? string.Empty,
                PatientNumber = visit.Patient?.PatientNumber ?? string.Empty,
                DoctorId = visit.DoctorId,
                DoctorName = visit.Doctor?.Name ?? string.Empty,
                DoctorSpecialization = visit.Doctor?.Specialization ?? string.Empty,
                ChiefComplaint = visit.ChiefComplaint,
                HistoryOfPresentIllness = visit.HistoryOfPresentIllness,
                PastMedicalHistory = visit.PastMedicalHistory,
                FamilyHistory = visit.FamilyHistory,
                ClinicalNotes = visit.ClinicalNotes,
                Diagnosis = visit.Diagnosis,
                TreatmentPlan = visit.TreatmentPlan,
                BloodPressure = visit.BloodPressure,
                Temperature = visit.Temperature,
                Pulse = visit.Pulse,
                RespiratoryRate = visit.RespiratoryRate,
                SPO2 = visit.SPO2,
                Weight = visit.Weight,
                Height = visit.Height,
                BMI = visit.BMI,
                GeneralExamination = visit.GeneralExamination,
                SystemicExamination = visit.SystemicExamination,
                LocalExamination = visit.LocalExamination,
                InvestigationsOrdered = visit.InvestigationsOrdered,
                Advice = visit.Advice,
                FollowUpInstructions = visit.FollowUpInstructions,
                FollowUpDate = visit.FollowUpDate,
                FollowUpDays = visit.FollowUpDays,
                ReferredToDoctorId = visit.ReferredToDoctorId,
                ReferredToDoctorName = visit.ReferredToDoctor?.Name,
                ReferralReason = visit.ReferralReason,
                OPDBillId = visit.OPDBillId,
                BillNumber = visit.Bill?.BillNumber,
                ConsultationDuration = visit.ConsultationDuration,
                CreatedAt = visit.CreatedAt,
                UpdatedAt = visit.UpdatedAt
            };
        }

        private VisitSummaryDto MapToVisitSummaryDto(OPDVisit visit)
        {
            return new VisitSummaryDto
            {
                Id = visit.Id,
                VisitNumber = visit.VisitNumber,
                VisitDate = visit.VisitDate,
                VisitType = visit.VisitType,
                Status = visit.Status,
                DoctorId = visit.DoctorId,
                DoctorName = visit.Doctor?.Name ?? string.Empty,
                DoctorSpecialization = visit.Doctor?.Specialization ?? string.Empty,
                ChiefComplaint = visit.ChiefComplaint,
                Diagnosis = visit.Diagnosis,
                ConsultationFee = visit.Bill?.ConsultationFee,
                TotalBillAmount = visit.Bill?.TotalAmount,
                PaidAmount = visit.Bill?.PaidAmount,
                BillStatus = visit.Bill?.Status,
                FollowUpDate = visit.FollowUpDate,
                HasFollowUp = visit.FollowUpDate.HasValue,
                HasPrescription = visit.PrescriptionId.HasValue,
                ConsultationDuration = visit.ConsultationDuration
            };
        }
        public async Task<OPDVisit> RestoreVisitAsync(Guid visitId, Guid restoredBy)
        {
            var visit = await _unitOfWork.Repository<OPDVisit>()
                .GetQueryable()
                .Where(v => v.Id == visitId && v.IsDeleted) // Only get deleted visits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .FirstOrDefaultAsync();

            if (visit == null)
                throw new NotFoundException(nameof(OPDVisit), visitId);

            // Restore the visit
            visit.Restore();

            // Add restoration note
            visit.AddClinicalNotes($"[RESTORED: {DateTime.UtcNow:yyyy-MM-dd HH:mm}]", restoredBy);

            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visit {VisitNumber} restored", visit.VisitNumber);
            return visit;
        }
    }
}