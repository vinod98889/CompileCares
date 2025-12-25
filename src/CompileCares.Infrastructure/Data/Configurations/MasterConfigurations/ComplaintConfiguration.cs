using CompileCares.Core.Entities.ClinicalMaster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Master
{
    public class ComplaintConfiguration : BaseEntityConfiguration<Complaint>
    {
        public override void Configure(EntityTypeBuilder<Complaint> builder)
        {
            base.Configure(builder);
            builder.ToTable("Complaints");

            builder.HasKey(c => c.Id);

            // Core
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Category)
                .HasMaxLength(100);

            // Status
            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(c => c.IsCommon)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(c => c.Name);

            builder.HasIndex(c => c.Category);

            builder.HasIndex(c => c.IsCommon);

            builder.HasIndex(c => c.IsActive);

            // Navigation
            builder.HasMany(c => c.PatientComplaints)
                .WithOne(pc => pc.Complaint)
                .HasForeignKey(pc => pc.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

        }        
    }
}