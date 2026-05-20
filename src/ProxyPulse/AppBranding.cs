using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ProxyPulse
{
    internal static class AppBranding
    {
        /// <summary>
        /// Title bar / taskbar icon while the app runs.
        /// ApplicationIcon only sets the .exe file icon; WinForms needs Form.Icon (default is colored squares).
        /// </summary>
        public static Icon LoadWindowIcon()
        {
            var fromResource = LoadFromEmbeddedResource();
            if (fromResource != null)
                return fromResource;

            try
            {
                var exe = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
                {
                    var extracted = Icon.ExtractAssociatedIcon(exe);
                    if (extracted != null)
                        return (Icon)extracted.Clone();
                }
            }
            catch
            {
                // fall through
            }

            return null;
        }

        static Icon LoadFromEmbeddedResource()
        {
            var asm = typeof(AppBranding).Assembly;
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith(".app.ico", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase))
                    continue;

                var stream = asm.GetManifestResourceStream(name);
                if (stream == null)
                    continue;

                try
                {
                    return new Icon(stream);
                }
                finally
                {
                    stream.Dispose();
                }
            }

            return null;
        }

        public static void ApplyWindowIcon(Form form)
        {
            if (form == null)
                return;

            var icon = LoadWindowIcon();
            if (icon == null)
                return;

            if (form.Icon != null)
                form.Icon.Dispose();

            form.Icon = icon;
        }
    }
}
