using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Templates
{
    public class TemplateAdvisedConfiguration : BaseEntityConfiguration<TemplateAdvised>
    {
        public override void Configure(EntityTypeBuilder<TemplateAdvised> builder)
        {
            base.Configure(builder);
            builder.ToTable("TemplateAdvisedItems");

            builder.HasKey(ta => ta.Id);

            // References
            builder.Property(ta => ta.PrescriptionTemplateId)
                .IsRequired();

            builder.Property(ta => ta.AdvisedId)
                .IsRequired(false);

            // Advice Details
            builder.Property(ta => ta.CustomText)
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(ta => ta.PrescriptionTemplateId);

            builder.HasIndex(ta => ta.AdvisedId);

            // Navigation
            builder.HasOne(ta => ta.PrescriptionTemplate)
                .WithMany(t => t.AdvisedItems)
                .HasForeignKey(ta => ta.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ta => ta.Advised)
                .WithMany()
                .HasForeignKey(ta => ta.AdvisedId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}