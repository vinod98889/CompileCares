// CompileCares.API/Extensions/ServiceExtensions.cs
using CompileCares.Infrastructure.Extensions;

namespace CompileCares.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ========== INFRASTRUCTURE SERVICES ==========
            // This includes ALL infrastructure services including health checks
            services.AddInfrastructureServices(configuration);

            // ========== API/PRESENTATION SERVICES ==========

            // Add Controllers with JSON options
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.DefaultIgnoreCondition =
                        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            // Add API Versioning (if using)
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            // Add Endpoint API Explorer for Swagger
            services.AddEndpointsApiExplorer();

            // Add Swagger/OpenAPI
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "CompileCares API",
                    Version = "v1",
                    Description = "Healthcare Management System API"
                });

                // Add JWT Bearer authentication to Swagger
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Add CORS Policy
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            // ✅ REMOVED duplicate health check - already registered in Infrastructure
            // services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

            // Add Logging
            services.AddLogging();

            // Add Problem Details for standardized error responses
            services.AddProblemDetails();

            // Add HTTP Context Accessor
            services.AddHttpContextAccessor();

            return services;
        }
    }
}