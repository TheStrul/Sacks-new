using Microsoft.Extensions.Configuration;

namespace SacksApp.Utils
{
    internal static class ConfigurationLoader
    {
        public static IConfiguration BuildConfiguration()
        {
            var environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            var basePath = System.AppContext.BaseDirectory;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();

            var baseConfig = configBuilder.Build();
            var configFiles = baseConfig.GetSection("ConfigurationFiles").Get<global::SacksDataLayer.Configuration.ConfigurationFileSettings>();

            if (configFiles != null)
            {
                var folder = configFiles.ConfigurationFolder;
                var main = configFiles.MainFileName;
                if (!string.IsNullOrWhiteSpace(folder) && !string.IsNullOrWhiteSpace(main))
                {
                    var relPath = System.IO.Path.Combine(folder, main).Replace("\\", "/");
                    configBuilder.AddJsonFile(relPath, optional: true, reloadOnChange: false);
                }
            }

            return configBuilder.Build();
        }
    }
}
