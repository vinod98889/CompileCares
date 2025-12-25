using CompileCares.Core.Entities.ClinicalMaster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Master
{
    public class DoseConfiguration : BaseEntityConfiguration<Dose>
    {
        public override void Configure(EntityTypeBuilder<Dose> builder)
        {
            base.Configure(builder);
            builder.ToTable("Doses");

            builder.HasKey(d => d.Id);

            // Core
            builder.Property(d => d.Code)
                .IsRequired()
                .HasMaxLength(20); // "OD", "BD", "TDS", "1-0-1"

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100); // "Once Daily", "Twice Daily"

            // Status
            builder.Property(d => d.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(d => d.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            // Indexes
            builder.HasIndex(d => d.Code)
                .IsUnique();

            builder.HasIndex(d => d.Name);

            builder.HasIndex(d => d.SortOrder);

            builder.HasIndex(d => d.IsActive);

            // Navigation
            builder.HasMany(d => d.PrescriptionMedicines)
                .WithOne(pm => pm.Dose)
                .HasForeignKey(pm => pm.DoseId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}