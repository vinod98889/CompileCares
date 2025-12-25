using CompileCares.Application.Common.Interfaces;
using CompileCares.Application.Interfaces;
using CompileCares.Application.Services;
using CompileCares.Infrastructure.Data;
using CompileCares.Infrastructure.Repositories;
using CompileCares.Infrastructure.Repositories.SpecificRepositories;
using CompileCares.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks; // Add this

namespace CompileCares.Infrastructure.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ========== DATABASE CONFIGURATION ==========
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            // ========== REPOSITORIES ==========
            // Generic Repository (registered via UnitOfWork)

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Specific Repositories
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IOPDVisitRepository, OPDVisitRepository>();
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            services.AddScoped<IBillingRepository, BillingRepository>();
            services.AddScoped<IMedicineRepository, MedicineRepository>();
            services.AddScoped<IMasterRepository, MasterRepository>();

            // ========== SERVICES ==========
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IDoctorService, DoctorService>();
            services.AddScoped<IConsultationService, ConsultationService>();
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IBillingService, BillingService>();

            // ========== HEALTH CHECKS ==========
            // Add database health check
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>(
                    name: "database",
                    tags: new[] { "ready" });

            // ========== OTHER SERVICES ==========
            // Add any other infrastructure services here (Email, File, etc.)

            return services;
        }

        public static IServiceCollection AddDatabaseDeveloperPageExceptionFilter(
            this IServiceCollection services)
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
            return services;
        }
    }
}