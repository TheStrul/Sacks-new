using FluentAssertions;
using SacksDataLayer.Parsing;
using Xunit;

namespace SacksDataLayer.Tests.Parsing;

/// <summary>
/// Tests for the centralized Transformer class.
/// </summary>
public class TransformerTests : BaseTest
{
    [Fact]
    public void ApplyTransformations_RemoveSymbols_ShouldRemoveNonNumericCharacters()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "Price: $129.90";
        var transformations = new List<string> { "removesymbols" };

        // Act
        var result = transformer.ApplyTransformations(value, transformations, "Price");

        // Assert
        result.Should().Be("129.90");
    }

    [Fact]
    public void ApplyTransformations_Capitalize_ShouldCapitalizeFirstLetterOnly()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "test value";
        var transformations = new List<string> { "capitalize" };

        // Act
        var result = transformer.ApplyTransformations(value, transformations, "Test");

        // Assert
        result.Should().Be("Test value");
    }

    [Fact]
    public void ExtractPriceAndCurrency_ValidPrice_ShouldExtractBothValues()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "$129.90 USD";

        // Act
        var result = transformer.ExtractPriceAndCurrency(value);

        // Assert
        result.Price.Should().Be("129.90");
        result.Currency.Should().Be("$");
    }

    [Fact]
    public void ExtractSizeAndUnits_ValidSize_ShouldExtractBothValues()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "100 mL bottle";

        // Act
        var result = transformer.ExtractSizeAndUnits(value);

        // Assert
        result.Size.Should().Be("100");
        result.Unit.Should().Be("ml");
    }

    [Fact]
    public void NormalizeDecimal_CommaAsDecimalSeparator_ShouldNormalizeToDot()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "129,90";

        // Act
        var result = transformer.NormalizeDecimal(value);

        // Assert
        result.Should().Be("129.90");
    }

    [Fact]
    public void NormalizeUnit_VariousUnits_ShouldNormalizeToStandardForm()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());

        // Act & Assert
        transformer.NormalizeUnit("ml").Should().Be("ml");
        transformer.NormalizeUnit("litre").Should().Be("l");
        transformer.NormalizeUnit("fl oz").Should().Be("fl oz");
        transformer.NormalizeUnit("g").Should().Be("g");
    }

    [Fact]
    public void UpperWords_ValidInput_ShouldExtractUppercaseWords()
    {
        // Arrange
        var input = "DOLCE & GABBANA One Man Intense";

        // Act
        var result = Transformer.ExtractUpperWordsFromStart(input);

        // Assert - with the new implementation, it correctly extracts "DOLCE & GABBANA" 
        // because GABBANA is uppercase and & is valid between two uppercase tokens
        result.Should().Be("DOLCE & GABBANA");
    }

    [Fact]
    public void Test_ExtractUpperWords()
    {
        // Test cases as specified by the user requirements
        
        // "ACME & Co. Deluxe 100ml" => "ACME"
        var result1 = Transformer.ExtractUpperWordsFromStart("ACME & Co. Deluxe 100ml");
        result1.Should().Be("ACME");
        
        // "ACME & CO. Deluxe 100ml" => "ACME & CO."
        var result2 = Transformer.ExtractUpperWordsFromStart("ACME & CO. Deluxe 100ml");
        result2.Should().Be("ACME & CO.");
        
        // "ACME & Co. Ltd." => "ACME"
        var result3 = Transformer.ExtractUpperWordsFromStart("ACME & Co. Ltd.");
        result3.Should().Be("ACME");
        
        // "Hello TO YOU" => ""
        var result4 = Transformer.ExtractUpperWordsFromStart("Hello TO YOU");
        result4.Should().Be("");
        
        // "A & B C" => "A & B C"
        var result5 = Transformer.ExtractUpperWordsFromStart("A & B C");
        result5.Should().Be("A & B C");
        
        // "A & B C D &" => "A & B C D"
        var result6 = Transformer.ExtractUpperWordsFromStart("A & B C D &");
        result6.Should().Be("A & B C D");

        // "A & B C D &" => "A & B C D"
        var result7 = Transformer.ExtractUpperWordsFromStart("A&B Good");
        result7.Should().Be("A&B");

    }

    [Fact]
    public void ApplyTransformationsWithExtraction_ExtractSizeAndUnits_ShouldExtractExtraProperties()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "Perfume 100ml bottle";
        var transformations = new List<string> { "extractsizeandunits" };

        // Act
        var result = transformer.ApplyTransformationsWithExtraction(value, transformations, "Size");

        // Assert
        result.TransformedValue.Should().Be("100");
        result.ExtraProperties.Should().NotBeNull();
        result.ExtraProperties!["Size"].Should().Be("100");
        result.ExtraProperties["Units"].Should().Be("ml");
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("TEST", "test")]
    [InlineData("Test & Value", "test value")]
    [InlineData("Test,Value", "test value")]
    public void NormalizeForMapping_VariousInputs_ShouldNormalizeConsistently(string input, string expected)
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());

        // Act
        var result = transformer.NormalizeForMapping(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Test_ExtractTitleFromStart()
    {
        // Test cases as specified in the method comments
        
        // "ACME & Co. Deluxe 100ml" => ""
        var result1 = Transformer.ExtractTitleFromStart("ACME & Co. Deluxe 100ml");
        result1.Should().Be("");
        
        // "Acme & CO. Deluxe 100ml" => "Acme"
        var result2 = Transformer.ExtractTitleFromStart("Acme & CO. Deluxe 100ml");
        result2.Should().Be("Acme");
        
        // "Acme & Co. Ltd." => "Acme & Co. Ltd."
        var result3 = Transformer.ExtractTitleFromStart("Acme & Co. Ltd.");
        result3.Should().Be("Acme & Co. Ltd.");
        
        // "Hello TO YOU" => "Hello"
        var result4 = Transformer.ExtractTitleFromStart("Hello TO YOU");
        result4.Should().Be("Hello");
        
        // "A & B C" => ""
        var result5 = Transformer.ExtractTitleFromStart("A & B C");
        result5.Should().Be("");
        
        // "Ab & B . Dolch 22 To Us ." => "Ab & B . Dolch 22 To Us ."
        var result6 = Transformer.ExtractTitleFromStart("Ab & B . Dolch 22 To Us .");
        result6.Should().Be("Ab & B . Dolch 22 To Us .");
        
        // "A&B Good" => ""
        var result7 = Transformer.ExtractTitleFromStart("A&B Good");
        result7.Should().Be("");
    }

    [Fact]
    public void ApplyTransformations_ExtractTitleFromStart_ShouldExtractTitleCaseText()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());
        var value = "Acme & Co. Ltd.";
        var transformations = new List<string> { "extracttitlefromstart" };

        // Act
        var result = transformer.ApplyTransformations(value, transformations, "Title");

        // Assert
        result.Should().Be("Acme & Co. Ltd.");
    }

    [Fact]
    public void RemoveSymbols_WithSymbolsAndDigits_ShouldKeepOnlyDigitsAndDecimalMarkers()
    {
        // Arrange
        var input = "Price: $129.90 USD";

        // Act
        var result = Transformer.RemoveSymbols(input);

        // Assert
        result.Should().Be("129.90");
    }

    [Fact]
    public void RemoveSpaces_WithSpaces_ShouldRemoveAllSpaces()
    {
        // Arrange
        var input = "Test With Spaces";

        // Act
        var result = Transformer.RemoveSpaces(input);

        // Assert
        result.Should().Be("TestWithSpaces");
    }

    [Fact]
    public void MapValue_WithValidMapping_ShouldReturnMappedValue()
    {
        // Arrange
        var valueMappings = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Brand", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "acme corp", "ACME Corporation" },
                    { "test value", "Mapped Test" }
                }
            }
        };
        var transformer = new Transformer(GetLogger<Transformer>(), valueMappings);

        // Act
        var result = transformer.MapValue("ACME Corp", "Brand");

        // Assert
        result.Should().Be("ACME Corporation");
    }

    [Fact]
    public void MapValue_WithoutMapping_ShouldReturnOriginalValue()
    {
        // Arrange
        var transformer = new Transformer(GetLogger<Transformer>());

        // Act
        var result = transformer.MapValue("Unknown Value", "Brand");

        // Assert
        result.Should().Be("Unknown Value");
    }
}