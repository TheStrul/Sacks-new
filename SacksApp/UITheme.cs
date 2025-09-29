using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SacksApp
{
    internal static class UITheme
    {
        public static Image CreateIconBitmap(Color bg, string glyph, int size = 40)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var brush = new SolidBrush(bg))
                {
                    g.FillEllipse(brush, 0, 0, size - 1, size - 1);
                }

                try
                {
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI Emoji", size * 3 / 5, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var fore = new SolidBrush(Color.White))
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        g.DrawString(glyph, font, fore, new RectangleF(0, 0, size, size), sf);
                    }
                }
                catch
                {
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var font = new Font("Segoe UI", size * 3 / 5, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var fore = new SolidBrush(Color.White))
                    {
                        g.DrawString("•", font, fore, new RectangleF(0, 0, size, size), sf);
                    }
                }
            }

            return bmp;
        }

        public static void ApplyButtonStyle(Button b, Color? bgColor = null, string? glyph = null)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.White;
            b.ForeColor = Color.FromArgb(30, 30, 30);
            b.TextImageRelation = TextImageRelation.ImageBeforeText;
            b.ImageAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(24, 12, 12, 12);
            b.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            if (glyph != null)
            {
                var color = bgColor ?? Color.FromArgb(33, 150, 243);
                try { b.Image = CreateIconBitmap(color, glyph); } catch { }
            }
        }
    }
}
