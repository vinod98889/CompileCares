using CompileCares.Core.Entities.Patients;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class PatientConfiguration : BaseEntityConfiguration<Patient>
    {
        public override void Configure(EntityTypeBuilder<Patient> builder)
        {
            base.Configure(builder);
            builder.ToTable("Patients");

            builder.HasKey(p => p.Id);

            // Basic Info
            builder.Property(p => p.PatientNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(p => p.Gender)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(p => p.DateOfBirth)
                .IsRequired(false);

            builder.Property(p => p.MaritalStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Contact Info
            builder.Property(p => p.Mobile)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(p => p.Email)
                .HasMaxLength(100);

            builder.Property(p => p.Address)
                .HasMaxLength(500);

            builder.Property(p => p.City)
                .HasMaxLength(100);

            builder.Property(p => p.Pincode)
                .HasMaxLength(10);

            // Medical Info
            builder.Property(p => p.BloodGroup)
                .HasMaxLength(10);

            builder.Property(p => p.Allergies)
                .HasMaxLength(1000);

            builder.Property(p => p.MedicalHistory)
                .HasMaxLength(2000);

            builder.Property(p => p.CurrentMedications)
                .HasMaxLength(1000);

            // Emergency Contact
            builder.Property(p => p.EmergencyContactName)
                .HasMaxLength(200);

            builder.Property(p => p.EmergencyContactPhone)
                .HasMaxLength(15);

            // Additional
            builder.Property(p => p.Occupation)
                .HasMaxLength(100);

            // Status
            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(p => p.PatientNumber)
                .IsUnique();

            builder.HasIndex(p => p.Mobile);

            builder.HasIndex(p => p.Name);

            builder.HasIndex(p => p.IsActive);

            builder.HasIndex(p => p.Status);            
        }       
    }
}