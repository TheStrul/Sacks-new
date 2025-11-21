using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sacks.Core.FileProcessing.Configuration;

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Sacks.Tests.Integration
{
    public class FileProcessingServiceIntegrationTests
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void LoggerFactory_ShouldCreateRuleBasedOfferNormalizerLogger()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            // Act
            var logger = loggerFactory.CreateLogger<Sacks.Core.Configuration.RuleBasedOfferNormalizer>();

            // Assert
            Assert.NotNull(logger);
        }

    }
}
