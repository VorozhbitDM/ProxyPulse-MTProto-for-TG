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

            MouseWheel += OnMouseWheel;
            _viewport.MouseWheel += OnMouseWheel;
            _scroll.MouseWheel += OnMouseWheel;
        }

        public Panel Viewport
        {
            get { return _viewport; }
        }

        public void SetContent(Control content)
        {
            if (_content != null)
            {
                DetachWheelHandlers(_content);
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
            AttachWheelHandlers(_content);
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
            if (e.Control != null)
                AttachWheelHandlers(e.Control);
            UpdateScrollMetrics();
        }

        private void AttachWheelHandlers(Control root)
        {
            if (root == null)
                return;

            root.MouseWheel -= OnMouseWheel;
            root.MouseWheel += OnMouseWheel;
            root.ControlAdded -= Content_Changed;
            root.ControlAdded += Content_Changed;

            foreach (Control child in root.Controls)
                AttachWheelHandlers(child);
        }

        private void DetachWheelHandlers(Control root)
        {
            if (root == null)
                return;

            root.MouseWheel -= OnMouseWheel;
            root.ControlAdded -= Content_Changed;
            foreach (Control child in root.Controls)
                DetachWheelHandlers(child);
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (!_scroll.Visible)
                return;

            var delta = e.Delta > 0 ? -_scroll.SmallChange : _scroll.SmallChange;
            var max = Math.Max(0, _scroll.Maximum - _scroll.LargeChange + 1);
            var next = Math.Max(0, Math.Min(max, _scroll.Value + delta));
            if (next == _scroll.Value)
                return;

            _scroll.Value = next;
            ApplyScrollOffset();
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
