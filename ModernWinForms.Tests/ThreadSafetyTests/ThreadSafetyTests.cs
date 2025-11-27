using System.Collections.Concurrent;

namespace ModernWinForms.Tests.ThreadSafetyTests;

/// <summary>
/// Thread safety tests using real concurrent access patterns.
/// Verifies that shared resources (ThemeManager, caches) are thread-safe.
/// </summary>
[Collection("WinForms Tests")]
public class ThreadSafetyTests
{
    [Fact]
    public void ThemeManager_ConcurrentThemeChanges_ShouldNotCrash()
    {
        // Arrange
        const int threadCount = 10;
        const int iterationsPerThread = 50;
        var exceptions = new ConcurrentBag<Exception>();
        var themes = new[] { Theme.Base, Theme.GitHub, Theme.Material, Theme.Fluent };
        var skins = new[] { Skin.Dracula, Skin.Nord, Skin.Monokai };

        // Act - Multiple threads changing themes simultaneously
        var threads = Enumerable.Range(0, threadCount).Select(i => new Thread(() =>
        {
            try
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var theme = themes[j % themes.Length];
                    var skin = skins[j % skins.Length];
                    ThemeManager.SetTheme(theme, skin);
                    Thread.Sleep(1); // Small delay to increase contention
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert
        exceptions.Should().BeEmpty("concurrent theme changes should not throw exceptions");
    }

    [Fact]
    public void ThemeManager_ConcurrentReadWrite_ShouldBeConsistent()
    {
        // Arrange
        const int readerThreads = 5;
        const int writerThreads = 3;
        const int iterations = 100;
        var exceptions = new ConcurrentBag<Exception>();
        var inconsistencies = new ConcurrentBag<string>();

        // Act - Readers and writers running concurrently
        var readers = Enumerable.Range(0, readerThreads).Select(i => new Thread(() =>
        {
            try
            {
                for (int j = 0; j < iterations; j++)
                {
                    var theme = ThemeManager.CurrentTheme;
                    var skin = ThemeManager.CurrentSkin;
                    
                    // Verify values are not null/empty (consistency check)
                    if (string.IsNullOrWhiteSpace(theme) || string.IsNullOrWhiteSpace(skin))
                    {
                        inconsistencies.Add($"Thread {i}: Got null/empty theme or skin");
                    }
                    
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToList();

        var writers = Enumerable.Range(0, writerThreads).Select(i => new Thread(() =>
        {
            try
            {
                for (int j = 0; j < iterations / 2; j++)
                {
                    ThemeManager.SetTheme(Theme.GitHub, Skin.Nord);
                    Thread.Sleep(2);
                    ThemeManager.SetTheme(Theme.Material, Skin.Dracula);
                    Thread.Sleep(2);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToList();

        var allThreads = readers.Concat(writers).ToList();
        allThreads.ForEach(t => t.Start());
        allThreads.ForEach(t => t.Join());

        // Assert
        exceptions.Should().BeEmpty("concurrent read/write should not throw");
        inconsistencies.Should().BeEmpty("theme values should always be consistent");
    }

    // ColorCache is internal, cannot be tested directly from here
    // Thread safety is covered by control creation and theme switching tests

    [Fact]
    public void ControlCreation_OnMultipleThreads_ShouldWork()
    {
        // Arrange
        const int threadCount = 8;
        const int controlsPerThread = 25;
        var exceptions = new ConcurrentBag<Exception>();
        var buttons = new ConcurrentBag<ModernButton>();

        // Act - Create controls on multiple threads
        var threads = Enumerable.Range(0, threadCount).Select(i => new Thread(() =>
        {
            try
            {
                for (int j = 0; j < controlsPerThread; j++)
                {
                    var button = new ModernButton { Text = $"Thread{i}-Button{j}" };
                    buttons.Add(button);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert
        exceptions.Should().BeEmpty("concurrent control creation should not throw");
        buttons.Should().HaveCount(threadCount * controlsPerThread);

        // Cleanup
        foreach (var button in buttons)
        {
            button.Dispose();
        }
    }

    [Fact]
    public void ThemeChanged_WithMultipleSubscribers_ShouldNotifyAll()
    {
        // Arrange
        const int subscriberCount = 20;
        var notificationCounts = new ConcurrentDictionary<int, int>();
        var exceptions = new ConcurrentBag<Exception>();

        // Create subscribers
        for (int i = 0; i < subscriberCount; i++)
        {
            var subscriberId = i;
            notificationCounts[subscriberId] = 0;
            
            ThemeManager.ThemeChanged += (s, e) =>
            {
                try
                {
                    notificationCounts[subscriberId]++;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            };
        }

        // Act - Change theme
        ThemeManager.SetTheme(Theme.GitHub, Skin.Nord);
        Thread.Sleep(100); // Allow notifications to propagate

        // Assert
        exceptions.Should().BeEmpty("event notifications should not throw");
        notificationCounts.Values.Should().AllSatisfy(count =>
            count.Should().BeGreaterThan(0, "all subscribers should be notified"));
    }

    [Fact]
    public void GetControlStyle_ConcurrentRequests_ShouldReturnValidStyles()
    {
        // Arrange
        const int threadCount = 10;
        const int requestsPerThread = 100;
        var exceptions = new ConcurrentBag<Exception>();
        var styles = new ConcurrentBag<ControlStyle>();
        var controlNames = new[] { "ModernButton", "ModernTextBox", "ModernPanel", "ModernLabel" };

        // Act - Multiple threads requesting control styles
        var threads = Enumerable.Range(0, threadCount).Select(i => new Thread(() =>
        {
            try
            {
                for (int j = 0; j < requestsPerThread; j++)
                {
                    var controlName = controlNames[j % controlNames.Length];
                    var style = ThemeManager.GetControlStyle(controlName);
                    if (style != null)
                    {
                        styles.Add(style);
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert
        exceptions.Should().BeEmpty("concurrent GetControlStyle should not throw");
        styles.Should().NotBeEmpty("should return styles");
        styles.Should().AllSatisfy(s =>
        {
            s.CornerRadius.Should().BeInRange(0, 100);
            s.States.Should().NotBeEmpty();
        });
    }
}
