using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Visits.DTOs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompileCares.UI.Services.VisitService
{
    public class VisitService : IVisitService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly NavigationManager _navigationManager;

        public VisitService(
            HttpClient httpClient,
            NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<ApiResponse<OPDVisitDto>> CreateVisitAsync(CreateVisitRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/visits", request, _jsonOptions);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error creating visit: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> GetVisitAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v1/visits/{id}");
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error getting visit: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> UpdateVisitAsync(Guid id, UpdateVisitRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/v1/visits/{id}", request, _jsonOptions);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error updating visit: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> StartConsultationAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/v1/visits/{id}/start-consultation", null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error starting consultation: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> CompleteConsultationAsync(Guid id, int? consultationDurationMinutes = null)
        {
            try
            {
                var url = $"api/v1/visits/{id}/complete-consultation";
                if (consultationDurationMinutes.HasValue)
                {
                    url += $"?consultationDurationMinutes={consultationDurationMinutes.Value}";
                }

                var response = await _httpClient.PostAsync(url, null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error completing consultation: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> CancelVisitAsync(Guid id, string reason)
        {
            try
            {
                var url = $"api/v1/visits/{id}/cancel?reason={Uri.EscapeDataString(reason)}";
                var response = await _httpClient.PostAsync(url, null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error cancelling visit: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> MarkAsNoShowAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/v1/visits/{id}/mark-no-show", null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error marking as no-show: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> UpdateVitalsAsync(Guid id, VisitVitalsDto vitals)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/v1/visits/{id}/vitals", vitals, _jsonOptions);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error updating vitals: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VisitVitalsDto>> GetVitalsAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v1/visits/{id}/vitals");
                return await HandleResponse<VisitVitalsDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<VisitVitalsDto>.ErrorResponse($"Error getting vitals: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> SetDiagnosisAsync(Guid id, string diagnosis)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/v1/visits/{id}/diagnosis", diagnosis, _jsonOptions);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error setting diagnosis: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> SetTreatmentPlanAsync(Guid id, string treatmentPlan)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/v1/visits/{id}/treatment-plan", treatmentPlan, _jsonOptions);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error setting treatment plan: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> AddClinicalNotesAsync(Guid id, string notes)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/v1/visits/{id}/clinical-notes", notes, _jsonOptions);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error adding clinical notes: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> SetFollowUpAsync(Guid id, DateTime followUpDate, string? instructions = null)
        {
            try
            {
                var url = $"api/v1/visits/{id}/follow-up?followUpDate={followUpDate:yyyy-MM-ddTHH:mm:ss}";
                if (!string.IsNullOrEmpty(instructions))
                {
                    url += $"&instructions={Uri.EscapeDataString(instructions)}";
                }

                var response = await _httpClient.PutAsync(url, null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error setting follow-up: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> ReferToDoctorAsync(Guid id, Guid referredToDoctorId, string reason)
        {
            try
            {
                var url = $"api/v1/visits/{id}/refer?referredToDoctorId={referredToDoctorId}&reason={Uri.EscapeDataString(reason)}";
                var response = await _httpClient.PostAsync(url, null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error referring to doctor: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<VisitSummaryDto>>> SearchVisitsAsync(VisitSearchRequest request)
        {
            try
            {
                // Build query string with all parameters
                var queryParams = new List<string>();

                if (request.PatientId.HasValue)
                    queryParams.Add($"patientId={request.PatientId}");

                if (request.DoctorId.HasValue)
                    queryParams.Add($"doctorId={request.DoctorId}");

                if (!string.IsNullOrEmpty(request.VisitNumber))
                    queryParams.Add($"visitNumber={Uri.EscapeDataString(request.VisitNumber)}");

                if (request.VisitType.HasValue)
                    queryParams.Add($"visitType={(int)request.VisitType.Value}");

                if (request.Status.HasValue)
                    queryParams.Add($"status={(int)request.Status.Value}");

                if (request.VisitDateFrom.HasValue)
                    queryParams.Add($"visitDateFrom={request.VisitDateFrom.Value:yyyy-MM-ddTHH:mm:ss}");

                if (request.VisitDateTo.HasValue)
                    queryParams.Add($"visitDateTo={request.VisitDateTo.Value:yyyy-MM-ddTHH:mm:ss}");

                if (request.HasFollowUp.HasValue)
                    queryParams.Add($"hasFollowUp={request.HasFollowUp.Value.ToString().ToLower()}");

                if (request.HasPrescription.HasValue)
                    queryParams.Add($"hasPrescription={request.HasPrescription.Value.ToString().ToLower()}");

                if (request.HasBill.HasValue)
                    queryParams.Add($"hasBill={request.HasBill.Value.ToString().ToLower()}");

                // Add pagination parameters
                queryParams.Add($"pageNumber={request.PageNumber}");
                queryParams.Add($"pageSize={request.PageSize}");

                // Add sorting parameters
                if (!string.IsNullOrEmpty(request.SortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

                queryParams.Add($"sortDescending={request.SortDescending.ToString().ToLower()}");

                var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
                var response = await _httpClient.GetAsync($"api/v1/visits/search{queryString}");

                return await HandleResponse<List<VisitSummaryDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VisitSummaryDto>>.ErrorResponse($"Error searching visits: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<VisitSummaryDto>>> GetTodaysVisitsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/v1/visits/today");
                return await HandleResponse<List<VisitSummaryDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VisitSummaryDto>>.ErrorResponse($"Error getting today's visits: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VisitStatisticsDto>> GetVisitStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/v1/visits/statistics");
                return await HandleResponse<VisitStatisticsDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<VisitStatisticsDto>.ErrorResponse($"Error getting statistics: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<DoctorScheduleDto>>> GetDoctorSchedulesAsync(DateTime? date = null)
        {
            try
            {
                var url = "api/v1/visits/doctor-schedules";
                if (date.HasValue)
                {
                    url += $"?date={date:yyyy-MM-dd}";
                }

                var response = await _httpClient.GetAsync(url);
                return await HandleResponse<List<DoctorScheduleDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<DoctorScheduleDto>>.ErrorResponse($"Error getting doctor schedules: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ValidateForPrescriptionAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v1/visits/{id}/validate-for-prescription");
                return await HandleResponse<bool>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error validating for prescription: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDVisitDto>> RestoreVisitAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/v1/visits/{id}/restore", null);
                return await HandleResponse<OPDVisitDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDVisitDto>.ErrorResponse($"Error restoring visit: {ex.Message}");
            }
        }

        private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                    return apiResponse ?? ApiResponse<T>.ErrorResponse("Invalid response from server");
                }
                catch (JsonException ex)
                {
                    return ApiResponse<T>.ErrorResponse($"Error parsing response: {ex.Message}");
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _navigationManager.NavigateTo("/login");
                return ApiResponse<T>.ErrorResponse("Authentication required");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return ApiResponse<T>.ErrorResponse("You don't have permission to perform this action");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                return ApiResponse<T>.ErrorResponse(error?.Message ?? "Resource not found");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                return ApiResponse<T>.ErrorResponse(error?.Message ?? "Invalid request");
            }
            else
            {
                return ApiResponse<T>.ErrorResponse($"Server error: {response.StatusCode}");
            }
        }
    }
}
