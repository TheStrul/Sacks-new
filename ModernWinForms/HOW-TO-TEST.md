# How to Test the Theming System

## ✅ Quick Build Test

```powershell
# Build ModernWinForms library
dotnet build ModernWinForms\ModernWinForms.csproj
# Should see: Build succeeded

# Build SacksApp (which uses the theming system)
dotnet build SacksApp\SacksApp.csproj  
# Should see: Build succeeded
```

## 🎨 Visual Testing with ThemeTestForm

I've created **`ThemeTestForm.cs`** in SacksApp - a complete test interface that lets you:

- Switch between themes (GitHub, Material, Fluent)
- Switch between skins (Light, Dark, Dracula, etc.)
- See live preview of controls
- View style information showing structure vs. colors

### To Run ThemeTestForm

**Option 1 - Modify Program.cs temporarily:**

```csharp
// In SacksApp/Program.cs, replace Application.Run line with:
Application.Run(new ThemeTestForm());
```

**Option 2 - Add menu item to MainForm:**
Add a "Test Themes" menu button that opens the ThemeTestForm

**Option 3 - Run directly:**

```powershell
cd SacksApp
# Add this to Program.cs Main method:
# var testForm = new ThemeTestForm();
# Application.Run(testForm);
dotnet run
```

## 🔍 What ThemeTestForm Shows

1. **Theme Selector** - Pick design system (GitHub/Material/Fluent)
   - Changes cornerRadius, borderWidth, spacing
   
2. **Skin Selector** - Pick color variant (Light/Dark/Dracula/etc.)
   - Changes only colors, preserves structure
   
3. **Live Controls**:
   - ModernButton with hover/pressed states
   - ModernTextBox with focus state
   - ModernGroupBox with borders
   - Disabled button
   
4. **Style Info Panel** - Shows:

   ```text
   === Button Style ===
   CornerRadius: 8px (from theme)
   BorderWidth: 1px (from theme)
   States: normal, hover, pressed, disabled
   Normal: Back=#FAFBFC, Fore=#0D1117, Border=#E0E6ED (from skin)
   
   === Architecture Validation ===
   ✅ Themes provide structure
   ✅ Skins provide colors
   ✅ ThemeManager.GetControlStyle() merges both
   ```

## 📋 Manual Test Checklist

### Architecture Tests

- [ ] `ControlStateColors` class exists (base class, states only)
- [ ] `ControlStyle` class exists (derived, adds structure)
- [ ] `SkinDefinition.Controls` uses `ControlStateColors`
- [ ] `ThemeDefinition.Controls` uses `ControlStyle`
- [ ] `ThemeManager.GetControlStyle()` method exists

### JSON Tests

- [ ] Theme files (GitHub.theme.json, Material.theme.json, Fluent.theme.json) have cornerRadius/borderWidth
- [ ] Skin files (Light.skin.json, Dark.skin.json, etc.) have only colors in states
- [ ] Inheritance works (Light inherits from BaseLight inherits from Base)

### Runtime Tests

- [ ] Switch theme from GitHub → Material → see cornerRadius change
- [ ] Switch skin from Light → Dark → see colors change, structure stays same
- [ ] Hover over button → see color transition
- [ ] Focus textbox → see border color change

## 📁 Test Files Created

1. **`ThemeTestForm.cs`** - Full interactive test form
2. **`TESTING.md`** - Comprehensive testing guide
3. This file - Quick start guide

## 🚀 Quick Validation

```powershell
# 1. Verify all class files exist
Get-ChildItem ModernWinForms\Theming\*.cs | Select-Object Name

# Expected: 11 files including:
# - ControlStateColors.cs
# - ControlStyle.cs
# - SkinDefinition.cs
# - ThemeDefinition.cs
# - ColorPalette.cs
# - Typography.cs
# - StateStyle.cs
# - ShadowStyle.cs
# - PaddingSpec.cs
# - SpacingSystem.cs
# - ThemingConfiguration.cs

# 2. Build succeeds
dotnet build ModernWinForms\ModernWinForms.csproj

# 3. SacksApp builds (uses theming)
dotnet build SacksApp\SacksApp.csproj
```

## ✅ Success = All Builds Pass

If both projects build successfully, the architecture is correct!

For detailed testing scenarios, see `TESTING.md`.
