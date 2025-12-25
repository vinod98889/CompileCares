using CompileCares.Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class PatientComplaintConfiguration : BaseEntityConfiguration<PatientComplaint>
    {
        public override void Configure(EntityTypeBuilder<PatientComplaint> builder)
        {
            base.Configure(builder);
            builder.ToTable("PatientComplaints");

            builder.HasKey(pc => pc.Id);

            // References
            builder.Property(pc => pc.PrescriptionId)
                .IsRequired();

            builder.Property(pc => pc.ComplaintId)
                .IsRequired(false);

            // Complaint Details
            builder.Property(pc => pc.CustomComplaint)
                .HasMaxLength(500);

            builder.Property(pc => pc.Duration)
                .HasMaxLength(100);

            builder.Property(pc => pc.Severity)
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(pc => pc.PrescriptionId);

            builder.HasIndex(pc => pc.ComplaintId);

            // Navigation
            builder.HasOne(pc => pc.Prescription)
                .WithMany(p => p.Complaints)
                .HasForeignKey(pc => pc.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pc => pc.Complaint)
                .WithMany(c => c.PatientComplaints)
                .HasForeignKey(pc => pc.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}