using CompileCares.UI.Services.ServerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.BaseAPIService
{
    public class BaseApiService : IBaseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IApiServerService _apiServerService;
        private readonly JsonSerializerOptions _jsonOptions;

        public BaseApiService(HttpClient httpClient, IApiServerService apiServerService)
        {
            _httpClient = httpClient;
            _apiServerService = apiServerService;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        private string BuildUrl(string endpoint)
        {
            if (!_apiServerService.IsRunning)
                throw new InvalidOperationException("API Server is not running. Start it first.");

            // Ensure endpoint starts with /
            if (!endpoint.StartsWith("/"))
                endpoint = "/" + endpoint;

            // Ensure endpoint has /api prefix if not already
            if (!endpoint.StartsWith("/api/") && !endpoint.StartsWith("/health") &&
                !endpoint.StartsWith("/api-docs") && !endpoint.StartsWith("/test"))
                endpoint = "/api" + endpoint;

            return $"{_apiServerService.ServerUrl}{endpoint}";
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var url = BuildUrl(endpoint);
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }

            throw await CreateApiException(response, $"GET {endpoint}");
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var url = BuildUrl(endpoint);
            var response = await _httpClient.PostAsJsonAsync(url, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }

            throw await CreateApiException(response, $"POST {endpoint}");
        }

        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            var url = BuildUrl(endpoint);
            var response = await _httpClient.PutAsJsonAsync(url, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }

            throw await CreateApiException(response, $"PUT {endpoint}");
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var url = BuildUrl(endpoint);
            var response = await _httpClient.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
                return true;

            throw await CreateApiException(response, $"DELETE {endpoint}");
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                if (!_apiServerService.IsRunning) return false;
                var response = await _httpClient.GetAsync(BuildUrl("/health"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetServerInfoAsync()
        {
            return _apiServerService.GetServerInfo();
        }

        private async Task<ApiException> CreateApiException(HttpResponseMessage response, string operation)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var statusCode = response.StatusCode;
            var message = $"API Error ({statusCode}): {operation}";

            if (!string.IsNullOrEmpty(errorContent))
            {
                try
                {
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    if (errorJson.TryGetProperty("message", out var messageProp))
                        message = messageProp.GetString() ?? message;
                }
                catch
                {
                    // If not JSON, use raw content
                    message = $"{message}. Response: {errorContent}";
                }
            }

            return new ApiException(message, statusCode, errorContent);
        }
    }

    public class ApiException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public string ResponseContent { get; }

        public ApiException(string message, System.Net.HttpStatusCode statusCode, string responseContent)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
