using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SacksDataLayer.FileProcessing.Configuration;

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace SacksDataLayer.Tests.Integration
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
            var logger = loggerFactory.CreateLogger<SacksDataLayer.Configuration.RuleBasedOfferNormalizer>();

            // Assert
            Assert.NotNull(logger);
        }

    }
}