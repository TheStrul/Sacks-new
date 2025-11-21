namespace Sacks.Core.Entities
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
        /// Updates the modification timestamp
        /// </summary>
        public virtual void UpdateModified()
        {
            ModifiedAt = DateTime.UtcNow;
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
