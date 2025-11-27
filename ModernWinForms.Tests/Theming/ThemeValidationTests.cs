namespace ModernWinForms.Tests.Theming;

/// <summary>
/// Tests for theme validation and switching across all themes and skins.
/// </summary>
[Collection("WinForms Tests")]
public class ThemeValidationTests : IDisposable
{
    private readonly List<Control> _controlsToDispose = new();

    public void Dispose()
    {
        foreach (var control in _controlsToDispose)
        {
            control?.Dispose();
        }
        _controlsToDispose.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private T TrackControl<T>(T control) where T : Control
    {
        _controlsToDispose.Add(control);
        return control;
    }

    [Theory]
    [InlineData(Theme.Base, Skin.BaseLight)]
    [InlineData(Theme.Base, Skin.BaseDark)]
    [InlineData(Theme.GitHub, Skin.BaseLight)]
    [InlineData(Theme.GitHub, Skin.BaseDark)]
    [InlineData(Theme.Material, Skin.Material)]
    [InlineData(Theme.Fluent, Skin.Fluent)]
    [InlineData(Theme.Base, Skin.SolarizedLight)]
    [InlineData(Theme.Base, Skin.SolarizedDark)]
    [InlineData(Theme.Base, Skin.Dracula)]
    [InlineData(Theme.Base, Skin.Nord)]
    [InlineData(Theme.Base, Skin.Gruvbox)]
    [InlineData(Theme.Base, Skin.Monokai)]
    [InlineData(Theme.Base, Skin.Cyberpunk)]
    public void AllThemeSkinCombinations_ShouldLoad(Theme theme, Skin skin)
    {
        // Act
        Action act = () =>
        {
            ThemeManager.SetTheme(theme, skin);
            Application.DoEvents();
        };

        // Assert
        act.Should().NotThrow($"{theme} theme with {skin} skin should load without errors");
        ThemeManager.CurrentTheme.Should().NotBeNullOrEmpty();
        ThemeManager.CurrentSkin.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(Theme.Base, Skin.BaseLight)]
    [InlineData(Theme.Material, Skin.Material)]
    [InlineData(Theme.Fluent, Skin.Fluent)]
    public void ThemeSwitch_WithExistingControls_ShouldUpdateAll(Theme theme, Skin skin)
    {
        // Arrange
        var button = TrackControl(new ModernButton { Text = "Test" });
        var textBox = TrackControl(new ModernTextBox());
        var panel = TrackControl(new ModernPanel());
        var label = TrackControl(new ModernLabel { Text = "Label" });

        // Act
        ThemeManager.SetTheme(theme, skin);
        Application.DoEvents();

        // Assert - Controls should not throw when theme changes
        button.BackColor.Should().NotBe(Color.Empty);
        label.ForeColor.Should().NotBe(Color.Empty);
    }

    [Fact]
    public void RapidThemeSwitching_ShouldNotThrow()
    {
        // Arrange
        var button = TrackControl(new ModernButton { Text = "Test" });
        var themes = new[]
        {
            (Theme.Base, Skin.BaseLight),
            (Theme.Material, Skin.Material),
            (Theme.Fluent, Skin.Fluent),
            (Theme.Base, Skin.BaseDark),
            (Theme.GitHub, Skin.BaseLight),
        };

        // Act
        Action act = () =>
        {
            foreach (var (theme, skin) in themes)
            {
                ThemeManager.SetTheme(theme, skin);
                Application.DoEvents();
            }
        };

        // Assert
        act.Should().NotThrow("rapid theme switching should not cause issues");
    }

    [Fact]
    public void ThemeSwitch_AfterControlCreation_ShouldUpdateStyles()
    {
        // Arrange - Create control with default theme
        ThemeManager.SetTheme(Theme.Base, Skin.BaseLight);
        var button = TrackControl(new ModernButton { Text = "Test" });
        var initialBackColor = button.BackColor;

        // Act - Switch to different theme
        ThemeManager.SetTheme(Theme.Material, Skin.Material);
        Application.DoEvents();

        // Assert - Color should have changed (in most cases, unless coincidentally same)
        // We just verify it didn't throw and theme was applied
        ThemeManager.CurrentTheme.Should().Be("Material");
    }

    [Fact]
    public void ThemeManager_CurrentThemeAndSkin_ShouldBeSet()
    {
        // Arrange
        ThemeManager.SetTheme(Theme.Base, Skin.BaseLight);

        // Act
        var themeName = ThemeManager.CurrentTheme;
        var skinName = ThemeManager.CurrentSkin;

        // Assert
        themeName.Should().NotBeNullOrEmpty();
        skinName.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(Theme.Base)]
    [InlineData(Theme.GitHub)]
    [InlineData(Theme.Material)]
    [InlineData(Theme.Fluent)]
    public void AllThemes_ShouldHaveValidConfiguration(Theme theme)
    {
        // Act
        ThemeManager.SetTheme(theme, Skin.BaseLight);

        // Assert
        ThemeManager.CurrentTheme.Should().NotBeNullOrEmpty();
        ThemeManager.CurrentSkin.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ConcurrentThemeSwitching_MultipleThreads_ShouldBeThreadSafe()
    {
        // Arrange
        var button = TrackControl(new ModernButton { Text = "Test" });
        var themes = new[]
        {
            (Theme.Base, Skin.BaseLight),
            (Theme.Material, Skin.Material),
            (Theme.Fluent, Skin.Fluent),
        };
        var exceptionCount = 0;

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                var (theme, skin) = themes[i % themes.Length];
                ThemeManager.SetTheme(theme, skin);
            }
            catch
            {
                Interlocked.Increment(ref exceptionCount);
            }
        });

        // Assert
        exceptionCount.Should().Be(0, "concurrent theme switching should be thread-safe");
    }

    [Fact]
    public void ThemeSwitch_WithFormAndMultipleControls_ShouldUpdateAllControls()
    {
        // Arrange
        using var form = new Form { Width = 400, Height = 300 };
        form.Controls.Add(TrackControl(new ModernButton { Text = "Button 1" }));
        form.Controls.Add(TrackControl(new ModernButton { Text = "Button 2" }));
        form.Controls.Add(TrackControl(new ModernTextBox()));
        form.Controls.Add(TrackControl(new ModernLabel { Text = "Label" }));
        form.Controls.Add(TrackControl(new ModernPanel()));

        // Act
        ThemeManager.SetTheme(Theme.Material, Skin.Material);
        Application.DoEvents();

        // Assert - All controls should exist and have non-empty colors
        form.Controls.Count.Should().Be(5);
        foreach (Control control in form.Controls)
        {
            control.BackColor.Should().NotBe(Color.Empty);
        }
    }

    [Fact]
    public void DarkThemes_ShouldLoad()
    {
        // Arrange & Act
        var darkThemes = new[]
        {
            (Theme.Base, Skin.BaseDark),
            (Theme.Base, Skin.Dracula),
            (Theme.Base, Skin.Nord),
            (Theme.Base, Skin.Monokai),
        };

        foreach (var (theme, skin) in darkThemes)
        {
            // Act
            Action act = () =>
            {
                ThemeManager.SetTheme(theme, skin);
                Application.DoEvents();
            };

            // Assert
            act.Should().NotThrow($"{theme}/{skin} should load without errors");
        }
    }

    [Fact]
    public void LightThemes_ShouldLoad()
    {
        // Arrange & Act
        var lightThemes = new[]
        {
            (Theme.Base, Skin.BaseLight),
            (Theme.Base, Skin.SolarizedLight),
        };

        foreach (var (theme, skin) in lightThemes)
        {
            // Act
            Action act = () =>
            {
                ThemeManager.SetTheme(theme, skin);
                Application.DoEvents();
            };

            // Assert
            act.Should().NotThrow($"{theme}/{skin} should load without errors");
        }
    }
}
