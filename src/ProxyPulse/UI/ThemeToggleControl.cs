using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    /// <summary>Переключатель «Тёмная | Белая» — одна панель, без рамок WinForms-кнопок.</summary>
    internal sealed class ThemeToggleControl : UserControl
    {
        private const int SegmentWidth = 96;
        private const int SegmentHeight = 28;
        private const int CaptionGap = 8;

        private readonly Label _caption;
        private readonly SegmentPanel _segments;

        public ThemeToggleControl()
        {
            AppFonts.EnsureInitialized();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = SegmentHeight;
            Width = 56 + CaptionGap + SegmentWidth * 2;

            _caption = new Label
            {
                Text = "Тема:",
                AutoSize = true,
                Location = new Point(0, (SegmentHeight - 16) / 2),
                Font = AppFonts.Ui,
                BackColor = Color.Transparent
            };

            _segments = new SegmentPanel
            {
                Location = new Point(56 + CaptionGap, 0),
                Size = new Size(SegmentWidth * 2, SegmentHeight)
            };
            _segments.ThemePicked += OnSegmentThemePicked;

            Controls.Add(_caption);
            Controls.Add(_segments);
        }

        public event EventHandler ThemeChanged;

        public bool IsDarkTheme { get; private set; }

        public void SetTheme(bool dark, bool raiseEvent)
        {
            IsDarkTheme = dark;
            _segments.IsDarkSelected = dark;
            ApplyTheme(dark ? AppTheme.Dark : AppTheme.Light);
            if (raiseEvent && ThemeChanged != null)
                ThemeChanged(this, EventArgs.Empty);
        }

        public void ApplyTheme()
        {
            ApplyTheme(IsDarkTheme ? AppTheme.Dark : AppTheme.Light);
        }

        public void ApplyTheme(ThemePalette t)
        {
            _caption.ForeColor = t.TextPrimary;
            _segments.PreviewPalette = t;
            _segments.Invalidate();
        }

        private void OnSegmentThemePicked(bool dark)
        {
            if (IsDarkTheme == dark)
                return;
            SetTheme(dark, raiseEvent: true);
        }

        private sealed class SegmentPanel : Panel
        {
            private const string DarkLabel = "Тёмная";
            private const string LightLabel = "Белая";

            public bool IsDarkSelected { get; set; } = true;

            public ThemePalette PreviewPalette { get; set; }

            public event Action<bool> ThemePicked;

            public SegmentPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                TabStop = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                var t = PreviewPalette ?? AppTheme.Current;
                var darkRect = new Rectangle(0, 0, SegmentWidth, SegmentHeight);
                var lightRect = new Rectangle(SegmentWidth, 0, SegmentWidth, SegmentHeight);
                var outer = new Rectangle(0, 0, Width - 1, Height - 1);

                using (var darkBg = new SolidBrush(IsDarkSelected ? t.Accent : t.BtnSurface))
                using (var lightBg = new SolidBrush(IsDarkSelected ? t.BtnSurface : t.Accent))
                using (var borderPen = new Pen(t.BtnBorder))
                using (var dividerPen = new Pen(t.BtnBorder))
                {
                    g.FillRectangle(darkBg, darkRect);
                    g.FillRectangle(lightBg, lightRect);
                    g.DrawRectangle(borderPen, outer);
                    g.DrawLine(dividerPen, SegmentWidth, 0, SegmentWidth, Height - 1);
                }

                var darkFore = IsDarkSelected ? t.AccentButtonFore : t.TextSecondary;
                var lightFore = IsDarkSelected ? t.TextSecondary : t.AccentButtonFore;
                DrawCenteredLabel(g, DarkLabel, darkRect, darkFore);
                DrawCenteredLabel(g, LightLabel, lightRect, lightFore);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left)
                    return;

                var pickDark = e.X < SegmentWidth;
                if (ThemePicked != null)
                    ThemePicked(pickDark);
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                {
                    if (ThemePicked != null)
                        ThemePicked(e.KeyCode == Keys.Left);
                    e.Handled = true;
                }
            }

            private static void DrawCenteredLabel(Graphics g, string text, Rectangle bounds, Color fore)
            {
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
                TextRenderer.DrawText(g, text, AppFonts.Ui, bounds, fore, flags);
            }
        }
    }
}
