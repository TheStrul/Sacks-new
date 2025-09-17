namespace ParsingEngine.Transformation;

/// <summary>
/// Represents a single transformation operation that can be applied to a value
/// </summary>
public interface ITransformation
{
    /// <summary>
    /// Apply the transformation to the input value
    /// </summary>
    /// <param name="input">The input value to transform</param>
    /// <param name="context">Transformation context with additional data</param>
    /// <returns>The transformed value</returns>
    string Transform(string input, TransformationContext context);
}

/// <summary>
/// Context for transformation operations
/// </summary>
public sealed class TransformationContext
{
    /// <summary>
    /// Lookup tables for value mapping
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Lookups { get; init; } = new();
    
    /// <summary>
    /// Culture for locale-specific transformations
    /// </summary>
    public string Culture { get; init; } = "en-US";
    
    /// <summary>
    /// Additional properties extracted from the same row
    /// </summary>
    public Dictionary<string, object?> RowProperties { get; init; } = new();
}

/// <summary>
/// Configuration for a transformation step
/// </summary>
public sealed class TransformationStep
{
    /// <summary>
    /// Type of transformation (e.g., "Capitalize", "MapValue", "RemoveSymbols")
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// Parameters for the transformation
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Configuration for property-level transformations
/// </summary>
public sealed class TransformationConfig
{
    /// <summary>
    /// Property name to apply transformations to
    /// </summary>
    public string Property { get; set; } = "";
    
    /// <summary>
    /// List of transformation steps to apply in order
    /// </summary>
    public List<TransformationStep> Steps { get; set; } = new();
}