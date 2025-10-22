using System.Text.Json;
using System.Text.Json.Serialization;

namespace SacksApp.Theming
{
    // POCOs matching button-theme.json
    public sealed class ButtonTheme
    {
        [JsonPropertyName("version")] public string? Version { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("defaults")] public DefaultsSection Defaults { get; set; } = new();
        [JsonPropertyName("colors")] public ColorsSection Colors { get; set; } = new();
        [JsonPropertyName("spacing")] public SpacingSection Spacing { get; set; } = new();
        [JsonPropertyName("rendering")] public RenderingSection Rendering { get; set; } = new();
        [JsonPropertyName("badge")] public BadgeSection Badge { get; set; } = new();
        [JsonPropertyName("typography")] public TypographySection Typography { get; set; } = new();
        [JsonPropertyName("presets")] public Dictionary<string, Preset>? Presets { get; set; }

        public sealed class DefaultsSection
        {
            [JsonPropertyName("cornerRadius")] public int CornerRadius { get; set; } = 12;
            [JsonPropertyName("badgeDiameter")] public int BadgeDiameter { get; set; } = 28;
            [JsonPropertyName("focusBorderWidth")] public int FocusBorderWidth { get; set; } = 1;
            [JsonPropertyName("badgeIconSizeRatio")] public double BadgeIconSizeRatio { get; set; } = 0.5;
            [JsonPropertyName("badgeHeightRatio")] public double BadgeHeightRatio { get; set; } = 0.55;
        }

        public sealed class ColorsSection
        {
            [JsonPropertyName("backColor")] public RgbColor? BackColor { get; set; }
            // Explicit state colors with opacity
            [JsonPropertyName("active")] public StateColor? Active { get; set; }
            [JsonPropertyName("inactive")] public StateColor? Inactive { get; set; }
            [JsonPropertyName("hoverColor")] public StateColor? HoverColor { get; set; }
            [JsonPropertyName("pressedColor")] public StateColor? PressedColor { get; set; }

            [JsonPropertyName("focusBorder")] public FocusBorderColors FocusBorder { get; set; } = new();
        }

        public sealed class FocusBorderColors
        {
            [JsonPropertyName("useSystemHighlight")] public bool UseSystemHighlight { get; set; } = true;
            [JsonPropertyName("customColor")] public RgbColor? CustomColor { get; set; }
        }

        public sealed class SpacingSection
        {
            [JsonPropertyName("badgeLeftOffset")] public int BadgeLeftOffset { get; set; } = 4;
            [JsonPropertyName("badgeLeftPaddingExtra")] public int BadgeLeftPaddingExtra { get; set; } = 22;
            [JsonPropertyName("defaultPadding")] public PaddingSpec DefaultPadding { get; set; } = new();
        }

        public sealed class RenderingSection
        {
            [JsonPropertyName("antiAlias")] public bool AntiAlias { get; set; } = true;
            [JsonPropertyName("highQualityPixelOffset")] public bool HighQualityPixelOffset { get; set; } = true;
            [JsonPropertyName("clearTypeText")] public bool ClearTypeText { get; set; } = true;
            [JsonPropertyName("backgroundExpansion")] public double BackgroundExpansion { get; set; } = 0.5;
            [JsonPropertyName("doubleBuffer")] public bool DoubleBuffer { get; set; } = true;
        }

        public sealed class BadgeSection
        {
            [JsonPropertyName("fontFamily")] public string FontFamily { get; set; } = "Segoe MDL2 Assets";
            [JsonPropertyName("fallbackToSystemFont")] public bool FallbackToSystemFont { get; set; } = true;
            [JsonPropertyName("glyphColor")] public RgbColor GlyphColor { get; set; } = new();
        }

        public sealed class TypographySection
        {
            [JsonPropertyName("textFontFamily")] public string? TextFontFamily { get; set; }
            [JsonPropertyName("textFontSize")] public float? TextFontSize { get; set; }
            [JsonPropertyName("textFontStyle")] public string? TextFontStyle { get; set; }
            [JsonPropertyName("textColor")] public RgbColor? TextColor { get; set; }
        }

        public sealed class Preset
        {
            [JsonPropertyName("cornerRadius")] public int? CornerRadius { get; set; }
            [JsonPropertyName("focusBorderWidth")] public int? FocusBorderWidth { get; set; }
        }

        public sealed class RgbColor
        {
            [JsonPropertyName("r")] public int R { get; set; } = 255;
            [JsonPropertyName("g")] public int G { get; set; } = 255;
            [JsonPropertyName("b")] public int B { get; set; } = 255;
            public Color ToColor() => Color.FromArgb(
                Math.Clamp(R, 0, 255), Math.Clamp(G, 0, 255), Math.Clamp(B, 0, 255));
        }

        public sealed class StateColor
        {
            [JsonPropertyName("color")] public RgbColor? Color { get; set; }
            // 0.0 - 1.0 (1.0 = fully opaque)
            [JsonPropertyName("opacity")] public double? Opacity { get; set; } = 1.0;
        }

        public sealed class PaddingSpec
        {
            [JsonPropertyName("left")] public int Left { get; set; } = 12;
            [JsonPropertyName("top")] public int Top { get; set; } = 12;
            [JsonPropertyName("right")] public int Right { get; set; } = 12;
            [JsonPropertyName("bottom")] public int Bottom { get; set; } = 12;

            public System.Windows.Forms.Padding ToPadding() => new(Left, Top, Right, Bottom);
        }
    }

    public static class ButtonThemeProvider
    {
        private static readonly object s_lock = new();
        private static FileSystemWatcher? s_watcher;
        private static ButtonTheme s_current = new();
        private static readonly JsonSerializerOptions s_json = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        public static event EventHandler? ThemeChanged;
        public static ButtonTheme Current
        {
            get { lock (s_lock) return s_current; }
        }

        public static void Initialize()
        {
            try
            {
                Load();
                StartWatcher();
            }
            catch
            {
                // Ignore init errors; control will use defaults
            }
        }

        private static string ResolvePath()
        {
            // Keep consistent with appsettings location under Configuration/
            var basePath = AppContext.BaseDirectory;
            var rel = Path.Combine("Configuration", "button-theme.json");
            var p = Path.Combine(basePath, rel);
            if (File.Exists(p)) return p;
            // Climb a few levels (design-time / test contexts)
            var dir = new DirectoryInfo(basePath);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var probe = Path.Combine(dir.FullName, rel);
                if (File.Exists(probe)) return probe;
                dir = dir.Parent;
            }
            return Path.Combine(basePath, rel);
        }

        public static void Load()
        {
            var path = ResolvePath();
            ButtonTheme loaded = new();
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    loaded = JsonSerializer.Deserialize<ButtonTheme>(json, s_json) ?? new ButtonTheme();
                }
            }
            catch
            {
                // keep defaults
            }

            lock (s_lock)
            {
                s_current = loaded;
            }
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void StartWatcher()
        {
            try
            {
                var path = ResolvePath();
                var dir = Path.GetDirectoryName(path)!;
                s_watcher?.Dispose();
                s_watcher = new FileSystemWatcher(dir, "button-theme.json")
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.CreationTime
                };
                s_watcher.Changed += (_, __) => DebouncedReload();
                s_watcher.Created += (_, __) => DebouncedReload();
                s_watcher.Renamed += (_, __) => DebouncedReload();
                s_watcher.Deleted += (_, __) => DebouncedReload();
            }
            catch { }
        }

        private static DateTime s_last;
        private static void DebouncedReload()
        {
            var now = DateTime.UtcNow;
            if ((now - s_last).TotalMilliseconds < 250) return;
            s_last = now;
            // small delay to avoid partial writes
            Task.Run(async () =>
            {
                await Task.Delay(300).ConfigureAwait(false);
                try { Load(); } catch { }
            });
        }
    }
}
