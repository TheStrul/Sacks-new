namespace ModernWinForms.Tests.Theming;

/// <summary>
/// Tests for ThemeManager - the core theming infrastructure.
/// Uses real theme files and real controls (no mocks).
/// </summary>
[Collection("WinForms Tests")]
public class ThemeManagerTests : IDisposable
{
    public ThemeManagerTests()
    {
        // Each test starts fresh
    }

    public void Dispose()
    {
        // Cleanup after each test
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public void ThemeManager_ShouldLoadDefaultConfiguration_OnStaticInitialization()
    {
        // Act
        var currentTheme = ThemeManager.CurrentTheme;
        var currentSkin = ThemeManager.CurrentSkin;

        // Assert
        currentTheme.Should().NotBeNullOrWhiteSpace("theme must be initialized");
        currentSkin.Should().NotBeNullOrWhiteSpace("skin must be initialized");
    }

    [Fact]
    public void ThemeManager_ShouldHaveAvailableThemes()
    {
        // Act
        var themes = ThemeManager.AvailableThemes.ToList();

        // Assert
        themes.Should().NotBeEmpty("at least one theme must be available");
        themes.Should().Contain(new[] { "Base", "GitHub", "Material", "Fluent" });
    }

    [Fact]
    public void ThemeManager_ShouldHaveAvailableSkins()
    {
        // Act
        var skins = ThemeManager.AvailableSkins.ToList();

        // Assert
        skins.Should().NotBeEmpty("at least one skin must be available");
        skins.Should().Contain("Dracula");
        skins.Should().Contain("Nord");
    }

    [Fact]
    public void SetTheme_WithValidEnums_ShouldChangeThemeAndSkin()
    {
        // Arrange
        var originalTheme = ThemeManager.CurrentTheme;
        var originalSkin = ThemeManager.CurrentSkin;
        var themeChangedFired = false;

        EventHandler handler = (s, e) => themeChangedFired = true;
        ThemeManager.ThemeChanged += handler;

        try
        {
            // Act
            ThemeManager.SetTheme(Theme.GitHub, Skin.Dracula);

            // Assert
            ThemeManager.CurrentTheme.Should().Be("GitHub");
            ThemeManager.CurrentSkin.Should().Be("Dracula");
            themeChangedFired.Should().BeTrue("ThemeChanged event should fire");
        }
        finally
        {
            ThemeManager.ThemeChanged -= handler;
            // Restore original
            ThemeManager.CurrentTheme = originalTheme;
            ThemeManager.CurrentSkin = originalSkin;
        }
    }

    [Fact]
    public void SetTheme_ShouldHandleAllThemeEnumValues()
    {
        // Arrange & Act & Assert
        foreach (Theme theme in Enum.GetValues<Theme>())
        {
            // Should not throw
            Action act = () => ThemeManager.SetTheme(theme, Skin.Dracula);
            act.Should().NotThrow($"SetTheme should handle {theme}");
        }
    }

    [Fact]
    public void SetTheme_ShouldHandleAllSkinEnumValues()
    {
        // Arrange & Act & Assert
        foreach (Skin skin in Enum.GetValues<Skin>())
        {
            // Should not throw
            Action act = () => ThemeManager.SetTheme(Theme.Base, skin);
            act.Should().NotThrow($"SetTheme should handle {skin}");
        }
    }

    [Fact]
    public void GetControlStyle_ShouldReturnStyleForValidControl()
    {
        // Act
        var buttonStyle = ThemeManager.GetControlStyle("ModernButton");

        // Assert
        buttonStyle.Should().NotBeNull("ModernButton style should exist");
        buttonStyle!.CornerRadius.Should().BeGreaterOrEqualTo(0);
        buttonStyle.CornerRadius.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void GetControlStyle_ShouldHaveValidStates()
    {
        // Act
        var buttonStyle = ThemeManager.GetControlStyle("ModernButton");

        // Assert
        buttonStyle.Should().NotBeNull();
        buttonStyle!.States.Should().ContainKey("normal");
        buttonStyle.States.Should().ContainKey("hover");
        buttonStyle.States.Should().ContainKey("pressed");
    }

    [Fact]
    public void CurrentTheme_PropertyAccess_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var exceptionCount = 0;

        // Act - Multiple threads reading/writing simultaneously
        var threads = Enumerable.Range(0, threadCount).Select(i => new Thread(() =>
        {
            try
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var theme = ThemeManager.CurrentTheme; // Read
                    ThemeManager.CurrentTheme = theme; // Write
                }
            }
            catch
            {
                Interlocked.Increment(ref exceptionCount);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert
        exceptionCount.Should().Be(0, "no exceptions should occur during concurrent access");
    }

    [Fact]
    public void ThemeManager_Cleanup_ShouldNotThrow()
    {
        // Act
        Action act = () => ThemeManager.Cleanup();

        // Assert
        act.Should().NotThrow("Cleanup should be safe to call");
    }
}
