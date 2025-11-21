using Microsoft.Extensions.Configuration;

namespace Sacks.Configuration;

/// <summary>
/// Helper class for building configuration from the centralized appsettings.json.
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Builds an IConfiguration instance from appsettings.json.
    /// Search order:
    /// 1. Current app directory (production deployment)
    /// 2. Solution root (development environment)
    /// 3. Explicit path if provided
    /// </summary>
    /// <param name="solutionRootPath">Optional explicit path to config root. If null, will auto-discover.</param>
    /// <param name="environmentName">Optional environment name (Development, Production, etc.) for environment-specific config files.</param>
    /// <returns>A configured IConfiguration instance.</returns>
    public static IConfiguration BuildConfiguration(string? solutionRootPath = null, string? environmentName = null)
    {
        var configRoot = solutionRootPath ?? FindConfigurationRoot();
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(configRoot)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Add environment-specific configuration if specified
        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
        }

        // Add environment variables with SACKS_ prefix
        builder.AddEnvironmentVariables(prefix: "SACKS_");

        return builder.Build();
    }

    /// <summary>
    /// Finds the configuration root directory using a multi-strategy approach.
    /// Strategy 1: Check app's own directory (production deployment)
    /// Strategy 2: Search upward for solution root (development environment)
    /// </summary>
    /// <returns>The configuration root directory path.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no valid configuration location is found.</exception>
    public static string FindConfigurationRoot()
    {
        var appDirectory = AppContext.BaseDirectory;

        // Strategy 1: Check if appsettings.json exists in the app's own directory (production)
        if (File.Exists(Path.Combine(appDirectory, "appsettings.json")))
        {
            return appDirectory;
        }

        // Strategy 2: Try to find solution root (development)
        try
        {
            return FindSolutionRoot();
        }
        catch
        {
            // Solution root not found - this is expected in production
        }

        // If we get here, no config found anywhere
        throw new InvalidOperationException(
            $"Could not find appsettings.json. Searched in:\n" +
            $"1. Application directory: {appDirectory}\n" +
            $"2. Solution root (not found - this is normal in production)\n\n" +
            $"Ensure appsettings.json is deployed with your application.");
    }

    /// <summary>
    /// Finds the solution root by searching upward for a .sln file.
    /// This is primarily used during development.
    /// </summary>
    /// <returns>The solution root directory path.</returns>
    /// <exception cref="InvalidOperationException">Thrown if solution root cannot be found.</exception>
    public static string FindSolutionRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory != null)
        {
            var solutionFile = currentDirectory.GetFiles("*.sln").FirstOrDefault();
            if (solutionFile != null)
            {
                return currentDirectory.FullName;
            }
            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find solution root (.sln file) starting from {AppContext.BaseDirectory}. " +
            "This is expected in production deployments.");
    }

    /// <summary>
    /// Gets strongly-typed configuration options.
    /// </summary>
    public static T GetOptions<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        
        var options = new T();
        configuration.GetSection(sectionName).Bind(options);
        return options;
    }
}
