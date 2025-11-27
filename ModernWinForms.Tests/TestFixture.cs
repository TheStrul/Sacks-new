using ModernWinForms.Theming;

namespace ModernWinForms.Tests;

/// <summary>
/// Test fixture to initialize WinForms for all tests.
/// </summary>
public class TestFixture : IDisposable
{
    public TestFixture()
    {
        // Initialize WinForms application context for all tests
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
        catch
        {
            // May already be initialized
        }
    }

    public void Dispose()
    {
        // Cleanup resources but don't dispose the semaphore
        // Each test collection will manage its own cleanup
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Custom test collection to ensure tests run sequentially with WinForms initialization.
/// </summary>
[CollectionDefinition("WinForms Tests", DisableParallelization = true)]
public class WinFormsTestCollection : ICollectionFixture<TestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
