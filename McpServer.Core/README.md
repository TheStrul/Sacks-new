# McpServer.Core

**100% Reusable MCP Server Foundation** - This generic class library provides base infrastructure for building MCP (Model Context Protocol) servers in any workspace.

## Purpose

McpServer.Core is a workspace-agnostic foundation that can be referenced by ANY project that needs MCP server capabilities. It contains:

- Base tool collection classes with common utilities (JSON formatting, validation, logging)
- Configuration options (server name, version, timeouts, logging)
- Base hosted service implementation for MCP server lifecycle
- Documentation and patterns for attribute-based tool registration

## Key Features

### BaseMcpToolCollection
Abstract base class for all tool collections providing:
- `FormatSuccess(object)` - Format successful responses as JSON
- `FormatError(string, string?)` - Format error responses consistently
- `ValidateRequired(string?, string)` - Validate required string parameters
- `ValidateRange(int, int, int, string)` - Validate numeric ranges
- Integrated `ILogger` support

### McpServerExtendedOptions
Configuration class for extended server options:
- `ServerName` - Name of your MCP server
- `Version` - Version string
- `MaxConcurrentTools` - Maximum concurrent tool executions (default: 10)
- `ToolTimeoutSeconds` - Tool execution timeout (default: 30)
- `EnableDetailedLogging` - Enable verbose logging (default: false)

Configuration section: `"McpServerExtended"`

### BaseMcpServer
Base `IHostedService` implementation handling:
- MCP server lifecycle (start/stop)
- Logging integration
- Configuration access
- Application lifetime coordination

## Usage Pattern

### 1. Reference this project
```xml
<ItemGroup>
  <ProjectReference Include="..\McpServer.Core\McpServer.Core.csproj" />
</ItemGroup>
```

### 2. Create your tool collection
```csharp
using McpServer.Core.Tools;
using ModelContextProtocol.Server;

[McpServerToolType]
public class MyTools : BaseMcpToolCollection
{
    public MyTools(ILogger<MyTools> logger) : base(logger) { }
    
    [McpServerTool]
    [Description("Your tool description")]
    public string MyTool(string input)
    {
        ValidateRequired(input, nameof(input));
        
        // Your logic here
        var result = ProcessInput(input);
        
        return FormatSuccess(result);
    }
}
```

### 3. Configure in Program.cs
```csharp
using McpServer.Core.Configuration;

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Services.Configure<McpServerExtendedOptions>(options =>
{
    options.ServerName = "MyMcpServer";
    options.Version = "1.0.0";
});
```

### 4. Configure in appsettings.json
```json
{
  "McpServerExtended": {
    "ServerName": "MyMcpServer",
    "Version": "1.0.0",
    "MaxConcurrentTools": 10,
    "ToolTimeoutSeconds": 30,
    "EnableDetailedLogging": false
  }
}
```

## Architecture Vision

This project is part of an extensible MCP server architecture:

```
McpServer.Core (generic base - any workspace)
    ↓
McpServer.Database (one implementation option)
McpServer.FileSystem (future option)
McpServer.WebApi (future option)
McpServer.Email (future option)
    ↓
YourSpecificMcpServer (specific implementation)
```

## Tool Registration Pattern

This project uses the **ModelContextProtocol SDK** (v0.4.0-preview.3) attribute-based tool registration:

1. Mark your tool collection class with `[McpServerToolType]` (optional but recommended)
2. Mark individual tool methods with `[McpServerTool]`
3. Use `[Description]` attributes for documentation
4. Call `.WithToolsFromAssembly()` in your startup code
5. The SDK automatically discovers and registers tools

See `Tools/ToolRegistryInfo.cs` for detailed documentation.

## Dependencies

- **ModelContextProtocol 0.4.0-preview.3** - Official Microsoft MCP SDK
- **Microsoft.Extensions.Hosting 10.0.0** - Hosted service pattern
- **Microsoft.Extensions.Logging 10.0.0** - Structured logging
- **Microsoft.Extensions.Options 10.0.0** - Configuration pattern

## Target Framework

- **.NET 10** (net10.0)

## Cross-Workspace Reusability

This project is intentionally generic:
- ✅ No workspace-specific naming (e.g., "SacksMcp", "MyAppMcp")
- ✅ No hardcoded business logic
- ✅ No external dependencies beyond Microsoft.Extensions.*
- ✅ Clean separation of concerns

You can copy this project to ANY workspace or reference it from a shared location.

## Example Implementation

See the **SacksMcp** project in this solution for a complete example of how to:
- Reference McpServer.Core
- Create specific tool collections
- Configure the server
- Implement domain-specific logic

## Future Extensions

Consider creating specialized extension projects:
- **McpServer.Database** - Database tool collections (EF Core integration)
- **McpServer.FileSystem** - File system operations
- **McpServer.WebApi** - HTTP/REST API tools
- **McpServer.Email** - Email operations
- **McpServer.Messaging** - Message queue integration

Each extension should reference McpServer.Core and provide specialized base classes and utilities.
