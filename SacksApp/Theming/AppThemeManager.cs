namespace SacksApp.Theming;

/// <summary>
/// Global theme manager for app-wide theme control (Light/Dark mode)
/// </summary>
public static class AppThemeManager
{
    private static string _currentTheme = "TestTheme";
    
    /// <summary>
    /// Gets or sets the current app-wide theme (Light, Dark, etc.)
    /// </summary>
    public static string CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
    
    /// <summary>
    /// Event raised when the app theme changes
    /// </summary>
    public static event EventHandler? ThemeChanged;
    
    /// <summary>
    /// Apply the current theme to all forms and controls
    /// </summary>
    public static void ApplyTheme(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        // Apply theme to form background
        form.BackColor = _currentTheme == "Dark" 
            ? System.Drawing.Color.FromArgb(30, 30, 30) 
            : System.Drawing.Color.FromArgb(250, 250, 252);
        
        // Apply theme to all ModernButtons recursively
        ApplyThemeToControls(form.Controls);
    }
    
    private static void ApplyThemeToControls(System.Windows.Forms.Control.ControlCollection controls)
    {
        foreach (System.Windows.Forms.Control control in controls)
        {
            if (control is ModernButton btn)
            {
                btn.Theme = _currentTheme;
            }
            
            // Recursively process child controls
            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls);
            }
        }
    }
}
