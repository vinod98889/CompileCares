using CompileCares.Core.Entities.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations.Master
{
    public class OPDItemMasterConfiguration : BaseEntityConfiguration<OPDItemMaster>
    {
        public override void Configure(EntityTypeBuilder<OPDItemMaster> builder)
        {
            base.Configure(builder);
            builder.ToTable("OPDItemMasters");

            builder.HasKey(i => i.Id);

            // Basic Information
            builder.Property(i => i.ItemCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(i => i.ItemName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(i => i.Description)
                .HasMaxLength(1000);

            // Classification
            builder.Property(i => i.ItemType)
                .IsRequired()
                .HasMaxLength(50); // Consultation, Procedure, Injection, etc.

            builder.Property(i => i.Category)
                .HasMaxLength(100);

            builder.Property(i => i.SubCategory)
                .HasMaxLength(100);

            // Pricing
            builder.Property(i => i.StandardPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(i => i.DoctorCommission)
                .HasColumnType("decimal(18,2)");

            builder.Property(i => i.IsCommissionPercentage)
                .IsRequired()
                .HasDefaultValue(true);

            // Medical Properties
            builder.Property(i => i.Instructions)
                .HasMaxLength(1000);

            builder.Property(i => i.PreparationRequired)
                .HasMaxLength(500);

            // Requirements
            builder.Property(i => i.RequiresSpecialist)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(i => i.RequiredEquipment)
                .HasMaxLength(500);

            builder.Property(i => i.RequiredConsent)
                .HasMaxLength(500);

            // Stock
            builder.Property(i => i.IsConsumable)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(i => i.CurrentStock)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(i => i.ReorderLevel)
                .IsRequired(false);

            // Status
            builder.Property(i => i.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(i => i.IsTaxable)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(i => i.IsDiscountable)
                .IsRequired()
                .HasDefaultValue(true);

            // Tax Information
            builder.Property(i => i.HSNCode)
                .HasMaxLength(20);

            builder.Property(i => i.GSTPercentage)
                .HasColumnType("decimal(5,2)");

            // Indexes
            builder.HasIndex(i => i.ItemCode)
                .IsUnique();

            builder.HasIndex(i => i.ItemName);

            builder.HasIndex(i => i.ItemType);

            builder.HasIndex(i => i.IsActive);

            builder.HasIndex(i => i.IsConsumable);

            // Navigation
            builder.HasMany(i => i.OPDBillItems)
                .WithOne(bi => bi.OPDItemMaster)
                .HasForeignKey(bi => bi.OPDItemMasterId)
                .OnDelete(DeleteBehavior.Restrict);            
        }       
    }
}