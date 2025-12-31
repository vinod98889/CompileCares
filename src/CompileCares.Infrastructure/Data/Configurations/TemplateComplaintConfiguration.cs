using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class TemplateComplaintConfiguration : BaseEntityConfiguration<TemplateComplaint>
    {
        public override void Configure(EntityTypeBuilder<TemplateComplaint> builder)
        {
            base.Configure(builder);
            builder.ToTable("TemplateComplaints");

            // Properties
            builder.Property(c => c.CustomText)
                .HasMaxLength(500);

            builder.Property(c => c.Duration)
                .HasMaxLength(100);

            builder.Property(c => c.Severity)
                .HasMaxLength(50);

            // Relationships
            builder.HasOne(c => c.PrescriptionTemplate)
                .WithMany(t => t.Complaints)
                .HasForeignKey(c => c.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Complaint)
                .WithMany()
                .HasForeignKey(c => c.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(c => c.PrescriptionTemplateId);

            builder.HasIndex(c => c.ComplaintId)
                .HasFilter("[ComplaintId] IS NOT NULL");

            // Composite Index
            builder.HasIndex(c => new { c.PrescriptionTemplateId, c.ComplaintId })
                .HasFilter("[ComplaintId] IS NOT NULL");
        }
    }
}