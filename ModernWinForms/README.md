# ModernWinForms

Modern, themeable WinForms controls library with fluent design and smooth animations.

## Features

- **Modern Controls**: Button, TextBox, and GroupBox with contemporary styling
- **Theme Support**: JSON-based theming system with multiple pre-built themes
- **Fluent Design**: Rounded corners, smooth transitions, and clean aesthetics
- **Easy Integration**: Simple API for applying themes to entire forms or individual controls
- **Fully Documented**: XML documentation for all public APIs
- **.NET 10**: Built for the latest .NET platform

## Controls

### ModernButton
- Rounded corners
- State-based styling (normal, hover, pressed, disabled)
- Transparent background support
- Full designer support

### ModernGroupBox
- Rounded corners
- Theme-aware borders and text
- Clean, modern appearance

### ModernTextBox
- Rounded corners with focus highlighting
- Multi-line support
- Scrollbars
- Placeholder text support

## Quick Start

### 1. Add Reference
Add a reference to the `ModernWinForms.dll` in your WinForms project.

### 2. Create a Skins Configuration
Create a `Skins` folder in your application directory and add a `skins.json` file:

```json
{
  "version": "1.0",
  "currentSkin": "Light",
  "skins": {
    "Light": {
      "description": "Clean light theme",
      "palette": {
        "primary": "#0969DA",
        "background": "#FAFBFC",
        "surface": "#FFFFFF",
        "text": "#0D1117",
        "border": "#E0E6ED"
      },
      "controls": {
        "ModernButton": {
          "cornerRadius": 8,
          "borderWidth": 1,
          "states": {
            "normal": {
              "backColor": "#FAFBFC",
              "foreColor": "#0D1117",
              "borderColor": "#E0E6ED"
            },
            "hover": {
              "backColor": "#F6F8FB",
              "borderColor": "#D0D7E0"
            }
          }
        }
      }
    }
  }
}
```

### 3. Apply Theme in Your Form

```csharp
using ModernWinForms.Theming;
using ModernWinForms.Controls;

public partial class MyForm : Form
{
    public MyForm()
    {
        InitializeComponent();
        
        // Apply theme to the entire form
        ThemeManager.ApplyTheme(this);
        
        // Or change the theme dynamically
        ThemeManager.CurrentTheme = "Dark";
    }
}
```

### 4. Use Controls
Simply add the modern controls to your form using the designer or programmatically:

```csharp
var button = new ModernButton
{
    Text = "Click Me",
    Location = new Point(10, 10),
    Size = new Size(120, 40)
};
this.Controls.Add(button);
```

## Theme Configuration

The theming system supports:
- **Palette**: Global colors (primary, background, surface, text, border)
- **Typography**: Font family and size
- **Control Styles**: Per-control configuration with state-based styling
- **States**: normal, hover, pressed, disabled, focused

## License

Created by TheStrul

## Requirements

- .NET 10.0 or later
- Windows 10.0.26100.0 or later
