using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Templates
{
    public class PrescriptionTemplateConfiguration : BaseEntityConfiguration<PrescriptionTemplate>
    {
        public override void Configure(EntityTypeBuilder<PrescriptionTemplate> builder)
        {
            base.Configure(builder);
            builder.ToTable("PrescriptionTemplates");

            builder.HasKey(t => t.Id);

            // Template Information
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            builder.Property(t => t.Category)
                .HasMaxLength(100);

            builder.Property(t => t.DoctorId)
                .IsRequired(false);

            builder.Property(t => t.IsPublic)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(t => t.Name);

            builder.HasIndex(t => t.Category);

            builder.HasIndex(t => t.DoctorId);

            builder.HasIndex(t => t.IsPublic);

            // Navigation
            builder.HasMany(t => t.Complaints)
                .WithOne(c => c.PrescriptionTemplate)
                .HasForeignKey(c => c.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Medicines)
                .WithOne(m => m.PrescriptionTemplate)
                .HasForeignKey(m => m.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.AdvisedItems)
                .WithOne(a => a.PrescriptionTemplate)
                .HasForeignKey(a => a.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

        }        
    }
}