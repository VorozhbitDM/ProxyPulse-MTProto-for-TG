using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    internal static class WindowChrome
    {
        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public static void DragWindow(Form form)
        {
            if (form == null || form.IsDisposed)
                return;

            ReleaseCapture();
            SendMessage(form.Handle, WmNcLButtonDown, HtCaption, 0);
        }

        public static void WireDrag(Control control, Form form)
        {
            if (control == null || form == null)
                return;

            control.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    DragWindow(form);
            };
        }
    }
}
