// ConsultationService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Consultations.DTOs;
using CompileCares.UI.Services.AuthService;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.ConsultationService
{
    public class ConsultationService : IConsultationService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigationManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConsultationService(
            HttpClient httpClient,
            IAuthService authService,
            NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _authService = authService;
            _navigationManager = navigationManager;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region Consultation Creation Methods

        public async Task<ApiResponse<ConsultationResult>> CompleteConsultationAsync(CompleteConsultationRequest request)
        {
            try
            {
                Console.WriteLine($"Starting complete consultation for doctor: {request.DoctorId}");

                // Get current user ID for header
                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ConsultationResult>.ErrorResponse("User not authenticated");
                }

                // Add user ID to headers
                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/consultations/complete",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Complete Consultation Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ConsultationResult>>(json, _jsonOptions);
                    return result ?? ApiResponse<ConsultationResult>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ConsultationResult>(response, json, "Complete consultation");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConsultationResult>> QuickConsultationAsync(QuickConsultationRequest request)
        {
            try
            {
                Console.WriteLine($"Starting quick consultation for patient: {request.PatientId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ConsultationResult>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/consultations/quick",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Quick Consultation Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ConsultationResult>>(json, _jsonOptions);
                    return result ?? ApiResponse<ConsultationResult>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ConsultationResult>(response, json, "Quick consultation");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConsultationResult>> UltraQuickConsultationAsync(UltraQuickConsultationRequest request)
        {
            try
            {
                Console.WriteLine($"Starting ultra-quick consultation. New Patient: {request.IsNewPatient}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ConsultationResult>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/consultations/ultra-quick",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ultra-Quick Consultation Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ConsultationResult>>(json, _jsonOptions);
                    return result ?? ApiResponse<ConsultationResult>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ConsultationResult>(response, json, "Ultra-quick consultation");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConsultationResult>> ApplyTemplateConsultationAsync(TemplateConsultationRequest request)
        {
            try
            {
                Console.WriteLine($"Applying template consultation. Template: {request.TemplateId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ConsultationResult>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/consultations/template",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Template Consultation Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ConsultationResult>>(json, _jsonOptions);
                    return result ?? ApiResponse<ConsultationResult>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ConsultationResult>(response, json, "Template consultation");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ConsultationResult>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Consultation Retrieval Methods

        public async Task<ApiResponse<string>> GetConsultationSummaryAsync(Guid visitId)
        {
            try
            {
                Console.WriteLine($"Getting consultation summary for visit: {visitId}");

                var response = await _httpClient.GetAsync($"api/consultations/{visitId}/summary");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<string>>(json, _jsonOptions);
                    return result ?? ApiResponse<string>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Get consultation summary");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<string>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> PrintConsultationSlipAsync(Guid visitId)
        {
            try
            {
                Console.WriteLine($"Generating print slip for visit: {visitId}");

                var response = await _httpClient.GetAsync($"api/consultations/{visitId}/print");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<string>>(json, _jsonOptions);
                    return result ?? ApiResponse<string>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Print consultation slip");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<string>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Statistics & Dashboard Methods

        public async Task<ApiResponse<TodaysStatsDto>> GetTodaysStatsAsync(Guid doctorId)
        {
            try
            {
                Console.WriteLine($"Getting today's stats for doctor: {doctorId}");

                var response = await _httpClient.GetAsync($"api/consultations/doctor/{doctorId}/today/stats");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<TodaysStatsDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<TodaysStatsDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<TodaysStatsDto>(response, json, "Get today's stats");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<TodaysStatsDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<TodaysStatsDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Dictionary<string, int>>> GetCommonDiagnosesAsync(Guid doctorId, DateTime? fromDate = null)
        {
            try
            {
                var url = $"api/consultations/doctor/{doctorId}/common-diagnoses";

                if (fromDate.HasValue)
                {
                    url += $"?fromDate={fromDate.Value:yyyy-MM-dd}";
                }

                Console.WriteLine($"Getting common diagnoses for doctor: {doctorId}");

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, int>>>(json, _jsonOptions);
                    return result ?? ApiResponse<Dictionary<string, int>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<Dictionary<string, int>>(response, json, "Get common diagnoses");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<Dictionary<string, int>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<Dictionary<string, int>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConsultationDashboardDto>> GetConsultationDashboardAsync(Guid doctorId, DateTime? date = null)
        {
            try
            {
                var url = $"api/consultations/dashboard?doctorId={doctorId}";

                if (date.HasValue)
                {
                    url += $"&date={date.Value:yyyy-MM-dd}";
                }

                Console.WriteLine($"Getting dashboard for doctor: {doctorId}");

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ConsultationDashboardDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<ConsultationDashboardDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ConsultationDashboardDto>(response, json, "Get dashboard");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ConsultationDashboardDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ConsultationDashboardDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        public async Task<string> GeneratePrescriptionPrintUrl(Guid consultationId)
        {
            var baseUrl = _navigationManager.BaseUri;
            return $"{baseUrl}api/consultations/{consultationId}/prescription/print";
        }

        public async Task<string> GenerateBillReceiptUrl(Guid consultationId)
        {
            var baseUrl = _navigationManager.BaseUri;
            return $"{baseUrl}api/consultations/{consultationId}/bill/print";
        }

        #endregion

        #region Helper Methods

        private async Task<ApiResponse<T>> HandleErrorResponse<T>(HttpResponseMessage response, string json, string operation)
        {
            try
            {
                Console.WriteLine($"{operation} failed with status: {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token expired
                    _authService.Logout();
                    return ApiResponse<T>.ErrorResponse("Session expired. Please login again.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<T>.ErrorResponse(errorResponse?.Message ?? "Resource not found");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<T>.ErrorResponse(errorResponse?.Message ?? "Bad request");
                }
                else
                {
                    // Try to parse error message
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                        return ApiResponse<T>.ErrorResponse(errorResponse?.Message ?? $"{operation} failed");
                    }
                    catch
                    {
                        return ApiResponse<T>.ErrorResponse($"{operation} failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResponse<T>.ErrorResponse($"{operation} error: {ex.Message}");
            }
        }

        #endregion
    }
}