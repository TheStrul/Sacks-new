using Microsoft.Extensions.Logging.Abstractions;
using SacksDataLayer.Configuration;
using SacksDataLayer.FileProcessing.Configuration;
using System.Text.Json;
using Xunit;

namespace SacksDataLayer.Tests.Configuration;

public class SupplierConfigurationIntegrationTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void LoadSupplierConfigurationFromFile_ShouldDeserializeCorrectly()
    {
        // Arrange
        var configPath = Path.Combine("..", "..", "..", "..", "..", "SacksApp", "Configuration", "supplier-formats.json");
        
        // Skip test if file doesn't exist (for CI/CD scenarios)
        if (!File.Exists(configPath))
        {
            return;
        }

        // Act
        var json = File.ReadAllText(configPath);
        var suppliersConfig = JsonSerializer.Deserialize<SuppliersConfiguration>(json, s_jsonOptions);

        // Assert
        Assert.NotNull(suppliersConfig);
        Assert.Equal("2.1", suppliersConfig.Version);
        Assert.NotEmpty(suppliersConfig.Suppliers);

        var handSupplier = suppliersConfig.Suppliers.FirstOrDefault(s => s.Name == "HAND");
        Assert.NotNull(handSupplier);
        Assert.Equal("USD", handSupplier.Currency);
        Assert.NotNull(handSupplier.ParserConfig);

        // Verify ParserConfig structure
        var parserConfig = handSupplier.ParserConfig;
        Assert.NotNull(parserConfig.Settings);
        Assert.Equal("en-US", parserConfig.Settings.DefaultCulture);
        Assert.True(parserConfig.Settings.PreferFirstAssignment);

        Assert.NotNull(parserConfig.Columns);
        Assert.NotEmpty(parserConfig.Columns);

        // Verify at least one column has rules
        var columnB = parserConfig.Columns.FirstOrDefault(c => c.Column == "B");
        Assert.NotNull(columnB);
        Assert.NotEmpty(columnB.Rules);

        var rule = columnB.Rules.First();
        Assert.Equal("Pipeline", rule.Type);
        Assert.Equal(100, rule.Priority);
    }

    [Fact]
    public void CreateRuleBasedOfferNormalizerFromConfigFile_ShouldWork()
    {
        // Arrange
        var configPath = Path.Combine("..", "..", "..", "..", "..", "SacksApp", "Configuration", "supplier-formats.json");
        
        // Skip test if file doesn't exist
        if (!File.Exists(configPath))
        {
            return;
        }

        var json = File.ReadAllText(configPath);
        var suppliersConfig = JsonSerializer.Deserialize<SuppliersConfiguration>(json, s_jsonOptions);
        var handSupplier = suppliersConfig!.Suppliers.First(s => s.Name == "HAND");

        var logger = NullLogger<RuleBasedOfferNormalizer>.Instance;

        // Act
        var normalizer = new RuleBasedOfferNormalizer(handSupplier, logger);

        // Assert
        Assert.NotNull(normalizer);
        Assert.Equal("HAND", normalizer.SupplierName);
        
        // Test that it can handle HAND files
        Assert.True(normalizer.CanHandle("hand_test.xlsx", Enumerable.Empty<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData>()));
        Assert.False(normalizer.CanHandle("other_test.xlsx", Enumerable.Empty<SacksAIPlatform.InfrastructuresLayer.FileProcessing.RowData>()));
    }
}