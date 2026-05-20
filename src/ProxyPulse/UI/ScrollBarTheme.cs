using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    /// <summary>Тёмная/светлая полоса прокрутки для панелей с AutoScroll.</summary>
    internal static class ScrollBarTheme
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        public static void Apply(Control control, bool dark)
        {
            if (control == null)
                return;

            void ApplyIfReady()
            {
                if (!control.IsHandleCreated)
                    return;

                try
                {
                    SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
                }
                catch
                {
                }
            }

            ApplyIfReady();
            control.HandleCreated += (_, __) => ApplyIfReady();
        }
    }
}
