using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.Pharmacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Application.Services
{
    public interface IPharmacyService
    {
        Task<Medicine> GetMedicineAsync(Guid medicineId);
        Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm, int pageSize = 10);
        Task<bool> CheckMedicineStockAsync(Guid medicineId, int requiredQuantity);
        Task<IEnumerable<Prescription>> GetPendingDispensingPrescriptionsAsync();
        Task<Prescription> DispensePrescriptionAsync(Guid prescriptionId, Guid dispensedBy);
    }

}
