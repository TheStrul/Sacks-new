using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SacksDataLayer.FileProcessing.Configuration;
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

        [Fact]
        public void SupplierConfigurationWithParserConfig_ShouldDeserializeFromJson()
        {
            // Arrange - This is the structure we expect to work with
            var json = """
            {
                "name": "HAND",
                "Currency": "USD",
                "detection": { "fileNamePatterns": [ "hand*.xls*" ] },
                "parserConfig": {
                    "settings": {
                        "stopOnFirstMatchPerColumn": false,
                        "defaultCulture": "en-US",
                        "preferFirstAssignment": true
                    },
                    "lookups": {},
                    "columns": [
                        {
                            "column": "B",
                            "rule": {
                                "steps": [
                                    { "op": "assign", "out": "Ref" }
                                ]
                            }
                        },
                        {
                            "column": "C",
                            "rule": {
                                "steps": [
                                    { "op": "assign", "out": "EAN" }
                                ]
                            }
                        }
                    ]
                },
                "fileStructure": {
                    "dataStartRowIndex": 2,
                    "expectedColumnCount": 7,
                    "headerRowIndex": 1
                }
            }
            """;

            // Act
            var supplierConfig = JsonSerializer.Deserialize<SupplierConfiguration>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(supplierConfig);
            Assert.Equal("HAND", supplierConfig.Name);
            Assert.NotNull(supplierConfig.ParserConfig);
            Assert.Equal(2, supplierConfig.ParserConfig.Columns.Count);
            
            // Verify the ParserConfig can be used to create a ParsingEngine
            var parsingEngine = new ParsingEngine.ParserEngine(supplierConfig.ParserConfig);
            Assert.NotNull(parsingEngine);
        }

        [Fact]
        public void RuleBasedOfferNormalizer_ShouldCreateWithValidConfiguration()
        {
            // Arrange
            var supplierConfig = new SupplierConfiguration
            {
                Name = "TEST",
                ParserConfig = new ParsingEngine.ParserConfig
                {
                    Settings = new ParsingEngine.Settings(),
                    Columns = new List<ParsingEngine.ColumnConfig>()
                }
            };

            var logger = NullLogger<SacksDataLayer.Configuration.RuleBasedOfferNormalizer>.Instance;

            // Act
            var normalizer = new SacksDataLayer.Configuration.RuleBasedOfferNormalizer(supplierConfig, logger);

            // Assert
            Assert.NotNull(normalizer);
            Assert.Equal("TEST", normalizer.SupplierName);
        }
    }
}