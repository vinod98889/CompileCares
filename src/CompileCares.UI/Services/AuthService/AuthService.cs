using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Auth.DTOs;
using Microsoft.Maui.Storage; // Add this for SecureStorage
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        // Storage keys
        private const string AuthTokenKey = "auth_token";
        private const string AuthUserKey = "auth_user";
        private const string AuthExpiryKey = "auth_expiry";
        private const string AuthLoginTimeKey = "auth_login_time";

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request, _jsonOptions);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"DEBUG - Response Status: {response.StatusCode}");
                Console.WriteLine($"DEBUG - Response JSON: {json}");

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, _jsonOptions);

                    if (loginResponse == null)
                    {
                        return ApiResponse<LoginResponse>.ErrorResponse("Invalid response format");
                    }

                    // Store token and user info
                    await StoreSessionDataAsync(loginResponse.Token, loginResponse.User, loginResponse.ExpiresIn);

                    // Set authorization header
                    SetAuthorizationToken(loginResponse.Token);

                    return ApiResponse<LoginResponse>.SuccessResponse(
                        loginResponse,
                        "Login successful");
                }
                else
                {
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
                        var errorMessage = errorObj?.ContainsKey("Message") == true
                            ? errorObj["Message"]
                            : errorObj?.ContainsKey("message") == true
                                ? errorObj["message"]
                                : "Login failed";

                        return ApiResponse<LoginResponse>.ErrorResponse(errorMessage);
                    }
                    catch
                    {
                        return ApiResponse<LoginResponse>.ErrorResponse($"Login failed: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<LoginResponse>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<LoginResponse>.ErrorResponse($"Login error: {ex.Message}");
            }
        }

        public async Task<bool> RestoreSessionAsync()
        {
            try
            {
                // Check if token exists and is valid
                var hasValidToken = await IsTokenValidAsync();
                if (!hasValidToken)
                {
                    return false;
                }

                // Get token from storage
                var token = await GetStoredTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // Set token in HttpClient
                SetAuthorizationToken(token);

                Console.WriteLine("Session restored successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Restore session error: {ex.Message}");
                return false;
            }
        }

        private async Task StoreSessionDataAsync(string token, UserInfo user, int expiresInSeconds)
        {
            try
            {
                // Store token in SecureStorage
                await SecureStorage.Default.SetAsync(AuthTokenKey, token);

                // Store user info as JSON
                var userJson = JsonSerializer.Serialize(user, _jsonOptions);
                await SecureStorage.Default.SetAsync(AuthUserKey, userJson);

                // Calculate and store expiry time
                var expiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                await SecureStorage.Default.SetAsync(AuthExpiryKey, expiryTime.ToString("o"));

                // Store login time
                await SecureStorage.Default.SetAsync(AuthLoginTimeKey, DateTime.UtcNow.ToString("o"));

                Console.WriteLine("Session data stored securely");
            }
            catch (Exception ex)
            {
                // Fallback to Preferences if SecureStorage fails
                Console.WriteLine($"SecureStorage error, using Preferences: {ex.Message}");

                Preferences.Set(AuthTokenKey, token);

                var userJson = JsonSerializer.Serialize(user, _jsonOptions);
                Preferences.Set(AuthUserKey, userJson);

                var expiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                Preferences.Set(AuthExpiryKey, expiryTime.ToString("o"));
                Preferences.Set(AuthLoginTimeKey, DateTime.UtcNow.ToString("o"));
            }
        }

        public async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                // Try SecureStorage first
                var token = await SecureStorage.Default.GetAsync(AuthTokenKey);
                if (!string.IsNullOrEmpty(token))
                {
                    return token;
                }

                // Fallback to Preferences
                return Preferences.Get(AuthTokenKey, null);
            }
            catch
            {
                return Preferences.Get(AuthTokenKey, null);
            }
        }

        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                var token = await GetStoredTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // Get expiry time
                string expiryString;
                try
                {
                    expiryString = await SecureStorage.Default.GetAsync(AuthExpiryKey);
                    if (string.IsNullOrEmpty(expiryString))
                    {
                        expiryString = Preferences.Get(AuthExpiryKey, null);
                    }
                }
                catch
                {
                    expiryString = Preferences.Get(AuthExpiryKey, null);
                }

                if (!string.IsNullOrEmpty(expiryString) &&
                    DateTime.TryParse(expiryString, out var expiryTime))
                {
                    // Check if token is still valid (with 5 minute buffer)
                    return expiryTime > DateTime.UtcNow.AddMinutes(-5);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserInfo?> GetStoredUserAsync()
        {
            try
            {
                string userJson;
                try
                {
                    userJson = await SecureStorage.Default.GetAsync(AuthUserKey);
                    if (string.IsNullOrEmpty(userJson))
                    {
                        userJson = Preferences.Get(AuthUserKey, null);
                    }
                }
                catch
                {
                    userJson = Preferences.Get(AuthUserKey, null);
                }

                if (!string.IsNullOrEmpty(userJson))
                {
                    return JsonSerializer.Deserialize<UserInfo>(userJson, _jsonOptions);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Logout()
        {
            // Clear authorization header
            SetAuthorizationToken("");

            // Clear stored data
            ClearStoredTokenAsync();

            Console.WriteLine("User logged out");
        }

        private async Task ClearStoredTokenAsync()
        {
            try
            {
                // Clear from SecureStorage
                SecureStorage.Default.Remove(AuthTokenKey);
                SecureStorage.Default.Remove(AuthUserKey);
                SecureStorage.Default.Remove(AuthExpiryKey);
                SecureStorage.Default.Remove(AuthLoginTimeKey);

                // Clear from Preferences
                Preferences.Remove(AuthTokenKey);
                Preferences.Remove(AuthUserKey);
                Preferences.Remove(AuthExpiryKey);
                Preferences.Remove(AuthLoginTimeKey);

                Console.WriteLine("Session data cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing session: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CurrentUserResponse>> GetCurrentUserAsync()
        {
            try
            {
                // First try to get from API
                var response = await _httpClient.GetAsync("api/v1/auth/me");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var currentUserResponse = JsonSerializer.Deserialize<CurrentUserResponse>(json, _jsonOptions);

                    if (currentUserResponse != null)
                    {
                        // Update stored user info
                        var token = await GetStoredTokenAsync();
                        if (!string.IsNullOrEmpty(token))
                        {
                            await StoreSessionDataAsync(
                                token,
                                new UserInfo
                                {
                                    Id = currentUserResponse.Id,
                                    Email = currentUserResponse.Email,
                                    FirstName = currentUserResponse.FirstName,
                                    LastName = currentUserResponse.LastName,
                                    Role = currentUserResponse.Role,
                                    DoctorId = currentUserResponse.DoctorId,
                                    DoctorName = currentUserResponse.DoctorName
                                },
                                3600); // Default 1 hour
                        }

                        return ApiResponse<CurrentUserResponse>.SuccessResponse(
                            currentUserResponse,
                            "User data retrieved");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token expired
                    await ClearStoredTokenAsync();
                    return ApiResponse<CurrentUserResponse>.ErrorResponse("Session expired");
                }

                // If API fails, try to get from storage
                var storedUser = await GetStoredUserAsync();
                if (storedUser != null)
                {
                    var currentUserResponse = new CurrentUserResponse
                    {
                        Id = storedUser.Id,
                        Email = storedUser.Email,
                        FirstName = storedUser.FirstName,
                        LastName = storedUser.LastName,
                        Role = storedUser.Role,
                        DoctorId = storedUser.DoctorId,
                        DoctorName = storedUser.DoctorName
                    };

                    return ApiResponse<CurrentUserResponse>.SuccessResponse(
                        currentUserResponse,
                        "User data loaded from cache");
                }

                return ApiResponse<CurrentUserResponse>.ErrorResponse("Failed to get user data");
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<CurrentUserResponse>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<CurrentUserResponse>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public void SetAuthorizationToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine("Authorization token set");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                Console.WriteLine("Authorization token cleared");
            }
        }

        // Keep your existing methods below (they remain the same)
        public async Task<ApiResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/auth/register", request, _jsonOptions);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                    var message = result?.ContainsKey("Message") == true
                        ? result["Message"]?.ToString()
                        : "Registration successful";

                    return ApiResponse.SuccessResponse(message ?? "Registration successful");
                }
                else
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
                    var errorMessage = errorObj?.ContainsKey("Message") == true
                        ? errorObj["Message"]
                        : "Registration failed";

                    return ApiResponse.ErrorResponse(errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse.ErrorResponse($"Registration error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TestUsersResponse>> CreateTestUsersAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("api/v1/auth/create-test-users", null);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var testUsersResponse = JsonSerializer.Deserialize<TestUsersResponse>(json, _jsonOptions);

                    if (testUsersResponse == null)
                    {
                        return ApiResponse<TestUsersResponse>.ErrorResponse("Invalid response format");
                    }

                    return ApiResponse<TestUsersResponse>.SuccessResponse(
                        testUsersResponse,
                        testUsersResponse.Message ?? "Test users created");
                }
                else
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
                    var errorMessage = errorObj?.ContainsKey("Message") == true
                        ? errorObj["Message"]
                        : "Failed to create test users";

                    return ApiResponse<TestUsersResponse>.ErrorResponse(errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<TestUsersResponse>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<TestUsersResponse>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VerifyConfigResponse>> VerifyConfigurationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/v1/auth/verify-config");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<VerifyConfigResponse>>(json, _jsonOptions);
                    return apiResponse ?? ApiResponse<VerifyConfigResponse>.ErrorResponse("Invalid response format");
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse<VerifyConfigResponse>>(json, _jsonOptions);
                        return errorResponse ?? ApiResponse<VerifyConfigResponse>.ErrorResponse("Configuration verification failed");
                    }
                    catch
                    {
                        return ApiResponse<VerifyConfigResponse>.ErrorResponse($"Failed: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<VerifyConfigResponse>.ErrorResponse($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<VerifyConfigResponse>.ErrorResponse($"Error: {ex.Message}");
            }
        }
    }
}