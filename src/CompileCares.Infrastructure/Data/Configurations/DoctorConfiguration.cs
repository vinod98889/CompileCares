using CompileCares.Core.Entities.Doctors;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class DoctorConfiguration : BaseEntityConfiguration<Doctor>
    {
        public override void Configure(EntityTypeBuilder<Doctor> builder)
        {
            base.Configure(builder);
            builder.ToTable("Doctors");

            builder.HasKey(d => d.Id);

            // Basic Info
            builder.Property(d => d.RegistrationNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Title)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(d => d.Gender)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(d => d.DateOfBirth)
                .IsRequired(false);

            builder.Property(d => d.DoctorType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            // Professional Info
            builder.Property(d => d.Qualification)
                .HasMaxLength(200);

            builder.Property(d => d.Specialization)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Department)
                .HasMaxLength(100);

            builder.Property(d => d.YearsOfExperience)
                .IsRequired()
                .HasDefaultValue(0);

            // Contact Info
            builder.Property(d => d.Mobile)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(d => d.Email)
                .HasMaxLength(100);

            builder.Property(d => d.Address)
                .HasMaxLength(500);

            builder.Property(d => d.City)
                .HasMaxLength(100);

            builder.Property(d => d.Pincode)
                .HasMaxLength(10);

            // Work Details
            builder.Property(d => d.HospitalName)
                .HasMaxLength(200);

            builder.Property(d => d.HospitalAddress)
                .HasMaxLength(500);

            builder.Property(d => d.ConsultationHours)
                .HasMaxLength(100);

            builder.Property(d => d.AvailableDays)
                .HasMaxLength(100);

            // Fees
            builder.Property(d => d.ConsultationFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.FollowUpFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.EmergencyFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // Status
            builder.Property(d => d.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(d => d.IsAvailable)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(d => d.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);

            // Additional
            builder.Property(d => d.SignaturePath)
                .HasMaxLength(500);

            builder.Property(d => d.DigitalSignature)
                .HasMaxLength(2000);

            // Indexes
            builder.HasIndex(d => d.RegistrationNumber)
                .IsUnique();

            builder.HasIndex(d => d.Mobile);

            builder.HasIndex(d => d.Name);

            builder.HasIndex(d => d.Specialization);

            builder.HasIndex(d => d.IsActive);

            builder.HasIndex(d => d.IsAvailable);

            // Navigation
            builder.HasMany(d => d.OPDVisits)
                .WithOne(v => v.Doctor)
                .HasForeignKey(v => v.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}