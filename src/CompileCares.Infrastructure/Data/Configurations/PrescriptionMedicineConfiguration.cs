using CompileCares.Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class PrescriptionMedicineConfiguration : BaseEntityConfiguration<PrescriptionMedicine>
    {
        public override void Configure(EntityTypeBuilder<PrescriptionMedicine> builder)
        {
            base.Configure(builder);
            builder.ToTable("PrescriptionMedicines");

            builder.HasKey(pm => pm.Id);

            // References
            builder.Property(pm => pm.PrescriptionId)
                .IsRequired();

            builder.Property(pm => pm.MedicineId)
                .IsRequired();

            builder.Property(pm => pm.DoseId)
                .IsRequired();

            // Dosage Information
            builder.Property(pm => pm.CustomDosage)
                .HasMaxLength(100);

            builder.Property(pm => pm.DurationDays)
                .IsRequired();

            builder.Property(pm => pm.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            // Instructions
            builder.Property(pm => pm.Instructions)
                .HasMaxLength(500);

            builder.Property(pm => pm.AdditionalNotes)
                .HasMaxLength(1000);

            // Status
            builder.Property(pm => pm.IsDispensed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(pm => pm.DispensedDate)
                .IsRequired(false);

            builder.Property(pm => pm.DispensedBy)
                .HasMaxLength(100);

            // Indexes
            builder.HasIndex(pm => pm.PrescriptionId);

            builder.HasIndex(pm => pm.MedicineId);

            builder.HasIndex(pm => pm.DoseId);

            builder.HasIndex(pm => pm.IsDispensed);

            // Navigation
            builder.HasOne(pm => pm.Prescription)
                .WithMany(p => p.Medicines)
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.Medicine)
                .WithMany(m => m.PrescriptionMedicines)
                .HasForeignKey(pm => pm.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pm => pm.Dose)
                .WithMany(d => d.PrescriptionMedicines)
                .HasForeignKey(pm => pm.DoseId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}