// PrescriptionService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.UI.Services.AuthService;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompileCares.UI.Services.PrescriptionService
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigationManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public PrescriptionService(
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

        #region Prescription CRUD Operations

        public async Task<ApiResponse<PrescriptionDetailDto>> CreatePrescriptionAsync(CreatePrescriptionRequest request)
        {
            try
            {
                Console.WriteLine($"Creating prescription for patient: {request.PatientId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PrescriptionDetailDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/prescriptions",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Prescription Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Create prescription");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PrescriptionDetailDto>> GetPrescriptionAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting prescription: {id}");

                var response = await _httpClient.GetAsync($"api/prescriptions/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Get prescription");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PrescriptionDetailDto>> AddMedicineAsync(Guid prescriptionId, AddMedicineRequest request)
        {
            try
            {
                Console.WriteLine($"Adding medicine to prescription: {prescriptionId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PrescriptionDetailDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    $"api/prescriptions/{prescriptionId}/medicines",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Add Medicine Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Add medicine");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PrescriptionDetailDto>> ApplyTemplateAsync(Guid prescriptionId, ApplyTemplateRequest request)
        {
            try
            {
                Console.WriteLine($"Applying template to prescription: {prescriptionId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PrescriptionDetailDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    $"api/prescriptions/{prescriptionId}/apply-template",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Apply Template Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Apply template");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PrescriptionDetailDto>> CompletePrescriptionAsync(Guid prescriptionId)
        {
            try
            {
                Console.WriteLine($"Completing prescription: {prescriptionId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PrescriptionDetailDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsync(
                    $"api/prescriptions/{prescriptionId}/complete",
                    null);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Complete Prescription Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Complete prescription");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> CancelPrescriptionAsync(Guid prescriptionId, string reason)
        {
            try
            {
                Console.WriteLine($"Cancelling prescription: {prescriptionId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var queryString = $"reason={Uri.EscapeDataString(reason)}";
                var response = await _httpClient.DeleteAsync($"api/prescriptions/{prescriptionId}?{queryString}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Prescription cancelled", result?.Message ?? "Prescription cancelled successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Cancel prescription");
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

        #region Patient Prescriptions

        public async Task<ApiResponse<List<PrescriptionDto>>> GetPatientPrescriptionsAsync(Guid patientId)
        {
            try
            {
                Console.WriteLine($"Getting prescriptions for patient: {patientId}");

                var response = await _httpClient.GetAsync($"api/prescriptions/patient/{patientId}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<PrescriptionDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<PrescriptionDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<PrescriptionDto>>(response, json, "Get patient prescriptions");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<PrescriptionDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PrescriptionDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Search & Lists

        public async Task<ApiResponse<List<PrescriptionDto>>> SearchPrescriptionsAsync(PrescriptionSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching prescriptions");

                // Build query string
                var queryParams = new List<string>();

                if (request.PatientId.HasValue)
                    queryParams.Add($"patientId={request.PatientId.Value}");

                if (request.DoctorId.HasValue)
                    queryParams.Add($"doctorId={request.DoctorId.Value}");

                if (request.VisitId.HasValue)
                    queryParams.Add($"visitId={request.VisitId.Value}");

                if (!string.IsNullOrEmpty(request.PrescriptionNumber))
                    queryParams.Add($"prescriptionNumber={Uri.EscapeDataString(request.PrescriptionNumber)}");

                if (request.Status.HasValue)
                    queryParams.Add($"status={request.Status.Value}");

                if (request.PrescriptionDateFrom.HasValue)
                    queryParams.Add($"prescriptionDateFrom={request.PrescriptionDateFrom.Value:yyyy-MM-dd}");

                if (request.PrescriptionDateTo.HasValue)
                    queryParams.Add($"prescriptionDateTo={request.PrescriptionDateTo.Value:yyyy-MM-dd}");

                if (request.IsValid.HasValue)
                    queryParams.Add($"isValid={request.IsValid.Value}");

                if (request.IsDispensed.HasValue)
                    queryParams.Add($"isDispensed={request.IsDispensed.Value}");

                queryParams.Add($"pageNumber={request.PageNumber}");
                queryParams.Add($"pageSize={request.PageSize}");

                if (!string.IsNullOrEmpty(request.SortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

                queryParams.Add($"sortDescending={request.SortDescending}");

                var queryString = string.Join("&", queryParams);
                var url = $"api/prescriptions/search?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<PrescriptionDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<PrescriptionDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<PrescriptionDto>>(response, json, "Search prescriptions");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<PrescriptionDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PrescriptionDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Validation & Printing

        public async Task<ApiResponse<bool>> ValidatePrescriptionAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Validating prescription: {id}");

                var response = await _httpClient.GetAsync($"api/prescriptions/{id}/validate");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(json, _jsonOptions);
                    return result ?? ApiResponse<bool>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<bool>(response, json, "Validate prescription");
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

        public async Task<ApiResponse<string>> PrintPrescriptionAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Printing prescription: {id}");

                var response = await _httpClient.GetAsync($"api/prescriptions/{id}/print");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<string>>(json, _jsonOptions);
                    return result ?? ApiResponse<string>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Print prescription");
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

        #region Dispensing

        public async Task<ApiResponse<PrescriptionDetailDto>> DispenseMedicineAsync(Guid prescriptionId, Guid medicineId, string dispensedBy)
        {
            try
            {
                Console.WriteLine($"Dispensing medicine {medicineId} from prescription {prescriptionId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PrescriptionDetailDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var queryString = $"dispensedBy={Uri.EscapeDataString(dispensedBy)}";
                var response = await _httpClient.PostAsync(
                    $"api/prescriptions/{prescriptionId}/medicines/{medicineId}/dispense?{queryString}",
                    null);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Dispense Medicine Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Dispense medicine");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Medicine Operations

        public async Task<ApiResponse<PrescriptionDetailDto>> RemoveMedicineAsync(Guid prescriptionId, Guid medicineId)
        {
            try
            {
                Console.WriteLine($"Removing medicine {medicineId} from prescription {prescriptionId}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<PrescriptionDetailDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.DeleteAsync(
                    $"api/prescriptions/{prescriptionId}/medicines/{medicineId}");

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Remove Medicine Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PrescriptionDetailDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<PrescriptionDetailDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PrescriptionDetailDto>(response, json, "Remove medicine");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PrescriptionDetailDto>.ErrorResponse($"Error: {ex.Message}");
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