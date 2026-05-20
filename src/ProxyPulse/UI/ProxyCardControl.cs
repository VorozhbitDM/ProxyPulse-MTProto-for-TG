using System;
using System.Drawing;
using System.Windows.Forms;
using ProxyPulse.Models;

namespace ProxyPulse.UI
{
    public sealed class ProxyCardControl : Panel
    {
        private static readonly Color BgCard = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(120, 120, 120);
        private static readonly Color StripUnknown = Color.FromArgb(180, 186, 194);

        private static readonly Color PingGood = Color.FromArgb(39, 174, 96);
        private static readonly Color PingMedium = Color.FromArgb(230, 126, 34);
        private static readonly Color PingBad = Color.FromArgb(231, 76, 60);

        private readonly Panel _accentStrip;
        private readonly Label _addressLabel;
        private readonly Label _pingLabel;
        private readonly Button _btnRecheck;
        private readonly ToolTip _toolTip;
        private bool _hover;
        private Color _accentColor = StripUnknown;

        public ProxyEntry Entry { get; private set; }

        public event EventHandler ConnectClick;
        public event EventHandler RecheckClick;

        public ProxyCardControl()
        {
            Height = 48;
            Margin = new Padding(0, 0, 0, 6);
            BackColor = BgCard;
            Cursor = Cursors.Hand;
            Padding = new Padding(0);

            _toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 400,
                ReshowDelay = 200,
                ShowAlways = true
            };
            _toolTip.SetToolTip(this, "Нажмите, чтобы подключить в Telegram");

            _accentStrip = new Panel
            {
                Width = 5,
                Dock = DockStyle.Left,
                BackColor = StripUnknown
            };

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 6, 8, 6),
                BackColor = Color.Transparent
            };

            _addressLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(0, 2),
                Height = 18,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _pingLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(0, 22)
            };

            _btnRecheck = new Button
            {
                Text = "Проверить",
                Size = new Size(76, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(70, 70, 70),
                Font = new Font("Segoe UI", 8.25f),
                TabStop = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnRecheck.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 224);
            _btnRecheck.FlatAppearance.BorderSize = 1;
            _btnRecheck.Click += OnRecheckClick;
            _toolTip.SetToolTip(_btnRecheck, "Повторно проверить доступность");

            body.Controls.Add(_addressLabel);
            body.Controls.Add(_pingLabel);
            body.Controls.Add(_btnRecheck);

            Controls.Add(body);
            Controls.Add(_accentStrip);

            WireHover(this);
            WireHover(body);
            WireHover(_addressLabel);
            WireHover(_pingLabel);

            WireConnectArea(this);
            WireConnectArea(body);
            WireConnectArea(_addressLabel);
            WireConnectArea(_pingLabel);

            Resize += (_, __) => LayoutBody();
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
                _pingLabel.ForeColor = TextMuted;
                _accentColor = StripUnknown;
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
            _pingLabel.ForeColor = TextMuted;
            _accentStrip.BackColor = StripUnknown;
        }

        private void LayoutBody()
        {
            var w = Width > 0 ? Width - 16 : 380;
            _addressLabel.Width = Math.Max(120, w - 88);
            _btnRecheck.Location = new Point(Math.Max(0, w - 76), 6);
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
            BackColor = hover ? Blend(BgCard, _accentColor, 72) : BgCard;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_hover)
                return;

            using (var pen = new Pen(_accentColor, 2))
            {
                var r = ClientRectangle;
                r.Width -= 1;
                r.Height -= 1;
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        private void OnRecheckClick(object sender, EventArgs e)
        {
            if (RecheckClick != null)
                RecheckClick(this, EventArgs.Empty);
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
