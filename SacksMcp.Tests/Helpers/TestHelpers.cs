using Microsoft.Extensions.Logging;
using Moq;
using SacksMcp.Services;

namespace SacksMcp.Tests.Helpers;

/// <summary>
/// Helper class for creating mock instances in tests
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a mock ConnectionTracker for testing
    /// </summary>
    public static ConnectionTracker CreateMockConnectionTracker()
    {
        var mockLogger = new Mock<ILogger<ConnectionTracker>>();
        return new ConnectionTracker(mockLogger.Object);
    }
}
