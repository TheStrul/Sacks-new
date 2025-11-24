# App-Wide Theme System

## Overview

The ModernButton now supports **app-wide theming** where all buttons respond to a central theme (Light/Dark mode).

## How It Works

### 1. AppThemeManager (Central Controller)

```csharp
// Change the entire app theme
AppThemeManager.CurrentTheme = "Dark";  // or "Light"

// Apply theme to a form and all its ModernButtons
AppThemeManager.ApplyTheme(myForm);
```

### 2. Button Theme Configuration

All buttons take their visual style from `Configuration/button-theme.json`:

```json
{
  "themes": {
    "Light": {
      "normal": { "backColor": "#FFFFFF", "foreColor": "#212529", ... },
      "hover": { ... },
      "pressed": { ... }
    },
    "Dark": {
      "normal": { "backColor": "#212529", "foreColor": "#FFFFFF", ... },
      ...
    }
  }
}
```

### 3. Explicit Property Overrides

You can still override individual button properties:

```csharp
// This button uses Dark theme from config
var btn = new ModernButton { Theme = "Dark" };

// This button uses Dark theme BUT with custom BackColor
var btn2 = new ModernButton { 
    Theme = "Dark",
    BackColor = Color.Blue  // Explicit override
};
```

## Adding New Themes

1. Edit `Configuration/button-theme.json`
2. Add a new theme preset:

```json
{
  "themes": {
    "MyCustomTheme": {
      "normal": { "backColor": "#FF5722", "foreColor": "#FFFFFF", ... },
      "hover": { ... },
      "pressed": { ... },
      "disabled": { ... },
      "focused": { ... }
    }
  }
}
```

3. Use it:

```csharp
AppThemeManager.CurrentTheme = "MyCustomTheme";
AppThemeManager.ApplyTheme(this);
```

## Theme Toggle Example

To add a theme toggle button to your app:

```csharp
private void ThemeToggleButton_Click(object sender, EventArgs e)
{
    // Toggle between Light and Dark
    AppThemeManager.CurrentTheme = 
        AppThemeManager.CurrentTheme == "Light" ? "Dark" : "Light";
    
    // Reapply theme to all forms
    AppThemeManager.ApplyTheme(this);
}
```

## Available Theme Presets

- **Light** - White background, dark text
- **Dark** - Dark background, light text
- **Primary** - Blue accent
- **Secondary** - Gray/neutral
- **Success** - Green
- **Danger** - Red
