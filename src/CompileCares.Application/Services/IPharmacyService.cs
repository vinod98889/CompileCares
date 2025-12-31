// File: IPharmacyService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Pharmacy.DTOs;

namespace CompileCares.Application.Features.Pharmacy.Services
{
    public interface IPharmacyService
    {
        Task<MedicineDto> CreateMedicineAsync(CreateMedicineRequest request, Guid createdBy);
        Task<MedicineDto> UpdateMedicineAsync(Guid id, UpdateMedicineRequest request, Guid updatedBy);
        Task<MedicineDto> GetMedicineAsync(Guid id);
        Task<PagedResponse<MedicineDto>> SearchMedicinesAsync(MedicineSearchRequest request);
        Task<MedicineStatisticsDto> GetMedicineStatisticsAsync();
        Task<MedicineDto> UpdateStockAsync(Guid medicineId, StockUpdateRequest request, Guid updatedBy);
        Task<bool> CheckAvailabilityAsync(Guid medicineId, int quantity);
    }
}