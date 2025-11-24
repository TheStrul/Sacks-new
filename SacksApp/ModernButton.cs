using System;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using SacksApp.Theming;

namespace SacksApp;

public class ModernButton : Button
{
    private ControlStyle _controlStyle = new();
    private bool _isHovered;
    private bool _isPressed;

    public ModernButton()
    {
        // 1. Enable double buffering and custom painting
        SetStyle(ControlStyles.UserPaint | 
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.OptimizedDoubleBuffer | 
                ControlStyles.ResizeRedraw | 
                ControlStyles.SupportsTransparentBackColor, true);

        // 2. IMPORTANT: Set BackColor to Transparent so WinForms attempts to draw the parent first
        BackColor = Color.Transparent;
        
        // 3. Remove default button styling
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;

        UpdateStyleFromSkin();
    }

    public void ApplySkin(SkinDefinition skin)
    {
        ArgumentNullException.ThrowIfNull(skin);
        UpdateStyleFromSkin(skin);
        Invalidate();
    }

    private void UpdateStyleFromSkin(SkinDefinition? skin = null)
    {
        // Default fallback style
        _controlStyle = new ControlStyle
        {
            CornerRadius = 8,
            BorderWidth = 1,
            States = new Dictionary<string, StateStyle>
            {
                ["normal"] = new() { BackColor = "#FAFBFC", ForeColor = "#0D1117", BorderColor = "#E0E6ED" },
                ["hover"] = new() { BackColor = "#F6F8FB", ForeColor = "#0D1117", BorderColor = "#D0D7E0" },
                ["pressed"] = new() { BackColor = "#EBEEF2", ForeColor = "#0D1117", BorderColor = "#B8C1CC" },
                ["disabled"] = new() { BackColor = "#F5F7FA", ForeColor = "#B0B8C3", BorderColor = "#E5EBF0" }
            }
        };

        if (skin?.Controls != null && skin.Controls.TryGetValue("ModernButton", out var style))
        {
            _controlStyle = style;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // --- STEP 1: SOLVE THE DIRTY CORNERS ---
        // We must manually fill the background with the EFFECTIVE parent color.
        // If the direct parent is transparent (like TableLayoutPanel), we need to look further up.
        var parentColor = GetEffectiveBackColor();
        using (var parentBrush = new SolidBrush(parentColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        // --- STEP 2: DETERMINE STATE ---
        var stateName = !Enabled ? "disabled" : _isPressed ? "pressed" : _isHovered ? "hover" : "normal";
        var stateStyle = _controlStyle.States.GetValueOrDefault(stateName) ?? _controlStyle.States["normal"];

        var backColor = ColorTranslator.FromHtml(stateStyle.BackColor ?? "#FFFFFF");
        var foreColor = ColorTranslator.FromHtml(stateStyle.ForeColor ?? "#000000");
        var borderColor = ColorTranslator.FromHtml(stateStyle.BorderColor ?? "#CCCCCC");

        // --- STEP 3: DRAW THE ROUNDED BUTTON ---
        using var path = GetRoundedPath(ClientRectangle, _controlStyle.CornerRadius);
        using var brush = new SolidBrush(backColor);
        using var pen = new Pen(borderColor, _controlStyle.BorderWidth);

        // Draw button background
        g.FillPath(brush, path);
        
        // Draw border (if width > 0)
        if (_controlStyle.BorderWidth > 0)
        {
            g.DrawPath(pen, path);
        }

        // --- STEP 4: DRAW TEXT ---
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, foreColor, 
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        
        // Adjust rect to ensure border stays inside
        // When drawing a border, the pen draws centered on the line. 
        // We need to shrink the rectangle slightly so the border doesn't get clipped.
        float adjustment = _controlStyle.BorderWidth / 2f;
        var r = new RectangleF(rect.X + adjustment, rect.Y + adjustment, 
                             rect.Width - _controlStyle.BorderWidth, 
                             rect.Height - _controlStyle.BorderWidth);

        if (radius <= 0)
        {
            path.AddRectangle(r);
            return path;
        }

        float d = radius * 2;
        
        // Top Left
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        // Top Right
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        // Bottom Right
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        // Bottom Left
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);

        path.CloseFigure();
        return path;
    }

    private Color GetEffectiveBackColor()
    {
        var current = Parent;
        while (current != null)
        {
            var color = current.BackColor;
            // Check if color is not transparent (A > 0) and not explicitly Color.Transparent
            if (color.A > 0 && color != Color.Transparent)
            {
                return color;
            }
            current = current.Parent;
        }
        return SystemColors.Control;
    }
}
