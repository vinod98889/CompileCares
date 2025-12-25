using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class PrescriptionConfiguration : BaseEntityConfiguration<Prescription>
    {
        public override void Configure(EntityTypeBuilder<Prescription> builder)
        {
            base.Configure(builder);
            builder.ToTable("Prescriptions");

            builder.HasKey(p => p.Id);

            // Prescription Information
            builder.Property(p => p.PrescriptionNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.PrescriptionDate)
                .IsRequired();

            builder.Property(p => p.Diagnosis)
                .HasMaxLength(1000);

            builder.Property(p => p.Instructions)
                .HasMaxLength(2000);

            builder.Property(p => p.FollowUpInstructions)
                .HasMaxLength(500);

            // Status
            builder.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Validity
            builder.Property(p => p.ValidityDays)
                .IsRequired()
                .HasDefaultValue(7);

            builder.Property(p => p.ValidUntil)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(p => p.PrescriptionNumber)
                .IsUnique();

            builder.HasIndex(p => p.PatientId);

            builder.HasIndex(p => p.DoctorId);

            builder.HasIndex(p => p.OPDVisitId);

            builder.HasIndex(p => p.Status);

            builder.HasIndex(p => p.ValidUntil);

            // Navigation
            builder.HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.OPDVisit)
                .WithOne(v => v.Prescription)
                .HasForeignKey<Prescription>(p => p.OPDVisitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Medicines)
                .WithOne(m => m.Prescription)
                .HasForeignKey(m => m.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Complaints)
                .WithOne(c => c.Prescription)
                .HasForeignKey(c => c.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.AdvisedItems)
                .WithOne(a => a.Prescription)
                .HasForeignKey(a => a.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }        
    }
}