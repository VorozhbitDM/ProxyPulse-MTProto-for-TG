using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    /// <summary>Прокручиваемая область со стилизованной вертикальной полосой.</summary>
    internal sealed class ThemedScrollPanel : Panel
    {
        private readonly Panel _viewport;
        private readonly VScrollBar _scroll;
        private Control _content;

        public ThemedScrollPanel()
        {
            Dock = DockStyle.Fill;
            AutoScroll = false;

            _viewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false
            };

            _scroll = new VScrollBar
            {
                Dock = DockStyle.Right,
                Width = 14,
                Visible = false
            };
            _scroll.Scroll += (_, __) => ApplyScrollOffset();
            _scroll.ValueChanged += (_, __) => ApplyScrollOffset();

            Controls.Add(_viewport);
            Controls.Add(_scroll);
            _viewport.Resize += (_, __) => UpdateScrollMetrics();
        }

        public Panel Viewport
        {
            get { return _viewport; }
        }

        public void SetContent(Control content)
        {
            if (_content != null)
            {
                _content.SizeChanged -= Content_SizeChanged;
                _content.ControlAdded -= Content_Changed;
                _content.ControlRemoved -= Content_Changed;
                _viewport.Controls.Remove(_content);
            }

            _content = content;
            if (_content == null)
                return;

            _content.Location = new Point(0, 0);
            _content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _viewport.Controls.Add(_content);
            _content.SizeChanged += Content_SizeChanged;
            _content.ControlAdded += Content_Changed;
            _content.ControlRemoved += Content_Changed;
            UpdateScrollMetrics();
        }

        public void RefreshScrollMetrics()
        {
            UpdateScrollMetrics();
        }

        public void ApplyTheme(bool dark)
        {
            var t = AppTheme.Current;
            BackColor = t.BgApp;
            _viewport.BackColor = t.BgApp;

            _scroll.BackColor = dark ? Color.FromArgb(38, 43, 51) : Color.FromArgb(228, 232, 238);
            _scroll.ForeColor = dark ? Color.FromArgb(90, 98, 110) : Color.FromArgb(180, 186, 196);
            ScrollBarTheme.Apply(_scroll, dark);
            ScrollBarTheme.Apply(this, dark);
        }

        private void Content_SizeChanged(object sender, EventArgs e)
        {
            UpdateScrollMetrics();
        }

        private void Content_Changed(object sender, ControlEventArgs e)
        {
            UpdateScrollMetrics();
        }

        private void UpdateScrollMetrics()
        {
            if (_content == null || _viewport == null)
                return;

            var viewH = Math.Max(0, _viewport.ClientSize.Height);
            var contentH = Math.Max(_content.Height, _content.PreferredSize.Height);
            var max = Math.Max(0, contentH - viewH);

            _scroll.SuspendLayout();
            _scroll.Maximum = max > 0 ? max + _scroll.LargeChange - 1 : 0;
            _scroll.LargeChange = Math.Max(1, viewH);
            _scroll.SmallChange = 24;
            _scroll.Visible = max > 0;
            if (_scroll.Value > max)
                _scroll.Value = max;
            _scroll.ResumeLayout();
            ApplyScrollOffset();
        }

        private void ApplyScrollOffset()
        {
            if (_content == null)
                return;

            _content.Top = -_scroll.Value;
        }
    }
}
