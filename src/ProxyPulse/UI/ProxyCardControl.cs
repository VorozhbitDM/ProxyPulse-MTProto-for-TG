using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ProxyPulse.Models;

namespace ProxyPulse.UI
{
    public sealed class ProxyCardControl : Panel
    {
        private const string ConnectHintText = "Нажмите, чтобы подключить";

        private Color _bgCard;
        private Color _textMuted;
        private Color _stripUnknown;

        private static readonly Color PingGood = Color.FromArgb(39, 174, 96);
        private static readonly Color PingMedium = Color.FromArgb(230, 126, 34);
        private static readonly Color PingBad = Color.FromArgb(231, 76, 60);

        private readonly Panel _accentStrip;
        private readonly Panel _body;
        private readonly Label _addressLabel;
        private readonly Label _pingLabel;
        private readonly Label _connectHintLabel;
        private readonly Button _btnRefresh;
        private readonly ToolTip _toolTip;
        private bool _hover;
        private bool _pointerOnRefresh;
        private Color _accentColor = Color.Gray;

        public ProxyEntry Entry { get; private set; }

        public event EventHandler ConnectClick;
        public event EventHandler RecheckClick;

        public ProxyCardControl()
        {
            AppFonts.EnsureInitialized();
            Height = 52;
            Margin = new Padding(0, 0, 0, 6);
            Cursor = Cursors.Hand;
            Padding = new Padding(0);

            _toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 300,
                ReshowDelay = 200,
                ShowAlways = true
            };

            _accentStrip = new Panel
            {
                Width = 5,
                Dock = DockStyle.Left,
                BackColor = _stripUnknown
            };

            _body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 6, 8, 6),
                BackColor = Color.Transparent
            };

            _addressLabel = new Label
            {
                AutoSize = false,
                Font = AppFonts.CardTitle,
                ForeColor = AppTheme.Current.CardAddress,
                Location = new Point(0, 2),
                Height = 18,
                AutoEllipsis = true
            };

            _pingLabel = new Label
            {
                AutoSize = true,
                Font = AppFonts.CardMeta,
                Location = new Point(0, 22)
            };

            _connectHintLabel = new Label
            {
                Text = ConnectHintText,
                AutoSize = true,
                Visible = false,
                Font = AppFonts.CardConnectHint,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            _btnRefresh = new Button
            {
                Text = "Обновить",
                Size = new Size(84, 28),
                MinimumSize = new Size(84, 28),
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.CardHint,
                TabStop = true,
                Cursor = Cursors.Hand
            };
            _btnRefresh.UseVisualStyleBackColor = false;
            _btnRefresh.FlatAppearance.BorderSize = 1;
            _btnRefresh.Click += OnRefreshClick;
            _toolTip.SetToolTip(_btnRefresh, "Повторно проверить доступность");

            _btnRefresh.MouseEnter += (_, __) =>
            {
                _pointerOnRefresh = true;
                UpdateConnectHint();
            };
            _btnRefresh.MouseLeave += (_, __) =>
            {
                _pointerOnRefresh = false;
                UpdateConnectHint();
            };

            _body.Controls.Add(_addressLabel);
            _body.Controls.Add(_pingLabel);
            _body.Controls.Add(_connectHintLabel);
            _body.Controls.Add(_btnRefresh);

            Controls.Add(_body);
            Controls.Add(_accentStrip);

            WireHover(this);
            WireHover(_body);
            WireHover(_addressLabel);
            WireHover(_pingLabel);
            WireHover(_connectHintLabel);

            WireConnectArea(this);
            WireConnectArea(_body);
            WireConnectArea(_addressLabel);
            WireConnectArea(_pingLabel);
            WireConnectArea(_connectHintLabel);

            Resize += (_, __) => LayoutBody();
            _body.Resize += (_, __) => LayoutBody();
            ApplyTheme();
        }

        private void WireConnectArea(Control c)
        {
            c.Click += (_, __) => RaiseConnect();
            c.Cursor = Cursors.Hand;
        }

        private void RaiseConnect()
        {
            if (ConnectClick != null)
                ConnectClick(this, EventArgs.Empty);
        }

        private void WireHover(Control c)
        {
            c.MouseEnter += OnPointerEnter;
            c.MouseLeave += OnPointerLeave;
        }

        private void OnPointerEnter(object sender, EventArgs e)
        {
            SetHover(true);
        }

        private void OnPointerLeave(object sender, EventArgs e)
        {
            if (!PointerInside())
                SetHover(false);
        }

        private bool PointerInside()
        {
            var pos = PointToClient(Cursor.Position);
            return ClientRectangle.Contains(pos);
        }

        public void ApplyTheme()
        {
            var t = AppTheme.Current;
            _bgCard = t.BgCard;
            _textMuted = t.TextMuted;
            _stripUnknown = t.Border;
            BackColor = _hover ? Blend(_bgCard, _accentColor, 72) : _bgCard;
            _addressLabel.ForeColor = t.CardAddress;
            _connectHintLabel.ForeColor = t.Accent;
            StyleRefreshButton();
            UpdatePing();
        }

        private void StyleRefreshButton()
        {
            var t = AppTheme.Current;
            _btnRefresh.BackColor = t.CardRecheckBg;
            _btnRefresh.ForeColor = t.CardRecheckText;
            _btnRefresh.FlatAppearance.BorderColor = t.CardRecheckBorder;
        }

        public void Bind(ProxyEntry entry)
        {
            Entry = entry;
            _addressLabel.Text = entry != null ? entry.DisplayLabel : "";
            UpdatePing();
            LayoutBody();
        }

        public void UpdatePing()
        {
            if (Entry == null)
                return;

            if (!Entry.IsAvailable || !Entry.PingMs.HasValue)
            {
                _pingLabel.Text = "—";
                _pingLabel.ForeColor = _textMuted;
                _accentColor = _stripUnknown;
                _accentStrip.BackColor = _accentColor;
                return;
            }

            var ms = Entry.PingMs.Value;
            _pingLabel.Text = string.Format("{0} ms", ms);
            _accentColor = GetPingColor(ms);
            _pingLabel.ForeColor = _accentColor;
            _accentStrip.BackColor = _accentColor;
        }

        public static Color GetPingColor(int ms)
        {
            if (ms < 200)
                return PingGood;
            if (ms <= 600)
                return PingMedium;
            return PingBad;
        }

        public void SetRechecking()
        {
            _pingLabel.Text = "проверка…";
            _pingLabel.ForeColor = _textMuted;
            _accentStrip.BackColor = _stripUnknown;
        }

        private void UpdateConnectHint()
        {
            _connectHintLabel.Visible = _hover && !_pointerOnRefresh;
            LayoutBody();
        }

        private void LayoutBody()
        {
            if (_body == null)
                return;

            var innerW = _body.ClientSize.Width - _body.Padding.Horizontal;
            if (innerW <= 0)
                return;

            const int btnW = 84;
            const int btnH = 28;
            const int gap = 8;
            var bodyH = _body.ClientSize.Height;
            var btnTop = Math.Max(0, (bodyH - btnH) / 2);
            _btnRefresh.Location = new Point(Math.Max(0, innerW - btnW), btnTop);

            var reservedRight = btnW + gap;
            if (_connectHintLabel.Visible)
                reservedRight += _connectHintLabel.Width + gap;

            _addressLabel.Width = Math.Max(80, innerW - reservedRight);

            if (_connectHintLabel.Visible)
            {
                var hintTop = Math.Max(0, (bodyH - _connectHintLabel.Height) / 2);
                _connectHintLabel.Location = new Point(
                    Math.Max(0, innerW - btnW - gap - _connectHintLabel.Width),
                    hintTop);
            }
        }

        private static Color Blend(Color baseColor, Color overlay, int overlayAlpha)
        {
            var a = overlayAlpha / 255f;
            return Color.FromArgb(
                255,
                (int)(baseColor.R * (1 - a) + overlay.R * a),
                (int)(baseColor.G * (1 - a) + overlay.G * a),
                (int)(baseColor.B * (1 - a) + overlay.B * a));
        }

        private void SetHover(bool hover)
        {
            if (_hover == hover)
                return;

            _hover = hover;
            BackColor = hover ? Blend(_bgCard, _accentColor, 72) : _bgCard;
            StyleRefreshButton();
            UpdateConnectHint();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_hover)
                return;

            var r = ClientRectangle;
            r.Width--;
            r.Height--;
            using (var path = CreateHoverOutline(r, 4))
            using (var pen = new Pen(_accentColor, 2))
                e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateHoverOutline(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var d = Math.Max(2, radius * 2);
            if (rect.Width < d || rect.Height < d)
            {
                path.AddRectangle(rect);
                return path;
            }

            var arc = new Rectangle(rect.Location, new Size(d, d));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            if (RecheckClick != null)
                RecheckClick(this, e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            RaiseConnect();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _toolTip != null)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
