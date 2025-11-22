using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Client.Configuration;

namespace McpServer.Client;

/// <summary>
/// HTTP-based MCP client implementation that communicates with MCP servers via HTTP.
/// Uses JSON-RPC 2.0 protocol over HTTP for tool execution.
/// </summary>
public class HttpMcpClient : IMcpClient, IDisposable
{
    private readonly ILogger<HttpMcpClient> _logger;
    private readonly McpClientOptions _options;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public HttpMcpClient(ILogger<HttpMcpClient> logger, IOptions<McpClientOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_options.ToolTimeoutSeconds)
        };
    }

    /// <inheritdoc/>
    public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name is required", nameof(toolName));
        }

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
    public async Task<List<McpToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
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
            var tools = new List<McpToolInfo>();
            
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("tools", out var toolsArray))
            {
                foreach (var tool in toolsArray.EnumerateArray())
                {
                    var toolInfo = new McpToolInfo
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
                            var paramInfo = new McpParameterInfo
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
            return new List<McpToolInfo>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString(),
                method = "tools/list"
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _ = await SendRequestAsync(request, cts.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> SendRequestAsync(object request, CancellationToken cancellationToken)
    {
        var requestJson = JsonSerializer.Serialize(request);
        _logger.LogDebug("Sending HTTP MCP request to {Url}: {Request}", _options.ServerUrl, requestJson);

        // Implement retry logic
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;
            try
            {
                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(_options.ServerUrl, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogWarning("HTTP MCP request failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorBody);
                    throw new HttpRequestException($"MCP server returned {response.StatusCode}: {errorBody}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Received HTTP MCP response: {Response}", responseJson);

                return responseJson;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP MCP request failed (attempt {Attempt}/{MaxAttempts})", 
                    attempt, _options.MaxRetryAttempts);

                if (attempt < _options.MaxRetryAttempts)
                {
                    await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
            {
                // Timeout (not user cancellation)
                lastException = ex;
                _logger.LogWarning(ex, "HTTP MCP request timed out (attempt {Attempt}/{MaxAttempts})", 
                    attempt, _options.MaxRetryAttempts);

                if (attempt < _options.MaxRetryAttempts)
                {
                    await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // All retries failed
        throw new InvalidOperationException(
            $"MCP server at {_options.ServerUrl} is not available after {_options.MaxRetryAttempts} attempts. " +
            "Ensure the MCP server is running.", lastException);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error during disposal");
        }
    }
}
