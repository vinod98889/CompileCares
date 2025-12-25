using CompileCares.Core.Entities.ClinicalMaster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Master
{
    public class AdvisedConfiguration : BaseEntityConfiguration<Advised>
    {
        public override void Configure(EntityTypeBuilder<Advised> builder)
        {
            base.Configure(builder);
            builder.ToTable("AdvisedItems");

            builder.HasKey(a => a.Id);

            // Core
            builder.Property(a => a.Text)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.Category)
                .HasMaxLength(100);

            // Status
            builder.Property(a => a.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(a => a.IsCommon)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(a => a.Text);

            builder.HasIndex(a => a.Category);

            builder.HasIndex(a => a.IsCommon);

            builder.HasIndex(a => a.IsActive);

            // Navigation
            builder.HasMany(a => a.PrescriptionAdvisedItems)
                .WithOne(pa => pa.Advised)
                .HasForeignKey(pa => pa.AdvisedId)
                .OnDelete(DeleteBehavior.Restrict);

        }        
    }
}