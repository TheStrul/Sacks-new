namespace McpServer.Transport;

/// <summary>
/// Base options for transport layer configuration.
/// </summary>
public abstract class TransportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the transport should log detailed connection information.
    /// </summary>
    public bool EnableConnectionLogging { get; set; } = true;
}

/// <summary>
/// Configuration options for HTTP transport.
/// </summary>
public class HttpTransportOptions : TransportOptions
{
    /// <summary>
    /// Gets or sets the port number the HTTP server listens on.
    /// </summary>
    public int Port { get; set; } = 5100;

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS is enabled.
    /// </summary>
    public bool EnableHttps { get; set; } = false;

    /// <summary>
    /// Gets or sets the path to the certificate file for HTTPS (required if EnableHttps is true).
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the certificate password for HTTPS.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Gets or sets the endpoint path for MCP requests.
    /// </summary>
    public string EndpointPath { get; set; } = "/mcp";
}
