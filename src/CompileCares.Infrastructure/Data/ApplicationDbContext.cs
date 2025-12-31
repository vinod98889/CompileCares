using CompileCares.Core.Entities;
using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Core.Entities.ClinicalMaster;
using CompileCares.Core.Entities.Doctors;
using CompileCares.Core.Entities.Master;
using CompileCares.Core.Entities.Patients;
using CompileCares.Core.Entities.Pharmacy;
using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;

namespace CompileCares.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        // ✅ ADD THIS: Parameterless constructor for design-time (migrations)
        public ApplicationDbContext()
        {
        }

        // Your existing constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ✅ ADD THIS: OnConfiguring method for design-time migrations
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This connection string is ONLY used during design-time (migrations)
                // It should match your appsettings.json
                optionsBuilder.UseSqlServer(
                    "Server=UMESH\\SQLEXPRESS;Database=CompileCareDB;Trusted_Connection=True;TrustServerCertificate=True;",
                    sqlOptions => sqlOptions.MigrationsAssembly("CompileCares.Infrastructure"));
            }
        }
        // ========== User management ==========
        public DbSet<User> Users { get; set; }
        // ========== PATIENT RELATED ==========
        public DbSet<Patient> Patients { get; set; }

        // ========== DOCTOR RELATED ==========
        public DbSet<Doctor> Doctors { get; set; }

        // ========== CLINICAL RELATED ==========
        public DbSet<OPDVisit> OPDVisits { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionMedicine> PrescriptionMedicines { get; set; }
        public DbSet<PatientComplaint> PatientComplaints { get; set; }
        public DbSet<PrescriptionAdvised> PrescriptionAdvisedItems { get; set; }

        // ========== BILLING RELATED ==========
        public DbSet<OPDBill> OPDBills { get; set; }
        public DbSet<OPDBillItem> OPDBillItems { get; set; }

        // ========== PHARMACY RELATED ==========
        public DbSet<Medicine> Medicines { get; set; }

        // ========== MASTER DATA ==========
        public DbSet<OPDItemMaster> OPDItemMasters { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Advised> AdvisedItems { get; set; }
        public DbSet<Dose> Doses { get; set; }

        // ========== TEMPLATES ==========
        public DbSet<PrescriptionTemplate> PrescriptionTemplates { get; set; }
        public DbSet<TemplateComplaint> TemplateComplaints { get; set; }
        public DbSet<TemplateMedicine> TemplateMedicines { get; set; }
        public DbSet<TemplateAdvised> TemplateAdvisedItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from Configurations folder
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Configure decimal precision
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        property.SetPrecision(18);
                        property.SetScale(2);
                    }
                }
            }
        }
    }
}