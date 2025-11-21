# Sacks.Configuration

**Centralized Configuration for All Sacks Applications**

This project provides a single source of truth for all configuration across the Sacks solution, eliminating duplication and inconsistency.

## Structure

```
Sacks.Configuration/ (this project)
├── appsettings.json (centralized config - auto-copied to all apps)
├── appsettings.Production.json (optional environment-specific)
├── SacksConfigurationOptions.cs (strongly-typed classes)
├── ConfigurationHelper.cs (loading utilities)
├── appsettings.template.json (template for reference)
└── README.md (this file)
```

## Design Philosophy

**This project is the single source of truth for ALL configuration:**

- ✅ **Configuration Classes** - Define structure and validation rules
- ✅ **Configuration Files** - `appsettings.json` with actual values
- ✅ **Helper Methods** - Load, bind, and discover configuration
- ✅ **Automatic Deployment** - Config files marked as `Content` are auto-copied to all consuming projects

**How it works:**

1. **Development**: Edit `Sacks.Configuration/appsettings.json` once
2. **Build**: MSBuild automatically copies it to every project that references `Sacks.Configuration`
3. **Runtime**: `ConfigurationHelper.FindConfigurationRoot()` finds it in the app's directory
4. **Deployment**: The file is already in each app's output folder, ready to deploy

**Benefits:**
1. **Single Source** - Edit config in ONE place (Sacks.Configuration project)
2. **Automatic Distribution** - MSBuild copies to all consuming projects automatically
3. **Production Ready** - Each app has its own copy for independent deployment
4. **No Manual Copying** - No project-specific configuration needed
5. **Environment Overrides** - Add `appsettings.Production.json` in same folder

## All Projects Use This

- **SacksApp** (WinForms)
- **SacksMcp.Console** (MCP Server)
- **SacksLogicLayer** (Business logic)
- **SacksDataLayer** (Data access)
- Any future projects

## Usage

### 1. Add Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="..\Sacks.Configuration\Sacks.Configuration.csproj" />
</ItemGroup>
```

### 2. Build Configuration in Your Application

```csharp
using Sacks.Configuration;

// Automatically finds solution root and loads appsettings.json
var configuration = ConfigurationHelper.BuildConfiguration();

// Or specify explicit path and environment
var configuration = ConfigurationHelper.BuildConfiguration(
    solutionRootPath: @"C:\MyProjects\Sacks-New",
    environmentName: "Development" // loads appsettings.Development.json if exists
);
```

### 3. Access Strongly-Typed Configuration

```csharp
// Get entire Sacks configuration
var sacksConfig = configuration.GetOptions<SacksConfigurationOptions>("Sacks");

// Or get individual sections
var dbOptions = configuration.GetOptions<DatabaseOptions>("Sacks:Database");
var mcpOptions = configuration.GetOptions<McpServerOptions>("Sacks:McpServer");
var fileOptions = configuration.GetOptions<FileProcessingOptions>("Sacks:FileProcessing");
```

### 4. Use with DI (ASP.NET Core / Console Apps)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sacks.Configuration;

var services = new ServiceCollection();

// Register configuration
var configuration = ConfigurationHelper.BuildConfiguration();
services.AddSingleton<IConfiguration>(configuration);

// Bind specific options for injection
services.Configure<DatabaseOptions>(configuration.GetSection("Sacks:Database"));
services.Configure<McpServerOptions>(configuration.GetSection("Sacks:McpServer"));

// Now inject IOptions<DatabaseOptions> anywhere
public class MyService
{
    public MyService(IOptions<DatabaseOptions> dbOptions)
    {
        var connectionString = dbOptions.Value.ConnectionString;
    }
}
```

## Configuration Sections

### Database
```json
"Database": {
  "ConnectionString": "Server=...;Database=...;",
  "Provider": "SqlServer",
  "CommandTimeout": 30,
  "RetryOnFailure": true,
  "MaxRetryCount": 3,
  "EnableSensitiveDataLogging": false,
  "EnableDetailedErrors": false
}
```

### MCP Server (for SacksMcp.Console)
```json
"McpServer": {
  "ServerName": "SacksMcp",
  "Version": "1.0.0",
  "MaxConcurrentTools": 10,
  "ToolTimeoutSeconds": 30,
  "EnableDetailedLogging": false
}
```

### MCP Client (for SacksApp)
```json
"McpClient": {
  "ServerExecutablePath": "dotnet",
  "ServerArguments": "run --project SacksMcp.Console/SacksMcp.Console.csproj",
  "ServerWorkingDirectory": null,
  "ToolTimeoutSeconds": 30
}
```

### File Processing
```json
"FileProcessing": {
  "InputDirectory": "Inputs",
  "OutputDirectory": "../Outputs",
  "ArchiveDirectory": "../Archive",
  "MaxFileSizeBytes": 104857600,
  "SupportedExtensions": [ ".xlsx", ".xls", ".csv" ]
}
```

### Configuration Files
```json
"ConfigurationFiles": {
  "ConfigurationFolder": "Configuration",
  "MainFileName": "supplier-formats.json"
}
```

## Environment-Specific Configuration

Create `appsettings.Development.json` or `appsettings.Production.json` in the solution root:

```json
{
  "Sacks": {
    "Database": {
      "ConnectionString": "Server=prodserver;Database=SacksProd;...",
      "EnableSensitiveDataLogging": false
    }
  }
}
```

Then load with environment name:
```csharp
var config = ConfigurationHelper.BuildConfiguration(environmentName: "Production");
```

## Environment Variables Override

Set environment variables with `SACKS_` prefix to override any setting:

```bash
# Windows
$env:SACKS_Database__ConnectionString="Server=override;..."

# Linux/Mac
export SACKS_Database__ConnectionString="Server=override;..."
```

The double underscore (`__`) represents nesting in the configuration hierarchy.

## Benefits

✅ **Single Source of Truth**: One `appsettings.json` for the entire solution  
✅ **No Duplication**: Configuration defined once, used everywhere  
✅ **Type Safety**: Strongly-typed options with IntelliSense  
✅ **Validation**: DataAnnotations support for required fields  
✅ **Flexibility**: Environment-specific overrides and environment variables  
✅ **Automatic Discovery**: Finds solution root automatically  
✅ **Easy Migration**: Helper methods make switching from old config seamless  

## Migration from Old Configuration

Old projects had scattered config files:
- `SacksApp\Configuration\appsettings.json`
- `SacksMcp.Console\appsettings.json`
- Various `*Settings.cs` classes in different projects

Now: **Everything references `Sacks.Configuration` and the root `appsettings.json`**.
