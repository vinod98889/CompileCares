using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class PrescriptionTemplateConfiguration : BaseEntityConfiguration<PrescriptionTemplate>
    {
        public override void Configure(EntityTypeBuilder<PrescriptionTemplate> builder)
        {
            base.Configure(builder);
            builder.ToTable("PrescriptionTemplates");

            // Basic Info
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.Property(t => t.Category)
                .HasMaxLength(100);

            builder.Property(t => t.IsPublic)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(t => t.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(t => t.LastUsed)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(t => t.Doctor)
                .WithMany()
                .HasForeignKey(t => t.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Navigation Properties
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

            // Indexes
            builder.HasIndex(t => t.Name);

            builder.HasIndex(t => t.Category);

            builder.HasIndex(t => t.DoctorId);

            builder.HasIndex(t => t.IsPublic);

            builder.HasIndex(t => t.UsageCount);

            builder.HasIndex(t => t.LastUsed);

            builder.HasIndex(t => new { t.DoctorId, t.IsPublic });

            builder.HasIndex(t => new { t.Category, t.IsPublic });
        }
    }
}