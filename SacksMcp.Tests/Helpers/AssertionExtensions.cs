using FluentAssertions;
using System.Text.Json;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Extension methods for common test assertions.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that a JSON string contains a specific property with an expected value.
    /// </summary>
    public static void ShouldContainJsonProperty(this string json, string propertyName, object expectedValue)
    {
        json.Should().NotBeNullOrEmpty();
        
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty(propertyName, out var property).Should().BeTrue(
            $"JSON should contain property '{propertyName}'");
        
        var actualValue = property.ValueKind switch
        {
            JsonValueKind.Number => (object)property.GetInt32(),
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => property.ToString()
        };
        
        actualValue.Should().Be(expectedValue);
    }

    /// <summary>
    /// Asserts that a JSON string represents a successful MCP tool response.
    /// </summary>
    public static void ShouldBeSuccessResponse(this string json)
    {
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\"error\":", "Response should not contain error");
    }

    /// <summary>
    /// Asserts that a JSON string represents an error MCP tool response.
    /// </summary>
    public static void ShouldBeErrorResponse(this string json)
    {
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"error\":", "Response should contain error");
    }

    /// <summary>
    /// Parses JSON string to JsonDocument for detailed assertions.
    /// </summary>
    public static JsonDocument ToJsonDocument(this string json)
    {
        return JsonDocument.Parse(json);
    }
}
