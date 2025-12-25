using CompileCares.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Check and configure common properties using reflection
        ConfigureCommonProperties(builder);
    }

    protected void ConfigureCommonProperties(EntityTypeBuilder<T> builder)
    {
        var entityType = typeof(T);

        // Check if property exists before configuring
        if (HasProperty(entityType, "CreatedAt"))
        {
            builder.Property<DateTime>("CreatedAt")
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        }

        if (HasProperty(entityType, "UpdatedAt"))
        {
            builder.Property<DateTime?>("UpdatedAt")
                .IsRequired(false);
        }

        if (HasProperty(entityType, "CreatedBy"))
        {
            builder.Property<Guid?>("CreatedBy")
                .IsRequired(false);
        }

        if (HasProperty(entityType, "UpdatedBy"))
        {
            builder.Property<Guid?>("UpdatedBy")
                .IsRequired(false);
        }

        if (HasProperty(entityType, "IsDeleted"))
        {
            builder.Property<bool>("IsDeleted")
                .IsRequired()
                .HasDefaultValue(false);

            // Add query filter for soft delete
            builder.HasQueryFilter(e => EF.Property<bool>(e, "IsDeleted") == false);
        }
    }

    private bool HasProperty(Type type, string propertyName)
    {
        return type.GetProperty(propertyName) != null;
    }
}