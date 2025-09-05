using Microsoft.Extensions.Logging;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Utility class for locating configuration files across multiple potential directories
    /// </summary>
    public static class ConfigurationFileLocator
    {
        /// <summary>
        /// Finds a configuration file by searching in multiple standard locations
        /// </summary>
        /// <param name="fileName">The configuration file name (e.g., "supplier-formats.json") or relative path (e.g., "Configuration/supplier-formats.json")</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>Full path to the configuration file, or null if not found</returns>
        public static string? FindConfigurationFile(string fileName, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            // Extract just the filename if a path was provided
            var actualFileName = Path.GetFileName(fileName);
            
            // Standard search locations for configuration files
            var searchPaths = new[]
            {
                // If a relative path was provided, try it as-is first
                fileName,
                
                // Current working directory
                actualFileName,
                
                // Configuration subdirectory of current directory
                Path.Combine("Configuration", actualFileName),
                
                // SacksConsoleApp Configuration directory (relative from root)
                Path.Combine("SacksConsoleApp", "Configuration", actualFileName),
                
                // SacksDataLayer Configuration directory (relative from root)
                Path.Combine("SacksDataLayer", "Configuration", actualFileName),
                
                // Parent directory configurations
                Path.Combine("..", "Configuration", actualFileName),
                Path.Combine("..", "SacksConsoleApp", "Configuration", actualFileName),
                Path.Combine("..", "SacksDataLayer", "Configuration", actualFileName),
                
                // Grandparent directory configurations
                Path.Combine("..", "..", "Configuration", actualFileName),
                Path.Combine("..", "..", "SacksConsoleApp", "Configuration", actualFileName),
                Path.Combine("..", "..", "SacksDataLayer", "Configuration", actualFileName)
            };

            logger?.LogDebug("Searching for configuration file: {FileName}", fileName);

            foreach (var searchPath in searchPaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(searchPath);
                    logger?.LogTrace("Checking path: {Path}", fullPath);
                    
                    if (File.Exists(fullPath))
                    {
                        logger?.LogInformation("Found configuration file at: {Path}", fullPath);
                        return fullPath;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogTrace("Error checking path {Path}: {Error}", searchPath, ex.Message);
                    // Continue searching other paths
                }
            }

            logger?.LogWarning("Configuration file not found: {FileName}. Searched {PathCount} locations.", fileName, searchPaths.Length);
            return null;
        }

        /// <summary>
        /// Finds a configuration file and throws an exception if not found
        /// </summary>
        /// <param name="fileName">The configuration file name</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>Full path to the configuration file</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file is not found</exception>
        public static string FindConfigurationFileOrThrow(string fileName, ILogger? logger = null)
        {
            var filePath = FindConfigurationFile(fileName, logger);
            if (filePath == null)
            {
                throw new FileNotFoundException($"Configuration file not found: {fileName}");
            }
            return filePath;
        }
    }
}
