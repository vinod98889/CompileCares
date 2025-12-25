using CompileCares.Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class OPDBillItemConfiguration : BaseEntityConfiguration<OPDBillItem>
    {
        public override void Configure(EntityTypeBuilder<OPDBillItem> builder)
        {
            base.Configure(builder);
            builder.ToTable("OPDBillItems");

            builder.HasKey(i => i.Id);

            // References
            builder.Property(i => i.OPDBillId)
                .IsRequired();

            builder.Property(i => i.OPDItemMasterId)
                .IsRequired();

            // Snapshot of item details
            builder.Property(i => i.ItemName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(i => i.ItemType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(i => i.UnitPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(i => i.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(i => i.TotalPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // Discount & Commission
            builder.Property(i => i.DiscountAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(i => i.DiscountPercentage)
                .IsRequired()
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0);

            builder.Property(i => i.DoctorCommission)
                .HasColumnType("decimal(18,2)");

            // Tax
            builder.Property(i => i.IsTaxable)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(i => i.TaxAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            // Status
            builder.Property(i => i.IsAdministered)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(i => i.AdministeredDate)
                .IsRequired(false);

            builder.Property(i => i.AdministeredBy)
                .HasMaxLength(100);

            builder.Property(i => i.AdministrationNotes)
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(i => i.OPDBillId);

            builder.HasIndex(i => i.OPDItemMasterId);

            builder.HasIndex(i => i.ItemType);

            builder.HasIndex(i => i.IsAdministered);

            // Navigation
            builder.HasOne(i => i.OPDBill)
                .WithMany(b => b.BillItems)
                .HasForeignKey(i => i.OPDBillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.OPDItemMaster)
                .WithMany(m => m.OPDBillItems)
                .HasForeignKey(i => i.OPDItemMasterId)
                .OnDelete(DeleteBehavior.Restrict);            
        }       
    }
}