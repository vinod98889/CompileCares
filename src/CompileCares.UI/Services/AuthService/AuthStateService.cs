using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.AuthService
{
    public class AuthStateService : IAuthStateService
    {
        private readonly IAuthService _authService;
        private readonly IHttpClientService _httpClientService;
        private readonly NavigationManager _navigationManager;

        private bool _isAuthenticated;
        private string _userName = string.Empty;
        private string _userRole = string.Empty;
        private bool _isCheckingAuth;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set
            {
                if (_isAuthenticated != value)
                {
                    _isAuthenticated = value;
                    OnPropertyChanged(nameof(IsAuthenticated));
                }
            }
        }

        public string UserName
        {
            get => _userName;
            private set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }

        public string UserRole
        {
            get => _userRole;
            private set
            {
                if (_userRole != value)
                {
                    _userRole = value;
                    OnPropertyChanged(nameof(UserRole));
                }
            }
        }

        public bool IsCheckingAuth
        {
            get => _isCheckingAuth;
            private set
            {
                if (_isCheckingAuth != value)
                {
                    _isCheckingAuth = value;
                    OnPropertyChanged(nameof(IsCheckingAuth));
                }
            }
        }

        public AuthStateService(
            IAuthService authService,
            IHttpClientService httpClientService,
            NavigationManager navigationManager)
        {
            _authService = authService;
            _httpClientService = httpClientService;
            _navigationManager = navigationManager;
        }

        public async Task InitializeAsync()
        {
            await CheckAuthenticationAsync();
        }

        public async Task CheckAuthenticationAsync()
        {
            IsCheckingAuth = true;

            try
            {
                // Check if token is valid
                var isValid = await _authService.IsTokenValidAsync();

                if (!isValid)
                {
                    Console.WriteLine("Token is not valid, redirecting to login");
                    IsAuthenticated = false;
                    UserName = string.Empty;
                    UserRole = string.Empty;

                    // Clear any invalid session
                    await LogoutAsync();
                    _navigationManager.NavigateTo("/login", true);
                    return;
                }

                // Get user info from storage
                var user = await _authService.GetStoredUserAsync();

                if (user != null)
                {
                    UserName = $"{user.FirstName} {user.LastName}";
                    UserRole = user.Role;
                    IsAuthenticated = true;

                    // Ensure HttpClient has the token
                    await _httpClientService.GetAuthenticatedClientAsync();
                    Console.WriteLine("Authentication state initialized");
                }
                else
                {
                    IsAuthenticated = false;
                    _navigationManager.NavigateTo("/login", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auth state check error: {ex.Message}");
                IsAuthenticated = false;
                _navigationManager.NavigateTo("/login", true);
            }
            finally
            {
                IsCheckingAuth = false;
            }
        }

        public async Task LogoutAsync()
        {
            _httpClientService.ClearAuthentication();
            _authService.Logout();

            IsAuthenticated = false;
            UserName = string.Empty;
            UserRole = string.Empty;

            _navigationManager.NavigateTo("/login", true);
        }

        public async Task RefreshAuthStateAsync()
        {
            await CheckAuthenticationAsync();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
