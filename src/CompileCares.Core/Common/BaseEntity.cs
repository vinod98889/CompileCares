// CompileCares.Core/Common/BaseEntity.cs
using System;

namespace CompileCares.Core.Common
{
    public abstract class BaseEntity
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        // Core properties
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }

        // Soft delete
        public bool IsDeleted { get; protected set; }
        public DateTime? DeletedAt { get; protected set; }

        // User tracking (nullable - not all entities need it)
        public Guid? CreatedBy { get; protected set; }
        public Guid? UpdatedBy { get; protected set; }
        public Guid? DeletedBy { get; protected set; }

        // Timestamp methods
        protected void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

        // Soft delete with optional user tracking
        public virtual void SoftDelete(Guid? deletedBy = null)
        {
            if (IsDeleted) return;

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            UpdateTimestamp();
        }

        // Restore
        public virtual void Restore()
        {
            if (!IsDeleted) return;

            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
            UpdateTimestamp();
        }

        // User tracking methods
        public void SetCreatedBy(Guid userId)
        {
            CreatedBy ??= userId; // Set only if null
        }

        public void SetUpdatedBy(Guid userId)
        {
            UpdatedBy = userId;
            UpdateTimestamp();
        }

        // Allows setting ID for testing, data imports, or special cases
        public void SetId(Guid id)
        {
            // Optional: Add validation if needed
            if (id == Guid.Empty)
                throw new ArgumentException("ID cannot be empty", nameof(id));

            Id = id;
        }

        // ✅ Optional: Also add a method to set CreatedAt for testing
        public void SetCreatedAt(DateTime createdAt)
        {
            CreatedAt = createdAt;
        }
    }
}