// DoctorService.cs
using CompileCares.API.Controllers;
using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Doctors.DTOs;
using CompileCares.UI.Services.AuthService;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompileCares.UI.Services.DoctorService
{
    public class DoctorService : IDoctorService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigationManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public DoctorService(
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

        #region Doctor CRUD Operations

        public async Task<ApiResponse<DoctorDto>> CreateDoctorAsync(CreateDoctorRequest request)
        {
            try
            {
                Console.WriteLine($"Creating doctor: {request.Name}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<DoctorDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/v1/doctors",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Doctor Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoctorDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoctorDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoctorDto>(response, json, "Create doctor");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoctorDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoctorDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DoctorDto>> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request)
        {
            try
            {
                Console.WriteLine($"Updating doctor: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<DoctorDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/v1/doctors/{id}",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Doctor Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoctorDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoctorDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoctorDto>(response, json, "Update doctor");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoctorDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoctorDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DoctorDto>> GetDoctorAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting doctor: {id}");

                var response = await _httpClient.GetAsync($"api/v1/doctors/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoctorDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoctorDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoctorDto>(response, json, "Get doctor");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoctorDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoctorDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Doctor Lists & Search

        public async Task<ApiResponse<PagedResponse<DoctorSummaryDto>>> SearchDoctorsAsync(DoctorSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching doctors with term: {request.SearchTerm}");

                // Build query string
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");

                if (!string.IsNullOrEmpty(request.Specialization))
                    queryParams.Add($"specialization={Uri.EscapeDataString(request.Specialization)}");

                if (request.IsAvailable.HasValue)
                    queryParams.Add($"isAvailable={request.IsAvailable.Value}");

                if (request.IsActive.HasValue)
                    queryParams.Add($"isActive={request.IsActive.Value}");

                if (request.IsVerified.HasValue)
                    queryParams.Add($"isVerified={request.IsVerified.Value}");

                queryParams.Add($"pageNumber={request.PageNumber}");
                queryParams.Add($"pageSize={request.PageSize}");
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy ?? "CreatedAt")}");
                queryParams.Add($"sortDescending={request.SortDescending}");

                var queryString = string.Join("&", queryParams);
                var url = $"api/v1/doctors/search?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<DoctorSummaryDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<DoctorSummaryDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PagedResponse<DoctorSummaryDto>>(response, json, "Search doctors");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PagedResponse<DoctorSummaryDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<DoctorSummaryDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<DoctorSummaryDto>>> GetAvailableDoctorsAsync()
        {
            try
            {
                Console.WriteLine($"Getting available doctors");

                var response = await _httpClient.GetAsync("api/v1/doctors/available");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<DoctorSummaryDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<DoctorSummaryDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<DoctorSummaryDto>>(response, json, "Get available doctors");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<DoctorSummaryDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<DoctorSummaryDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Doctor Operations

        public async Task<ApiResponse<string>> VerifyDoctorAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Verifying doctor: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsync(
                    $"api/v1/doctors/{id}/verify",
                    null);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Verify Doctor Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Doctor verified", result?.Message ?? "Doctor verified successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Verify doctor");
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

        public async Task<ApiResponse<string>> SetAvailabilityAsync(Guid id, bool isAvailable)
        {
            try
            {
                Console.WriteLine($"Setting availability for doctor {id} to: {isAvailable}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var request = new SetAvailabilityRequest { IsAvailable = isAvailable };
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/v1/doctors/{id}/availability",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Set Availability Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse(
                        $"Availability set to {(isAvailable ? "Available" : "Unavailable")}",
                        result?.Message ?? "Doctor availability updated successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Set availability");
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

        public async Task<ApiResponse<string>> ActivateDoctorAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Activating doctor: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsync(
                    $"api/v1/doctors/{id}/activate",
                    null);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Activate Doctor Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Doctor activated", result?.Message ?? "Doctor activated successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Activate doctor");
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

        public async Task<ApiResponse<string>> DeactivateDoctorAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Deactivating doctor: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsync(
                    $"api/v1/doctors/{id}/deactivate",
                    null);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Deactivate Doctor Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Doctor deactivated", result?.Message ?? "Doctor deactivated successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Deactivate doctor");
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

        public async Task<ApiResponse<string>> UpdateSignatureAsync(Guid id, UpdateSignatureRequest request)
        {
            try
            {
                Console.WriteLine($"Updating signature for doctor: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/v1/doctors/{id}/signature",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Signature Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Signature updated", result?.Message ?? "Doctor signature updated successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Update signature");
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

        #region Statistics

        public async Task<ApiResponse<DoctorStatisticsDto>> GetDoctorStatisticsAsync()
        {
            try
            {
                Console.WriteLine($"Getting doctor statistics");

                var response = await _httpClient.GetAsync("api/v1/doctors/statistics");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoctorStatisticsDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoctorStatisticsDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoctorStatisticsDto>(response, json, "Get doctor statistics");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoctorStatisticsDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoctorStatisticsDto>.ErrorResponse($"Error: {ex.Message}");
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
    }
}