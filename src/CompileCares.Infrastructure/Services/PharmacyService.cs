//using CompileCares.Application.Common.Exceptions;
//using CompileCares.Application.Common.Interfaces;
//using CompileCares.Application.Services;
//using CompileCares.Core.Entities.Clinical;
//using CompileCares.Core.Entities.Pharmacy;
//using CompileCares.Shared.Enums;
//using Microsoft.EntityFrameworkCore;
//using System.ComponentModel.DataAnnotations;
//using static CompileCares.Application.Features.Pharmacy.DTOs.MedicineDto;

//namespace CompileCares.Infrastructure.Services
//{
//    public class PharmacyService : IPharmacyService
//    {
//        private readonly IUnitOfWork _unitOfWork;

//        public PharmacyService(IUnitOfWork unitOfWork)
//        {
//            _unitOfWork = unitOfWork;
//        }
//        public async Task<Medicine> GetMedicineAsync(Guid medicineId)
//        {
//            var medicine = await _unitOfWork.Repository<Medicine>()
//                .GetQueryable()
//                .FirstOrDefaultAsync(m => m.Id == medicineId && !m.IsDeleted);

//            if (medicine == null)
//                throw new NotFoundException(nameof(Medicine), medicineId);

//            return medicine;
//        }

//        public async Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm, int pageSize = 10)
//        {
//            if (string.IsNullOrWhiteSpace(searchTerm))
//                return Enumerable.Empty<Medicine>();

//            var medicines = await _unitOfWork.Repository<Medicine>()
//                .GetQueryable()
//                .Where(m => !m.IsDeleted &&
//                           (m.Name.Contains(searchTerm) ||
//                            m.GenericName != null && m.GenericName.Contains(searchTerm) ||
//                            m.Manufacturer != null && m.Manufacturer.Contains(searchTerm)))
//                .OrderBy(m => m.Name)
//                .Take(pageSize)
//                .ToListAsync();

//            return medicines;
//        }

//        public async Task<bool> CheckMedicineStockAsync(Guid medicineId, int requiredQuantity)
//        {
//            var medicine = await GetMedicineAsync(medicineId);

//            // Check if current stock meets requirement
//            if (medicine.CurrentStock >= requiredQuantity)
//                return true;

//            // Check if we have sufficient stock considering reorder level
//            if (medicine.CurrentStock + (medicine.ReorderQuantity ?? 0) >= requiredQuantity)
//            {
//                // We can fulfill after restocking
//                return true;
//            }

//            return false;
//        }

//        public async Task<IEnumerable<Prescription>> GetPendingDispensingPrescriptionsAsync()
//        {
//            var prescriptions = await _unitOfWork.Repository<Prescription>()
//                .GetQueryable()
//                .Where(p => !p.IsDeleted &&
//                           p.Status == PrescriptionStatus.Active && // Only active prescriptions
//                           p.Medicines.Any(m => !m.IsDispensed))   // With undispensed medicines
//                .Include(p => p.Patient)
//                .Include(p => p.Doctor)
//                .Include(p => p.Medicines)
//                    .ThenInclude(m => m.Medicine)
//                .OrderBy(p => p.PrescriptionDate)
//                .ToListAsync();

//            return prescriptions;
//        }

//        public async Task<Prescription> DispensePrescriptionAsync(Guid prescriptionId, Guid dispensedBy)
//        {
//            var prescription = await _unitOfWork.Repository<Prescription>()
//                .GetQueryable()
//                .Where(p => p.Id == prescriptionId && !p.IsDeleted)
//                .Include(p => p.Medicines)
//                    .ThenInclude(m => m.Medicine)
//                .FirstOrDefaultAsync();

//            if (prescription == null)
//                throw new NotFoundException(nameof(Prescription), prescriptionId);

//            if (prescription.Status != PrescriptionStatus.Active)
//                throw new ValidationException($"Cannot dispense prescription with status: {prescription.Status}");

//            // Check stock for all medicines
//            var outOfStockMedicines = new List<string>();
//            foreach (var medicine in prescription.Medicines.Where(m => !m.IsDispensed))
//            {
//                if (medicine.Medicine != null && medicine.Medicine.CurrentStock < medicine.Quantity)
//                {
//                    outOfStockMedicines.Add(medicine.Medicine.Name);
//                }
//            }

//            if (outOfStockMedicines.Any())
//            {
//                throw new ValidationException($"Insufficient stock for medicines: {string.Join(", ", outOfStockMedicines)}");
//            }

//            // Mark all medicines as dispensed and reduce stock
//            foreach (var medicine in prescription.Medicines.Where(m => !m.IsDispensed))
//            {
//                if (medicine.Medicine != null)
//                {
//                    medicine.MarkAsDispensed("Pharmacy System", dispensedBy);

//                    // Reduce stock
//                    medicine.Medicine.ReduceStock(medicine.Quantity, dispensedBy);

//                    // Check and update reorder status if needed
//                    if (medicine.Medicine.CurrentStock <= medicine.Medicine.ReorderLevel)
//                    {
//                        medicine.Medicine.MarkForReorder(dispensedBy);
//                    }
//                }
//            }

//            // Update prescription
//            prescription.SetUpdatedBy(dispensedBy);

//            await _unitOfWork.Repository<Prescription>().UpdateAsync(prescription);
//            await _unitOfWork.SaveChangesAsync();

//            return prescription;
//        }

//        // Additional methods that might be useful
//        public async Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync()
//        {
//            var medicines = await _unitOfWork.Repository<Medicine>()
//                .GetQueryable()
//                .Where(m => !m.IsDeleted &&
//                           m.CurrentStock <= m.ReorderLevel &&
//                           m.IsActive)
//                .OrderBy(m => m.CurrentStock)
//                .Take(50)
//                .ToListAsync();

//            return medicines;
//        }

//        public async Task<IEnumerable<Medicine>> GetExpiringMedicinesAsync(int daysThreshold = 30)
//        {
//            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

//            var medicines = await _unitOfWork.Repository<Medicine>()
//                .GetQueryable()
//                .Where(m => !m.IsDeleted &&
//                           m.ExpiryDate.HasValue &&
//                           m.ExpiryDate.Value <= thresholdDate &&
//                           m.ExpiryDate.Value > DateTime.UtcNow &&
//                           m.CurrentStock > 0)
//                .OrderBy(m => m.ExpiryDate)
//                .Take(50)
//                .ToListAsync();

//            return medicines;
//        }

//        public async Task<Medicine> UpdateMedicineStockAsync(Guid medicineId, int quantity, StockUpdateType updateType, string batchNumber, Guid updatedBy)
//        {
//            var medicine = await GetMedicineAsync(medicineId);

//            switch (updateType)
//            {
//                case StockUpdateType.Received:
//                    medicine.AddStock(quantity, batchNumber, updatedBy);
//                    break;
//                case StockUpdateType.Dispensed:
//                    medicine.ReduceStock(quantity, updatedBy);
//                    break;
//                case StockUpdateType.Adjusted:
//                    medicine.AdjustStock(quantity, updatedBy);
//                    break;
//                case StockUpdateType.Damaged:
//                    medicine.MarkAsDamaged(quantity, updatedBy);
//                    break;
//                case StockUpdateType.Expired:
//                    medicine.MarkAsExpired(quantity, updatedBy);
//                    break;
//                default:
//                    throw new ArgumentException("Invalid stock update type");
//            }

//            await _unitOfWork.Repository<Medicine>().UpdateAsync(medicine);
//            await _unitOfWork.SaveChangesAsync();

//            return medicine;
//        }

//        // Helper method for the Medicine entity
//        public async Task<IEnumerable<MedicineBatch>> GetMedicineBatchesAsync(Guid medicineId)
//        {
//            var batches = await _unitOfWork.Repository<MedicineBatch>()
//                .GetQueryable()
//                .Where(b => b.MedicineId == medicineId && !b.IsDeleted)
//                .OrderByDescending(b => b.ExpiryDate)
//                .ToListAsync();

//            return batches;
//        }
//    }    
//}