using System.Globalization;

namespace ParsingEngine.Transformation;

/// <summary>
/// Engine for applying property-level transformations after parsing
/// </summary>
public sealed class TransformationEngine
{
    private readonly Dictionary<string, ITransformation> _transformations;
    private readonly TransformationContext _context;

    public TransformationEngine(TransformationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _transformations = new Dictionary<string, ITransformation>(StringComparer.OrdinalIgnoreCase)
        {
            ["Capitalize"] = new CapitalizeTransformation(),
            ["RemoveSymbols"] = new RemoveSymbolsTransformation(),
            ["MapValue"] = new MapValueTransformation(),
            ["ExtractSizeAndUnits"] = new ExtractSizeAndUnitsTransformation(),
            ["Trim"] = new TrimTransformation(),
            ["ToUpper"] = new ToUpperTransformation(),
            ["ToLower"] = new ToLowerTransformation()
        };
    }

    /// <summary>
    /// Apply transformations to a PropertyBag
    /// </summary>
    /// <param name="propertyBag">The PropertyBag to transform</param>
    /// <param name="transformationConfigs">List of transformation configurations</param>
    /// <returns>A new PropertyBag with transformed values</returns>
    public PropertyBag Transform(PropertyBag propertyBag, IEnumerable<TransformationConfig> transformationConfigs)
    {
        ArgumentNullException.ThrowIfNull(propertyBag);
        ArgumentNullException.ThrowIfNull(transformationConfigs);

        var result = new PropertyBag 
        { 
            PreferFirstAssignment = propertyBag.PreferFirstAssignment 
        };

        // Copy all original values
        foreach (var kvp in propertyBag.Values)
        {
            result.Values[kvp.Key] = kvp.Value;
        }

        // Update context with current row properties
        _context.RowProperties.Clear();
        foreach (var kvp in propertyBag.Values)
        {
            _context.RowProperties[kvp.Key] = kvp.Value;
        }

        // Apply transformations
        foreach (var config in transformationConfigs)
        {
            if (result.Values.TryGetValue(config.Property, out var value) && value != null)
            {
                var transformedValue = ApplyTransformationSteps(value.ToString() ?? "", config.Steps);
                result.Set(config.Property, transformedValue, $"Transform:{config.Property}");
            }
        }

        return result;
    }

    private string ApplyTransformationSteps(string input, IEnumerable<TransformationStep> steps)
    {
        var current = input;
        
        foreach (var step in steps)
        {
            if (_transformations.TryGetValue(step.Type, out var transformation))
            {
                // Create step-specific context
                var stepContext = new TransformationContext
                {
                    Lookups = _context.Lookups,
                    Culture = _context.Culture,
                    RowProperties = new Dictionary<string, object?>(_context.RowProperties)
                };

                // Add step parameters to context
                foreach (var param in step.Parameters)
                {
                    stepContext.RowProperties[$"param:{param.Key}"] = param.Value;
                }

                current = transformation.Transform(current, stepContext);
            }
        }

        return current;
    }
}