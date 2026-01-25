// BasePageComponent.cs
using CompileCares.UI.Services.AuthService;
using Microsoft.AspNetCore.Components;

namespace CompileCares.UI.Components
{
    public class BasePageComponent : ComponentBase
    {
        [Inject]
        protected IAuthService AuthService { get; set; } = default!;

        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;

        protected bool isAuthenticated = false;
        protected bool isCheckingAuth = true;
        protected string userName = string.Empty;
        protected string userRole = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await CheckAuthentication();
        }

        protected virtual async Task CheckAuthentication()
        {
            try
            {
                isCheckingAuth = true;

                // Check if token is valid
                var hasValidSession = await AuthService.IsTokenValidAsync();

                if (!hasValidSession)
                {
                    Navigation.NavigateTo("/login", true);
                    return;
                }

                // Get user from storage
                var storedUser = await AuthService.GetStoredUserAsync();
                if (storedUser != null)
                {
                    userName = $"{storedUser.FirstName} {storedUser.LastName}";
                    userRole = storedUser.Role;
                }

                // Set token in HttpClient
                var token = await AuthService.GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    AuthService.SetAuthorizationToken(token);
                }

                isAuthenticated = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auth error: {ex.Message}");
                Navigation.NavigateTo("/login", true);
            }
            finally
            {
                isCheckingAuth = false;
            }
        }
    }
}