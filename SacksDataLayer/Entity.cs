namespace SacksDataLayer
{
    public abstract class Entity
    {
        /// <summary>
        /// Primary key identifier for the entity
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Date and time when the entity was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the entity was last modified
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who created this entity
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User who last modified this entity
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Indicates whether the entity is soft deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Date and time when the entity was soft deleted
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// User who soft deleted this entity
        /// </summary>
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Updates the modification timestamp and user
        /// </summary>
        /// <param name="modifiedBy">User making the modification</param>
        public virtual void UpdateModified(string? modifiedBy = null)
        {
            ModifiedAt = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Soft deletes the entity
        /// </summary>
        /// <param name="deletedBy">User performing the deletion</param>
        public virtual void SoftDelete(string? deletedBy = null)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
        }

        /// <summary>
        /// Restores a soft deleted entity
        /// </summary>
        public virtual void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }

        /// <summary>
        /// Determines if two entities are equal based on their Id
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not Entity other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Id == 0 || other.Id == 0)
                return false;

            return Id == other.Id && GetType() == other.GetType();
        }

        /// <summary>
        /// Gets the hash code for the entity based on its Id
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// String representation of the entity
        /// </summary>
        public override string ToString()
        {
            return $"{GetType().Name} [Id: {Id}]";
        }
    }
}
