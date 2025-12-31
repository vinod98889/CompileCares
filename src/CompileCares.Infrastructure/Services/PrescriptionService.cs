using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Patients;
using CompileCares.Core.Entities.Pharmacy;
using CompileCares.Core.Entities.Templates;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CompileCares.Infrastructure.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PrescriptionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Prescription> CreatePrescriptionAsync(Guid patientId, Guid doctorId, Guid visitId, Guid createdBy)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), patientId);

            // Validate doctor exists
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId);
            if (doctor == null)
                throw new NotFoundException(nameof(Doctor), doctorId);

            // Validate visit exists
            var visit = await _unitOfWork.Repository<OPDVisit>().GetByIdAsync(visitId);
            if (visit == null)
                throw new NotFoundException(nameof(OPDVisit), visitId);

            // Create prescription
            var prescription = new Prescription(patientId, doctorId, visitId, createdBy);

            await _unitOfWork.Repository<Prescription>().AddAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            // Link prescription to visit
            visit.LinkPrescription(prescription);
            await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> AddMedicineToPrescriptionAsync(Guid prescriptionId, Guid medicineId, Guid doseId, int durationDays, int quantity, string? instructions, string? customDosage, Guid updatedBy)
        {
            var prescription = await GetPrescriptionWithMedicinesAsync(prescriptionId);

            // Validate medicine exists
            var medicine = await _unitOfWork.Repository<Medicine>().GetByIdAsync(medicineId);
            if (medicine == null)
                throw new NotFoundException(nameof(Medicine), medicineId);

            // Validate dose exists
            var dose = await _unitOfWork.Repository<Dose>().GetByIdAsync(doseId);
            if (dose == null)
                throw new NotFoundException(nameof(Dose), doseId);

            // Add medicine to prescription
            prescription.AddMedicine(medicineId, doseId, durationDays, quantity, instructions, customDosage, updatedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> ApplyTemplateToPrescriptionAsync(Guid prescriptionId, Guid templateId, Guid appliedBy)
        {
            var prescription = await GetPrescriptionWithAllDetailsAsync(prescriptionId);

            // Validate template exists
            var template = await _unitOfWork.Repository<PrescriptionTemplate>()
                .GetQueryable()
                .Include(t => t.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(t => t.Medicines)
                    .ThenInclude(m => m.Dose)
                .Include(t => t.Complaints)
                    .ThenInclude(c => c.Complaint)
                .Include(t => t.AdvisedItems)
                    .ThenInclude(a => a.Advised)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
                throw new NotFoundException(nameof(PrescriptionTemplate), templateId);

            // Check if doctor can use this template
            if (!template.IsPublic && template.DoctorId != prescription.DoctorId)
                throw new ValidationException($"Doctor does not have access to template '{template.Name}'");

            // Apply template to prescription
            prescription.ApplyTemplate(template, appliedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> CompletePrescriptionAsync(Guid prescriptionId, Guid completedBy)
        {
            var prescription = await GetPrescriptionWithMedicinesAsync(prescriptionId);

            // Validate prescription has medicines
            if (!prescription.Medicines.Any())
                throw new ValidationException("Cannot complete prescription without medicines");

            // Validate all medicines are in stock
            var outOfStockMedicines = new List<string>();
            foreach (var medicine in prescription.Medicines)
            {
                if (medicine.Medicine != null && medicine.Medicine.CurrentStock < medicine.CalculateTotalUnits())
                {
                    outOfStockMedicines.Add(medicine.Medicine.Name);
                }
            }

            if (outOfStockMedicines.Any())
            {
                throw new ValidationException($"Insufficient stock for medicines: {string.Join(", ", outOfStockMedicines)}");
            }

            // Complete prescription
            prescription.Complete(completedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<IEnumerable<Prescription>> GetPatientPrescriptionsAsync(Guid patientId)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), patientId);

            // Get prescriptions with details
            var prescriptions = await _unitOfWork.Repository<Prescription>()
                .GetQueryable()
                .Where(p => p.PatientId == patientId && !p.IsDeleted)
                .Include(p => p.Doctor)
                .Include(p => p.Patient)
                .Include(p => p.OPDVisit)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Dose)
                .Include(p => p.Complaints)
                    .ThenInclude(c => c.Complaint)
                .Include(p => p.AdvisedItems)
                    .ThenInclude(a => a.Advised)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return prescriptions;
        }

        public async Task<bool> ValidatePrescriptionAsync(Guid prescriptionId)
        {
            var prescription = await GetPrescriptionWithAllDetailsAsync(prescriptionId);

            // Check if prescription is valid
            if (!prescription.IsValid())
                return false;

            // Check if all required medicines are in stock
            foreach (var medicine in prescription.Medicines)
            {
                if (medicine.Medicine != null && medicine.Medicine.CurrentStock < medicine.CalculateTotalUnits())
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<string> GeneratePrescriptionPrintAsync(Guid prescriptionId)
        {
            var prescription = await GetPrescriptionWithAllDetailsAsync(prescriptionId);
            var doctor = prescription.Doctor;
            var patient = prescription.Patient;
            var visit = prescription.OPDVisit;

            if (doctor == null || patient == null)
                throw new NotFoundException("Doctor or Patient not found for this prescription");

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                    PRESCRIPTION                           ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            // Prescription Info
            sb.AppendLine($"PRESCRIPTION NO:  {prescription.PrescriptionNumber}");
            sb.AppendLine($"DATE:             {prescription.PrescriptionDate:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"VALID UNTIL:      {prescription.ValidUntil:dd/MM/yyyy}");
            sb.AppendLine();

            // Patient Info
            sb.AppendLine("PATIENT INFORMATION:");
            sb.AppendLine("────────────────────");
            sb.AppendLine($"Name:     {patient.Name}");
            sb.AppendLine($"Age:      {patient.Age?.ToString() ?? "N/A"} years");
            sb.AppendLine($"Gender:   {patient.Gender}");
            sb.AppendLine($"Mobile:   {patient.Mobile}");
            if (!string.IsNullOrWhiteSpace(patient.BloodGroup))
                sb.AppendLine($"Blood Grp: {patient.BloodGroup}");
            sb.AppendLine();

            // Doctor Info
            sb.AppendLine("DOCTOR INFORMATION:");
            sb.AppendLine("────────────────────");
            sb.AppendLine($"Name:     Dr. {doctor.Name}");
            sb.AppendLine($"Reg No:   {doctor.RegistrationNumber}");
            sb.AppendLine($"Specialty: {doctor.Specialization}");
            if (!string.IsNullOrWhiteSpace(doctor.Qualification))
                sb.AppendLine($"Qualification: {doctor.Qualification}");
            sb.AppendLine();

            // Clinical Information
            if (!string.IsNullOrWhiteSpace(prescription.Diagnosis))
            {
                sb.AppendLine("DIAGNOSIS:");
                sb.AppendLine("──────────");
                sb.AppendLine(prescription.Diagnosis);
                sb.AppendLine();
            }

            // Complaints
            if (prescription.Complaints.Any())
            {
                sb.AppendLine("COMPLAINTS:");
                sb.AppendLine("───────────");
                foreach (var complaint in prescription.Complaints)
                {
                    var complaintText = complaint.CustomComplaint ?? complaint.Complaint?.Name ?? "Unknown";
                    sb.Append($"• {complaintText}");

                    if (!string.IsNullOrWhiteSpace(complaint.Duration))
                        sb.Append($" ({complaint.Duration})");

                    if (!string.IsNullOrWhiteSpace(complaint.Severity))
                        sb.Append($" [{complaint.Severity}]");

                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            // Medicines
            if (prescription.Medicines.Any())
            {
                sb.AppendLine("PRESCRIBED MEDICINES:");
                sb.AppendLine("──────────────────────");
                sb.AppendLine("┌────┬────────────────────────────┬────────────────────┬──────────┬────────────┐");
                sb.AppendLine("│ No │ Medicine                   │ Dosage             │ Duration │ Instructions │");
                sb.AppendLine("├────┼────────────────────────────┼────────────────────┼──────────┼────────────┤");

                int counter = 1;
                foreach (var medicine in prescription.Medicines.OrderBy(m => m.Medicine?.Name))
                {
                    var medicineName = medicine.Medicine?.Name ?? "Unknown Medicine";
                    var strength = medicine.Medicine?.Strength;
                    if (!string.IsNullOrWhiteSpace(strength))
                        medicineName += $" ({strength})";

                    var dosage = medicine.GetDosageDisplay();
                    var duration = $"{medicine.DurationDays} days";
                    var instructions = medicine.Instructions ?? "";

                    // Truncate if too long for display
                    if (medicineName.Length > 26) medicineName = medicineName[..26] + "...";
                    if (dosage.Length > 18) dosage = dosage[..18] + "...";
                    if (instructions.Length > 10) instructions = instructions[..10] + "...";

                    sb.AppendLine($"│ {counter,2} │ {medicineName,-26} │ {dosage,-18} │ {duration,-8} │ {instructions,-10} │");
                    counter++;
                }

                sb.AppendLine("└────┴────────────────────────────┴────────────────────┴──────────┴────────────┘");
                sb.AppendLine();
            }

            // Instructions and Advice
            if (!string.IsNullOrWhiteSpace(prescription.Instructions))
            {
                sb.AppendLine("SPECIAL INSTRUCTIONS:");
                sb.AppendLine("─────────────────────");
                sb.AppendLine(prescription.Instructions);
                sb.AppendLine();
            }

            // Advised Items
            if (prescription.AdvisedItems.Any())
            {
                sb.AppendLine("ADVICE:");
                sb.AppendLine("───────");
                foreach (var advice in prescription.AdvisedItems)
                {
                    var adviceText = advice.CustomAdvice ?? advice.Advised?.Text ?? "Unknown";
                    sb.AppendLine($"• {adviceText}");
                }
                sb.AppendLine();
            }

            // Follow-up Instructions
            if (!string.IsNullOrWhiteSpace(prescription.FollowUpInstructions))
            {
                sb.AppendLine("FOLLOW-UP INSTRUCTIONS:");
                sb.AppendLine("───────────────────────");
                sb.AppendLine(prescription.FollowUpInstructions);
                sb.AppendLine();
            }

            if (visit?.FollowUpDate.HasValue == true)
            {
                sb.AppendLine($"FOLLOW-UP DATE: {visit.FollowUpDate.Value:dd/MM/yyyy}");
                sb.AppendLine();
            }

            // Footer
            sb.AppendLine("══════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("Doctor's Signature: ________________________");
            if (!string.IsNullOrWhiteSpace(doctor.SignaturePath))
                sb.AppendLine($"[Signature on file: {doctor.SignaturePath}]");
            sb.AppendLine();
            sb.AppendLine("══════════════════════════════════════════════════════════");
            sb.AppendLine("Note: This prescription is valid until the date mentioned above.");
            sb.AppendLine("      Do not share medicines. Complete the full course.");
            sb.AppendLine("      For emergency, contact: [Clinic/Hospital Contact]");

            return sb.ToString();
        }

        public async Task<Prescription> GetPrescriptionWithDetailsAsync(Guid prescriptionId)
        {
            return await GetPrescriptionWithAllDetailsAsync(prescriptionId);
        }

        public async Task<PrescriptionDetailDto> GetPrescriptionDetailAsync(Guid prescriptionId)
        {
            var prescription = await GetPrescriptionWithAllDetailsAsync(prescriptionId);
            return MapToPrescriptionDetailDto(prescription);
        }

        public async Task<IEnumerable<PrescriptionDto>> SearchPrescriptionsAsync(PrescriptionSearchRequest request)
        {
            var query = _unitOfWork.Repository<Prescription>()
                .GetQueryable()
                .Where(p => !p.IsDeleted);

            // Apply filters
            if (request.PatientId.HasValue)
                query = query.Where(p => p.PatientId == request.PatientId.Value);

            if (request.DoctorId.HasValue)
                query = query.Where(p => p.DoctorId == request.DoctorId.Value);

            if (request.VisitId.HasValue)
                query = query.Where(p => p.OPDVisitId == request.VisitId.Value);

            if (!string.IsNullOrWhiteSpace(request.PrescriptionNumber))
                query = query.Where(p => p.PrescriptionNumber.Contains(request.PrescriptionNumber));

            if (request.Status.HasValue)
                query = query.Where(p => p.Status == request.Status.Value);

            if (request.PrescriptionDateFrom.HasValue)
                query = query.Where(p => p.PrescriptionDate >= request.PrescriptionDateFrom.Value);

            if (request.PrescriptionDateTo.HasValue)
                query = query.Where(p => p.PrescriptionDate <= request.PrescriptionDateTo.Value);

            if (request.IsValid.HasValue)
            {
                query = request.IsValid.Value
                    ? query.Where(p => p.IsValid())
                    : query.Where(p => !p.IsValid());
            }

            if (request.IsDispensed.HasValue)
            {
                query = query.Where(p => p.Medicines.Any(m => m.IsDispensed == request.IsDispensed.Value));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "prescriptiondate" => request.SortDescending
                    ? query.OrderByDescending(p => p.PrescriptionDate)
                    : query.OrderBy(p => p.PrescriptionDate),
                "patientname" => request.SortDescending
                    ? query.OrderByDescending(p => p.Patient!.Name)
                    : query.OrderBy(p => p.Patient!.Name),
                "doctorname" => request.SortDescending
                    ? query.OrderByDescending(p => p.Doctor!.Name)
                    : query.OrderBy(p => p.Doctor!.Name),
                "status" => request.SortDescending
                    ? query.OrderByDescending(p => p.Status)
                    : query.OrderBy(p => p.Status),
                _ => request.SortDescending
                    ? query.OrderByDescending(p => p.PrescriptionDate)
                    : query.OrderBy(p => p.PrescriptionDate)
            };

            // Apply pagination
            var prescriptions = await query
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.OPDVisit)
                .Include(p => p.Medicines)
                .Include(p => p.Complaints)
                .Include(p => p.AdvisedItems)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return prescriptions.Select(MapToPrescriptionDto);
        }

        public async Task<Prescription> UpdatePrescriptionDiagnosisAsync(Guid prescriptionId, string diagnosis, Guid updatedBy)
        {
            var prescription = await GetPrescriptionWithMedicinesAsync(prescriptionId);

            prescription.UpdateDiagnosis(diagnosis, updatedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> UpdatePrescriptionInstructionsAsync(Guid prescriptionId, string instructions, Guid updatedBy)
        {
            var prescription = await GetPrescriptionWithMedicinesAsync(prescriptionId);

            prescription.UpdateInstructions(instructions, updatedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> AddComplaintToPrescriptionAsync(Guid prescriptionId, Guid? complaintId, string? customComplaint, string? duration, string? severity, Guid updatedBy)
        {
            var prescription = await GetPrescriptionWithAllDetailsAsync(prescriptionId);

            prescription.AddComplaint(complaintId, customComplaint, duration, severity, updatedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> AddAdviceToPrescriptionAsync(Guid prescriptionId, Guid? advisedId, string? customAdvice, Guid updatedBy)
        {
            var prescription = await GetPrescriptionWithAllDetailsAsync(prescriptionId);

            prescription.AddAdvised(advisedId, customAdvice, updatedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<Prescription> MarkMedicineAsDispensedAsync(Guid prescriptionId, Guid medicineId, string dispensedBy, Guid updatedBy)
        {
            var prescription = await GetPrescriptionWithMedicinesAsync(prescriptionId);

            var prescriptionMedicine = prescription.Medicines.FirstOrDefault(m => m.Id == medicineId);
            if (prescriptionMedicine == null)
                throw new NotFoundException("PrescriptionMedicine not found");

            prescriptionMedicine.MarkAsDispensed(dispensedBy, updatedBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return prescription;
        }

        public async Task<bool> CancelPrescriptionAsync(Guid prescriptionId, string reason, Guid cancelledBy)
        {
            var prescription = await GetPrescriptionWithMedicinesAsync(prescriptionId);

            prescription.Cancel(reason, cancelledBy);

            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<Prescription> UpdatePrescriptionAsync(Prescription prescription)
        {
            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();
            return prescription;
        }

        // Helper Methods
        private async Task<Prescription> GetPrescriptionWithMedicinesAsync(Guid prescriptionId)
        {
            var prescription = await _unitOfWork.Repository<Prescription>()
                .GetQueryable()
                .Where(p => p.Id == prescriptionId && !p.IsDeleted)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Dose)
                .FirstOrDefaultAsync();

            if (prescription == null)
                throw new NotFoundException(nameof(Prescription), prescriptionId);

            return prescription;
        }

        private async Task<Prescription> GetPrescriptionWithAllDetailsAsync(Guid prescriptionId)
        {
            var prescription = await _unitOfWork.Repository<Prescription>()
                .GetQueryable()
                .Where(p => p.Id == prescriptionId && !p.IsDeleted)
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.OPDVisit)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Dose)
                .Include(p => p.Complaints)
                    .ThenInclude(c => c.Complaint)
                .Include(p => p.AdvisedItems)
                    .ThenInclude(a => a.Advised)
                .FirstOrDefaultAsync();

            if (prescription == null)
                throw new NotFoundException(nameof(Prescription), prescriptionId);

            return prescription;
        }

        // Mapping Methods
        private PrescriptionDto MapToPrescriptionDto(Prescription prescription)
        {
            return new PrescriptionDto
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                PrescriptionDate = prescription.PrescriptionDate,
                PatientId = prescription.PatientId,
                PatientName = prescription.Patient?.Name ?? string.Empty,
                PatientNumber = prescription.Patient?.PatientNumber ?? string.Empty,
                DoctorId = prescription.DoctorId,
                DoctorName = prescription.Doctor?.Name ?? string.Empty,
                DoctorRegistrationNumber = prescription.Doctor?.RegistrationNumber ?? string.Empty,
                OPDVisitId = prescription.OPDVisitId,
                VisitNumber = prescription.OPDVisit?.VisitNumber ?? string.Empty,
                Diagnosis = prescription.Diagnosis,
                Instructions = prescription.Instructions,
                FollowUpInstructions = prescription.FollowUpInstructions,
                Status = prescription.Status,
                ValidityDays = prescription.ValidityDays,
                ValidUntil = prescription.ValidUntil,
                IsValid = prescription.IsValid(),
                MedicineCount = prescription.Medicines.Count,
                ComplaintCount = prescription.Complaints.Count,
                AdviceCount = prescription.AdvisedItems.Count,
                CreatedAt = prescription.CreatedAt,
                UpdatedAt = prescription.UpdatedAt
            };
        }

        private PrescriptionDetailDto MapToPrescriptionDetailDto(Prescription prescription)
        {
            var dto = new PrescriptionDetailDto
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                PrescriptionDate = prescription.PrescriptionDate,
                PatientId = prescription.PatientId,
                PatientName = prescription.Patient?.Name ?? string.Empty,
                PatientNumber = prescription.Patient?.PatientNumber ?? string.Empty,
                DoctorId = prescription.DoctorId,
                DoctorName = prescription.Doctor?.Name ?? string.Empty,
                DoctorRegistrationNumber = prescription.Doctor?.RegistrationNumber ?? string.Empty,
                OPDVisitId = prescription.OPDVisitId,
                VisitNumber = prescription.OPDVisit?.VisitNumber ?? string.Empty,
                Diagnosis = prescription.Diagnosis,
                Instructions = prescription.Instructions,
                FollowUpInstructions = prescription.FollowUpInstructions,
                Status = prescription.Status,
                ValidityDays = prescription.ValidityDays,
                ValidUntil = prescription.ValidUntil,
                IsValid = prescription.IsValid(),
                MedicineCount = prescription.Medicines.Count,
                ComplaintCount = prescription.Complaints.Count,
                AdviceCount = prescription.AdvisedItems.Count,
                CreatedAt = prescription.CreatedAt,
                UpdatedAt = prescription.UpdatedAt,
                DoctorSignaturePath = prescription.Doctor?.SignaturePath,
                DoctorDigitalSignature = prescription.Doctor?.SignaturePath,
                IsDispensed = prescription.Medicines.Any() && prescription.Medicines.All(m => m.IsDispensed),
                DispensedDate = prescription.Medicines.Any() ? prescription.Medicines.Max(m => m.DispensedDate) : null,
                DispensedBy = prescription.Medicines.FirstOrDefault(m => m.IsDispensed)?.DispensedBy
            };

            // Map medicines
            dto.Medicines = prescription.Medicines.Select(m => new PrescriptionMedicineDto
            {
                Id = m.Id,
                MedicineId = m.MedicineId,
                MedicineName = m.Medicine?.Name ?? string.Empty,
                GenericName = m.Medicine?.GenericName,
                Strength = m.Medicine?.Strength,
                Form = m.Medicine?.Form,
                DoseId = m.DoseId,
                DoseCode = m.Dose?.Code ?? string.Empty,
                DoseName = m.Dose?.Name ?? string.Empty,
                CustomDosage = m.CustomDosage,
                DurationDays = m.DurationDays,
                Quantity = m.Quantity,
                TotalUnits = m.CalculateTotalUnits(),
                Instructions = m.Instructions,
                AdditionalNotes = m.AdditionalNotes,
                IsDispensed = m.IsDispensed,
                DispensedDate = m.DispensedDate,
                DispensedBy = m.DispensedBy,
                AvailableStock = m.Medicine?.CurrentStock ?? 0
            }).ToList();

            // Map complaints
            dto.Complaints = prescription.Complaints.Select(c => new PatientComplaintDto
            {
                Id = c.Id,
                ComplaintId = c.ComplaintId,
                ComplaintName = c.Complaint?.Name,
                CustomComplaint = c.CustomComplaint,
                Duration = c.Duration,
                Severity = c.Severity
            }).ToList();

            // Map advised items
            dto.AdvisedItems = prescription.AdvisedItems.Select(a => new PrescriptionAdvisedDto
            {
                Id = a.Id,
                AdvisedId = a.AdvisedId,
                AdvisedText = a.Advised?.Text,
                CustomAdvice = a.CustomAdvice,
                Category = a.Advised?.Category
            }).ToList();

            return dto;
        }
    }
}