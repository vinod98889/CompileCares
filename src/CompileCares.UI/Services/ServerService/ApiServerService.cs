using CompileCares.API.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace CompileCares.UI.Services.ServerService
{
    public class ApiServerService : IApiServerService
    {
        private WebApplication? _app;
        private DateTime? _startTime;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiServerService>? _logger;

        public bool IsRunning => _app != null;
        public string ServerUrl { get; private set; } = string.Empty;

        public ApiServerService(IConfiguration configuration, ILogger<ApiServerService>? logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void StartServer(string url = "http://localhost:7194")
        {
            if (_app != null) return;

            ServerUrl = url;
            _startTime = DateTime.Now;

            Task.Run(() =>
            {
                try
                {
                    var builder = WebApplication.CreateBuilder();

                    // Configuration
                    builder.Configuration.AddConfiguration(_configuration);

                    // Configure Kestrel
                    builder.WebHost.UseUrls(url);

                    // Add services
                    ConfigureServices(builder.Services, builder.Configuration);

                    _app = builder.Build();

                    // Configure middleware pipeline
                    ConfigureMiddleware(_app);

                    Console.WriteLine($"🚀 CompileCares API Server starting on: {url}");
                    Console.WriteLine($"📅 Started at: {_startTime}");

                    // Log that server is about to run
                    Console.WriteLine("✅ Server setup complete. Starting Kestrel...");

                    // Start the server
                    _app.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ API Server failed to start: {ex.Message}");
                    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                    // Reset state on failure
                    _app = null;
                    ServerUrl = string.Empty;
                    _startTime = null;
                }
            });

            // Wait for server to start - but don't wait forever
            Task.Delay(2000).Wait();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            try
            {
                Console.WriteLine("🔧 Configuring API services...");

                // Add controllers from CompileCares.API assembly
                var apiAssembly = typeof(CompileCares.API.Controllers.DoctorsController).Assembly;

                // Add MVC services
                services.AddControllers()
                        .AddApplicationPart(apiAssembly);

                // Add API Explorer
                services.AddEndpointsApiExplorer();

                // Configure Swagger
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "CompileCares API",
                        Version = "v1.0",
                        Description = "Medical Prescription and Management System API",
                        Contact = new OpenApiContact
                        {
                            Name = "CompileCares Team",
                            Email = "support@compilecares.com"
                        }
                    });

                    // Include XML comments
                    var xmlFile = $"{apiAssembly.GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }

                    // Add Bearer token authentication
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme.",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                        }
                    });
                });
                services.AddApplicationServices(configuration);
                // Add CORS - Allow MAUI Blazor UI
                services.AddCors(options =>
                {
                    options.AddPolicy("CompileCaresPolicy", policy =>
                    {
                        policy.WithOrigins(
                                "https://localhost:5001", // Blazor WebAssembly
                                "http://localhost:5000",
                                "http://localhost",        // MAUI
                                "https://localhost")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });

                // Register your application services
                RegisterApplicationServices(services, configuration);

                // Database Contexts (adjust based on your actual DbContext names)
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                Console.WriteLine($"🔗 Database Connection: {connectionString}");

                // Add your DbContexts here
                // services.AddDbContext<CompileCaresDbContext>(options =>
                //     options.UseSqlServer(connectionString));

                Console.WriteLine("✅ Services configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Service configuration failed: {ex.Message}");
                throw;
            }
        }

        private void RegisterApplicationServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register your CompileCares services here
            // Example:
            // services.AddScoped<IMedicineService, MedicineService>();
            // services.AddScoped<IPatientService, PatientService>();
            // services.AddScoped<IPrescriptionService, PrescriptionService>();
            // services.AddScoped<IComplaintService, ComplaintService>();
            // services.AddScoped<IAdvisedService, AdvisedService>();
            // services.AddScoped<IDoseService, DoseService>();

            Console.WriteLine("📋 Registered application services");
        }

        private void ConfigureMiddleware(WebApplication app)
        {
            try
            {
                Console.WriteLine("🔗 Configuring middleware...");

                // Enable CORS
                app.UseCors("CompileCaresPolicy");

                // Swagger for development
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CompileCares API v1.0");
                        c.RoutePrefix = "api-docs";
                        c.DocumentTitle = "CompileCares API Documentation";
                    });
                }

                // Routing
                app.UseRouting();

                // Authentication & Authorization
                app.UseAuthentication();
                app.UseAuthorization();

                // Endpoints
                app.MapControllers();

                // Health check endpoint
                app.MapGet("/health", () => new
                {
                    status = "Healthy",
                    service = "CompileCares API",
                    version = "1.0.0",
                    timestamp = DateTime.UtcNow,
                    uptime = DateTime.Now - _startTime
                });

                // API root endpoint
                app.MapGet("/", () => "CompileCares API Server is running. Visit /api-docs for Swagger documentation.");

                // Test endpoint
                app.MapGet("/api/test", () => new
                {
                    message = "CompileCares API is working!",
                    endpoints = new[] {
                        "/api/medicine",
                        "/api/patient",
                        "/api/prescription",
                        "/api/complaint",
                        "/api/advised",
                        "/api/dose"
                    }
                });

                Console.WriteLine("✅ Middleware configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Middleware configuration failed: {ex.Message}");
                throw;
            }
        }

        public void StopServer()
        {
            try
            {
                if (_app != null)
                {
                    Console.WriteLine("🛑 Stopping CompileCares API Server...");
                    _app.StopAsync().Wait(5000);
                    _app.DisposeAsync();
                    _app = null;
                    ServerUrl = string.Empty;
                    _startTime = null;
                    Console.WriteLine("✅ API Server stopped successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error stopping API server: {ex.Message}");
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            if (!IsRunning) return false;

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var response = await client.GetAsync($"{ServerUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public string GetServerInfo()
        {
            return JsonSerializer.Serialize(new
            {
                IsRunning,
                ServerUrl,
                StartTime = _startTime,
                Uptime = _startTime.HasValue ? DateTime.Now - _startTime.Value : TimeSpan.Zero,
                Endpoints = new[]
                {
                    $"{ServerUrl}/api-docs",
                    $"{ServerUrl}/health",
                    $"{ServerUrl}/api/test"
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
