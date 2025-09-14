using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SacksApp.Utils
{
    internal static class WindowStateHelper
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        private sealed class PersistedWindowState
        {
            public string? ScreenDeviceName { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsMaximized { get; set; }
        }

        private static string GetAppFolder()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(folder, "SacksApp");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            return appFolder;
        }

        private static string GetFilePath(string fileName) => Path.Combine(GetAppFolder(), fileName);

        public static void SaveWindowState(Form form, string fileName)
        {
            if (form == null) return;
            try
            {
                var state = new PersistedWindowState
                {
                    ScreenDeviceName = Screen.FromControl(form)?.DeviceName,
                    X = form.Location.X,
                    Y = form.Location.Y,
                    Width = form.Width,
                    Height = form.Height,
                    IsMaximized = form.WindowState == FormWindowState.Maximized
                };

                var path = GetFilePath(fileName);
                var json = JsonSerializer.Serialize(state, s_jsonOptions);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { }
        }

        public static void RestoreWindowState(Form form, string fileName, bool restoreEnabled)
        {
            if (form == null) return;
            if (!restoreEnabled)
            {
                // default positioning if restore disabled
                try
                {
                    CenterOnOwnerOrPrimary(form);
                }
                catch { }
                return;
            }

            var path = GetFilePath(fileName);
            if (!File.Exists(path))
            {
                try { CenterOnOwnerOrPrimary(form); } catch { }
                return;
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var state = JsonSerializer.Deserialize<PersistedWindowState>(json);
                if (state == null)
                {
                    try { CenterOnOwnerOrPrimary(form); } catch { }
                    return;
                }

                var screen = Screen.AllScreens.FirstOrDefault(s => string.Equals(s.DeviceName, state.ScreenDeviceName, StringComparison.OrdinalIgnoreCase));
                if (screen == null)
                {
                    try { screen = Screen.FromPoint(Cursor.Position); } catch { screen = Screen.PrimaryScreen; }
                }

                var wa = (screen ?? Screen.AllScreens.First()).WorkingArea;
                var x = Math.Min(Math.Max(wa.Left, state.X), wa.Right - Math.Min(state.Width, wa.Width));
                var y = Math.Min(Math.Max(wa.Top, state.Y), wa.Bottom - Math.Min(state.Height, wa.Height));
                var w = Math.Min(state.Width, wa.Width);
                var h = Math.Min(state.Height, wa.Height);

                form.StartPosition = FormStartPosition.Manual;
                form.Bounds = new Rectangle(x, y, w, h);
                if (state.IsMaximized) form.WindowState = FormWindowState.Maximized;
            }
            catch
            {
                try { CenterOnOwnerOrPrimary(form); } catch { }
            }
        }

        private static void CenterOnOwnerOrPrimary(Form form)
        {
            if (form.Width <= 0 || form.Height <= 0) return;

            Form? owner = form.Owner;
            if (owner == null)
            {
                owner = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f != form && f.Visible && f.WindowState != FormWindowState.Minimized);
            }

            Screen? screen = null;
            if (owner != null && owner.IsHandleCreated)
            {
                try { screen = Screen.FromControl(owner); } catch { screen = Screen.PrimaryScreen; }
            }
            else
            {
                try { screen = Screen.FromPoint(Cursor.Position); } catch { screen = Screen.PrimaryScreen; }
            }

            if (screen == null) screen = Screen.AllScreens.First();
            var wa = screen.WorkingArea;

            var x = wa.Left + Math.Max(0, (wa.Width - form.Width) / 2);
            var y = wa.Top + Math.Max(0, (wa.Height - form.Height) / 2);

            x = Math.Min(Math.Max(wa.Left, x), wa.Right - form.Width);
            y = Math.Min(Math.Max(wa.Top, y), wa.Bottom - form.Height);

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(x, y);
        }
    }
}
