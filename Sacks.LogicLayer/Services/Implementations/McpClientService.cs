using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sacks.Configuration;
using Sacks.Core.Services.Interfaces;

namespace Sacks.LogicLayer.Services.Implementations;

/// <summary>
/// MCP client implementation that communicates with the SacksMcp server via stdio.
/// Uses the MCP (Model Context Protocol) to send requests and receive responses.
/// </summary>
public class McpClientService : IMcpClientService, IDisposable
{
    private readonly ILogger<McpClientService> _logger;
    private readonly McpClientOptions _options;
    private Process? _mcpProcess;
    private StreamWriter? _processInput;
    private StreamReader? _processOutput;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public McpClientService(ILogger<McpClientService> logger, IOptions<McpClientOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name is required", nameof(toolName));
        }

        await EnsureServerStartedAsync(cancellationToken).ConfigureAwait(false);

        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments = parameters ?? new Dictionary<string, object>()
            }
        };

        return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query is required", nameof(query));
        }

        // For natural language queries, we'd typically use an AI model to interpret
        // and route to appropriate tools. For now, return a helpful message.
        _logger.LogInformation("Processing query: {Query}", query);
        
        return await Task.FromResult(
            "Natural language query processing requires integration with an AI model (OpenAI, Azure OpenAI, etc.). " +
            "Use ExecuteToolAsync to call specific tools directly."
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<ToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureServerStartedAsync(cancellationToken).ConfigureAwait(false);

        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "tools/list"
        };

        var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        
        // Parse response and extract tool information
        try
        {
            using var doc = JsonDocument.Parse(response);
            var tools = new List<ToolInfo>();
            
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("tools", out var toolsArray))
            {
                foreach (var tool in toolsArray.EnumerateArray())
                {
                    var toolInfo = new ToolInfo
                    {
                        Name = tool.GetProperty("name").GetString() ?? string.Empty,
                        Description = tool.TryGetProperty("description", out var desc) 
                            ? desc.GetString() ?? string.Empty 
                            : string.Empty
                    };

                    if (tool.TryGetProperty("inputSchema", out var schema) &&
                        schema.TryGetProperty("properties", out var props))
                    {
                        foreach (var prop in props.EnumerateObject())
                        {
                            var paramInfo = new ParameterInfo
                            {
                                Type = prop.Value.TryGetProperty("type", out var type) 
                                    ? type.GetString() ?? "string" 
                                    : "string",
                                Description = prop.Value.TryGetProperty("description", out var pDesc)
                                    ? pDesc.GetString() ?? string.Empty
                                    : string.Empty,
                                Required = schema.TryGetProperty("required", out var required) &&
                                          required.EnumerateArray().Any(r => r.GetString() == prop.Name)
                            };

                            toolInfo.Parameters[prop.Name] = paramInfo;
                        }
                    }

                    tools.Add(toolInfo);
                }
            }

            return tools;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse tools list response");
            return new List<ToolInfo>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsServerAvailableAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            return IsProcessRunning();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task StartServerAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (IsProcessRunning())
            {
                _logger.LogInformation("MCP server is already running");
                return;
            }

            _logger.LogInformation("Starting MCP server: {Path}", _options.ServerExecutablePath);

            // Resolve the working directory - use solution root if relative path provided
            var workingDir = _options.ServerWorkingDirectory;
            if (string.IsNullOrWhiteSpace(workingDir))
            {
                // Try to find solution root for proper relative path resolution
                workingDir = FindSolutionRootOrUseAppBase();
            }

            _logger.LogInformation("MCP server working directory: {WorkingDir}", workingDir);
            _logger.LogInformation("MCP server arguments: {Arguments}", _options.ServerArguments);
            _logger.LogInformation("Full command: {Executable} {Arguments} (in {WorkingDir})", 
                _options.ServerExecutablePath, _options.ServerArguments, workingDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.ServerExecutablePath,
                Arguments = _options.ServerArguments,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _mcpProcess = new Process { StartInfo = startInfo };
            
            // Log stderr for debugging
            _mcpProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogDebug("MCP Server stderr: {Message}", e.Data);
                }
            };

            try
            {
                _mcpProcess.Start();
                _mcpProcess.BeginErrorReadLine();

                _processInput = _mcpProcess.StandardInput;
                _processOutput = _mcpProcess.StandardOutput;

                _logger.LogInformation("MCP server started successfully (PID: {ProcessId})", _mcpProcess.Id);

                // Wait a bit for server to initialize
                await Task.Delay(1000).ConfigureAwait(false);

                // Verify server is actually responding before returning
                if (_mcpProcess.HasExited)
                {
                    var exitCode = _mcpProcess.ExitCode;
                    _logger.LogError("MCP server exited immediately with code {ExitCode}", exitCode);
                    throw new InvalidOperationException($"MCP server exited immediately with code {exitCode}");
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Failed to start MCP server process");
                throw new InvalidOperationException($"Failed to start MCP server: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MCP server");
            CleanupProcess();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Finds the solution root directory or falls back to AppContext.BaseDirectory
    /// </summary>
    private string FindSolutionRootOrUseAppBase()
    {
        try
        {
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            
            // Search upward for solution file (.sln)
            while (currentDirectory != null)
            {
                var solutionFile = currentDirectory.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    _logger.LogDebug("Found solution root at: {SolutionRoot}", currentDirectory.FullName);
                    return currentDirectory.FullName;
                }
                currentDirectory = currentDirectory.Parent;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching for solution root");
        }

        // Fallback to AppContext.BaseDirectory
        var fallback = AppContext.BaseDirectory;
        _logger.LogDebug("Using AppContext.BaseDirectory as fallback: {BaseDirectory}", fallback);
        return fallback;
    }

    /// <inheritdoc/>
    public async Task StopServerAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_mcpProcess != null && !_mcpProcess.HasExited)
            {
                _logger.LogInformation("Stopping MCP server (PID: {ProcessId})", _mcpProcess.Id);
                
                try
                {
                    _processInput?.Close();
                    _processOutput?.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error closing streams during shutdown");
                }

                if (!_mcpProcess.WaitForExit(5000))
                {
                    _logger.LogWarning("MCP server did not exit gracefully, killing process");
                    try
                    {
                        _mcpProcess.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error killing process");
                    }
                }

                _logger.LogInformation("MCP server stopped");
            }

            CleanupProcess();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureServerStartedAsync(CancellationToken cancellationToken)
    {
        if (!await IsServerAvailableAsync().ConfigureAwait(false))
        {
            await StartServerAsync().ConfigureAwait(false);
        }
    }

    private async Task<string> SendRequestAsync(object request, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Verify server is still running and streams are valid
            if (!IsProcessRunning() || _processInput == null || _processOutput == null)
            {
                _logger.LogWarning("Server process not running or streams are null. Attempting restart.");
                
                // Clean up and restart while still holding lock to prevent race conditions
                try
                {
                    CleanupProcess();
                    // Start with lock held - no other thread can interfere
                    if (IsProcessRunning())
                    {
                        _logger.LogInformation("MCP server already running after cleanup");
                        return await RetrySendRequestAsync(request, cancellationToken).ConfigureAwait(false);
                    }

                    // Release lock for the async operation, but reacquire afterward
                    _lock.Release();
                    try
                    {
                        await StartServerAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // Check again after restart
                    if (!IsProcessRunning() || _processInput == null || _processOutput == null)
                    {
                        throw new InvalidOperationException("MCP server failed to start properly");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restart MCP server");
                    throw;
                }
            }

            return await SendRequestInternalAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string> RetrySendRequestAsync(object request, CancellationToken cancellationToken)
    {
        // Try once more to send the request after restart
        if (!IsProcessRunning() || _processInput == null || _processOutput == null)
        {
            throw new InvalidOperationException("MCP server is still not available after restart");
        }

        return await SendRequestInternalAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SendRequestInternalAsync(object request, CancellationToken cancellationToken)
    {
        if (_processInput == null || _processOutput == null)
        {
            throw new InvalidOperationException("MCP server is not running");
        }

        var requestJson = JsonSerializer.Serialize(request);
        _logger.LogDebug("Sending MCP request: {Request}", requestJson);

        // Create a timeout for tool execution if not already set
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.ToolTimeoutSeconds > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ToolTimeoutSeconds));
        }

        try
        {
            await _processInput.WriteLineAsync(requestJson).ConfigureAwait(false);
            await _processInput.FlushAsync(cts.Token).ConfigureAwait(false);
        }
        catch (IOException ex) when (ex.Message.Contains("pipe", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Pipe closed while writing to MCP server. Server may have crashed.");
            CleanupProcess();
            throw new InvalidOperationException("MCP server pipe closed unexpectedly during write", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to MCP server was cancelled or timed out after {TimeoutSeconds}s", _options.ToolTimeoutSeconds);
            CleanupProcess();
            throw;
        }

        string? response;
        try
        {
            response = await _processOutput.ReadLineAsync(cts.Token).ConfigureAwait(false);
        }
        catch (IOException ex) when (ex.Message.Contains("pipe", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Pipe closed while reading from MCP server. Server may have crashed.");
            CleanupProcess();
            throw new InvalidOperationException("MCP server pipe closed unexpectedly during read", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Response from MCP server was cancelled or timed out after {TimeoutSeconds}s", _options.ToolTimeoutSeconds);
            CleanupProcess();
            throw;
        }
        
        if (string.IsNullOrEmpty(response))
        {
            _logger.LogWarning("Received empty response from MCP server");
            throw new InvalidOperationException("Received empty response from MCP server");
        }

        _logger.LogDebug("Received MCP response: {Response}", response);

        return response;
    }

    /// <summary>
    /// Checks if the process is running and responsive.
    /// </summary>
    private bool IsProcessRunning()
    {
        return _mcpProcess != null && !_mcpProcess.HasExited;
    }

    /// <summary>
    /// Cleans up process and stream resources.
    /// </summary>
    private void CleanupProcess()
    {
        try
        {
            if (_processInput != null)
            {
                try
                {
                    _processInput.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing process input");
                }
                _processInput = null;
            }

            if (_processOutput != null)
            {
                try
                {
                    _processOutput.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing process output");
                }
                _processOutput = null;
            }

            if (_mcpProcess != null)
            {
                try
                {
                    if (!_mcpProcess.HasExited)
                    {
                        _mcpProcess.Kill(entireProcessTree: true);
                    }
                    _mcpProcess.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing process");
                }
                _mcpProcess = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unexpected error during process cleanup");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _lock.Wait();
            try
            {
                CleanupProcess();
                _lock.Dispose();
                _disposed = true;
            }
            finally
            {
                if (!_disposed)
                {
                    _lock.Release();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error during disposal");
        }
    }
}
