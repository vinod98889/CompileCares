using CompileCares.Application.Common.Exceptions;
using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Features.Consultations.DTOs;
using CompileCares.Application.Services;
using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Patients;
using CompileCares.Core.Entities.Templates;
using CompileCares.Shared.Enums;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Add this line

namespace CompileCares.Infrastructure.Services
{
    public class ConsultationService : IConsultationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConsultationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ConsultationResult> CompleteConsultationAsync(
     CompleteConsultationRequest request,
     Guid performedBy,
     CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"=== CONSULTATION SERVICE START ===");
            Console.WriteLine($"Request ID: {Guid.NewGuid()}");
            Console.WriteLine($"IsNewPatient: {request.IsNewPatient}");
            Console.WriteLine($"AllowMultipleVisitsPerDay: {request.AllowMultipleVisitsPerDay}");
            Console.WriteLine($"OverrideExisting: {request.OverrideExisting}");

            // Get execution strategy from DbContext
            var executionStrategy = _unitOfWork.GetDbContext().Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                Console.WriteLine($"=== EXECUTION STRATEGY START ===");

                // Begin transaction directly from DbContext
                await using var transaction = await _unitOfWork.GetDbContext().Database.BeginTransactionAsync();
                Console.WriteLine($"Transaction started");

                try
                {
                    Console.WriteLine($"=== STEP 1: PATIENT HANDLING ===");
                    // 1. Handle Patient (New or Existing)
                    Patient patient;
                    if (request.IsNewPatient && request.NewPatient != null)
                    {
                        Console.WriteLine($"Creating NEW patient");
                        Console.WriteLine($"Patient Name: {request.NewPatient.Name}");
                        Console.WriteLine($"Patient Mobile: {request.NewPatient.Mobile}");

                        patient = new Patient(
                            request.NewPatient.Name,
                            request.NewPatient.Title,
                            request.NewPatient.Gender,
                            request.NewPatient.Mobile,
                            performedBy);

                        if (request.NewPatient.DateOfBirth.HasValue)
                            patient.SetDateOfBirth(request.NewPatient.DateOfBirth.Value, performedBy);

                        await _unitOfWork.Repository<Patient>().AddAsync(patient);
                        Console.WriteLine($"Patient created with ID: {patient.Id}");
                    }
                    else if (request.ExistingPatientId.HasValue)
                    {
                        Console.WriteLine($"Fetching EXISTING patient: {request.ExistingPatientId}");
                        patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(request.ExistingPatientId.Value);
                        if (patient == null)
                            throw new Application.Common.Exceptions.NotFoundException(nameof(Patient), request.ExistingPatientId.Value);

                        Console.WriteLine($"Found existing patient: {patient.Name} (ID: {patient.Id})");
                    }
                    else
                    {
                        throw new ArgumentException("Either NewPatient or ExistingPatientId must be provided");
                    }

                    Console.WriteLine($"=== STEP 2: VISIT HANDLING ===");
                    // 2. Create or Get Visit for Today
                    var today = DateTime.UtcNow.Date;
                    Console.WriteLine($"Today's date: {today:yyyy-MM-dd}");
                    Console.WriteLine($"Patient ID: {patient.Id}");
                    Console.WriteLine($"Doctor ID: {request.DoctorId}");

                    var existingVisit = (await _unitOfWork.Repository<OPDVisit>()
                        .FindAsync(v =>
                            v.PatientId == patient.Id &&
                            v.DoctorId == request.DoctorId &&
                            v.VisitDate.Date == today &&
                            v.Status != VisitStatus.Cancelled &&
                            v.Status != VisitStatus.NoShow))
                        .FirstOrDefault();

                    Console.WriteLine($"Found existing visit: {existingVisit != null}");
                    Console.WriteLine($"Existing visit ID: {existingVisit?.Id}");
                    Console.WriteLine($"Existing visit PrescriptionId: {existingVisit?.PrescriptionId}");
                    Console.WriteLine($"Existing visit Prescription != null: {existingVisit?.Prescription != null}");

                    OPDVisit visit;
                    if (existingVisit != null && !request.AllowMultipleVisitsPerDay)
                    {
                        Console.WriteLine("USING EXISTING VISIT");
                        visit = existingVisit;

                        // Check entity state
                        var visitEntry = _unitOfWork.GetDbContext().Entry(visit);
                        Console.WriteLine($"Existing visit entity state: {visitEntry.State}");
                        Console.WriteLine($"Is Prescription loaded: {visitEntry.Reference(v => v.Prescription).IsLoaded}");

                        UpdateVisitDetails(visit, request, performedBy);
                        await _unitOfWork.Repository<OPDVisit>().UpdateAsync(visit);
                    }
                    else
                    {
                        Console.WriteLine("CREATING NEW VISIT");
                        visit = new OPDVisit(
                            patient.Id,
                            request.DoctorId,
                            VisitType.New,
                            performedBy);

                        Console.WriteLine($"New Visit ID: {visit.Id}");
                        Console.WriteLine($"New Visit PrescriptionId: {visit.PrescriptionId}");
                        Console.WriteLine($"New Visit Prescription != null: {visit.Prescription != null}");

                        visit.StartConsultation(performedBy);
                        UpdateVisitDetails(visit, request, performedBy);

                        Console.WriteLine($"New Visit ID (before save): {visit.Id}");
                        await _unitOfWork.Repository<OPDVisit>().AddAsync(visit);

                        // Check entity state after Add
                        var visitEntry = _unitOfWork.GetDbContext().Entry(visit);
                        Console.WriteLine($"New visit entity state after Add: {visitEntry.State}");
                        Console.WriteLine($"Is Prescription loaded: {visitEntry.Reference(v => v.Prescription).IsLoaded}");
                    }

                    Console.WriteLine($"=== STEP 3: PRESCRIPTION HANDLING ===");
                    Console.WriteLine($"Final Visit ID: {visit.Id}");
                    Console.WriteLine($"Visit PrescriptionId: {visit.PrescriptionId}");
                    Console.WriteLine($"Visit Prescription != null: {visit.Prescription != null}");

                    // Check entity state one more time
                    var finalVisitEntry = _unitOfWork.GetDbContext().Entry(visit);
                    Console.WriteLine($"Final visit entity state: {finalVisitEntry.State}");
                    Console.WriteLine($"Is Prescription loaded: {finalVisitEntry.Reference(v => v.Prescription).IsLoaded}");

                    // 3. Create or Update Prescription
                    var existingPrescription = (await _unitOfWork.Repository<Prescription>()
                        .FindAsync(p => p.OPDVisitId == visit.Id))
                        .FirstOrDefault();

                    Console.WriteLine($"Found existing prescription in DB: {existingPrescription != null}");
                    Console.WriteLine($"Existing prescription ID: {existingPrescription?.Id}");
                    Console.WriteLine($"Existing prescription OPDVisitId: {existingPrescription?.OPDVisitId}");

                    Prescription prescription;
                    if (existingPrescription != null)
                    {
                        Console.WriteLine("UPDATING EXISTING PRESCRIPTION");
                        prescription = existingPrescription;

                        if (request.OverrideExisting)
                        {
                            Console.WriteLine("Clearing existing prescription items");
                            prescription.Medicines.Clear();
                            prescription.Complaints.Clear();
                            prescription.AdvisedItems.Clear();
                        }

                        // Update prescription details
                        if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.Diagnosis))
                            prescription.UpdateDiagnosis(request.ConsultationDetails.Diagnosis, performedBy);

                        if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.Advice))
                            prescription.UpdateInstructions(request.ConsultationDetails.Advice, performedBy);

                        await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
                    }
                    else
                    {
                        Console.WriteLine("CREATING NEW PRESCRIPTION");
                        prescription = new Prescription(
                            patient.Id,
                            request.DoctorId,
                            visit.Id,
                            performedBy);

                        Console.WriteLine($"New Prescription ID: {prescription.Id}");
                        Console.WriteLine($"New Prescription OPDVisitId: {prescription.OPDVisitId}");

                        if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.Diagnosis))
                            prescription.UpdateDiagnosis(request.ConsultationDetails.Diagnosis, performedBy);

                        if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.Advice))
                            prescription.UpdateInstructions(request.ConsultationDetails.Advice, performedBy);

                        await _unitOfWork.Repository<Prescription>().AddAsync(prescription);
                        Console.WriteLine($"Prescription added to context");
                    }

                    Console.WriteLine($"=== STEP 3.1: ADDING MEDICINES ===");
                    Console.WriteLine($"Number of medicines to add: {request.Medicines?.Count ?? 0}");

                    // Add medicines
                    int medicineCount = 0;
                    foreach (var medicineRequest in request.Medicines)
                    {
                        medicineCount++;
                        Console.WriteLine($"Adding medicine #{medicineCount}:");
                        Console.WriteLine($"  MedicineId: {medicineRequest.MedicineId}");
                        Console.WriteLine($"  DoseId: {medicineRequest.DoseId}");
                        Console.WriteLine($"  DurationDays: {medicineRequest.DurationDays}");
                        Console.WriteLine($"  Quantity: {medicineRequest.Quantity}");

                        try
                        {
                            prescription.AddMedicine(
                                medicineRequest.MedicineId,
                                medicineRequest.DoseId,
                                medicineRequest.DurationDays,
                                medicineRequest.Quantity,
                                medicineRequest.Instructions,
                                medicineRequest.CustomDosage,
                                performedBy);
                            Console.WriteLine($"  Medicine added successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ERROR adding medicine: {ex.Message}");
                            throw;
                        }
                    }

                    // Add advice
                    foreach (var adviceRequest in request.Advice)
                    {
                        prescription.AddAdvised(
                            adviceRequest.AdvisedId,
                            adviceRequest.CustomAdvice,
                            performedBy);
                    }

                    Console.WriteLine($"=== STEP 3.2: LINKING PRESCRIPTION ===");
                    Console.WriteLine($"existingPrescription == null: {existingPrescription == null}");
                    Console.WriteLine($"visit.PrescriptionId.HasValue: {visit.PrescriptionId.HasValue}");
                    Console.WriteLine($"visit.Prescription != null: {visit.Prescription != null}");
                    Console.WriteLine($"prescription.Id: {prescription.Id}");
                    Console.WriteLine($"prescription.OPDVisitId: {prescription.OPDVisitId}");

                    // Link prescription to visit (only if new)
                    if (existingPrescription == null)
                    {
                        Console.WriteLine("Attempting to link prescription...");
                        try
                        {
                            // TEMPORARY: Check entity state before linking
                            var tempVisit = await _unitOfWork.Repository<OPDVisit>().GetByIdAsync(visit.Id);
                            Console.WriteLine($"TEMP: Refetched visit PrescriptionId: {tempVisit.PrescriptionId}");
                            Console.WriteLine($"TEMP: Refetched visit Prescription != null: {tempVisit.Prescription != null}");

                            visit.LinkPrescription(prescription);
                            Console.WriteLine("Prescription linked successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR linking prescription: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            throw;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Skipping link - prescription already exists");
                    }

                    Console.WriteLine($"=== STEP 4: BILLING HANDLING ===");
                    // 4. Create Bill (only if doesn't exist)
                    OPDBill bill;
                    var existingBill = (await _unitOfWork.Repository<OPDBill>()
                        .FindAsync(b => b.OPDVisitId == visit.Id))
                        .FirstOrDefault();

                    if (existingBill != null)
                    {
                        Console.WriteLine("UPDATING EXISTING BILL");
                        bill = existingBill;

                        // Update bill details if needed
                        bill.UpdateDiscount(request.DiscountPercentage, performedBy);
                        bill.UpdateTax(request.TaxPercentage, performedBy);

                        await _unitOfWork.Repository<OPDBill>().UpdateAsync(bill);
                    }
                    else
                    {
                        Console.WriteLine("CREATING NEW BILL");
                        bill = new OPDBill(
                            visit.Id,
                            patient.Id,
                            request.DoctorId,
                            request.ConsultationFee,
                            performedBy);

                        bill.UpdateDiscount(request.DiscountPercentage, performedBy);
                        bill.UpdateTax(request.TaxPercentage, performedBy);
                        bill.GenerateBill(performedBy);

                        await _unitOfWork.Repository<OPDBill>().AddAsync(bill);
                    }

                    // Add payment if provided
                    if (request.Payment != null && request.Payment.Amount > 0)
                    {
                        bill.AddPayment(
                            request.Payment.Amount,
                            request.Payment.PaymentMode,
                            request.Payment.TransactionId,
                            performedBy);
                    }

                    // Link bill to visit (only if new)
                    if (existingBill == null)
                    {
                        Console.WriteLine("Linking bill to visit");
                        visit.LinkBill(bill);
                    }

                    Console.WriteLine($"=== STEP 5: COMPLETE VISIT ===");
                    // 5. Complete visit if not already completed
                    if (visit.Status != VisitStatus.Completed)
                    {
                        Console.WriteLine("Completing consultation");
                        visit.CompleteConsultation(null, performedBy);
                    }

                    Console.WriteLine($"=== STEP 6: SAVE CHANGES ===");
                    // 6. Save all changes
                    var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
                    Console.WriteLine($"SaveChanges result: {saveResult} records saved");

                    Console.WriteLine($"=== STEP 7: COMMIT TRANSACTION ===");
                    // Commit transaction
                    await transaction.CommitAsync();
                    Console.WriteLine("Transaction committed successfully");

                    Console.WriteLine($"=== STEP 8: PREPARE RESULT ===");
                    // 7. Prepare result
                    return new ConsultationResult
                    {
                        ConsultationId = visit.Id,
                        Patient = MapToPatientDto(patient),
                        Visit = MapToVisitDto(visit),
                        Prescription = await MapToPrescriptionDetailDto(prescription),
                        Bill = await MapToBillDetailDto(bill),
                        TotalAmount = bill.TotalAmount,
                        PaidAmount = bill.PaidAmount,
                        DueAmount = bill.DueAmount,
                        IsFullyPaid = bill.IsFullyPaid(),
                        ConsultationDate = visit.VisitDate,
                        FollowUpDate = visit.FollowUpDate
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== CONSULTATION FAILED ===");
                    Console.WriteLine($"Error Type: {ex.GetType().Name}");
                    Console.WriteLine($"Error Message: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }

                    // Transaction will auto-rollback when disposed if not committed
                    // No need to call RollbackAsync explicitly when using "await using"
                    throw;
                }
                finally
                {
                    Console.WriteLine($"=== EXECUTION STRATEGY END ===");
                }
            });
        }

        // Helper method to update visit details
        private void UpdateVisitDetails(OPDVisit visit, CompleteConsultationRequest request, Guid performedBy)
        {
            if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.ChiefComplaint))
            {
                visit.UpdateChiefComplaint(
                    request.ConsultationDetails.ChiefComplaint,
                    request.ConsultationDetails.HistoryOfPresentIllness,
                    request.ConsultationDetails.PastMedicalHistory,
                    request.ConsultationDetails.FamilyHistory,
                    performedBy);
            }

            // Update vitals if provided
            if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.BloodPressure) ||
                request.ConsultationDetails.Temperature.HasValue ||
                request.ConsultationDetails.Pulse.HasValue ||
                request.ConsultationDetails.RespiratoryRate.HasValue ||
                request.ConsultationDetails.SPO2.HasValue ||
                request.ConsultationDetails.Weight.HasValue ||
                request.ConsultationDetails.Height.HasValue)
            {
                visit.UpdateVitals(
                    request.ConsultationDetails.BloodPressure,
                    request.ConsultationDetails.Temperature,
                    request.ConsultationDetails.Pulse,
                    request.ConsultationDetails.RespiratoryRate,
                    request.ConsultationDetails.SPO2,
                    request.ConsultationDetails.Weight,
                    request.ConsultationDetails.Height,
                    performedBy);
            }

            // Update examination findings
            if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.GeneralExamination) ||
                !string.IsNullOrWhiteSpace(request.ConsultationDetails.SystemicExamination) ||
                !string.IsNullOrWhiteSpace(request.ConsultationDetails.LocalExamination))
            {
                visit.UpdateExaminationFindings(
                    request.ConsultationDetails.GeneralExamination,
                    request.ConsultationDetails.SystemicExamination,
                    request.ConsultationDetails.LocalExamination,
                    performedBy);
            }

            // Set diagnosis if provided
            if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.Diagnosis))
                visit.SetDiagnosis(request.ConsultationDetails.Diagnosis, performedBy);

            // Set treatment plan if provided
            if (!string.IsNullOrWhiteSpace(request.ConsultationDetails.TreatmentPlan))
                visit.SetTreatmentPlan(request.ConsultationDetails.TreatmentPlan, performedBy);

            // Add clinical notes if provided
            if (!string.IsNullOrWhiteSpace(request.ConsultationNotes))
                visit.AddClinicalNotes(request.ConsultationNotes, performedBy);

            // Set follow-up if requested
            if (request.SetFollowUp && request.FollowUpDays.HasValue)
            {
                visit.SetFollowUp(
                    DateTime.UtcNow.AddDays(request.FollowUpDays.Value),
                    request.FollowUpInstructions,
                    request.FollowUpDays,
                    performedBy);
            }
        }

        public async Task<ConsultationResult> QuickConsultationAsync(
    Guid patientId,
    Guid doctorId,
    string chiefComplaint,
    string diagnosis,
    List<Guid> commonMedicineIds,
    Guid performedBy,
    CancellationToken cancellationToken = default)
        {
            // Validate input
            if (patientId == Guid.Empty)
                throw new ArgumentException("Patient ID is required", nameof(patientId));

            if (doctorId == Guid.Empty)
                throw new ArgumentException("Doctor ID is required", nameof(doctorId));

            if (string.IsNullOrWhiteSpace(chiefComplaint))
                throw new ArgumentException("Chief complaint is required", nameof(chiefComplaint));

            if (string.IsNullOrWhiteSpace(diagnosis))
                throw new ArgumentException("Diagnosis is required", nameof(diagnosis));

            if (commonMedicineIds == null || !commonMedicineIds.Any())
                throw new ArgumentException("At least one medicine is required", nameof(commonMedicineIds));

            // Fetch all required data in parallel for better performance
            var doseTask = GetDefaultDoseId(cancellationToken);
            var feeTask = GetDoctorConsultationFee(doctorId);

            await Task.WhenAll(doseTask, feeTask).ConfigureAwait(false);

            var defaultDoseId = doseTask.Result;
            var consultationFee = feeTask.Result;
            // Prepare medicines list
            var medicines = commonMedicineIds.Select(id => new Application.Features.Prescriptions.DTOs.AddMedicineRequest
            {
                MedicineId = id,
                DoseId = defaultDoseId,
                DurationDays = 5, // Default 5 days for quick consultation
                Quantity = 1, // Default quantity
                Instructions = "After food" // Default instruction
            }).ToList();

            // Create consultation request
            var request = new CompleteConsultationRequest
            {
                IsNewPatient = false,
                ExistingPatientId = patientId,
                DoctorId = doctorId,
                ConsultationDetails = new Application.Features.Visits.DTOs.UpdateVisitRequest
                {
                    ChiefComplaint = chiefComplaint.Trim(),
                    Diagnosis = diagnosis.Trim()
                },
                Medicines = medicines,
                ConsultationFee = consultationFee,
                Payment = new Application.Features.Billing.DTOs.AddPaymentRequest
                {
                    Amount = consultationFee,
                    PaymentMode = "Cash",
                    TransactionId = $"QC_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"
                },
                ConsultationNotes = "Quick consultation - standard prescription"
            };

            // Process the consultation
            return await CompleteConsultationAsync(request, performedBy, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<ConsultationResult> ApplyTemplateConsultationAsync(
            Guid patientId,
            Guid doctorId,
            Guid templateId,
            Guid performedBy,
            CancellationToken cancellationToken = default)
        {
            // Get template
            var template = await _unitOfWork.Repository<PrescriptionTemplate>().GetByIdAsync(templateId);
            if (template == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(PrescriptionTemplate), templateId);

            // Create consultation request from template
            var request = new CompleteConsultationRequest
            {
                IsNewPatient = false,
                ExistingPatientId = patientId,
                DoctorId = doctorId,
                ConsultationFee = await GetDoctorConsultationFee(doctorId),
                Payment = new Application.Features.Billing.DTOs.AddPaymentRequest
                {
                    Amount = await GetDoctorConsultationFee(doctorId),
                    PaymentMode = "Cash"
                }
            };

            return await CompleteConsultationAsync(request, performedBy, cancellationToken);
        }

        public async Task<string> GenerateConsultationSummaryAsync(Guid visitId, CancellationToken cancellationToken = default)
        {
            var visit = await _unitOfWork.Repository<OPDVisit>().GetByIdAsync(visitId);
            if (visit == null)
                throw new Application.Common.Exceptions.NotFoundException(nameof(OPDVisit), visitId);

            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(visit.PatientId);
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(visit.DoctorId);

            var summary = $"Consultation Summary\n" +
                         $"====================\n" +
                         $"Date: {visit.VisitDate:dd/MM/yyyy HH:mm}\n" +
                         $"Patient: {patient?.Name} ({patient?.PatientNumber})\n" +
                         $"Doctor: {doctor?.Name} ({doctor?.Specialization})\n" +
                         $"Complaint: {visit.ChiefComplaint ?? "N/A"}\n" +
                         $"Diagnosis: {visit.Diagnosis ?? "N/A"}\n" +
                         $"Status: {visit.Status}\n";

            if (visit.FollowUpDate.HasValue)
                summary += $"Follow-up: {visit.FollowUpDate.Value:dd/MM/yyyy}\n";

            return summary;
        }

        public async Task<string> PrintCombinedSlipAsync(Guid visitId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Load visit with all related data in one query
                var visit = await _unitOfWork.OPDVisits.GetWithDetailsAsync(visitId, cancellationToken);

                if (visit == null)
                    throw new NotFoundException(nameof(OPDVisit), visitId);

                // If GetWithDetailsAsync doesn't load everything, fetch separately
                Prescription? prescription = visit.Prescription;
                OPDBill? bill = visit.Bill;

                // Fallback: If prescription not loaded, fetch it
                if (prescription == null && visit.PrescriptionId.HasValue)
                {
                    prescription = await _unitOfWork.Prescriptions.GetWithDetailsAsync(
                        visit.PrescriptionId.Value, cancellationToken);
                }

                // Fallback: If bill not loaded, fetch it
                if (bill == null && visit.OPDBillId.HasValue)
                {
                    bill = await _unitOfWork.Bills.GetWithItemsAsync(
                        visit.OPDBillId.Value, cancellationToken);
                }

                // Generate the slip
                return GenerateCombinedSlip(visit, prescription, bill);
            }
            catch (Exception ex)
            {
                // Log the error (use your preferred logging framework)
                // _logger.LogError(ex, "Error generating combined slip for visit {VisitId}", visitId);

                // Return a simple error message or rethrow
                return $"Error generating slip: {ex.Message}";
            }
        }

        public async Task<int> GetTodaysConsultationsCountAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var visits = await _unitOfWork.OPDVisits.GetVisitsByDoctorAsync(doctorId, cancellationToken);
            return visits.Count(v => v.VisitDate.Date == today);
        }

        public async Task<decimal> GetTodaysRevenueAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var bills = await _unitOfWork.Bills.GetBillsByDoctorAsync(doctorId, cancellationToken);
            return bills.Where(b => b.BillDate.Date == today && b.Status == BillStatus.Paid)
                       .Sum(b => b.TotalAmount);
        }

        public async Task<Dictionary<string, int>> GetCommonDiagnosesAsync(Guid doctorId, DateTime fromDate, CancellationToken cancellationToken = default)
        {
            var visits = await _unitOfWork.OPDVisits.GetVisitsByDoctorAsync(doctorId, cancellationToken);
            var recentVisits = visits.Where(v => v.VisitDate >= fromDate && !string.IsNullOrWhiteSpace(v.Diagnosis));

            return recentVisits
                .GroupBy(v => v.Diagnosis!)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        // Helper methods
        private async Task<Guid> GetDefaultDoseId(CancellationToken cancellationToken = default)
        {
            var defaultDoses = await _unitOfWork.MasterRepository
                .GetActiveDosesAsync(cancellationToken)
                .ConfigureAwait(false);

            if (defaultDoses == null)
                throw new InvalidOperationException("Failed to retrieve doses from the database");

            if (!defaultDoses.Any())
                throw new InvalidOperationException("No active doses configured in the system");

            // Try to get BD (twice daily) dose first, then OD (once daily), then any dose
            var bdDose = defaultDoses.FirstOrDefault(d =>
                d.Code.Equals("BD", StringComparison.OrdinalIgnoreCase));

            if (bdDose != null)
                return bdDose.Id;

            var odDose = defaultDoses.FirstOrDefault(d =>
                d.Code.Equals("OD", StringComparison.OrdinalIgnoreCase));

            if (odDose != null)
                return odDose.Id;

            // Return the first dose as fallback
            return defaultDoses.First().Id;
        }

        private async Task<decimal> GetDoctorConsultationFee(Guid doctorId)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId);
            return doctor?.ConsultationFee ?? 500; // Default 500 if not found
        }

        private string GenerateCombinedSlip(OPDVisit visit, Prescription? prescription, OPDBill? bill)
        {
            var slip = new StringBuilder();

            // Header
            slip.AppendLine("╔══════════════════════════════════════════╗");
            slip.AppendLine("║       MEDICAL CONSULTATION SLIP          ║");
            slip.AppendLine("╚══════════════════════════════════════════╝");
            slip.AppendLine();

            // Visit Information
            slip.AppendLine($"DATE:        {visit.VisitDate:dd/MM/yyyy hh:mm tt}");
            slip.AppendLine($"VISIT NO:    {visit.VisitNumber}");
            slip.AppendLine($"PATIENT:     {visit.Patient?.Name ?? "N/A"}");
            slip.AppendLine($"DOCTOR:      {visit.Doctor?.Name ?? "N/A"}");
            if (!string.IsNullOrWhiteSpace(visit.Doctor?.Specialization))
                slip.AppendLine($"SPECIALTY:   {visit.Doctor.Specialization}");
            slip.AppendLine();

            // Clinical Information
            slip.AppendLine("CLINICAL INFORMATION:");
            slip.AppendLine($"─────────────────────");
            slip.AppendLine($"Chief Complaint: {visit.ChiefComplaint ?? "N/A"}");
            slip.AppendLine($"Diagnosis:       {visit.Diagnosis ?? "N/A"}");

            if (!string.IsNullOrWhiteSpace(visit.TreatmentPlan))
            {
                slip.AppendLine($"Treatment Plan:  {visit.TreatmentPlan}");
            }
            slip.AppendLine();

            // Prescription Section
            if (prescription != null && prescription.Medicines.Any())
            {
                slip.AppendLine("PRESCRIPTION:");
                slip.AppendLine($"─────────────");

                int counter = 1;
                foreach (var medicine in prescription.Medicines)
                {
                    slip.AppendLine($"{counter}. {medicine.Medicine?.Name ?? "Unknown Medicine"}");
                    slip.AppendLine($"   Dosage: {medicine.GetDosageDisplay() ?? "As directed"}");
                    slip.AppendLine($"   Duration: {medicine.DurationDays} days");

                    if (!string.IsNullOrWhiteSpace(medicine.Instructions))
                        slip.AppendLine($"   Instructions: {medicine.Instructions}");

                    slip.AppendLine();
                    counter++;
                }
            }
            else
            {
                slip.AppendLine("PRESCRIPTION: No medicines prescribed");
                slip.AppendLine();
            }

            // Billing Section
            if (bill != null)
            {
                slip.AppendLine("BILLING DETAILS:");
                slip.AppendLine($"───────────────");
                slip.AppendLine($"Bill No:      {bill.BillNumber}");
                slip.AppendLine($"Bill Date:    {bill.BillDate:dd/MM/yyyy}");
                slip.AppendLine($"Total Amount: ₹{bill.TotalAmount:N2}");
                slip.AppendLine($"Paid Amount:  ₹{bill.PaidAmount:N2}");
                slip.AppendLine($"Due Amount:   ₹{bill.DueAmount:N2}");
                slip.AppendLine($"Status:       {bill.Status}");

                if (bill.BillItems.Any())
                {
                    slip.AppendLine();
                    slip.AppendLine("Items:");
                    foreach (var item in bill.BillItems)
                    {
                        slip.AppendLine($"  • {item.ItemName} x {item.Quantity}: ₹{item.GetTotalWithTax():N2}");
                    }
                }
            }
            else
            {
                slip.AppendLine("BILLING: No bill generated");
            }

            slip.AppendLine();
            slip.AppendLine("══════════════════════════════════════════");
            slip.AppendLine("Thank you for your visit!");
            slip.AppendLine("For queries, contact: [Clinic Phone/Email]");

            return slip.ToString();
        }

        // Mapping methods (simplified for brevity)
        private Application.Features.Patients.DTOs.PatientDto MapToPatientDto(Patient patient) => new();
        private Application.Features.Visits.DTOs.OPDVisitDto MapToVisitDto(OPDVisit visit) => new();
        private async Task<Application.Features.Prescriptions.DTOs.PrescriptionDetailDto> MapToPrescriptionDetailDto(Prescription prescription) => new();
        private async Task<Application.Features.Billing.DTOs.BillDetailDto> MapToBillDetailDto(OPDBill bill) => new();
    }
}