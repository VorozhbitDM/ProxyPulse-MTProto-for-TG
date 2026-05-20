using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    /// <summary>Изменение размера окна без WS_THICKFRAME (убирает светлую полосу сверху на Win10).</summary>
    internal static class BorderlessResize
    {
        private const int WmNcHitTest = 0x0084;
        private const int HtClient = 1;
        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;

        public static void AfterWndProc(Form form, ref Message m, int grip = 8)
        {
            if (m.Msg != WmNcHitTest || form == null || form.IsDisposed)
                return;

            if (m.Result.ToInt32() != HtClient)
                return;

            var screen = LParamToPoint(m.LParam);
            var client = form.PointToClient(screen);
            var w = form.ClientSize.Width;
            var h = form.ClientSize.Height;

            var left = client.X < grip;
            var right = client.X >= w - grip;
            var top = client.Y < grip;
            var bottom = client.Y >= h - grip;

            if (left && top)
                m.Result = (IntPtr)HtTopLeft;
            else if (right && top)
                m.Result = (IntPtr)HtTopRight;
            else if (left && bottom)
                m.Result = (IntPtr)HtBottomLeft;
            else if (right && bottom)
                m.Result = (IntPtr)HtBottomRight;
            else if (left)
                m.Result = (IntPtr)HtLeft;
            else if (right)
                m.Result = (IntPtr)HtRight;
            else if (bottom)
                m.Result = (IntPtr)HtBottom;
            else if (top)
                m.Result = (IntPtr)HtTop;
        }

        private static Point LParamToPoint(IntPtr lParam)
        {
            var lp = lParam.ToInt64();
            return new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
        }
    }
}
