using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpServer.Transport.Http;

/// <summary>
/// HTTP-based transport layer for MCP server using ASP.NET Core minimal API.
/// Accepts JSON-RPC 2.0 requests over HTTP POST.
/// </summary>
public class HttpTransportLayer : ITransportLayer
{
    private readonly ILogger<HttpTransportLayer> _logger;
    private readonly HttpTransportOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private WebApplication? _app;
    private bool _isRunning;

    public HttpTransportLayer(
        ILogger<HttpTransportLayer> logger,
        IOptions<HttpTransportOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public bool IsRunning => _isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("HTTP transport is already running");
            return;
        }

        try
        {
            var builder = WebApplication.CreateBuilder();
            
            // Configure Kestrel to listen on the specified port
            var scheme = _options.EnableHttps ? "https" : "http";
            _logger.LogInformation("Configuring HTTP transport on {Scheme}://localhost:{Port}", scheme, _options.Port);
            
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                if (_options.EnableHttps)
                {
                    if (string.IsNullOrWhiteSpace(_options.CertificatePath))
                    {
                        throw new InvalidOperationException("CertificatePath is required when EnableHttps is true");
                    }
                    
                    options.ListenLocalhost(_options.Port, listenOptions =>
                    {
                        listenOptions.UseHttps(_options.CertificatePath, _options.CertificatePassword);
                    });
                }
                else
                {
                    options.ListenLocalhost(_options.Port);
                }
            });

            // Don't use the default services, we'll use the injected service provider
            _app = builder.Build();

            // Configure MCP endpoint
            _app.MapPost(_options.EndpointPath, async (HttpContext context) =>
            {
                await HandleMcpRequestAsync(context, cancellationToken);
            });

            _logger.LogInformation("Starting HTTP transport on {Scheme}://localhost:{Port}{Endpoint}",
                _options.EnableHttps ? "https" : "http",
                _options.Port,
                _options.EndpointPath);

            await _app.StartAsync(cancellationToken);
            _isRunning = true;

            _logger.LogInformation("HTTP transport started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start HTTP transport");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _app == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping HTTP transport");
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
            _app = null;
            _isRunning = false;
            _logger.LogInformation("HTTP transport stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping HTTP transport");
            throw;
        }
    }

    private async Task HandleMcpRequestAsync(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Read JSON-RPC request
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);
            
            if (_options.EnableConnectionLogging)
            {
                _logger.LogInformation("[HTTP MCP] Received request: {Request}", requestBody);
            }

            JsonDocument? requestDoc = null;
            try
            {
                requestDoc = JsonDocument.Parse(requestBody);
                var root = requestDoc.RootElement;

                // Validate JSON-RPC 2.0 structure
                if (!root.TryGetProperty("jsonrpc", out var jsonrpcProp) || jsonrpcProp.GetString() != "2.0")
                {
                    await SendErrorResponseAsync(context, null, -32600, "Invalid Request: jsonrpc must be '2.0'");
                    return;
                }

                if (!root.TryGetProperty("method", out var methodProp))
                {
                    await SendErrorResponseAsync(context, null, -32600, "Invalid Request: method is required");
                    return;
                }

                var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var method = methodProp.GetString();

                object? result = method switch
                {
                    "tools/list" => await HandleToolsListAsync(cancellationToken),
                    "tools/call" => await HandleToolsCallAsync(root, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown method: {method}")
                };

                await SendSuccessResponseAsync(context, id, result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in request");
                await SendErrorResponseAsync(context, null, -32700, "Parse error: Invalid JSON");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MCP request");
                await SendErrorResponseAsync(context, null, -32603, $"Internal error: {ex.Message}");
            }
            finally
            {
                requestDoc?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in HTTP MCP handler");
            context.Response.StatusCode = 500;
        }
    }

    private async Task<object> HandleToolsListAsync(CancellationToken cancellationToken)
    {
        // Find all tool types registered with [McpServerToolType] attribute
        var toolTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
            .ToList();

        var tools = new List<object>();

        foreach (var toolType in toolTypes)
        {
            var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

            foreach (var method in methods)
            {
                var descriptionAttr = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                var parameters = method.GetParameters()
                    .Where(p => p.ParameterType != typeof(CancellationToken))
                    .Select(p =>
                    {
                        var paramDesc = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                        return new
                        {
                            name = p.Name,
                            type = GetJsonSchemaType(p.ParameterType),
                            description = paramDesc?.Description ?? "",
                            required = !p.HasDefaultValue
                        };
                    })
                    .ToList();

                tools.Add(new
                {
                    name = method.Name,
                    description = descriptionAttr?.Description ?? "",
                    inputSchema = new
                    {
                        type = "object",
                        properties = parameters.ToDictionary(
                            p => p.name!,
                            p => new { type = p.type, description = p.description }
                        ),
                        required = parameters.Where(p => p.required).Select(p => p.name).ToArray()
                    }
                });
            }
        }

        return new { tools };
    }

    private async Task<object> HandleToolsCallAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("params", out var paramsElement))
        {
            throw new InvalidOperationException("params is required for tools/call");
        }

        if (!paramsElement.TryGetProperty("name", out var nameElement))
        {
            throw new InvalidOperationException("Tool name is required");
        }

        var toolName = nameElement.GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var argsElement)
            ? argsElement
            : default;

        // Find the tool method
        var toolTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null);

        foreach (var toolType in toolTypes)
        {
            var method = toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == toolName &&
                    m.GetCustomAttribute<McpServerToolAttribute>() != null);

            if (method != null)
            {
                // Resolve tool instance from DI
                var toolInstance = _serviceProvider.GetRequiredService(toolType);

                // Build parameter array
                var parameters = method.GetParameters();
                var args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    
                    if (param.ParameterType == typeof(CancellationToken))
                    {
                        args[i] = cancellationToken;
                    }
                    else if (arguments.ValueKind != JsonValueKind.Undefined &&
                             arguments.TryGetProperty(param.Name!, out var argValue))
                    {
                        args[i] = JsonSerializer.Deserialize(argValue.GetRawText(), param.ParameterType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Required parameter '{param.Name}' not provided");
                    }
                }

                // Invoke the method
                var result = method.Invoke(toolInstance, args);

                // Handle async results
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                    
                    // Get the result from Task<T>
                    var resultProperty = task.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }

                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = result?.ToString() ?? ""
                        }
                    }
                };
            }
        }

        throw new InvalidOperationException($"Tool not found: {toolName}");
    }

    private static async Task SendSuccessResponseAsync(HttpContext context, string? id, object? result)
    {
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            jsonrpc = "2.0",
            id,
            result
        });
    }

    private static async Task SendErrorResponseAsync(HttpContext context, string? id, int code, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200; // JSON-RPC errors still return 200 OK
        await JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            jsonrpc = "2.0",
            id,
            error = new
            {
                code,
                message
            }
        });
    }

    private static string GetJsonSchemaType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(double) || type == typeof(decimal) || type == typeof(float)) return "number";
        return "object";
    }
}
