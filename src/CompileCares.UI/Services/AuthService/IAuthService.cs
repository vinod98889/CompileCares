using CompileCares.API.Models.Responses;
using CompileCares.Application.Features.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.AuthService
{
    public interface IAuthService
    {
        // Authentication methods
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<TestUsersResponse>> CreateTestUsersAsync();
        Task<ApiResponse<CurrentUserResponse>> GetCurrentUserAsync();
        Task<ApiResponse<VerifyConfigResponse>> VerifyConfigurationAsync();

        // Token management
        void SetAuthorizationToken(string token);
        void Logout();

        // Session persistence
        Task<string?> GetStoredTokenAsync();
        Task<bool> IsTokenValidAsync();
        Task<UserInfo?> GetStoredUserAsync();
        Task<bool> RestoreSessionAsync();
    }
}
