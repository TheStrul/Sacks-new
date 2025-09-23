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
                if (!string.IsNullOrEmpty(configFiles.SupplierFormats))
                    configBuilder.AddJsonFile(configFiles.SupplierFormats, optional: true, reloadOnChange: false);
            }

            return configBuilder.Build();
        }
    }
}
