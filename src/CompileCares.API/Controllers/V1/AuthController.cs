// AuthController.cs (Updated)
using CompileCares.Application.Features.Auth.DTOs;
using CompileCares.Application.Services.Auth;
using CompileCares.Core.Entities;
using CompileCares.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CompileCares.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]    
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }
        [HttpGet("verify-config")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyConfiguration()
        {
            try
            {
                // Check if all DbSets are accessible
                var results = new
                {
                    Users = await _context.Users.CountAsync(),
                    Doctors = await _context.Doctors.CountAsync(),
                    Medicines = await _context.Medicines.CountAsync(),
                    Doses = await _context.Doses.CountAsync(),
                    Complaints = await _context.Complaints.CountAsync(),
                    AdvisedItems = await _context.AdvisedItems.CountAsync(),
                    PrescriptionTemplates = await _context.PrescriptionTemplates.CountAsync(),

                    // Check if we can query
                    CanQueryMedicines = _context.Medicines.Any(),
                    CanQueryDoses = _context.Doses.Any(),
                    CanQueryUsers = _context.Users.Any()
                };

                return Ok(new
                {
                    Success = true,
                    Message = "All configurations loaded successfully",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Configuration error",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                // Find user
                var user = await _context.Users
                    .Include(u => u.Doctor)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Email}", request.Email);
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for user: {Email}", request.Email);
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                // Generate token
                var token = _tokenService.GenerateToken(user);

                _logger.LogInformation("User logged in successfully: {Email}", user.Email);

                return Ok(new
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresIn = 3600, // 1 hour in seconds
                    User = new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.Role,
                        user.DoctorId,
                        DoctorName = user.Doctor?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new { Message = "An error occurred during login" });
            }
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")] // Only admins can register new users
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Check if email exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                    return BadRequest(new { Message = "Email already registered" });

                // Hash password
                var passwordHash = HashPassword(request.Password);

                // Create user
                var user = new User(
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    email: request.Email,
                    passwordHash: passwordHash,
                    role: request.Role,
                    doctorId: request.DoctorId);

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered: {Email}", user.Email);

                return Ok(new
                {
                    Message = "User registered successfully",
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", request.Email);
                return StatusCode(500, new { Message = "An error occurred during registration" });
            }
        }

        [HttpPost("create-test-users")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateTestUsers()
        {
            try
            {
                if (!await _context.Users.AnyAsync())
                {
                    var users = new List<User>
                    {
                        new User("Admin", "User", "admin@compilecares.com",
                            HashPassword("Admin@123"), "Admin"),

                        new User("John", "Doctor", "doctor@compilecares.com",
                            HashPassword("Doctor@123"), "Doctor",
                            doctorId: Guid.NewGuid()),

                        new User("Sarah", "Nurse", "nurse@compilecares.com",
                            HashPassword("Nurse@123"), "Nurse"),

                        new User("Mike", "Receptionist", "reception@compilecares.com",
                            HashPassword("Reception@123"), "Receptionist")
                    };

                    await _context.Users.AddRangeAsync(users);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created {Count} test users", users.Count);

                    return Ok(new
                    {
                        Message = "Test users created successfully",
                        Count = users.Count,
                        Users = users.Select(u => new
                        {
                            u.Email,
                            u.Role,
                            Password = GetDefaultPassword(u.Role) // For testing
                        })
                    });
                }

                return Ok(new { Message = "Test users already exist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test users");
                return StatusCode(500, new { Message = "Error creating test users" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var user = await _context.Users
                    .Include(u => u.Doctor)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                    return Unauthorized(new { Message = "User not found" });

                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.DoctorId,
                    DoctorName = user.Doctor?.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { Message = "Error getting user information" });
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == storedHash;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GetDefaultPassword(string role)
        {
            return role switch
            {
                "Admin" => "Admin@123",
                "Doctor" => "Doctor@123",
                "Nurse" => "Nurse@123",
                "Receptionist" => "Reception@123",
                _ => "Password@123"
            };
        }

        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ??
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");

            return userId;
        }
    }    
}