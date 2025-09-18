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
        var supplierConfig = CreateTestSupplierConfiguration();
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;
        var normalizer = new RuleBasedOfferNormalizer(supplierConfig, logger);
        Assert.NotNull(normalizer);
        Assert.Equal("HAND", normalizer.SupplierName);
    }

    [Fact]
    public void Constructor_WithNullParserConfig_ShouldThrowArgumentException()
    {
        var supplierConfig = new SupplierConfiguration { Name = "TEST", ParserConfig = null };
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;
        var ex = Assert.Throws<ArgumentException>(() => new RuleBasedOfferNormalizer(supplierConfig, logger));
        Assert.Contains("SupplierConfiguration must have a ParserConfig", ex.Message);
    }

    [Fact]
    public void CanHandle_WithMatchingFileName_ShouldReturnTrue()
    {
        var supplierConfig = CreateTestSupplierConfiguration();
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;
        var normalizer = new RuleBasedOfferNormalizer(supplierConfig, logger);
        var result = normalizer.CanHandle("hand_test.xlsx", Enumerable.Empty<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData>());
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_WithNonMatchingFileName_ShouldReturnFalse()
    {
        var supplierConfig = CreateTestSupplierConfiguration();
        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;
        var normalizer = new RuleBasedOfferNormalizer(supplierConfig, logger);
        var result = normalizer.CanHandle("other_supplier.xlsx", Enumerable.Empty<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData>());
        Assert.False(result);
    }

    [Fact]
    public void LoadSupplierConfigurationFromJson_ShouldDeserializeCorrectly()
    {
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

        var supplierConfig = JsonSerializer.Deserialize<SupplierConfiguration>(json, s_jsonOptions);
        Assert.NotNull(supplierConfig);
        Assert.Equal("HAND", supplierConfig!.Name);
        Assert.Equal("USD", supplierConfig.Currency);
        Assert.NotNull(supplierConfig.ParserConfig);
        Assert.Equal("en-US", supplierConfig.ParserConfig!.Settings.DefaultCulture);
        Assert.True(supplierConfig.ParserConfig.Settings.PreferFirstAssignment);
        Assert.Single(supplierConfig.ParserConfig.Columns);
        Assert.Equal("B", supplierConfig.ParserConfig.Columns[0].Column);
        Assert.NotNull(supplierConfig.ParserConfig.Columns[0].Rule);
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
            Rule = new ParsingEngine.RuleConfig
            {
              Steps = new List<ParsingEngine.PipelineStep>
              {
                new ParsingEngine.PipelineStep { Op = "assign", Out = "Ref" }
              }
            }
                    }
                }
            }
        };
    }
}