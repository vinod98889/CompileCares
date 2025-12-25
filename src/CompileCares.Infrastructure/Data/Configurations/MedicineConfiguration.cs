using CompileCares.Core.Entities.Pharmacy;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class MedicineConfiguration : BaseEntityConfiguration<Medicine>
    {
        public override void Configure(EntityTypeBuilder<Medicine> builder)
        {
            base.Configure(builder);
            builder.ToTable("Medicines");

            builder.HasKey(m => m.Id);

            // Basic Information
            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.GenericName)
                .HasMaxLength(200);

            builder.Property(m => m.BrandName)
                .HasMaxLength(100);

            builder.Property(m => m.MedicineType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            // Classification
            builder.Property(m => m.Category)
                .HasMaxLength(100);

            builder.Property(m => m.SubCategory)
                .HasMaxLength(100);

            builder.Property(m => m.TherapeuticClass)
                .HasMaxLength(100);

            // Form & Strength
            builder.Property(m => m.Form)
                .HasMaxLength(50);

            builder.Property(m => m.Strength)
                .HasMaxLength(100);

            builder.Property(m => m.PackSize)
                .HasMaxLength(100);

            builder.Property(m => m.Composition)
                .HasMaxLength(500);

            // Manufacturer
            builder.Property(m => m.Manufacturer)
                .HasMaxLength(200);

            builder.Property(m => m.ManufacturerCode)
                .HasMaxLength(100);

            // Stock Management
            builder.Property(m => m.CurrentStock)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(m => m.MinimumStockLevel)
                .IsRequired()
                .HasDefaultValue(10);

            builder.Property(m => m.ReorderLevel)
                .IsRequired()
                .HasDefaultValue(20);

            builder.Property(m => m.StockUnit)
                .HasMaxLength(50)
                .HasDefaultValue("Units");

            // Pricing
            builder.Property(m => m.PurchasePrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(m => m.SellingPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(m => m.MRP)
                .HasColumnType("decimal(18,2)");

            builder.Property(m => m.GSTPercentage)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(5.0m);

            // Regulatory
            builder.Property(m => m.HSNCode)
                .HasMaxLength(20);

            builder.Property(m => m.Schedule)
                .HasMaxLength(10);

            builder.Property(m => m.RequiresPrescription)
                .IsRequired()
                .HasDefaultValue(true);

            // Safety Information
            builder.Property(m => m.SideEffects)
                .HasMaxLength(2000);

            builder.Property(m => m.Contraindications)
                .HasMaxLength(2000);

            builder.Property(m => m.Precautions)
                .HasMaxLength(2000);

            builder.Property(m => m.StorageInstructions)
                .HasMaxLength(500);

            // Status
            builder.Property(m => m.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(m => m.ExpiryDate)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(m => m.Name);

            builder.HasIndex(m => m.GenericName);

            builder.HasIndex(m => m.Category);

            builder.HasIndex(m => m.IsActive);

            builder.HasIndex(m => m.RequiresPrescription);

            builder.HasIndex(m => m.CurrentStock);

            // Navigation
            builder.HasMany(m => m.PrescriptionMedicines)
                .WithOne(pm => pm.Medicine)
                .HasForeignKey(pm => pm.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);            
        }        
    }
}