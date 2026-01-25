// HorizontalMenu.razor.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using CompileCares.UI.Services;
using CompileCares.UI.Services.AuthService;
using System.ComponentModel;

namespace CompileCares.UI.Components.Layout
{
    public partial class HorizontalMenu : ComponentBase, IDisposable
    {
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        [Inject]
        private IAuthService AuthService { get; set; } = default!;

        [Inject]
        private IAuthStateService AuthStateService { get; set; } = default!;

        [Inject]
        private IHttpClientService HttpClientService { get; set; } = default!;

        private bool showUserMenu = false;
        private bool _disposed = false;
        private string userAvatarInitials = "U";
        private Guid? userDoctorId = null;


        
        private bool isLoading = true;
        private string userName = "Loading...";
        private string userRole = "User";
        private string userEmail = string.Empty;
        

        // Properties from AuthStateService
        private string UserName => AuthStateService.UserName;
        private string UserRole => AuthStateService.UserRole;
        private bool IsAuthenticated => AuthStateService.IsAuthenticated;
        private bool IsLoading => AuthStateService.IsCheckingAuth;

        protected override async Task OnInitializedAsync()
        {
            // Subscribe to auth state changes
            AuthStateService.PropertyChanged += OnAuthStateChanged;

            // Initialize user data
            await InitializeUserData();
        }

        private async void OnAuthStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AuthStateService.UserName) ||
                e.PropertyName == nameof(AuthStateService.UserRole) ||
                e.PropertyName == nameof(AuthStateService.IsAuthenticated))
            {
                await UpdateUserDetails();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task InitializeUserData()
        {
            try
            {
                await AuthStateService.InitializeAsync();
                await UpdateUserDetails();

                // Try to refresh user data in background
                _ = Task.Run(async () => await TryRefreshUserDataAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing menu user data: {ex.Message}");
            }
        }

        private async Task UpdateUserDetails()
        {
            if (!IsAuthenticated || string.IsNullOrEmpty(UserName))
            {
                userAvatarInitials = "G";
                userDoctorId = null;
                return;
            }

            // Update avatar initials
            var nameParts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length >= 2)
            {
                userAvatarInitials = $"{nameParts[0][0]}{nameParts[^1][0]}".ToUpper();
            }
            else if (nameParts.Length == 1 && nameParts[0].Length > 0)
            {
                userAvatarInitials = nameParts[0][0].ToString().ToUpper();
            }
            else
            {
                userAvatarInitials = "U";
            }

            // Try to get doctor ID from stored user data
            try
            {
                var storedUser = await AuthService.GetStoredUserAsync();
                if (storedUser != null)
                {
                    userDoctorId = storedUser.DoctorId;
                }
            }
            catch
            {
                userDoctorId = null;
            }
        }

        private async Task TryRefreshUserDataAsync()
        {
            try
            {
                if (!IsAuthenticated)
                    return;

                var userResult = await AuthService.GetCurrentUserAsync();

                if (userResult.Success && userResult.Data != null)
                {
                    Console.WriteLine("User data refreshed in background");

                    // Update doctor ID if available
                    userDoctorId = userResult.Data.DoctorId;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background user data refresh failed: {ex.Message}");
            }
        }

        private void ToggleUserMenu()
        {
            showUserMenu = !showUserMenu;
            StateHasChanged();
        }

        private void CloseUserMenu()
        {
            showUserMenu = false;
            StateHasChanged();
        }

        private async Task HandleLogout()
        {
            try
            {
                CloseUserMenu();
                await Task.Delay(100);
                await AuthStateService.LogoutAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                Navigation.NavigateTo("/login", true);
            }
        }

        private void NavigateToProfile()
        {
            CloseUserMenu();
            Navigation.NavigateTo("/profile");
        }

        private void NavigateToSettings()
        {
            CloseUserMenu();
            Navigation.NavigateTo("/settings");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                AuthStateService.PropertyChanged -= OnAuthStateChanged;
                _disposed = true;
            }
        }
    }
}