# MCP Client Service - Troubleshooting Guide

## Common Issues and Solutions

### Issue 1: "MCP server failed to start properly"

**Symptoms**:
- `System.InvalidOperationException: MCP server failed to start properly`
- Tools not loading on dashboard startup

**Root Causes**:
1. SacksMcp project path is incorrect
2. Working directory is not set properly
3. Server process exits immediately

**Solutions**:

? **Check 1: Verify Project Path**
```json
// In SacksApp\Configuration\appsettings.json
"McpClient": {
  "ServerExecutablePath": "dotnet",
  "ServerArguments": "run --project SacksMcp/SacksMcp.csproj --no-build",
  "ServerWorkingDirectory": null
}
```

? **Check 2: Verify SacksMcp Builds**
```bash
cd SacksMcp
dotnet build
dotnet run
```

? **Check 3: Enable Debug Logging**
```json
// In appsettings.json under Serilog
"MinimumLevel": {
  "Default": "Debug",  // Changed from Information
  "Override": { ... }
}
```

? **Check 4: View Logs**
- Logs location: `logs/sacks-YYYY-MM-DD.log`
- Search for "MCP server stderr" for server errors

### Issue 2: "Pipe closed while writing to MCP server"

**Symptoms**:
- `System.IO.IOException: The pipe is being closed`
- Error occurs during tool execution
- Server crashes intermittently

**Root Causes**:
1. Server process crashed or exited
2. Stdio streams got disconnected
3. Server resource exhaustion

**Solutions**:

? **Automatic Recovery**: Service should auto-restart the server
- Check logs for "Attempting restart"
- If not restarting, check StartServerAsync logs

? **Manual Recovery**: 
```csharp
var mcpClient = serviceProvider.GetRequiredService<IMcpClientService>();
await mcpClient.StopServerAsync();
await mcpClient.StartServerAsync();
```

? **Check Server Logs**:
```bash
# View real-time logs
Get-Content -Path logs/sacks-*.log -Tail 50 -Wait
```

### Issue 3: "Request to MCP server was cancelled or timed out"

**Symptoms**:
- `System.OperationCanceledException`
- Timeouts after 30 seconds
- Only happens on specific tools

**Root Causes**:
1. MCP server is slow responding
2. Tool execution is taking too long
3. Timeout is too aggressive

**Solutions**:

? **Increase Timeout**:
```json
"McpClient": {
  "ToolTimeoutSeconds": 60  // Increase from 30
}
```

? **Profile Tool Performance**:
- Check SacksMcp logs for tool execution times
- Optimize slow queries in tool code

? **Disable Timeout** (Debug Only):
```json
"McpClient": {
  "ToolTimeoutSeconds": 0  // Disables timeout
}
```

### Issue 4: "MCP server is still not available after restart"

**Symptoms**:
- Auto-restart attempted but failed
- Tools still unavailable
- Logs show multiple restart attempts

**Root Causes**:
1. Server keeps crashing immediately
2. Dependency injection configuration broken
3. Database connection failures

**Solutions**:

? **Test Server Directly**:
```bash
cd SacksMcp
dotnet run
# If it crashes, you'll see the error immediately
```

? **Check Dependencies**:
- Verify SacksDbContext configuration
- Verify database connection string
- Verify EntityFrameworkCore packages

? **Review Server Logs**:
```bash
# In SacksMcp\logs or logs directory
# Look for stack traces at startup
```

## Diagnostic Commands

### Check if SacksMcp runs standalone
```bash
cd C:\Users\avist\source\repos\GitHubLocal\Customers\Sacks-New
dotnet run --project SacksMcp/SacksMcp.csproj
```

### Check if SacksMcp can be discovered
```powershell
Get-ChildItem -Recurse -Filter "SacksMcp.csproj"
```

### View logs in real-time
```bash
Get-Content -Path logs/sacks-*.log -Tail 100 -Wait
```

### Kill stuck processes
```powershell
Get-Process -Name "dotnet" | Where-Object { $_.CommandLine -like "*SacksMcp*" } | Stop-Process -Force
```

## Logging Strategy

### Enable Full Debug Logging
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Key Log Messages to Watch

| Message | Meaning | Action |
|---------|---------|--------|
| `Starting MCP server: dotnet` | Server startup initiated | Normal |
| `MCP server started successfully (PID: ...)` | Server running | Good |
| `MCP server stderr: ...` | Server logged to stderr | Check message |
| `Pipe closed while writing` | Server crashed | Auto-restart triggered |
| `Request to MCP server was cancelled` | Timeout occurred | Increase ToolTimeoutSeconds |
| `MCP server failed to start properly` | Startup failed completely | Check configuration |

## Environment Variables

Set these if having path issues:

```powershell
# Set solution root explicitly
$env:SACKS_SOLUTION_ROOT = "C:\Users\avist\source\repos\GitHubLocal\Customers\Sacks-New"

# Enable verbose logging
$env:SERILOG_MINIMUM_LEVEL = "Debug"

# Increase timeout
$env:MCP_TOOL_TIMEOUT_SECONDS = "60"
```

## Performance Monitoring

### Monitor Process Memory
```powershell
# Check dotnet process memory
Get-Process dotnet | Select-Object ProcessName, WorkingSet, @{Name="MemoryMB"; Expression={$_.WorkingSet / 1MB -as [int]}}
```

### Monitor Network Activity
```powershell
# Check for stdio communication
Get-NetTCPConnection | Where-Object { $_.LocalPort -like "Stdinput" }
```

## Reset to Known Good State

### 1. Clean everything
```bash
cd SacksMcp
dotnet clean
cd ..
dotnet clean
```

### 2. Rebuild
```bash
dotnet build
```

### 3. Test server
```bash
cd SacksMcp
dotnet run
# Press Ctrl+C to stop
```

### 4. Test client
```bash
cd SacksApp
dotnet run
# Try loading tools from dashboard
```

## Support Information

When reporting issues, include:
1. **Error message** (full stack trace)
2. **Log file snippet** (relevant entries)
3. **Configuration** (appsettings.json relevant sections)
4. **Environment** (OS, .NET version, VS version)

---

**Last Updated**: 2024  
**Status**: ? Complete
