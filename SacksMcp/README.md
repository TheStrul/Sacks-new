# SacksMcp Architecture

## Overview

SacksMcp is now a **layered MCP (Model Context Protocol) server architecture** designed for flexibility across multiple hosting environments.

## Project Structure

```
McpServer.Core (generic MCP base)
    ↓
McpServer.Database (generic database MCP extension)
    ↓
SacksMcp (Sacks-specific tools library)
    ↓
SacksMcp.Console (Console host - CURRENT)
SacksMcp.WindowsService (Windows Service host - PLANNED)
SacksMcp.AzureFunction (Azure Function host - PLANNED)
```

## Projects

### **SacksMcp** (Class Library)
The core library containing:
- **Tools**: `ProductTools`, `OfferTools`, `SupplierTools` - all MCP-exposed database operations
- **Configuration**: `SacksMcpServiceExtensions` - DI registration for any host
- **Dependencies**: McpServer.Core, McpServer.Database, SacksDataLayer

**Key Feature**: 100% reusable across different hosting environments.

### **SacksMcp.Console** (Console Application - CURRENT)
Minimal console host that:
- References SacksMcp library
- Calls `AddSacksMcpServices()` extension method
- Configures logging to stderr (MCP protocol requirement)
- Runs as a headless process communicating via stdio

**Usage**: AI agents launch this as a child process for MCP communication.

### **SacksMcp.WindowsService** (Planned - NEXT)
Windows Service host that will:
- Reference SacksMcp library
- Use same `AddSacksMcpServices()` extension
- Run as a Windows background service
- Provide named pipe or TCP transport instead of stdio

### **SacksMcp.AzureFunction** (Planned - PRODUCTION)
Azure Function host that will:
- Reference SacksMcp library
- Use same `AddSacksMcpServices()` extension
- Expose tools via HTTP triggers
- Scale automatically in cloud environment

## Key Design Principles

✅ **Separation of Concerns**: Tools logic separated from hosting  
✅ **Reusability**: Same tools library used across all hosts  
✅ **Flexibility**: Easy to add new hosting options (AWS Lambda, Docker, etc.)  
✅ **Maintainability**: Changes to tools don't require host project changes  
✅ **Testability**: Tools can be unit tested without hosting concerns  

## Adding a New Tool

1. Create a new class in `SacksMcp/Tools/` inheriting from `BaseDatabaseToolCollection<SacksDbContext>`
2. Mark class with `[McpServerToolType]`
3. Add methods marked with `[McpServerTool]` and `[Description]`
4. No changes needed to any host project - tools auto-discovered via reflection

## Creating a New Host

1. Create new project (Console/Service/Function)
2. Reference `SacksMcp.csproj`
3. Copy `appsettings.json` and customize if needed
4. Call `services.AddSacksMcpServices(configuration, configureLogging)` in startup
5. Configure host-specific transport (stdio/namedpipe/http)

## Configuration

All hosts share the same configuration structure in `appsettings.json`:
- `McpServerExtended`: Server name, version, timeouts, concurrency
- `Database`: Connection string, provider, retry policy, logging
- `Logging`: Standard .NET logging configuration

## Migration Path

- **Current**: Console app for development and local AI agent testing
- **Next**: Windows Service for always-on background availability
- **Production**: Azure Functions for cloud-scale, serverless deployment

## Benefits of This Architecture

1. **No Code Duplication**: Tools written once, hosted anywhere
2. **Easy Testing**: Test tools independently of hosting
3. **Flexible Deployment**: Choose hosting based on requirements
4. **Future-Proof**: New hosting options trivial to add
5. **Clean Dependencies**: Clear separation between layers
