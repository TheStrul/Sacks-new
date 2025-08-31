using System.Reflection;

namespace SacksConsoleApp
{
    /// <summary>
    /// Provides application version and deployment information
    /// </summary>
    public static class ApplicationInfo
    {
        /// <summary>
        /// Gets the application version from assembly
        /// </summary>
        public static string Version => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() 
            ?? "Unknown";

        /// <summary>
        /// Gets the build date from assembly
        /// </summary>
        public static DateTime BuildDate => GetBuildDate();

        /// <summary>
        /// Gets the last deployment timestamp from configuration or environment
        /// </summary>
        public static DateTime? LastDeployment => GetLastDeploymentDate();

        /// <summary>
        /// Gets deployment environment (Development, Staging, Production)
        /// </summary>
        public static string Environment => System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        /// <summary>
        /// Gets deployment target (Local, Azure, etc.)
        /// </summary>
        public static string DeploymentTarget => System.Environment.GetEnvironmentVariable("DEPLOYMENT_TARGET") ?? "Local";

        private static DateTime GetBuildDate()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var location = assembly.Location;
                if (File.Exists(location))
                {
                    return File.GetLastWriteTime(location);
                }
            }
            catch
            {
                // Fallback to current time if unable to determine
            }
            
            return DateTime.UtcNow;
        }

        private static DateTime? GetLastDeploymentDate()
        {
            // Try to get from environment variable (set during deployment)
            var deploymentDate = System.Environment.GetEnvironmentVariable("DEPLOYMENT_DATE");
            if (DateTime.TryParse(deploymentDate, out var date))
            {
                return date;
            }

            // Try to get from deployment marker file
            var deploymentFile = Path.Combine(AppContext.BaseDirectory, ".deployment-info");
            if (File.Exists(deploymentFile))
            {
                try
                {
                    var content = File.ReadAllText(deploymentFile);
                    if (DateTime.TryParse(content.Trim(), out var fileDate))
                    {
                        return fileDate;
                    }
                }
                catch
                {
                    // Ignore file read errors
                }
            }

            return null;
        }

        /// <summary>
        /// Records deployment timestamp
        /// </summary>
        public static void RecordDeployment()
        {
            try
            {
                var deploymentFile = Path.Combine(AppContext.BaseDirectory, ".deployment-info");
                File.WriteAllText(deploymentFile, DateTime.UtcNow.ToString("O"));
            }
            catch
            {
                // Ignore write errors
            }
        }

        /// <summary>
        /// Gets formatted deployment information
        /// </summary>
        public static string GetDeploymentInfo()
        {
            var info = new List<string>
            {
                $"Version: {Version}",
                $"Build Date: {BuildDate:yyyy-MM-dd HH:mm:ss} UTC",
                $"Environment: {Environment}",
                $"Target: {DeploymentTarget}"
            };

            if (LastDeployment.HasValue)
            {
                info.Add($"Last Deployment: {LastDeployment.Value:yyyy-MM-dd HH:mm:ss} UTC");
                info.Add($"Days Since Deployment: {(DateTime.UtcNow - LastDeployment.Value).TotalDays:F1}");
            }
            else
            {
                info.Add("Last Deployment: Not recorded");
            }

            return string.Join("\n", info);
        }
    }
}
