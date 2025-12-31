using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class TemplateMedicineConfiguration : BaseEntityConfiguration<TemplateMedicine>
    {
        public override void Configure(EntityTypeBuilder<TemplateMedicine> builder)
        {
            base.Configure(builder);
            builder.ToTable("TemplateMedicines");

            // Properties
            builder.Property(m => m.DurationDays)
                .IsRequired();

            builder.Property(m => m.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(m => m.Instructions)
                .HasMaxLength(500);

            // Relationships
            builder.HasOne(m => m.PrescriptionTemplate)
                .WithMany(t => t.Medicines)
                .HasForeignKey(m => m.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Medicine)
                .WithMany()
                .HasForeignKey(m => m.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Dose)
                .WithMany()
                .HasForeignKey(m => m.DoseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(m => m.PrescriptionTemplateId);

            builder.HasIndex(m => m.MedicineId);

            builder.HasIndex(m => m.DoseId);

            builder.HasIndex(m => m.DurationDays);

            // Composite Index
            builder.HasIndex(m => new { m.PrescriptionTemplateId, m.MedicineId, m.DoseId });
        }
    }
}