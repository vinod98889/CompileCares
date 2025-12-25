using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Templates
{
    public class TemplateMedicineConfiguration : BaseEntityConfiguration<TemplateMedicine>
    {
        public override void Configure(EntityTypeBuilder<TemplateMedicine> builder)
        {
            base.Configure(builder);
            builder.ToTable("TemplateMedicines");

            builder.HasKey(tm => tm.Id);

            // References
            builder.Property(tm => tm.PrescriptionTemplateId)
                .IsRequired();

            builder.Property(tm => tm.MedicineId)
                .IsRequired();

            builder.Property(tm => tm.DoseId)
                .IsRequired();

            // Medicine Details
            builder.Property(tm => tm.DurationDays)
                .IsRequired();

            builder.Property(tm => tm.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(tm => tm.Instructions)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(tm => tm.PrescriptionTemplateId);

            builder.HasIndex(tm => tm.MedicineId);

            builder.HasIndex(tm => tm.DoseId);

            // Navigation
            builder.HasOne(tm => tm.PrescriptionTemplate)
                .WithMany(t => t.Medicines)
                .HasForeignKey(tm => tm.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tm => tm.Medicine)
                .WithMany()
                .HasForeignKey(tm => tm.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(tm => tm.Dose)
                .WithMany()
                .HasForeignKey(tm => tm.DoseId)
                .OnDelete(DeleteBehavior.Restrict);            
        }       
    }
}