using CompileCares.Infrastructure.Extensions;

namespace CompileCares.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Add Infrastructure services (includes DbContext)
            services.AddInfrastructureServices(configuration);

            // 2. Add Controllers
            services.AddControllers();

            // 3. Add Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "CompileCares API",
                    Version = "v1"
                });
            });

            // 4. Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader());
            });

            return services;
        }
    }
}