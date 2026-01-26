// PatientService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.UI.Services.AuthService;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompileCares.UI.Services.PatientService
{
    public class PatientService : IPatientService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigationManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public PatientService(
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

        #region Patient CRUD Operations

        public async Task<ApiResponse<PatientDto>> CreatePatientAsync(CreatePatientRequest request)
        {
            try
            {
                Console.WriteLine($"Creating patient: {request.Name}");

                // Get current user ID for header
                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PatientDto>.ErrorResponse("User not authenticated");
                }

                // Add user ID to headers
                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/patients",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Patient Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PatientDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PatientDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PatientDto>(response, json, "Create patient");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientRequest request)
        {
            try
            {
                Console.WriteLine($"Updating patient: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PatientDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/patients/{id}",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Patient Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PatientDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PatientDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PatientDto>(response, json, "Update patient");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PatientDto>> GetPatientAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting patient: {id}");

                var response = await _httpClient.GetAsync($"api/patients/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PatientDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PatientDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PatientDto>(response, json, "Get patient");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeactivatePatientAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Deactivating patient: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<bool>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.DeleteAsync($"api/patients/{id}/deactivate");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<bool>.SuccessResponse(true, result?.Message ?? "Patient deactivated successfully");
                }
                else
                {
                    return await HandleErrorResponse<bool>(response, json, "Deactivate patient");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Patient Search & Lists

        public async Task<ApiResponse<PagedResponse<PatientSummaryDto>>> SearchPatientsAsync(PatientSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching patients with term: {request.SearchTerm}");

                // Build query string
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");

                if (!string.IsNullOrEmpty(request.PatientNumber))
                    queryParams.Add($"patientNumber={Uri.EscapeDataString(request.PatientNumber)}");

                if (!string.IsNullOrEmpty(request.Mobile))
                    queryParams.Add($"mobile={Uri.EscapeDataString(request.Mobile)}");

                if (!string.IsNullOrEmpty(request.Name))
                    queryParams.Add($"name={Uri.EscapeDataString(request.Name)}");

                if (request.IsActive.HasValue)
                    queryParams.Add($"isActive={request.IsActive.Value}");

                if (request.CreatedFrom.HasValue)
                    queryParams.Add($"createdFrom={request.CreatedFrom.Value:yyyy-MM-dd}");

                if (request.CreatedTo.HasValue)
                    queryParams.Add($"createdTo={request.CreatedTo.Value:yyyy-MM-dd}");

                queryParams.Add($"pageNumber={request.PageNumber}");
                queryParams.Add($"pageSize={request.PageSize}");
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy ?? "CreatedAt")}");
                queryParams.Add($"sortDescending={request.SortDescending}");

                var queryString = string.Join("&", queryParams);
                var url = $"api/patients/search?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<PatientSummaryDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<PatientSummaryDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PagedResponse<PatientSummaryDto>>(response, json, "Search patients");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PagedResponse<PatientSummaryDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<PatientSummaryDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<PatientSummaryDto>>> GetActivePatientsAsync()
        {
            try
            {
                Console.WriteLine($"Getting active patients");

                // Create search request for active patients only
                var request = new PatientSearchRequest
                {
                    IsActive = true,
                    PageSize = 100 // Get more results for dropdown
                };

                var response = await SearchPatientsAsync(request);

                if (response.Success && response.Data != null)
                {
                    var patients = response.Data.Data.ToList();
                    return ApiResponse<List<PatientSummaryDto>>.SuccessResponse(
                        patients,
                        $"Found {patients.Count} active patients");
                }
                else
                {
                    return ApiResponse<List<PatientSummaryDto>>.ErrorResponse(response.Message ?? "Failed to get active patients");
                }
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PatientSummaryDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Quick Operations

        public async Task<ApiResponse<PatientDto>> QuickCreatePatientAsync(PatientQuickCreateRequest request)
        {
            try
            {
                Console.WriteLine($"Quick creating patient: {request.Name}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PatientDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                // Convert to full CreatePatientRequest
                var fullRequest = new CreatePatientRequest
                {
                    Name = request.Name,
                    Title = request.Title,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    Mobile = request.Mobile,
                    Address = request.Address,
                    MaritalStatus = Shared.Enums.MaritalStatus.Single
                };

                // Call the regular create endpoint
                return await CreatePatientAsync(fullRequest);
            }
            catch (Exception ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Error in quick create: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PatientDto>> GetPatientByMobileAsync(string mobile)
        {
            try
            {
                Console.WriteLine($"Getting patient by mobile: {mobile}");

                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return ApiResponse<PatientDto>.ErrorResponse("Mobile number is required");
                }

                // Search by mobile
                var request = new PatientSearchRequest
                {
                    Mobile = mobile,
                    PageSize = 1
                };

                var response = await SearchPatientsAsync(request);

                if (response.Success && response.Data != null && response.Data.Data.Any())
                {
                    var patientSummary = response.Data.Data.First();

                    // Get full patient details
                    return await GetPatientAsync(patientSummary.Id);
                }
                else
                {
                    return ApiResponse<PatientDto>.ErrorResponse($"Patient with mobile {mobile} not found");
                }
            }
            catch (Exception ex)
            {
                return ApiResponse<PatientDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CheckPatientExistsAsync(string mobile)
        {
            try
            {
                Console.WriteLine($"Checking patient exists: {mobile}");

                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return ApiResponse<bool>.SuccessResponse(false, "Mobile number is empty");
                }

                var response = await GetPatientByMobileAsync(mobile);
                return ApiResponse<bool>.SuccessResponse(
                    response.Success && response.Data != null,
                    response.Success ? "Patient exists" : "Patient not found"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Statistics

        public async Task<ApiResponse<PatientStatisticsDto>> GetPatientStatisticsAsync()
        {
            try
            {
                Console.WriteLine($"Getting patient statistics");

                var response = await _httpClient.GetAsync("api/patients/statistics");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PatientStatisticsDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PatientStatisticsDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PatientStatisticsDto>(response, json, "Get patient statistics");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PatientStatisticsDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PatientStatisticsDto>.ErrorResponse($"Error: {ex.Message}");
            }
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

        #region Health Check

        public async Task<ApiResponse<bool>> HealthCheckAsync()
        {
            try
            {
                Console.WriteLine($"Checking patient service health");

                var response = await _httpClient.GetAsync("api/patients/health");
                if (response.IsSuccessStatusCode)
                {
                    return ApiResponse<bool>.SuccessResponse(true, "Patient service is healthy");
                }
                else
                {
                    return ApiResponse<bool>.ErrorResponse("Patient service is not responding");
                }
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Health check failed: {ex.Message}");
            }
        }

        #endregion
    }
}