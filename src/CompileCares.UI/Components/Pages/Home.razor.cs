using CompileCares.Core.Entities.Patients;
using CompileCares.UI.Services.AuthService;
using CompileCares.UI.Services.ServerService;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Net.Http.Json;

namespace CompileCares.UI.Components.Pages
{
    public partial class Home : ComponentBase,IDisposable
    {
        [Inject]
        private IAuthStateService AuthStateService { get; set; } = default!;

        [Inject]
        private IHttpClientService HttpClientService { get; set; } = default!;

        [Inject]
        private IApiServerService ApiServerService { get; set; } = default!;

        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        private List<Patient> patients = new();
        private bool isLoading = false;
        private string errorMessage = string.Empty;
        private bool _disposed;

        protected override async Task OnInitializedAsync()
        {
            // Subscribe to auth state changes
            AuthStateService.PropertyChanged += OnAuthStateChanged;

            // Initialize auth state
            await AuthStateService.InitializeAsync();

            if (AuthStateService.IsAuthenticated)
            {
                await LoadPatients();
            }
        }

        private async void OnAuthStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AuthStateService.IsAuthenticated))
            {
                if (!AuthStateService.IsAuthenticated)
                {
                    // If we become unauthenticated, redirect to login
                    Console.WriteLine("Auth state changed to unauthenticated");
                    Navigation.NavigateTo("/login", true);
                }
                else if (!isLoading && patients.Count == 0)
                {
                    // If we become authenticated, load patients
                    await LoadPatients();
                }
            }

            // Re-render component when auth state changes
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadPatients()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                // Ensure we're authenticated
                if (!AuthStateService.IsAuthenticated)
                {
                    errorMessage = "Not authenticated";
                    return;
                }

                // Get authenticated HttpClient
                var httpClient = await HttpClientService.GetAuthenticatedClientAsync();

                if (!HttpClientService.IsAuthenticated)
                {
                    errorMessage = "Authentication failed";
                    return;
                }

                // Check API server
                if (!ApiServerService.IsRunning)
                {
                    Console.WriteLine("Starting API server");
                    ApiServerService.StartServer("http://localhost:7194");
                    await Task.Delay(2000);
                }

                // Make API call
                Console.WriteLine($"Loading patients from: {ApiServerService.ServerUrl}/api/v1.0/patient");

                var response = await httpClient.GetAsync($"{ApiServerService.ServerUrl}/api/v1.0/patient");

                if (response.IsSuccessStatusCode)
                {
                    patients = await response.Content.ReadFromJsonAsync<List<Patient>>() ?? new List<Patient>();
                    Console.WriteLine($"Loaded {patients.Count} patients");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token expired during API call
                    Console.WriteLine("API returned Unauthorized");
                    errorMessage = "Session expired";

                    // Logout and redirect
                    await AuthStateService.LogoutAsync();
                }
                else
                {
                    errorMessage = $"API Error: {response.StatusCode}";
                    Console.WriteLine($"API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading patients: {ex.Message}";
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task RefreshData()
        {
            await LoadPatients();
        }

        private async Task Logout()
        {
            await AuthStateService.LogoutAsync();
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