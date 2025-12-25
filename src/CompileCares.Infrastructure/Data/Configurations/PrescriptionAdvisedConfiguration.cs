using CompileCares.Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class PrescriptionAdvisedConfiguration : BaseEntityConfiguration<PrescriptionAdvised>
    {
        public override void Configure(EntityTypeBuilder<PrescriptionAdvised> builder)
        {
            base.Configure(builder);
            builder.ToTable("PrescriptionAdvisedItems");

            builder.HasKey(pa => pa.Id);

            // References
            builder.Property(pa => pa.PrescriptionId)
                .IsRequired();

            builder.Property(pa => pa.AdvisedId)
                .IsRequired(false);

            // Advice Details
            builder.Property(pa => pa.CustomAdvice)
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(pa => pa.PrescriptionId);

            builder.HasIndex(pa => pa.AdvisedId);

            // Navigation
            builder.HasOne(pa => pa.Prescription)
                .WithMany(p => p.AdvisedItems)
                .HasForeignKey(pa => pa.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pa => pa.Advised)
                .WithMany(a => a.PrescriptionAdvisedItems)
                .HasForeignKey(pa => pa.AdvisedId)
                .OnDelete(DeleteBehavior.Restrict);            
        }       
    }
}