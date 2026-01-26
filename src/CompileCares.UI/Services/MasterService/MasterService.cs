// MasterService.cs
using CompileCares.API.Models.Responses;
using CompileCares.Application.Common.DTOs;
using CompileCares.Application.Features.Master.DTOs;
using CompileCares.Shared.Enums;
using CompileCares.UI.Services.AuthService;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompileCares.UI.Services.MasterService
{
    public class MasterService : IMasterService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigationManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public MasterService(
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

        #region Complaints Operations

        public async Task<ApiResponse<ComplaintDto>> CreateComplaintAsync(CreateComplaintRequest request)
        {
            try
            {
                Console.WriteLine($"Creating complaint: {request.Name}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ComplaintDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/complaints",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Complaint Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ComplaintDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<ComplaintDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ComplaintDto>(response, json, "Create complaint");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ComplaintDto>> UpdateComplaintAsync(Guid id, CreateComplaintRequest request)
        {
            try
            {
                Console.WriteLine($"Updating complaint: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ComplaintDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/master/complaints/{id}",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Complaint Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ComplaintDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<ComplaintDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ComplaintDto>(response, json, "Update complaint");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ComplaintDto>> GetComplaintAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting complaint: {id}");

                var response = await _httpClient.GetAsync($"api/master/complaints/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ComplaintDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<ComplaintDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ComplaintDto>(response, json, "Get complaint");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResponse<ComplaintDto>>> SearchComplaintsAsync(MasterSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching complaints");

                var queryString = BuildMasterSearchQueryString(request);
                var url = $"api/master/complaints?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<ComplaintDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<ComplaintDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PagedResponse<ComplaintDto>>(response, json, "Search complaints");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PagedResponse<ComplaintDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<ComplaintDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteComplaintAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Deleting complaint: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.DeleteAsync($"api/master/complaints/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Complaint deleted", result?.Message ?? "Complaint deleted successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Delete complaint");
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

        public async Task<ApiResponse<ComplaintDto>> ToggleComplaintStatusAsync(Guid id, bool isActive)
        {
            try
            {
                Console.WriteLine($"Toggling complaint status: {id} to {(isActive ? "Active" : "Inactive")}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ComplaintDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/complaints/{id}/toggle-status",
                    isActive,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Toggle Complaint Status Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ComplaintDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<ComplaintDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ComplaintDto>(response, json, "Toggle complaint status");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ComplaintDto>> ToggleComplaintCommonAsync(Guid id, bool isCommon)
        {
            try
            {
                Console.WriteLine($"Toggling complaint common: {id} to {(isCommon ? "Common" : "Uncommon")}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<ComplaintDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/complaints/{id}/toggle-common",
                    isCommon,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Toggle Complaint Common Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<ComplaintDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<ComplaintDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<ComplaintDto>(response, json, "Toggle complaint common");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<ComplaintDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Advised Items Operations

        public async Task<ApiResponse<AdvisedDto>> CreateAdvisedAsync(CreateAdvisedRequest request)
        {
            try
            {
                Console.WriteLine($"Creating advised item: {request.Text}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<AdvisedDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/advised",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Advised Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<AdvisedDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<AdvisedDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<AdvisedDto>(response, json, "Create advised");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdvisedDto>> UpdateAdvisedAsync(Guid id, CreateAdvisedRequest request)
        {
            try
            {
                Console.WriteLine($"Updating advised item: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<AdvisedDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/master/advised/{id}",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Advised Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<AdvisedDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<AdvisedDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<AdvisedDto>(response, json, "Update advised");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdvisedDto>> GetAdvisedAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting advised item: {id}");

                var response = await _httpClient.GetAsync($"api/master/advised/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<AdvisedDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<AdvisedDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<AdvisedDto>(response, json, "Get advised");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResponse<AdvisedDto>>> SearchAdvisedAsync(MasterSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching advised items");

                var queryString = BuildMasterSearchQueryString(request);
                var url = $"api/master/advised?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<AdvisedDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<AdvisedDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PagedResponse<AdvisedDto>>(response, json, "Search advised");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PagedResponse<AdvisedDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<AdvisedDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteAdvisedAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Deleting advised item: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.DeleteAsync($"api/master/advised/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Advised item deleted", result?.Message ?? "Advised item deleted successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Delete advised");
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

        public async Task<ApiResponse<AdvisedDto>> ToggleAdvisedStatusAsync(Guid id, bool isActive)
        {
            try
            {
                Console.WriteLine($"Toggling advised status: {id} to {(isActive ? "Active" : "Inactive")}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<AdvisedDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/advised/{id}/toggle-status",
                    isActive,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Toggle Advised Status Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<AdvisedDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<AdvisedDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<AdvisedDto>(response, json, "Toggle advised status");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdvisedDto>> ToggleAdvisedCommonAsync(Guid id, bool isCommon)
        {
            try
            {
                Console.WriteLine($"Toggling advised common: {id} to {(isCommon ? "Common" : "Uncommon")}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<AdvisedDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/advised/{id}/toggle-common",
                    isCommon,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Toggle Advised Common Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<AdvisedDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<AdvisedDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<AdvisedDto>(response, json, "Toggle advised common");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<AdvisedDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Doses Operations

        public async Task<ApiResponse<DoseDto>> CreateDoseAsync(CreateDoseRequest request)
        {
            try
            {
                Console.WriteLine($"Creating dose: {request.Name}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<DoseDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/doses",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Dose Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoseDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoseDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoseDto>(response, json, "Create dose");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DoseDto>> UpdateDoseAsync(Guid id, CreateDoseRequest request)
        {
            try
            {
                Console.WriteLine($"Updating dose: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<DoseDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/master/doses/{id}",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Dose Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoseDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoseDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoseDto>(response, json, "Update dose");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DoseDto>> GetDoseAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting dose: {id}");

                var response = await _httpClient.GetAsync($"api/master/doses/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoseDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoseDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoseDto>(response, json, "Get dose");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResponse<DoseDto>>> SearchDosesAsync(MasterSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching doses");

                var queryString = BuildMasterSearchQueryString(request);
                var url = $"api/master/doses?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<DoseDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<DoseDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PagedResponse<DoseDto>>(response, json, "Search doses");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PagedResponse<DoseDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<DoseDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteDoseAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Deleting dose: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.DeleteAsync($"api/master/doses/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Dose deleted", result?.Message ?? "Dose deleted successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Delete dose");
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

        public async Task<ApiResponse<DoseDto>> ToggleDoseStatusAsync(Guid id, bool isActive)
        {
            try
            {
                Console.WriteLine($"Toggling dose status: {id} to {(isActive ? "Active" : "Inactive")}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<DoseDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/doses/{id}/toggle-status",
                    isActive,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Toggle Dose Status Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<DoseDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<DoseDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<DoseDto>(response, json, "Toggle dose status");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<DoseDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateDoseSortOrderAsync(Guid id, int sortOrder)
        {
            try
            {
                Console.WriteLine($"Updating dose sort order: {id} to {sortOrder}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/doses/{id}/sort-order",
                    sortOrder,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update Dose Sort Order Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Sort order updated", result?.Message ?? "Dose sort order updated successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Update dose sort order");
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

        public async Task<ApiResponse<string>> ReorderDosesAsync(List<Guid> doseIds)
        {
            try
            {
                Console.WriteLine($"Reordering doses");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/doses/reorder",
                    doseIds,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Reorder Doses Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Doses reordered", result?.Message ?? "Doses reordered successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Reorder doses");
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

        #region OPD Items Operations

        public async Task<ApiResponse<OPDItemMasterDto>> CreateOPDItemAsync(CreateOPDItemRequest request)
        {
            try
            {
                Console.WriteLine($"Creating OPD item: {request.ItemName}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<OPDItemMasterDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/opd-items",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create OPD Item Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<OPDItemMasterDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<OPDItemMasterDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<OPDItemMasterDto>(response, json, "Create OPD item");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDItemMasterDto>> UpdateOPDItemAsync(Guid id, CreateOPDItemRequest request)
        {
            try
            {
                Console.WriteLine($"Updating OPD item: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<OPDItemMasterDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/master/opd-items/{id}",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update OPD Item Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<OPDItemMasterDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<OPDItemMasterDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<OPDItemMasterDto>(response, json, "Update OPD item");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OPDItemMasterDto>> GetOPDItemAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Getting OPD item: {id}");

                var response = await _httpClient.GetAsync($"api/master/opd-items/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<OPDItemMasterDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<OPDItemMasterDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<OPDItemMasterDto>(response, json, "Get OPD item");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResponse<OPDItemMasterDto>>> SearchOPDItemsAsync(MasterSearchRequest request)
        {
            try
            {
                Console.WriteLine($"Searching OPD items");

                var queryString = BuildMasterSearchQueryString(request);
                var url = $"api/master/opd-items?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<OPDItemMasterDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<OPDItemMasterDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<PagedResponse<OPDItemMasterDto>>(response, json, "Search OPD items");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<PagedResponse<OPDItemMasterDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<OPDItemMasterDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteOPDItemAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"Deleting OPD item: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.DeleteAsync($"api/master/opd-items/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("OPD item deleted", result?.Message ?? "OPD item deleted successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Delete OPD item");
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

        public async Task<ApiResponse<OPDItemMasterDto>> ToggleOPDItemStatusAsync(Guid id, bool isActive)
        {
            try
            {
                Console.WriteLine($"Toggling OPD item status: {id} to {(isActive ? "Active" : "Inactive")}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<OPDItemMasterDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/opd-items/{id}/toggle-status",
                    isActive,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Toggle OPD Item Status Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<OPDItemMasterDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<OPDItemMasterDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<OPDItemMasterDto>(response, json, "Toggle OPD item status");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateOPDItemStockAsync(Guid id, int quantity, StockAction action, decimal? purchasePrice = null, DateTime? expiryDate = null, string? notes = null, string? referenceNumber = null)
        {
            try
            {
                Console.WriteLine($"Updating OPD item stock: {id}, Quantity: {quantity}, Action: {action}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<string>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var request = new UpdateStockRequest
                {
                    Quantity = quantity,
                    Action = action,
                    PurchasePrice = purchasePrice,
                    ExpiryDate = expiryDate,
                    Notes = notes,
                    ReferenceNumber = referenceNumber
                };

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/opd-items/{id}/stock",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update OPD Item Stock Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
                    return ApiResponse<string>.SuccessResponse("Stock updated", result?.Message ?? "OPD item stock updated successfully");
                }
                else
                {
                    return await HandleErrorResponse<string>(response, json, "Update OPD item stock");
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

        public async Task<ApiResponse<OPDItemMasterDto>> UpdateOPDItemPricingAsync(Guid id, decimal standardPrice, decimal doctorCommission, bool isCommissionPercentage)
        {
            try
            {
                Console.WriteLine($"Updating OPD item pricing: {id}");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<OPDItemMasterDto>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var request = new UpdatePricingRequest
                {
                    StandardPrice = standardPrice,
                    DoctorCommission = doctorCommission,
                    IsCommissionPercentage = isCommissionPercentage
                };

                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/master/opd-items/{id}/pricing",
                    request,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update OPD Item Pricing Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<OPDItemMasterDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<OPDItemMasterDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<OPDItemMasterDto>(response, json, "Update OPD item pricing");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OPDItemMasterDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Statistics

        public async Task<ApiResponse<MasterStatisticsDto>> GetMasterStatisticsAsync()
        {
            try
            {
                Console.WriteLine($"Getting master statistics");

                var response = await _httpClient.GetAsync("api/master/statistics");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<MasterStatisticsDto>>(json, _jsonOptions);
                    return result ?? ApiResponse<MasterStatisticsDto>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<MasterStatisticsDto>(response, json, "Get master statistics");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<MasterStatisticsDto>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<MasterStatisticsDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Dictionary<string, int>>> GetUsageStatisticsAsync(string type, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                Console.WriteLine($"Getting usage statistics for type: {type}");

                var queryParams = new List<string>();
                queryParams.Add($"type={Uri.EscapeDataString(type)}");

                if (fromDate.HasValue)
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");

                if (toDate.HasValue)
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

                var queryString = string.Join("&", queryParams);
                var url = $"api/master/usage-statistics/{type}?{queryString}";

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, int>>>(json, _jsonOptions);
                    return result ?? ApiResponse<Dictionary<string, int>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<Dictionary<string, int>>(response, json, "Get usage statistics");
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

        public async Task<ApiResponse<List<ComplaintDto>>> GetMostUsedComplaintsAsync(int count = 10)
        {
            try
            {
                Console.WriteLine($"Getting most used complaints (count: {count})");

                var response = await _httpClient.GetAsync($"api/master/most-used/complaints?count={count}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<ComplaintDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<ComplaintDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<ComplaintDto>>(response, json, "Get most used complaints");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<ComplaintDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ComplaintDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<AdvisedDto>>> GetMostUsedAdvisedAsync(int count = 10)
        {
            try
            {
                Console.WriteLine($"Getting most used advised items (count: {count})");

                var response = await _httpClient.GetAsync($"api/master/most-used/advised?count={count}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<AdvisedDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<AdvisedDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<AdvisedDto>>(response, json, "Get most used advised");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<AdvisedDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<AdvisedDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<DoseDto>>> GetMostUsedDosesAsync(int count = 10)
        {
            try
            {
                Console.WriteLine($"Getting most used doses (count: {count})");

                var response = await _httpClient.GetAsync($"api/master/most-used/doses?count={count}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<DoseDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<DoseDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<DoseDto>>(response, json, "Get most used doses");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<DoseDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<DoseDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OPDItemMasterDto>>> GetMostUsedOPDItemsAsync(int count = 10)
        {
            try
            {
                Console.WriteLine($"Getting most used OPD items (count: {count})");

                var response = await _httpClient.GetAsync($"api/master/most-used/opd-items?count={count}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<OPDItemMasterDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<OPDItemMasterDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<OPDItemMasterDto>>(response, json, "Get most used OPD items");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<OPDItemMasterDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OPDItemMasterDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Bulk Operations

        public async Task<ApiResponse<List<ComplaintDto>>> BulkCreateComplaintsAsync(List<CreateComplaintRequest> requests)
        {
            try
            {
                Console.WriteLine($"Bulk creating {requests.Count} complaints");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<List<ComplaintDto>>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/bulk/complaints",
                    requests,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Bulk Create Complaints Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<ComplaintDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<ComplaintDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<ComplaintDto>>(response, json, "Bulk create complaints");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<ComplaintDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ComplaintDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<AdvisedDto>>> BulkCreateAdvisedAsync(List<CreateAdvisedRequest> requests)
        {
            try
            {
                Console.WriteLine($"Bulk creating {requests.Count} advised items");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<List<AdvisedDto>>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/bulk/advised",
                    requests,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Bulk Create Advised Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<AdvisedDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<AdvisedDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<AdvisedDto>>(response, json, "Bulk create advised");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<AdvisedDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<AdvisedDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<DoseDto>>> BulkCreateDosesAsync(List<CreateDoseRequest> requests)
        {
            try
            {
                Console.WriteLine($"Bulk creating {requests.Count} doses");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<List<DoseDto>>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/bulk/doses",
                    requests,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Bulk Create Doses Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<DoseDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<DoseDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<DoseDto>>(response, json, "Bulk create doses");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<DoseDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<DoseDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OPDItemMasterDto>>> BulkCreateOPDItemsAsync(List<CreateOPDItemRequest> requests)
        {
            try
            {
                Console.WriteLine($"Bulk creating {requests.Count} OPD items");

                var currentUser = await _authService.GetStoredUserAsync();
                if (currentUser == null)
                {
                    return ApiResponse<List<OPDItemMasterDto>>.ErrorResponse("User not authenticated");
                }

                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", currentUser.Id.ToString());

                var response = await _httpClient.PostAsJsonAsync(
                    "api/master/bulk/opd-items",
                    requests,
                    _jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Bulk Create OPD Items Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<OPDItemMasterDto>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<OPDItemMasterDto>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<OPDItemMasterDto>>(response, json, "Bulk create OPD items");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<OPDItemMasterDto>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OPDItemMasterDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Categories

        public async Task<ApiResponse<List<string>>> GetComplaintCategoriesAsync()
        {
            try
            {
                Console.WriteLine($"Getting complaint categories");

                var response = await _httpClient.GetAsync("api/master/categories/complaints");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<string>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<string>>(response, json, "Get complaint categories");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<string>>> GetAdvisedCategoriesAsync()
        {
            try
            {
                Console.WriteLine($"Getting advised categories");

                var response = await _httpClient.GetAsync("api/master/categories/advised");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<string>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<string>>(response, json, "Get advised categories");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<string>>> GetOPDItemTypesAsync()
        {
            try
            {
                Console.WriteLine($"Getting OPD item types");

                var response = await _httpClient.GetAsync("api/master/categories/opd-items/types");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<string>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<string>>(response, json, "Get OPD item types");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<string>>> GetOPDItemCategoriesAsync()
        {
            try
            {
                Console.WriteLine($"Getting OPD item categories");

                var response = await _httpClient.GetAsync("api/master/categories/opd-items");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<string>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<string>>(response, json, "Get OPD item categories");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<string>>> GetOPDItemSubCategoriesAsync(string category)
        {
            try
            {
                Console.WriteLine($"Getting OPD item subcategories for category: {category}");

                var encodedCategory = Uri.EscapeDataString(category);
                var response = await _httpClient.GetAsync($"api/master/categories/opd-items/{encodedCategory}/subcategories");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<string>>>(json, _jsonOptions);
                    return result ?? ApiResponse<List<string>>.ErrorResponse("Invalid response format");
                }
                else
                {
                    return await HandleErrorResponse<List<string>>(response, json, "Get OPD item subcategories");
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<string>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private string BuildMasterSearchQueryString(MasterSearchRequest request)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(request.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");

            if (!string.IsNullOrEmpty(request.Category))
                queryParams.Add($"category={Uri.EscapeDataString(request.Category)}");

            if (!string.IsNullOrEmpty(request.Type))
                queryParams.Add($"type={Uri.EscapeDataString(request.Type)}");

            if (request.IsActive.HasValue)
                queryParams.Add($"isActive={request.IsActive.Value}");

            if (request.IsCommon.HasValue)
                queryParams.Add($"isCommon={request.IsCommon.Value}");

            if (request.IsConsumable.HasValue)
                queryParams.Add($"isConsumable={request.IsConsumable.Value}");

            if (request.IsLowStock.HasValue)
                queryParams.Add($"isLowStock={request.IsLowStock.Value}");

            if (request.MinPrice.HasValue)
                queryParams.Add($"minPrice={request.MinPrice.Value}");

            if (request.MaxPrice.HasValue)
                queryParams.Add($"maxPrice={request.MaxPrice.Value}");

            queryParams.Add($"pageNumber={request.PageNumber}");
            queryParams.Add($"pageSize={request.PageSize}");

            if (!string.IsNullOrEmpty(request.SortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

            queryParams.Add($"sortDescending={request.SortDescending}");

            return string.Join("&", queryParams);
        }

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