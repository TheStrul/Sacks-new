# ModernWinForms - Simple Theme API

## Zero Configuration Usage

Drop any Modern control onto your form - it just works!

```csharp
using ModernWinForms.Controls;

// Zero configuration needed - works immediately with default theme
var button = new ModernButton { Text = "Click Me", Location = new Point(10, 10) };
var textBox = new ModernTextBox { Location = new Point(10, 50) };
var panel = new ModernPanel { Size = new Size(200, 100), Location = new Point(10, 90) };

this.Controls.Add(button);
this.Controls.Add(textBox);
this.Controls.Add(panel);
```

## Simple Theme Switching

Want a different look? One line of code:

```csharp
using ModernWinForms.Theming;

// Switch to GitHub theme with Dracula colors
ThemeManager.SetTheme(Theme.GitHub, Skin.Dracula);

// Switch to Material theme with Nord colors
ThemeManager.SetTheme(Theme.Material, Skin.Nord);

// Switch to Fluent theme with Monokai colors
ThemeManager.SetTheme(Theme.Fluent, Skin.Monokai);
```

## Available Themes

- `Theme.Base` - Simple, clean default design
- `Theme.GitHub` - GitHub-inspired professional look
- `Theme.Material` - Google Material Design
- `Theme.Fluent` - Microsoft Fluent Design

## Available Skins (Colors)

- `Skin.BaseLight` - Standard light theme
- `Skin.BaseDark` - Standard dark theme
- `Skin.Fluent` - Microsoft Fluent colors
- `Skin.Material` - Google Material colors
- `Skin.SolarizedLight` - Solarized Light color scheme
- `Skin.SolarizedDark` - Solarized Dark color scheme
- `Skin.Dracula` - Dracula theme with purple accents
- `Skin.Nord` - Arctic, north-bluish palette
- `Skin.Gruvbox` - Retro groove color scheme
- `Skin.Monokai` - Classic Sublime Text dark theme
- `Skin.Cyberpunk` - Neon, futuristic colors

## Apply Theme to Entire Form

```csharp
// Apply current theme to all controls on the form
ThemeManager.ApplyTheme(this);
```

## Listen to Theme Changes

```csharp
ThemeManager.ThemeChanged += (sender, e) => 
{
    // Theme was changed - controls automatically update
    Console.WriteLine("Theme changed!");
};
```

## That's It!

No configuration files to edit. No initialization code. No complex setup.

Just:
1. Add Modern controls to your form
2. Optionally call `SetTheme()` if you want a different look
3. Done!
