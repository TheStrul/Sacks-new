using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpServer.Core.Tools;

/// <summary>
/// Base class for MCP tool collections. This class provides common utilities and patterns
/// for implementing MCP tools using the attribute-based approach.
/// 
/// The ModelContextProtocol SDK uses attributes for tool registration:
/// - Mark your class with [McpServerToolType] (optional, for tool grouping)
/// - Mark tool methods with [McpServerTool] attribute
/// - Add [Description] attribute to methods for tool documentation
/// - Method parameters become tool input parameters automatically
/// 
/// Example:
/// <code>
/// [McpServerToolType]
/// public class MyTools : BaseMcpToolCollection
/// {
///     private readonly MyDbContext _db;
///     
///     public MyTools(MyDbContext db, ILogger&lt;MyTools&gt; logger) 
///         : base(logger)
///     {
///         _db = db;
///     }
///     
///     [McpServerTool]
///     [Description("Description of what this tool does")]
///     public async Task&lt;string&gt; MyTool(string parameter)
///     {
///         // Your implementation here
///     }
/// }
/// </code>
/// </summary>
public abstract class BaseMcpToolCollection
{
    protected readonly ILogger Logger;
    
    // Cached JSON serializer options to avoid creating new instances
    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        WriteIndented = true 
    };

    protected BaseMcpToolCollection(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Helper method to format successful tool results as JSON.
    /// </summary>
    protected string FormatSuccess(object data)
    {
        return JsonSerializer.Serialize(new
        {
            success = true,
            data
        }, _jsonOptions);
    }

    /// <summary>
    /// Helper method to format error results as JSON.
    /// </summary>
    protected string FormatError(string message, string? details = null)
    {
        Logger.LogError("Tool error: {Message}. Details: {Details}", message, details);
        
        return JsonSerializer.Serialize(new
        {
            success = false,
            error = message,
            details
        }, _jsonOptions);
    }

    /// <summary>
    /// Helper method to validate required string parameters.
    /// Throws ArgumentException if invalid.
    /// </summary>
    protected void ValidateRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Parameter '{parameterName}' is required and cannot be empty.");
        }
    }

    /// <summary>
    /// Helper method to validate numeric ranges.
    /// Throws ArgumentOutOfRangeException if invalid.
    /// </summary>
    protected void ValidateRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(parameterName, 
                $"Parameter '{parameterName}' must be between {min} and {max}.");
        }
    }
}
