using System.Drawing;
using System.Text.Json.Serialization;

namespace SacksApp.Theming;

/// <summary>
/// Root configuration for CustomButton theming.
/// </summary>
public sealed class ButtonThemeConfig
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("defaults")]
    public required DefaultsConfig Defaults { get; init; }

    [JsonPropertyName("colors")]
    public required ColorsConfig Colors { get; init; }

    [JsonPropertyName("spacing")]
    public required SpacingConfig Spacing { get; init; }

    [JsonPropertyName("rendering")]
    public required RenderingConfig Rendering { get; init; }

    [JsonPropertyName("badge")]
    public required BadgeConfig Badge { get; init; }

    [JsonPropertyName("presets")]
    public Dictionary<string, PresetConfig>? Presets { get; init; }
}

public sealed class DefaultsConfig
{
    [JsonPropertyName("cornerRadius")]
    public int CornerRadius { get; init; } = 12;

    [JsonPropertyName("badgeDiameter")]
    public int BadgeDiameter { get; init; } = 28;

    [JsonPropertyName("focusBorderWidth")]
    public int FocusBorderWidth { get; init; } = 1;

    [JsonPropertyName("badgeIconSizeRatio")]
    public float BadgeIconSizeRatio { get; init; } = 0.50f;

    [JsonPropertyName("badgeHeightRatio")]
    public float BadgeHeightRatio { get; init; } = 0.55f;
}

public sealed class ColorsConfig
{
    [JsonPropertyName("hover")]
    public required HoverConfig Hover { get; init; }

    [JsonPropertyName("pressed")]
    public required PressedConfig Pressed { get; init; }

    [JsonPropertyName("focusBorder")]
    public required FocusBorderConfig FocusBorder { get; init; }
}

public sealed class HoverConfig
{
    [JsonPropertyName("lightBackgroundDarkenAmount")]
    public float LightBackgroundDarkenAmount { get; init; } = 0.05f;

    [JsonPropertyName("darkBackgroundLightenAmount")]
    public float DarkBackgroundLightenAmount { get; init; } = 0.20f;

    [JsonPropertyName("brightnesThreshold")]
    public float BrightnessThreshold { get; init; } = 0.9f;
}

public sealed class PressedConfig
{
    [JsonPropertyName("darkenAmount")]
    public float DarkenAmount { get; init; } = 0.10f;
}

public sealed class FocusBorderConfig
{
    [JsonPropertyName("useSystemHighlight")]
    public bool UseSystemHighlight { get; init; } = true;

    [JsonPropertyName("customColor")]
    public string? CustomColor { get; init; }

    [JsonIgnore]
    public Color? ParsedCustomColor
    {
        get
        {
            if (string.IsNullOrEmpty(CustomColor)) return null;
            try
            {
                return ColorTranslator.FromHtml(CustomColor);
            }
            catch
            {
                return null;
            }
        }
    }
}

public sealed class SpacingConfig
{
    [JsonPropertyName("badgeLeftOffset")]
    public int BadgeLeftOffset { get; init; } = 4;

    [JsonPropertyName("badgeLeftPaddingExtra")]
    public int BadgeLeftPaddingExtra { get; init; } = 22;

    [JsonPropertyName("defaultPadding")]
    public required PaddingConfig DefaultPadding { get; init; }
}

public sealed class PaddingConfig
{
    [JsonPropertyName("left")]
    public int Left { get; init; } = 12;

    [JsonPropertyName("top")]
    public int Top { get; init; } = 12;

    [JsonPropertyName("right")]
    public int Right { get; init; } = 12;

    [JsonPropertyName("bottom")]
    public int Bottom { get; init; } = 12;
}

public sealed class RenderingConfig
{
    [JsonPropertyName("antiAlias")]
    public bool AntiAlias { get; init; } = true;

    [JsonPropertyName("highQualityPixelOffset")]
    public bool HighQualityPixelOffset { get; init; } = true;

    [JsonPropertyName("clearTypeText")]
    public bool ClearTypeText { get; init; } = true;

    [JsonPropertyName("backgroundExpansion")]
    public float BackgroundExpansion { get; init; } = 0.5f;

    [JsonPropertyName("doubleBuffer")]
    public bool DoubleBuffer { get; init; } = true;
}

public sealed class BadgeConfig
{
    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; init; } = "Segoe MDL2 Assets";

    [JsonPropertyName("fallbackToSystemFont")]
    public bool FallbackToSystemFont { get; init; } = true;

    [JsonPropertyName("glyphColor")]
    public required ColorRgb GlyphColor { get; init; }
}

public sealed class ColorRgb
{
    [JsonPropertyName("r")]
    public int R { get; init; }

    [JsonPropertyName("g")]
    public int G { get; init; }

    [JsonPropertyName("b")]
    public int B { get; init; }

    //[JsonIgnore]
    public Color ToColor() => Color.FromArgb(R, G, B);
}

public sealed class PresetConfig
{
    [JsonPropertyName("cornerRadius")]
    public int? CornerRadius { get; init; }

    [JsonPropertyName("focusBorderWidth")]
    public int? FocusBorderWidth { get; init; }

    [JsonPropertyName("hover")]
    public HoverConfig? Hover { get; init; }
}
