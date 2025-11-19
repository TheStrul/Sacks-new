using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SacksMcp.Tools;

namespace SacksMcp.Examples;

/// <summary>
/// Example tool collection demonstrating the ModelContextProtocol SDK tool patterns.
/// This serves as a template for creating your own tools.
/// 
/// To create a new tool:
/// 1. Create a class (optionally mark with [McpServerToolType])
/// 2. Add a constructor that accepts dependencies via DI
/// 3. Create public methods marked with [McpServerTool]
/// 4. Add [Description] attributes to document tool behavior
/// 5. Parameters are automatically converted to JSON schema
/// 6. Return string, Task&lt;string&gt;, or other serializable types
/// </summary>
[McpServerToolType]
public class ExampleTools : BaseMcpToolCollection
{
    public ExampleTools(ILogger<ExampleTools> logger) : base(logger)
    {
    }

    /// <summary>
    /// Simple echo tool that demonstrates basic parameter handling.
    /// </summary>
    [McpServerTool]
    [Description("Echoes the provided message back to the caller. Useful for testing MCP connectivity.")]
    public string Echo(
        [Description("The message to echo back")] string message)
    {
        ValidateRequired(message, nameof(message));
        
        Logger.LogInformation("Echo tool called with message: {Message}", message);
        
        return FormatSuccess(new
        {
            echo = message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Demonstrates async operations and optional parameters.
    /// </summary>
    [McpServerTool]
    [Description("Performs a calculation and demonstrates async tool execution with optional parameters.")]
    public async Task<string> Calculate(
        [Description("First number")] int number1,
        [Description("Second number")] int number2,
        [Description("Operation to perform: add, subtract, multiply, divide")] string operation = "add")
    {
        ValidateRequired(operation, nameof(operation));
        
        Logger.LogInformation("Calculate tool called: {Number1} {Operation} {Number2}", 
            number1, operation, number2);

        try
        {
            // Simulate async operation
            await Task.Delay(100).ConfigureAwait(false);

            decimal result = operation.ToLowerInvariant() switch
            {
                "add" => number1 + number2,
                "subtract" => number1 - number2,
                "multiply" => number1 * number2,
                "divide" => number2 != 0 ? (decimal)number1 / number2 : throw new DivideByZeroException(),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };

            return FormatSuccess(new
            {
                operation,
                number1,
                number2,
                result
            });
        }
        catch (Exception ex)
        {
            return FormatError($"Calculation failed: {ex.Message}", ex.GetType().Name);
        }
    }

    /// <summary>
    /// Demonstrates error handling and validation.
    /// </summary>
    [McpServerTool]
    [Description("Validates an email address format. Returns detailed validation results.")]
    public string ValidateEmail(
        [Description("Email address to validate")] string email)
    {
        try
        {
            ValidateRequired(email, nameof(email));

            // Simple email validation
            var isValid = email.Contains('@') && 
                         email.Contains('.') && 
                         email.IndexOf('@') < email.LastIndexOf('.');

            return FormatSuccess(new
            {
                email,
                isValid,
                checks = new
                {
                    hasAtSymbol = email.Contains('@'),
                    hasDot = email.Contains('.'),
                    validStructure = isValid
                }
            });
        }
        catch (ArgumentException ex)
        {
            return FormatError("Validation error", ex.Message);
        }
    }
}
