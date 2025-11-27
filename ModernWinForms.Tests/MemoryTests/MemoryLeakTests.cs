using System.Diagnostics;

namespace ModernWinForms.Tests.MemoryTests;

/// <summary>
/// Memory leak detection tests using real controls and actual memory measurements.
/// These tests verify that controls properly clean up resources.
/// </summary>
[Collection("WinForms Tests")]
public class MemoryLeakTests
{
    [Fact]
    public void CreateAndDispose_100Buttons_ShouldNotLeakMemory()
    {
        // Arrange
        const int iterations = 100;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryBefore = GC.GetTotalMemory(true);

        // Act - Create and properly dispose buttons
        for (int i = 0; i < iterations; i++)
        {
            using var button = new ModernButton { Text = $"Button {i}" };
            // Auto-disposed by using
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryAfter = GC.GetTotalMemory(true);
        var memoryGrowth = memoryAfter - memoryBefore;

        // Assert - Memory growth should be minimal (< 1MB for 100 buttons)
        memoryGrowth.Should().BeLessThan(1024 * 1024, 
            $"memory should not leak significantly. Growth: {memoryGrowth / 1024}KB");
    }

    [Fact]
    public void ThemeChanges_ShouldNotLeakMemory()
    {
        // Arrange
        const int themeChangeCount = 20;
        using var button = new ModernButton { Text = "Test" };
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryBefore = GC.GetTotalMemory(true);

        // Act - Change themes multiple times
        for (int i = 0; i < themeChangeCount; i++)
        {
            ThemeManager.SetTheme(
                i % 2 == 0 ? Theme.GitHub : Theme.Material,
                i % 3 == 0 ? Skin.Dracula : Skin.Nord
            );
            Thread.Sleep(10); // Let theme propagate
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryAfter = GC.GetTotalMemory(true);
        var memoryGrowth = memoryAfter - memoryBefore;

        // Assert - Minimal memory growth
        memoryGrowth.Should().BeLessThan(512 * 1024, 
            $"theme changes should not leak memory. Growth: {memoryGrowth / 1024}KB");
    }

    [Fact]
    public void EventSubscription_AfterDispose_ShouldNotLeak()
    {
        // Arrange
        const int iterations = 50;
        var weakReferences = new List<WeakReference>();

        // Act - Create buttons with event subscriptions, then dispose
        for (int i = 0; i < iterations; i++)
        {
            var button = new ModernButton { Text = $"Button {i}" };
            button.Click += (s, e) => { /* Event handler */ };
            
            weakReferences.Add(new WeakReference(button));
            button.Dispose();
        }

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert - Most weak references should be collected
        var aliveCount = weakReferences.Count(wr => wr.IsAlive);
        aliveCount.Should().BeLessThan(iterations / 4, 
            $"disposed buttons should be garbage collected. Alive: {aliveCount}/{iterations}");
    }

    [Fact]
    public void GraphicsPathPool_ShouldReusePathsNotLeak()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryBefore = GC.GetTotalMemory(true);

        // Act - Create many panels that use graphics paths
        for (int i = 0; i < 100; i++)
        {
            using var panel = new ModernPanel { Size = new Size(200, 200) };
            using var form = new Form();
            form.Controls.Add(panel);
            form.Show();
            panel.Refresh();
            Application.DoEvents();
            form.Close();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryAfter = GC.GetTotalMemory(true);
        var memoryGrowth = memoryAfter - memoryBefore;

        // Assert
        memoryGrowth.Should().BeLessThan(2 * 1024 * 1024, 
            $"graphics path pooling should prevent leaks. Growth: {memoryGrowth / 1024}KB");
    }

    // ColorCache is internal, cannot be tested directly from here
    // Memory behavior is covered by control creation tests

    [Fact]
    public void AnimationEngine_Disposal_ShouldReleaseResources()
    {
        // Arrange
        const int iterations = 50;
        var weakReferences = new List<WeakReference>();

        // Act - Create controls with animations, then dispose
        for (int i = 0; i < iterations; i++)
        {
            var textBox = new ModernTextBox { Text = $"TextBox {i}" };
            // Focus animation is created in constructor
            
            weakReferences.Add(new WeakReference(textBox));
            textBox.Dispose();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert
        var aliveCount = weakReferences.Count(wr => wr.IsAlive);
        aliveCount.Should().BeLessThan(iterations / 4, 
            $"disposed controls with animations should be collected. Alive: {aliveCount}/{iterations}");
    }

    [Fact]
    public void Form_WithManyControls_DisposalShouldCleanup()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryBefore = GC.GetTotalMemory(true);

        // Act - Create form with many controls, then dispose
        for (int iteration = 0; iteration < 10; iteration++)
        {
            using var form = new Form { Size = new Size(800, 600) };
            
            for (int i = 0; i < 20; i++)
            {
                form.Controls.Add(new ModernButton { Text = $"Button {i}", Location = new Point(10 + i * 35, 10) });
                form.Controls.Add(new ModernTextBox { Location = new Point(10 + i * 35, 50) });
                form.Controls.Add(new ModernLabel { Text = $"Label {i}", Location = new Point(10 + i * 35, 90) });
            }

            form.Show();
            Application.DoEvents();
            form.Close();
            // Form disposed by using
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryAfter = GC.GetTotalMemory(true);
        var memoryGrowth = memoryAfter - memoryBefore;

        // Assert
        memoryGrowth.Should().BeLessThan(5 * 1024 * 1024, 
            $"forms with many controls should cleanup properly. Growth: {memoryGrowth / 1024}KB");
    }
}
