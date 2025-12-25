using CompileCares.Core.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Templates
{
    public class TemplateComplaintConfiguration : BaseEntityConfiguration<TemplateComplaint>
    {
        public override void Configure(EntityTypeBuilder<TemplateComplaint> builder)
        {
            base.Configure(builder);
            builder.ToTable("TemplateComplaints");

            builder.HasKey(tc => tc.Id);

            // References
            builder.Property(tc => tc.PrescriptionTemplateId)
                .IsRequired();

            builder.Property(tc => tc.ComplaintId)
                .IsRequired(false);

            // Complaint Details
            builder.Property(tc => tc.CustomText)
                .HasMaxLength(500);

            builder.Property(tc => tc.Duration)
                .HasMaxLength(100);

            builder.Property(tc => tc.Severity)
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(tc => tc.PrescriptionTemplateId);

            builder.HasIndex(tc => tc.ComplaintId);

            // Navigation
            builder.HasOne(tc => tc.PrescriptionTemplate)
                .WithMany(t => t.Complaints)
                .HasForeignKey(tc => tc.PrescriptionTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tc => tc.Complaint)
                .WithMany()
                .HasForeignKey(tc => tc.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}