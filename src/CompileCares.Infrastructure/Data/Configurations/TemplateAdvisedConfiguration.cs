using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class TemplateAdvisedConfiguration : BaseEntityConfiguration<TemplateAdvised>
    {
        public override void Configure(EntityTypeBuilder<TemplateAdvised> builder)
        {
            base.Configure(builder);
            builder.ToTable("TemplateAdvisedItems");

            // Properties
            builder.Property(a => a.CustomText)
                .HasMaxLength(500);

            // Relationships
            builder.HasOne(a => a.PrescriptionTemplate)
                .WithMany(t => t.AdvisedItems)
                .HasForeignKey(a => a.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Advised)
                .WithMany()
                .HasForeignKey(a => a.AdvisedId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(a => a.PrescriptionTemplateId);

            builder.HasIndex(a => a.AdvisedId)
                .HasFilter("[AdvisedId] IS NOT NULL");

            // Composite Index
            builder.HasIndex(a => new { a.PrescriptionTemplateId, a.AdvisedId })
                .HasFilter("[AdvisedId] IS NOT NULL");
        }
    }
}