namespace SacksMcp.Tools;

/// <summary>
/// NOTE: The ModelContextProtocol SDK uses automatic tool discovery via attributes.
/// Tools are automatically registered when you use .WithToolsFromAssembly() in Program.cs.
/// 
/// This file is kept for documentation purposes and potential custom tool management scenarios.
/// In most cases, you don't need to manually register tools - just decorate methods with
/// [McpServerTool] and they will be discovered automatically.
/// 
/// Example of attribute-based tool registration:
/// <code>
/// // In Program.cs:
/// builder.Services.AddMcpServer()
///     .WithStdioTransport()
///     .WithToolsFromAssembly();  // Automatically discovers all [McpServerTool] methods
/// 
/// // In your tool class:
/// [McpServerToolType]
/// public class MyTools
/// {
///     [McpServerTool]
///     [Description("Description of what this tool does")]
///     public async Task&lt;string&gt; MyTool(string parameter1, int parameter2)
///     {
///         // Tool implementation
///     }
/// }
/// </code>
/// </summary>
public static class ToolRegistryInfo
{
    /// <summary>
    /// Information about how tool registration works in ModelContextProtocol SDK
    /// </summary>
    public const string RegistrationInfo = @"
Tool Registration with ModelContextProtocol SDK:

1. Mark your tool class with [McpServerToolType] (optional)
2. Mark tool methods with [McpServerTool]
3. Add [Description] attribute for tool documentation
4. Use .WithToolsFromAssembly() in Program.cs to auto-discover tools

The SDK automatically:
- Discovers all methods marked with [McpServerTool]
- Generates JSON schemas from method parameters
- Handles parameter parsing and validation
- Supports dependency injection in tool class constructors
- Maps method return values to tool responses
";
}
