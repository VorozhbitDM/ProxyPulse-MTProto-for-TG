using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    /// <summary>Цвет заголовка диалогов в стиле приложения (Windows 10/11).</summary>
    internal static class WindowCaptionTheme
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;
        private const int DwmwaBorderColor = 34;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint attrValue, int attrSize);

        /// <summary>Рамка главного окна (Win11: цвет DWM; Win10: BorderlessResize).</summary>
        public static void ApplyMainWindow(Form form, bool dark)
        {
            if (form == null)
                return;

            void ApplyIfReady()
            {
                if (!form.IsHandleCreated)
                    return;

                SetImmersiveDarkMode(form, false);
                var t = dark ? AppTheme.Dark : AppTheme.Light;
                TrySetBorderColor(form, t.WindowBorder);
            }

            ApplyIfReady();
            form.HandleCreated += (_, __) => ApplyIfReady();
        }

        public static void Apply(Form form, bool dark)
        {
            if (form == null)
                return;

            void ApplyIfReady()
            {
                if (!form.IsHandleCreated)
                    return;

                if (dark)
                {
                    var t = AppTheme.Dark;
                    SetImmersiveDarkMode(form, false);
                    if (!TrySetCaptionColors(form, t.BgSurface, t.TextPrimary))
                        SetImmersiveDarkMode(form, true);
                }
                else
                {
                    var t = AppTheme.Light;
                    SetImmersiveDarkMode(form, false);
                    if (!TrySetCaptionColors(form, t.BgSurface, t.TextPrimary))
                        TrySetCaptionColors(form, Color.White, Color.Black);
                }
            }

            ApplyIfReady();
            form.HandleCreated += (_, __) => ApplyIfReady();
        }

        private static void SetImmersiveDarkMode(Form form, bool enabled)
        {
            var value = enabled ? 1 : 0;
            if (DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkModeBefore20H1, ref value, sizeof(int));
        }

        private static bool TrySetCaptionColors(Form form, Color background, Color text)
        {
            try
            {
                var bg = ToColorRef(background);
                var fg = ToColorRef(text);
                if (DwmSetWindowAttribute(form.Handle, DwmwaCaptionColor, ref bg, sizeof(uint)) != 0)
                    return false;
                DwmSetWindowAttribute(form.Handle, DwmwaTextColor, ref fg, sizeof(uint));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySetBorderColor(Form form, Color color)
        {
            try
            {
                var border = ToColorRef(color);
                return DwmSetWindowAttribute(form.Handle, DwmwaBorderColor, ref border, sizeof(uint)) == 0;
            }
            catch
            {
                return false;
            }
        }

        private static uint ToColorRef(Color color)
        {
            return (uint)(color.R | (color.G << 8) | (color.B << 16));
        }
    }
}
