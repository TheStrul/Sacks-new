using System.ComponentModel.DataAnnotations;

namespace Sacks.Configuration;

/// <summary>
/// Root configuration options for all Sacks applications.
/// </summary>
public class SacksConfigurationOptions
{
    public const string SectionName = "Sacks";

    public DatabaseOptions Database { get; set; } = new();
    public FileProcessingOptions FileProcessing { get; set; } = new();
    public OpenBeautyFactsOptions OpenBeautyFacts { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public McpServerOptions McpServer { get; set; } = new();
    public McpClientOptions McpClient { get; set; } = new();
    public LlmOptions Llm { get; set; } = new();
    public ConfigurationFilesOptions ConfigurationFiles { get; set; } = new();
    public UIOptions UI { get; set; } = new();
}

/// <summary>
/// Database connection and behavior configuration.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database connection string. Can include integrated security or SQL authentication.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database provider type (SqlServer, PostgreSQL, MySQL, etc.)
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Command timeout in seconds for database operations.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Enable automatic retry on transient failures.
    /// </summary>
    public bool RetryOnFailure { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts for transient database failures.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Whether to enable sensitive data logging (passwords, connection strings).
    /// WARNING: Only enable in development!
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable detailed query logging for debugging.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
}

/// <summary>
/// File processing configuration for CSV/Excel import operations.
/// </summary>
public class FileProcessingOptions
{
    public const string SectionName = "FileProcessing";

    public string InputDirectory { get; set; } = "Inputs";
    public string OutputDirectory { get; set; } = "../Outputs";
    public string ArchiveDirectory { get; set; } = "../Archive";
    public long MaxFileSizeBytes { get; set; } = 104857600; // 100 MB
    public string[] SupportedExtensions { get; set; } = new[] { ".xlsx", ".xls", ".csv" };
}

/// <summary>
/// Open Beauty Facts import configuration.
/// </summary>
public class OpenBeautyFactsOptions
{
    public const string SectionName = "OpenBeautyFacts";

    public string InputDirectory { get; set; } = "AllInputs/db";
    public int BatchSize { get; set; } = 100;
    public bool EnableProgressLogging { get; set; } = true;
    public string[] SupportedFileExtensions { get; set; } = new[] { ".jsonl" };
    public long MaxFileSizeBytes { get; set; } = 524288000; // 500 MB
}

/// <summary>
/// Logging behavior configuration.
/// </summary>
public class LoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>
    /// Whether to delete existing log files on application startup.
    /// </summary>
    public bool DeleteLogFilesOnStartup { get; set; } = false;

    /// <summary>
    /// Paths to log directories to clean up on startup (relative to solution root).
    /// </summary>
    public string[] LogFilePaths { get; set; } = Array.Empty<string>();
}

/// <summary>
/// MCP Server configuration (for SacksMcp.Console and other MCP hosts).
/// </summary>
public class McpServerOptions
{
    public const string SectionName = "McpServer";

    public string ServerName { get; set; } = "SacksMcp";
    public string Version { get; set; } = "1.0.0";
    public int HttpPort { get; set; } = 5100;
    public bool EnableHttps { get; set; } = false;
    public int MaxConcurrentTools { get; set; } = 10;
    public int ToolTimeoutSeconds { get; set; } = 30;
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// MCP Client configuration (for applications that connect to MCP servers).
/// </summary>
public class McpClientOptions
{
    public const string SectionName = "McpClient";

    public string ServerUrl { get; set; } = "http://localhost:5100";
    public int ToolTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;
}

/// <summary>
/// Configuration file paths for supplier formats and other external configs.
/// </summary>
public class ConfigurationFilesOptions
{
    public const string SectionName = "ConfigurationFiles";

    /// <summary>
    /// Folder name (relative to base directory) where configuration files reside.
    /// </summary>
    [Required]
    public string ConfigurationFolder { get; set; } = "Configuration";

    /// <summary>
    /// Mandatory main file name inside the configuration folder.
    /// </summary>
    [Required]
    public string MainFileName { get; set; } = "supplier-formats.json";
}

/// <summary>
/// LLM (Large Language Model) configuration alias for McpServer.Client.Configuration.LlmOptions.
/// This is just a Sacks-specific wrapper with section name - the actual generic options are in McpServer.Client.
/// </summary>
public class LlmOptions : McpServer.Client.Configuration.LlmOptions
{
    public const string SectionName = "Llm";
}

/// <summary>
/// UI-specific settings for WinForms applications.
/// </summary>
public class UIOptions
{
    public const string SectionName = "UI";

    public bool RestoreWindowPositions { get; set; } = true;
}
