using CompileCares.Core.Entities.Billing;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class OPDBillConfiguration : BaseEntityConfiguration<OPDBill>
    {
        public override void Configure(EntityTypeBuilder<OPDBill> builder)
        {
            base.Configure(builder);
            builder.ToTable("OPDBills");

            builder.HasKey(b => b.Id);

            // References
            builder.Property(b => b.OPDVisitId)
                .IsRequired();

            builder.Property(b => b.PatientId)
                .IsRequired();

            builder.Property(b => b.DoctorId)
                .IsRequired();

            // Bill Information
            builder.Property(b => b.BillNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(b => b.BillDate)
                .IsRequired();

            builder.Property(b => b.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Charges
            builder.Property(b => b.ConsultationFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.ProcedureFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.MedicineFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.LabTestFee)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.OtherCharges)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            // Calculations
            builder.Property(b => b.SubTotal)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.DiscountAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.TaxAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.PaidAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.DueAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            // Payment Information
            builder.Property(b => b.PaymentMode)
                .HasMaxLength(50);

            builder.Property(b => b.TransactionId)
                .HasMaxLength(100);

            // Insurance
            builder.Property(b => b.InsuranceProvider)
                .HasMaxLength(200);

            builder.Property(b => b.InsurancePolicyNumber)
                .HasMaxLength(100);

            builder.Property(b => b.InsuranceCoveredAmount)
                .HasColumnType("decimal(18,2)");

            // Discount & Tax
            builder.Property(b => b.DiscountPercentage)
                .IsRequired()
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0);

            builder.Property(b => b.TaxPercentage)
                .IsRequired()
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(5.0m);

            // Notes
            builder.Property(b => b.Notes)
                .HasMaxLength(2000);

            // Indexes
            builder.HasIndex(b => b.BillNumber)
                .IsUnique();

            builder.HasIndex(b => b.OPDVisitId)
                .IsUnique();

            builder.HasIndex(b => b.PatientId);

            builder.HasIndex(b => b.DoctorId);

            builder.HasIndex(b => b.Status);

            builder.HasIndex(b => b.BillDate);

            // Navigation
            builder.HasOne(b => b.OPDVisit)
                .WithOne(v => v.Bill)
                .HasForeignKey<OPDBill>(b => b.OPDVisitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Patient)
                .WithMany()
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Doctor)
                .WithMany()
                .HasForeignKey(b => b.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.BillItems)
                .WithOne(i => i.OPDBill)
                .HasForeignKey(i => i.OPDBillId)
                .OnDelete(DeleteBehavior.Cascade);            
        }        
    }
}