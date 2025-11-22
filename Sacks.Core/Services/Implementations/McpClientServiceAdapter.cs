using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;

namespace Sacks.Core.Services.Implementations;

/// <summary>
/// Adapter that bridges the Sacks-specific IMcpClientService interface to the generic IMcpClient.
/// This allows legacy code to continue working while using the new generic MCP client.
/// </summary>
public class McpClientServiceAdapter : Interfaces.IMcpClientService
{
    private readonly IMcpClient _mcpClient;

    public McpClientServiceAdapter(IMcpClient mcpClient)
    {
        _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
    }

    public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        return await _mcpClient.ExecuteToolAsync(toolName, parameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        return await _mcpClient.QueryAsync(query, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<Interfaces.ToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = await _mcpClient.ListToolsAsync(cancellationToken).ConfigureAwait(false);
        
        // Convert from generic McpToolInfo to Sacks-specific ToolInfo
        return tools.Select(t => new Interfaces.ToolInfo
        {
            Name = t.Name,
            Description = t.Description,
            Parameters = t.Parameters.ToDictionary(
                p => p.Key,
                p => new Interfaces.ParameterInfo
                {
                    Type = p.Value.Type,
                    Description = p.Value.Description,
                    Required = p.Value.Required,
                    DefaultValue = p.Value.DefaultValue
                })
        }).ToList();
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        return await _mcpClient.IsServerAvailableAsync().ConfigureAwait(false);
    }

    public Task StartServerAsync()
    {
        // No-op: HTTP transport doesn't spawn servers
        return Task.CompletedTask;
    }

    public Task StopServerAsync()
    {
        // No-op: HTTP transport doesn't manage server lifecycle
        return Task.CompletedTask;
    }
}
