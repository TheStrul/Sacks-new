namespace SacksApp
{
    public static class UITheme
    {
        /// <summary>
        /// Creates a CustomButton with modern styling (rounded corners, badge, hover effects).
        /// </summary>
        public static CustomButton CreateBadgeButton(string text, Color badgeColor, string glyph, int badgeDiameter = 28, int cornerRadius = 12)
        {
            return new CustomButton
            {
                Text = text,
                BadgeColor = badgeColor,
                Glyph = glyph,
                BadgeDiameter = badgeDiameter,
                CornerRadius = cornerRadius,
                AutoSize = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 12F),
                Padding = new Padding(24, 12, 12, 12)
            };
        }

        /// <summary>
        /// Applies modern badge styling to CustomButton instances.
        /// For regular Button instances, this is a no-op - replace with CustomButton instead.
        /// </summary>
        public static void ApplyBadgeStyle(Button btn, Color badgeColor, string glyph, int badgeDiameter = 28, int cornerRadius = 12)
        {
            if (btn is CustomButton customBtn)
            {
                customBtn.BadgeColor = badgeColor;
                customBtn.Glyph = glyph;
                customBtn.BadgeDiameter = badgeDiameter;
                customBtn.CornerRadius = cornerRadius;
            }
            // For regular buttons, do nothing - users should replace with CustomButton
        }

        /// <summary>
        /// Legacy lightweight button styling (no custom drawing).
        /// </summary>
        public static void ApplyButtonStyle(Button btn, Color accentColor, string glyph)
        {
            if (btn is null) return;
            var original = btn.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(glyph) && !original.StartsWith(glyph, StringComparison.Ordinal))
            {
                btn.Text = string.IsNullOrWhiteSpace(original) ? glyph : ($"{glyph}  {original}");
            }
            btn.UseVisualStyleBackColor = true;
            btn.FlatStyle = FlatStyle.System;
            btn.Padding = new Padding(Math.Max(6, btn.Padding.Left), Math.Max(4, btn.Padding.Top), Math.Max(6, btn.Padding.Right), Math.Max(4, btn.Padding.Bottom));
        }
    }
}
