using System.Text.Json.Serialization;

namespace SacksApp.Theming;

/// <summary>
/// Root configuration for the skin system
/// </summary>
public sealed class SkinConfiguration
{
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("currentSkin")] public string CurrentSkin { get; set; } = "Light";
    [JsonPropertyName("skins")] public Dictionary<string, SkinDefinition> Skins { get; set; } = new();
}

/// <summary>
/// Definition of a single skin (Light, Dark, etc.)
/// </summary>
public sealed class SkinDefinition
{
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("palette")] public SkinPalette Palette { get; set; } = new();
    [JsonPropertyName("typography")] public SkinTypography Typography { get; set; } = new();
    [JsonPropertyName("controls")] public Dictionary<string, ControlStyle> Controls { get; set; } = new();
}

public sealed class SkinPalette
{
    [JsonPropertyName("primary")] public string? Primary { get; set; }
    [JsonPropertyName("secondary")] public string? Secondary { get; set; }
    [JsonPropertyName("success")] public string? Success { get; set; }
    [JsonPropertyName("danger")] public string? Danger { get; set; }
    [JsonPropertyName("warning")] public string? Warning { get; set; }
    [JsonPropertyName("info")] public string? Info { get; set; }
    [JsonPropertyName("background")] public string? Background { get; set; }
    [JsonPropertyName("surface")] public string? Surface { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("border")] public string? Border { get; set; }
}

public sealed class SkinTypography
{
    [JsonPropertyName("fontFamily")] public string FontFamily { get; set; } = "Segoe UI";
    [JsonPropertyName("fontSize")] public float FontSize { get; set; } = 9.0f;
}

public sealed class ControlStyle
{
    [JsonPropertyName("cornerRadius")] public int CornerRadius { get; set; }
    [JsonPropertyName("borderWidth")] public int BorderWidth { get; set; }
    [JsonPropertyName("padding")] public PaddingSpec? Padding { get; set; }
    [JsonPropertyName("states")] public Dictionary<string, StateStyle> States { get; set; } = new();
}

public sealed class StateStyle
{
    [JsonPropertyName("backColor")] public string? BackColor { get; set; }
    [JsonPropertyName("foreColor")] public string? ForeColor { get; set; }
    [JsonPropertyName("borderColor")] public string? BorderColor { get; set; }
    [JsonPropertyName("shadow")] public ShadowStyle? Shadow { get; set; }
}

public sealed class ShadowStyle
{
    [JsonPropertyName("color")] public string? Color { get; set; }
    [JsonPropertyName("blur")] public int Blur { get; set; }
    [JsonPropertyName("offsetX")] public int OffsetX { get; set; }
    [JsonPropertyName("offsetY")] public int OffsetY { get; set; }
}

public sealed class PaddingSpec
{
    [JsonPropertyName("left")] public int Left { get; set; }
    [JsonPropertyName("top")] public int Top { get; set; }
    [JsonPropertyName("right")] public int Right { get; set; }
    [JsonPropertyName("bottom")] public int Bottom { get; set; }

    public Padding ToPadding() => new(Left, Top, Right, Bottom);
}
