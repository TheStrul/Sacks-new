# Theme System Testing Guide

## 🎯 What We're Testing
The ModernWinForms theming system with proper separation:
- **Themes** (design systems) provide structure: cornerRadius, borderWidth, padding
- **Skins** (color variants) provide only colors: state colors (normal, hover, pressed, etc.)
- **ControlStateColors** base class enforces color-only definitions
- **ControlStyle** derived class adds structural properties
- **ThemeManager.GetControlStyle()** merges theme structure + skin colors

## 🚀 Quick Visual Test (Recommended)

### Option 1: Use the Test Form
1. Open SacksApp project
2. Run `ThemeTestForm.cs`:
   ```powershell
   cd SacksApp
   dotnet run --project SacksApp.csproj
   # or modify Program.cs to launch ThemeTestForm
   ```

3. The test form shows:
   - ✅ Theme dropdown (GitHub, Material, Fluent)
   - ✅ Skin dropdown (Light, Dark, Dracula, etc.)
   - ✅ Live preview of ModernButton, ModernTextBox, ModernGroupBox
   - ✅ Style information display showing:
     - Structural properties (from theme)
     - Color properties (from skin)
     - Validation that architecture is correct

### Option 2: Quick PowerShell Test
```powershell
# Build the project
dotnet build ModernWinForms\ModernWinForms.csproj

# Check for errors
$? # Should be True
```

## 📋 Manual Testing Checklist

### ✅ Architecture Validation
1. **Verify class separation:**
   ```powershell
   # All these files should exist:
   Get-ChildItem ModernWinForms\Theming\*.cs
   ```
   Expected files:
   - ControlStateColors.cs (base - states only)
   - ControlStyle.cs (derived - adds structure)
   - SkinDefinition.cs (uses ControlStateColors)
   - ThemeDefinition.cs (uses ControlStyle)
   - ThemingConfiguration.cs, ColorPalette.cs, Typography.cs, StateStyle.cs, ShadowStyle.cs, PaddingSpec.cs, SpacingSystem.cs

2. **Verify build success:**
   ```powershell
   dotnet build ModernWinForms\ModernWinForms.csproj
   # Should see: Build succeeded
   ```

### ✅ JSON File Validation
1. **Check theme files have structure:**
   ```powershell
   Get-Content ModernWinForms\Themes\GitHub.theme.json
   ```
   Should contain: `cornerRadius`, `borderWidth`, `typography`, `spacing`

2. **Check skin files have colors only:**
   ```powershell
   Get-Content ModernWinForms\Skins\Light.skin.json
   ```
   Should contain: `palette`, `controls.*.states` (colors)
   Should NOT contain cornerRadius/borderWidth at root level (only in controls if overriding)

### ✅ Inheritance Testing
1. **Base skins:**
   - Base.skin.json → root inheritance
   - BaseLight.skin.json → inherits from Base
   - BaseDark.skin.json → inherits from Base

2. **Specific skins:**
   - Light.skin.json → inherits from BaseLight
   - Dark.skin.json → inherits from BaseDark
   - Dracula.skin.json → inherits from BaseDark

3. **Verify inheritance chain works:**
   ```csharp
   // In SacksApp or test console:
   var light = ThemeManager.CurrentSkinDefinition;
   Console.WriteLine(light.InheritsFrom); // Should show "BaseLight"
   ```

### ✅ Runtime Testing
1. **Theme switching:**
   - Switch from GitHub → Material → Fluent
   - Verify cornerRadius changes (GitHub=8, Material=4, Fluent=4)
   - Verify borderWidth changes appropriately

2. **Skin switching:**
   - Within GitHub theme: Light → Dark → Dracula
   - Verify colors change but structure stays same
   - Hover over buttons to see state transitions

3. **Control validation:**
   - ModernButton shows proper hover/pressed/disabled states
   - ModernTextBox shows focus state
   - ModernGroupBox shows proper border styling

## 🔍 Code Inspection Tests

### Test 1: Base Class Architecture
```csharp
// ControlStateColors should ONLY have States
var stateColors = new ControlStateColors();
// Properties: States ✅
// No CornerRadius, BorderWidth, Padding ❌

// ControlStyle should have States + Structure
var controlStyle = new ControlStyle();
// Properties: States, CornerRadius, BorderWidth, Padding ✅
// Inherits from ControlStateColors ✅
```

### Test 2: SkinDefinition Uses Base Class
```csharp
var skin = new SkinDefinition();
// skin.Controls type should be Dictionary<string, ControlStateColors> ✅
// NOT Dictionary<string, ControlStyle> ❌
```

### Test 3: ThemeDefinition Uses Derived Class
```csharp
var theme = new ThemeDefinition();
// theme.Controls type should be Dictionary<string, ControlStyle> ✅
```

### Test 4: GetControlStyle Merges Properly
```csharp
var mergedStyle = ThemeManager.GetControlStyle("ModernButton");
// mergedStyle.CornerRadius should come from theme ✅
// mergedStyle.States["normal"] should come from skin ✅
```

## 🐛 Troubleshooting

### Build Errors
- Ensure all 11 class files exist in ModernWinForms\Theming\
- Check that ControlStyle inherits from ControlStateColors
- Verify SkinInheritanceExtensions has MergeControlStateColors method

### Runtime Errors
- ThemeManager.GetControlStyle() returns null → check that theme/skin files loaded correctly
- Colors not applying → verify skin JSON has proper "states" structure
- Structure not applying → verify theme JSON has cornerRadius/borderWidth

### JSON Errors
- Use JSON validator: `Get-Content *.json | ConvertFrom-Json`
- Check for missing commas, quotes
- Verify "inheritsFrom" references existing skin names

## ✅ Success Criteria
- ✅ All 11 class files compile without errors
- ✅ ThemeTestForm launches and displays controls
- ✅ Switching themes changes structure (cornerRadius, borderWidth)
- ✅ Switching skins changes colors but preserves structure
- ✅ Hover/pressed states work on buttons
- ✅ Style info display shows proper merge of theme + skin
- ✅ No runtime exceptions when switching themes/skins

## 📝 Test Results Template
```
Test Date: _______________
Tester: __________________

[ ] Build successful
[ ] ThemeTestForm launches
[ ] Theme switching works (GitHub/Material/Fluent)
[ ] Skin switching works (Light/Dark/Dracula)
[ ] Control states display correctly
[ ] Inheritance chain works
[ ] No console errors
[ ] Architecture validated (themes=structure, skins=colors)

Notes:
_________________________________
_________________________________
```
