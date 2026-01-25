using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.AuthService
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        private string _currentToken = string.Empty;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentToken);

        public HttpClientService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            // If we already have a token set and it matches what's in storage, return
            if (!string.IsNullOrEmpty(_currentToken) &&
                _httpClient.DefaultRequestHeaders.Authorization != null)
            {
                return _httpClient;
            }

            // Get fresh token from storage
            var token = await _authService.GetStoredTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                // Check if token is still valid
                var isValid = await _authService.IsTokenValidAsync();

                if (isValid)
                {
                    _currentToken = token;
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine($"HttpClient authenticated with token");
                }
                else
                {
                    ClearAuthentication();
                }
            }

            return _httpClient;
        }

        public void ClearAuthentication()
        {
            _currentToken = string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            Console.WriteLine("HttpClient authentication cleared");
        }
    }
}
