using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SacksDataLayer.Configuration;
using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using System.Text.Json;
using Xunit;

namespace SacksDataLayer.Tests.Configuration;

public class RuleBasedOfferNormalizerTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Constructor_WithValidSupplierConfiguration_ShouldCreateInstance()
    {
        // Arrange
        var supplierConfig = CreateTestSupplierConfiguration();
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;

        // Act
        var normalizer = new RuleBasedOfferNormalizer(supplierConfig, logger);

        // Assert
        Assert.NotNull(normalizer);
        Assert.Equal("HAND", normalizer.SupplierName);
    }

    [Fact]
    public void Constructor_WithNullParserConfig_ShouldThrowArgumentException()
    {
        // Arrange
        var supplierConfig = new SupplierConfiguration
        {
            Name = "TEST",
            ParserConfig = null
        };
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new RuleBasedOfferNormalizer(supplierConfig, logger));
        
        Assert.Contains("SupplierConfiguration must have a ParserConfig", exception.Message);
    }

    [Fact]
    public void CanHandle_WithMatchingFileName_ShouldReturnTrue()
    {
        // Arrange
        var supplierConfig = CreateTestSupplierConfiguration();
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;
        var normalizer = new RuleBasedOfferNormalizer(supplierConfig, logger);

        // Act
        var result = normalizer.CanHandle("hand_test.xlsx", Enumerable.Empty<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData>());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_WithNonMatchingFileName_ShouldReturnFalse()
    {
        // Arrange
        var supplierConfig = CreateTestSupplierConfiguration();
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;
        var normalizer = new RuleBasedOfferNormalizer(supplierConfig, logger);

        // Act
        var result = normalizer.CanHandle("other_supplier.xlsx", Enumerable.Empty<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData>());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadSupplierConfigurationFromJson_ShouldDeserializeCorrectly()
    {
        // Arrange
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
                        "rules": [
                            {
                                "id": "ref-direct",
                                "priority": 100,
                                "type": "Pipeline",
                                "steps": [
                                    {
                                        "op": "assign",
                                        "out": "Ref"
                                    }
                                ]
                            }
                        ]
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
        Assert.Equal("USD", supplierConfig.Currency);
        Assert.NotNull(supplierConfig.ParserConfig);
        Assert.Equal("en-US", supplierConfig.ParserConfig.Settings.DefaultCulture);
        Assert.True(supplierConfig.ParserConfig.Settings.PreferFirstAssignment);
        Assert.Single(supplierConfig.ParserConfig.Columns);
        Assert.Equal("B", supplierConfig.ParserConfig.Columns[0].Column);
        Assert.Single(supplierConfig.ParserConfig.Columns[0].Rules);
        Assert.Equal("Pipeline", supplierConfig.ParserConfig.Columns[0].Rules[0].Type);
    }

    private static SupplierConfiguration CreateTestSupplierConfiguration()
    {
        return new SupplierConfiguration
        {
            Name = "HAND",
            Currency = "USD",
            ParserConfig = new ParsingEngine.ParserConfig
            {
                Settings = new ParsingEngine.Settings
                {
                    DefaultCulture = "en-US",
                    PreferFirstAssignment = true
                },
                Columns = new List<ParsingEngine.ColumnConfig>
                {
                    new ParsingEngine.ColumnConfig
                    {
                        Column = "B",
                        Rules = new List<ParsingEngine.RuleConfig>
                        {
                            new ParsingEngine.RuleConfig
                            {
                                Id = "ref-direct",
                                Priority = 100,
                                Type = "Pipeline",
                                Steps = new List<ParsingEngine.PipelineStep>
                                {
                                    new ParsingEngine.PipelineStep
                                    {
                                        Op = "assign",
                                        Out = "Ref"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}