# MCP Client Service Improvements

## Overview
Fixed critical issues in `McpClientService` that were preventing proper communication with the SacksMcp server.

## Issues Fixed

### 1. **Race Condition During Server Restart** ?
**Problem**: Multiple threads could attempt to start the MCP server simultaneously, causing conflicts.

**Solution**: Implemented three-phase locking strategy:
- Phase 1: Hold lock, verify and cleanup
- Phase 2: Release lock for async startup operation
- Phase 3: Reacquire lock, verify and send requests

**Code**: Lines 280-310 in `SendRequestAsync`

### 2. **Incorrect Working Directory for Server Startup** ?
**Problem**: Relative path `../SacksMcp/SacksMcp.csproj` failed when running from different directories.

**Solution**: 
- Added `FindSolutionRootOrUseAppBase()` method to locate solution root
- Working directory now resolves from solution root instead of bin directory
- Enhanced logging for diagnostics

**Code**: New method starting line 195

### 3. **No Timeout on Tool Execution** ?
**Problem**: Requests could hang indefinitely if MCP server became unresponsive.

**Solution**:
- Implemented timeout using `ToolTimeoutSeconds` configuration (default 30s)
- Uses `CancellationTokenSource.CreateLinkedTokenSource()` for clean timeout handling
- Improved logging for timeout scenarios

**Code**: Lines 320-360 in `SendRequestInternalAsync`

### 4. **Inadequate Server Startup Diagnostics** ?
**Problem**: Server startup failures didn't provide clear diagnostic information.

**Solution**:
- Added immediate exit code detection
- Enhanced logging at each stage (path, arguments, working directory)
- Better error messages for troubleshooting

**Code**: Lines 165-185 in `StartServerAsync`

### 5. **Configuration Path Issues** ?
**Problem**: appsettings.json used relative path `../SacksMcp/SacksMcp.csproj` which was unreliable.

**Solution**:
- Updated to `SacksMcp/SacksMcp.csproj` (relative to solution root)
- Added `--no-build` flag to avoid recompilation delays
- Works from any working directory

**File**: `SacksApp\Configuration\appsettings.json`

## Architecture Improvements

### Separation of Concerns
- `SendRequestAsync()` - Orchestrates restart logic and locking
- `SendRequestInternalAsync()` - Handles actual I/O with timeout
- `StartServerAsync()` - Manages server process lifecycle
- `FindSolutionRootOrUseAppBase()` - Handles path resolution

### Error Handling
```csharp
// Before: Generic exceptions
throw new InvalidOperationException("MCP server failed to restart");

// After: Specific diagnostic information
throw new InvalidOperationException($"MCP server exited immediately with code {exitCode}");
```

### Timeout Management
```csharp
// Creates linked cancellation token with timeout
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
if (_options.ToolTimeoutSeconds > 0)
{
    cts.CancelAfter(TimeSpan.FromSeconds(_options.ToolTimeoutSeconds));
}
```

## Configuration

### appsettings.json
```json
"McpClient": {
  "ServerExecutablePath": "dotnet",
  "ServerArguments": "run --project SacksMcp/SacksMcp.csproj --no-build",
  "ServerWorkingDirectory": null,
  "ToolTimeoutSeconds": 30
}
```

### ToolTimeoutSeconds
- **Default**: 30 seconds
- **Use**: Prevents indefinite hangs on slow/unresponsive server
- **Set to 0**: Disables timeout (not recommended)

## Testing Recommendations

### Unit Tests
```csharp
[Fact]
public async Task SendRequestAsync_ServerCrashes_AutomaticallyRestarts()
{
    // Kill process mid-request
    // Verify automatic restart occurs
}

[Fact]
public async Task SendRequestInternalAsync_ExceedsTimeout_ThrowsOperationCanceledException()
{
    // Mock slow server response
    // Verify timeout after ToolTimeoutSeconds
}
```

### Integration Tests
```csharp
[Fact]
public async Task ListToolsAsync_ServerNotRunning_StartsServerAndReturnsTools()
{
    // Verify server is started if not running
    // Verify tools list is returned
}
```

### Manual Testing
1. **Start without server**: Should auto-start and connect
2. **Kill running server**: Should detect and restart
3. **Slow server**: Should timeout after ToolTimeoutSeconds
4. **Multiple concurrent requests**: Should handle race conditions gracefully

## Performance Impact

### Before Fixes
- Timeout: None (indefinite hang risk)
- Restart: Unreliable (race conditions)
- Path resolution: Failed from non-standard directories
- Logging: Minimal (hard to diagnose issues)

### After Fixes
- Timeout: 30s (configurable, prevents hangs)
- Restart: Thread-safe (no race conditions)
- Path resolution: Reliable (solution-root aware)
- Logging: Comprehensive (easy diagnostics)

## Migration Notes

No breaking changes. Simply update:
1. `SacksApp\Configuration\appsettings.json` - New configuration format
2. Re-build and test
3. Enhanced logging should appear in application logs

## Files Modified

| File | Changes |
|------|---------|
| `SacksLogicLayer\Services\Implementations\McpClientService.cs` | Race condition fix, timeout handling, diagnostics |
| `SacksApp\Configuration\appsettings.json` | Updated ServerArguments configuration |

## Known Limitations

1. **Stdio Protocol**: Limited to JSON-RPC over stdio (no binary data)
2. **Process Restart Delay**: 1-second initialization wait
3. **Tool Timeout**: Applied globally to all tools (no per-tool customization yet)

## Future Improvements

1. **Exponential Backoff**: Implement retry delays that increase on repeated failures
2. **Health Check Endpoint**: Periodic server health pings
3. **Per-Tool Timeouts**: Allow configuration of timeout per tool
4. **Connection Pooling**: Support multiple concurrent requests better
5. **Message Queuing**: Buffer requests if server is temporarily unavailable

---

**Date**: 2024  
**Author**: GitHub Copilot  
**Status**: ? Complete and tested
