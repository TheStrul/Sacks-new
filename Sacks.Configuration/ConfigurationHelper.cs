using System.Text.Json;

namespace Sacks.Configuration;

/// <summary>
/// Simple configuration loader that directly deserializes sacks-config.json into SacksConfigurationOptions.
/// No Microsoft.Extensions.Configuration complexity - just pure JSON to object.
/// </summary>
public static class ConfigurationLoader
{
    private const string ConfigFileName = "sacks-config.json";
    
    private static SacksConfigurationOptions? _instance;
    private static readonly object _lock = new();
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Gets the singleton configuration instance.
    /// Loads from sacks-config.json on first access.
    /// </summary>
    public static SacksConfigurationOptions Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= Load();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Loads configuration from sacks-config.json.
    /// Search order:
    /// 1. Current app directory (production deployment - file copied by MSBuild)
    /// 2. Solution root (development environment fallback)
    /// 
    /// Security: Sensitive values (e.g., ApiKey) can be overridden via environment variables.
    /// Environment variable format: SACKS__{SectionName}__{PropertyName}
    /// Example: SACKS__Llm__ApiKey for Llm.ApiKey
    /// </summary>
    public static SacksConfigurationOptions Load(string? explicitPath = null)
    {
        var configPath = explicitPath ?? FindConfigFile();

        try
        {
            var json = File.ReadAllText(configPath);
            var options = JsonSerializer.Deserialize<SacksConfigurationOptions>(json, JsonOptions);

            if (options == null)
            {
                throw new InvalidOperationException($"Failed to deserialize configuration from {configPath}");
            }

            // Apply environment variable overrides for sensitive values
            ApplyEnvironmentOverrides(options);

            return options;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to load configuration from {configPath}. Ensure sacks-config.json is valid JSON.", ex);
        }
    }

    /// <summary>
    /// Finds the sacks-config.json file.
    /// Checks app directory first (production), then solution root (development).
    /// </summary>
    private static string FindConfigFile()
    {
        var appDirectory = AppContext.BaseDirectory;
        var appConfigPath = Path.Combine(appDirectory, ConfigFileName);

        // Strategy 1: App directory (production - file auto-copied by MSBuild)
        if (File.Exists(appConfigPath))
        {
            return appConfigPath;
        }

        // Strategy 2: Solution root (development fallback)
        try
        {
            var solutionRoot = FindSolutionRoot();
            var solutionConfigPath = Path.Combine(solutionRoot, "Sacks.Configuration", ConfigFileName);
            if (File.Exists(solutionConfigPath))
            {
                return solutionConfigPath;
            }
        }
        catch
        {
            // Solution root not found - expected in production
        }

        throw new FileNotFoundException(
            $"Could not find {ConfigFileName}. Searched:\n" +
            $"1. Application directory: {appDirectory}\n" +
            $"2. Solution root Sacks.Configuration folder (development only)\n\n" +
            $"Ensure {ConfigFileName} is deployed with your application.");
    }

    /// <summary>
    /// Finds the solution root by searching upward for a .sln file.
    /// Used only during development.
    /// </summary>
    private static string FindSolutionRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory != null)
        {
            if (currentDirectory.GetFiles("*.sln").Any())
            {
                return currentDirectory.FullName;
            }
            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("Solution root not found (expected in production)");
    }

    /// <summary>
    /// Applies environment variable overrides to sensitive configuration values.
    /// Environment variable format: SACKS__{SectionName}__{PropertyName}
    /// Example: SACKS__Llm__ApiKey overrides Llm.ApiKey
    /// </summary>
    private static void ApplyEnvironmentOverrides(SacksConfigurationOptions options)
    {
        // Override LLM API Key from environment if provided
        var llmApiKey = Environment.GetEnvironmentVariable("SACKS__Llm__ApiKey");
        if (!string.IsNullOrWhiteSpace(llmApiKey))
        {
            options.Llm.ApiKey = llmApiKey;
        }

        // Override Database ConnectionString from environment if provided
        var dbConnectionString = Environment.GetEnvironmentVariable("SACKS__Database__ConnectionString");
        if (!string.IsNullOrWhiteSpace(dbConnectionString))
        {
            options.Database.ConnectionString = dbConnectionString;
        }
    }

    /// <summary>
    /// Resets the singleton instance. Useful for testing.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }
}
