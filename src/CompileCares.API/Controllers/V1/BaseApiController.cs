// BaseApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CompileCares.Api.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid GetCurrentUserId()
        {
            var userId = User.FindFirst("sub")?.Value ??
                        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
                throw new UnauthorizedAccessException("User ID not found in token");

            return id;
        }

        protected string GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   string.Empty;
        }

        protected string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        protected Guid? GetCurrentDoctorId()
        {
            // Get DoctorId from claim
            var doctorId = User.FindFirst("DoctorId")?.Value;
            if (!string.IsNullOrEmpty(doctorId) && Guid.TryParse(doctorId, out var id))
                return id;

            // If user is a doctor, UserId might be DoctorId
            if (IsDoctor())
                return GetCurrentUserId();

            return null;
        }

        protected bool IsDoctor()
        {
            return User.IsInRole("Doctor") || GetCurrentUserRole() == "Doctor";
        }

        protected bool IsAdmin()
        {
            return User.IsInRole("Admin") || GetCurrentUserRole() == "Admin";
        }

        protected bool IsInRole(string role)
        {
            return User.IsInRole(role) || GetCurrentUserRole() == role;
        }
    }
}