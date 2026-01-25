using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// File: CompileCares.Application/Features/Auth/DTOs/TestUsersResponse.cs
namespace CompileCares.Application.Features.Auth.DTOs
{
    public class TestUsersResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<TestUserInfo> Users { get; set; } = new List<TestUserInfo>();
    }

    public class TestUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
