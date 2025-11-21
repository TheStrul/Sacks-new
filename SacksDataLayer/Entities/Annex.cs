namespace Sacks.Core.Entities;

/// <summary>
/// Base class for auxiliary/junction entities that are not standalone business entities
/// but rather support relationships between main entities or store supplementary data.
/// Examples: junction tables, configuration tables, relationship tables, etc.
/// </summary>
public abstract class Annex
{
    /// <summary>
    /// Primary key identifier for the annex record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date and time when the annex record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the annex record was last modified
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
    /// Determines if two annex records are equal based on their Id
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Annex other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id == 0 || other.Id == 0)
            return false;

        return Id == other.Id && GetType() == other.GetType();
    }

    /// <summary>
    /// Gets the hash code for the annex record based on its Id
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// String representation of the annex record
    /// </summary>
    public override string ToString()
    {
        return $"{GetType().Name} [Id: {Id}]";
    }
}
