using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// File: CompileCares.Application/Features/Auth/DTOs/LoginResponse.cs
namespace CompileCares.Application.Features.Auth.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public UserInfo User { get; set; } = new UserInfo();
    }

    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? DoctorId { get; set; }
        public string? DoctorName { get; set; }
    }
}
