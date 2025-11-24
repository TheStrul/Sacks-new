# Script to clean ModernButton properties in Designer files
# Removes CustomButton-specific properties and ensures ModernButton compatibility

$designerFiles = @(
    "SacksApp\DashBoard.Designer.cs",
    "SacksApp\MainForm.Designer.cs"
)

foreach ($file in $designerFiles) {
    Write-Host "Processing $file..."
    
    $content = Get-Content $file -Raw
    
    # Remove CustomButton-specific properties
    $propertiesToRemove = @(
        'BadgeColor',
        'Glyph',
        'BadgeDiameter',
        'DisabledColor',
        'HoverColor',
        'PressedColor',
        'NormalColor',
        'IconColor',
        'IconText',
        'IconSize',
        'IconSpacing'
    )
    
    foreach ($prop in $propertiesToRemove) {
        # Remove lines containing these properties
        $content = $content -replace "(?m)^\s+\w+\.$prop = [^;]+;\r?\n", ""
    }
    
    # Remove ImageAlign and TextImageRelation (not used by ModernButton)
    $content = $content -replace "(?m)^\s+\w+\.ImageAlign = [^;]+;\r?\n", ""
    $content = $content -replace "(?m)^\s+\w+\.TextImageRelation = [^;]+;\r?\n", ""
    
    # Save cleaned content
    $content | Set-Content $file -NoNewline
    
    Write-Host "✓ Cleaned $file"
}

Write-Host "`nDone! Designer files cleaned."
