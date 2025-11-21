namespace McpServer.Database.Configuration;

/// <summary>
/// Database connection configuration. Supports multiple database types and connection strategies.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// The section name in appsettings.json where these options are stored.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Database connection string. Can include integrated security or SQL authentication.
    /// Example: "Server=localhost;Database=MyDb;Integrated Security=true;"
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database provider type (SqlServer, PostgreSQL, MySQL, etc.)
    /// Default: "SqlServer"
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Command timeout in seconds for database operations.
    /// Default: 30 seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Whether to enable sensitive data logging (passwords, connection strings).
    /// WARNING: Only enable in development!
    /// Default: false
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable detailed query logging for debugging.
    /// Default: false
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Maximum retry attempts for transient database failures.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
