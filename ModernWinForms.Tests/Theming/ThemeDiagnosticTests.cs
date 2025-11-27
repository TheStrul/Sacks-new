using Xunit.Abstractions;

namespace ModernWinForms.Tests.Theming;

[Collection("WinForms Tests")]
public class ThemeDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public ThemeDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Diagnostic_ShowCurrentThemeState()
    {
        // Get current theme and skin
        var currentTheme = ThemeManager.CurrentTheme;
        var currentSkin = ThemeManager.CurrentSkin;
        var themeDef = ThemeManager.CurrentThemeDefinition;
        var skinDef = ThemeManager.CurrentSkinDefinition;

        _output.WriteLine($"Current Theme: {currentTheme}");
        _output.WriteLine($"Current Skin: {currentSkin}");
        _output.WriteLine($"Theme Definition null? {themeDef == null}");
        _output.WriteLine($"Skin Definition null? {skinDef == null}");
        
        if (themeDef != null)
        {
            _output.WriteLine($"Theme Controls count: {themeDef.Controls?.Count ?? 0}");
            _output.WriteLine($"Theme PaletteMappings count: {themeDef.PaletteMappings?.Count ?? 0}");
            
            if (themeDef.PaletteMappings != null)
            {
                _output.WriteLine("\nPaletteMappings in theme:");
                foreach (var controlName in themeDef.PaletteMappings.Keys.Take(10))
                {
                    var mapping = themeDef.PaletteMappings[controlName];
                    _output.WriteLine($"  {controlName}: {mapping.States.Count} states");
                }
            }
            
            if (themeDef.Controls != null && themeDef.Controls.ContainsKey("ModernButton"))
            {
                var buttonControl = themeDef.Controls["ModernButton"];
                _output.WriteLine($"\nModernButton in theme.Controls:");
                _output.WriteLine($"  States count: {buttonControl.States.Count}");
                foreach (var state in buttonControl.States.Keys)
                {
                    _output.WriteLine($"    - {state}");
                }
            }
        }
        
        if (skinDef != null)
        {
            _output.WriteLine($"\nSkin Palette null? {skinDef.Palette == null}");
            _output.WriteLine($"Skin Controls count: {skinDef.Controls?.Count ?? 0}");
            
            if (skinDef.Palette != null)
            {
                _output.WriteLine($"  Primary: {skinDef.Palette.Primary}");
                _output.WriteLine($"  Background: {skinDef.Palette.Background}");
            }
        }
        
        // Test GetControlStyle
        var buttonStyle = ThemeManager.GetControlStyle("ModernButton");
        _output.WriteLine($"\nGetControlStyle('ModernButton') result:");
        _output.WriteLine($"  Null? {buttonStyle == null}");
        if (buttonStyle != null)
        {
            _output.WriteLine($"  States count: {buttonStyle.States.Count}");
            foreach (var state in buttonStyle.States.Keys)
            {
                var stateStyle = buttonStyle.States[state];
                _output.WriteLine($"    {state}: BackColor={stateStyle.BackColor}, ForeColor={stateStyle.ForeColor}");
            }
        }
        
        // This test always passes - it's just for diagnostics
        Assert.True(true);
    }
}
