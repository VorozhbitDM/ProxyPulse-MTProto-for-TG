using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProxyPulse.Models;
using ProxyPulse.Services;
using ProxyPulse.UI;

namespace ProxyPulse
{
    public sealed class MainForm : Form
    {
        private const string AppVersion = "2.4";
        private const string WelcomeTagline = "Ищем MTProto-прокси и проверяем доступность";
        private static readonly Color Accent = Color.FromArgb(42, 171, 238);
        private static readonly Color BgApp = Color.FromArgb(240, 243, 247);

        private readonly ProxyHealthService _healthService = new ProxyHealthService();

        private Panel _welcomePanel;
        private Panel _scanPanel;
        private ProgressBar _progressBar;
        private Label _progressLabel;
        private Label _statusLabel;
        private Button _btnStart;
        private Button _btnCancel;
        private Button _btnNewSearch;
        private Panel _footerPanel;
        private Panel _cardsHost;
        private FlowLayoutPanel _cardsFlow;
        private readonly Dictionary<string, ProxyCardControl> _cardsByKey = new Dictionary<string, ProxyCardControl>();

        private CancellationTokenSource _scanCts;
        private readonly Dictionary<string, ProxyEntry> _availableByKey = new Dictionary<string, ProxyEntry>();
        private readonly List<string> _sortedKeys = new List<string>();
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = string.Format("ProxyPulse {0}", AppVersion);
            MinimumSize = new Size(480, 560);
            Size = new Size(520, 680);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = BgApp;

            BuildWelcomePanel();
            BuildScanPanel();
            BuildFooter();
            _scanPanel.Visible = false;

            Controls.Add(_welcomePanel);
            Controls.Add(_scanPanel);
            Controls.Add(_footerPanel);
        }

        private void BuildWelcomePanel()
        {
            _welcomePanel = new Panel { Dock = DockStyle.Fill, BackColor = BgApp };

            const int cardWidth = 448;
            const int padH = 36;
            const int contentW = cardWidth - padH * 2;

            var card = new Panel
            {
                Width = cardWidth,
                BackColor = Color.White,
                Anchor = AnchorStyles.None
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var r = card.ClientRectangle;
                r.Width--;
                r.Height--;
                using (var pen = new Pen(Color.FromArgb(220, 226, 234)))
                    g.DrawRectangle(pen, r);
                using (var accent = new SolidBrush(Accent))
                    g.FillRectangle(accent, 0, 0, card.Width, 4);
            };

            var title = new Label
            {
                Text = "ProxyPulse",
                Font = new Font("Segoe UI", 30, FontStyle.Bold),
                ForeColor = Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(contentW, 52)
            };

            var desc = new Label
            {
                Text = WelcomeTagline,
                AutoSize = false,
                Size = new Size(contentW, 26),
                ForeColor = Color.FromArgb(95, 100, 108),
                Font = new Font("Segoe UI", 10.25f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnStart = new Button
            {
                Text = "Начать поиск",
                Size = new Size(contentW, 46),
                BackColor = Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += async (_, __) => await StartSearchAsync();

            var ver = new Label
            {
                Text = string.Format("v{0}", AppVersion),
                ForeColor = Color.FromArgb(170, 175, 182),
                AutoSize = false,
                Size = new Size(contentW, 18),
                Font = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            const int topPad = 32;
            var y = topPad;
            title.Location = new Point(padH, y);
            card.Controls.Add(title);
            y += title.Height + 12;

            desc.Location = new Point(padH, y);
            card.Controls.Add(desc);
            y += desc.Height + 22;

            _btnStart.Location = new Point(padH, y);
            card.Controls.Add(_btnStart);
            y += _btnStart.Height + 14;

            ver.Location = new Point(padH, y);
            card.Controls.Add(ver);
            y += ver.Height + 28;

            card.Height = y;

            void CenterCard()
            {
                card.Left = Math.Max(16, (_welcomePanel.Width - card.Width) / 2);
                card.Top = Math.Max(24, (_welcomePanel.Height - card.Height) / 2);
            }

            _welcomePanel.Resize += (_, __) => CenterCard();
            _welcomePanel.Controls.Add(card);
            CenterCard();
        }

        private void BuildFooter()
        {
            _footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = BgApp,
                Padding = new Padding(16, 0, 12, 6)
            };

            var links = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            links.Controls.Add(CreateLinkButton("Справка", (_, __) => new HelpForm().ShowDialog(this)));

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(110, 110, 110),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = ""
            };

            _footerPanel.Controls.Add(_statusLabel);
            _footerPanel.Controls.Add(links);
        }

        private void BuildScanPanel()
        {
            _scanPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgApp, Padding = new Padding(16) };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 8)
            };

            var headerStack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            headerStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 6f));
            headerStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 6,
                Style = ProgressBarStyle.Continuous,
                Margin = new Padding(0)
            };

            _progressLabel = new Label
            {
                Text = "Сканирование…",
                AutoSize = true,
                MaximumSize = new Size(2000, 0),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Margin = new Padding(0, 12, 0, 6),
                Padding = new Padding(0)
            };

            var btnRow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 0)
            };

            _btnCancel = CreateGhostButton("Прервать");
            _btnCancel.Click += (_, __) => { if (_scanCts != null) _scanCts.Cancel(); };

            _btnNewSearch = CreateGhostButton("Новый поиск");
            _btnNewSearch.Enabled = false;
            _btnNewSearch.Visible = false;
            _btnNewSearch.Click += async (_, __) => await StartSearchAsync(restart: true);

            btnRow.Controls.Add(_btnCancel);
            btnRow.Controls.Add(_btnNewSearch);

            headerStack.Controls.Add(_progressBar, 0, 0);
            headerStack.Controls.Add(_progressLabel, 0, 1);
            headerStack.Controls.Add(btnRow, 0, 2);

            header.Controls.Add(headerStack);

            _cardsHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgApp,
                Padding = new Padding(0, 8, 4, 0)
            };
            EnableDoubleBuffer(_cardsHost);

            _cardsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Width = 460,
                BackColor = BgApp
            };
            EnableDoubleBuffer(_cardsFlow);

            _cardsHost.Controls.Add(_cardsFlow);
            _cardsHost.Resize += (_, __) => _cardsFlow.Width = Math.Max(200, _cardsHost.ClientSize.Width - 8);

            _scanPanel.Controls.Add(_cardsHost);
            _scanPanel.Controls.Add(header);
        }

        private static Button CreateLinkButton(string text, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(42, 171, 238),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Underline)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private static Button CreateGhostButton(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(70, 70, 70),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(220, 224, 230);
            return b;
        }

        private static void EnableDoubleBuffer(Control c)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                c,
                new object[] { true });
        }

        private void SetSearchActive(bool active)
        {
            _btnCancel.Visible = active;
            _btnCancel.Enabled = active;
            _btnNewSearch.Visible = !active;
            _btnNewSearch.Enabled = !active;
        }

        private async Task StartSearchAsync(bool restart = false)
        {
            if (!restart)
                _welcomePanel.Visible = false;

            _scanPanel.Visible = true;
            ResetScanUi();
            _btnStart.Enabled = false;
            SetSearchActive(true);
            _statusLabel.Text = "";
            _progressLabel.Text = "Сканирование…";
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = 30;

            IProgress<string> fetchLog = new Progress<string>(SetActivityLine);

            ProxyFeedFetchResult feed;
            try
            {
                feed = await Task.Run(() => new ProxyFeedService().Fetch(fetchLog.Report))
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnStart.Enabled = true;
                _welcomePanel.Visible = true;
                _scanPanel.Visible = false;
                return;
            }

            if (feed.Proxies.Count == 0)
            {
                var details = feed.Errors.Count > 0
                    ? string.Join(Environment.NewLine, feed.Errors)
                    : "Список пуст.";
                MessageBox.Show(
                    this,
                    "Не удалось загрузить прокси.\r\n\r\n" + details,
                    "ProxyPulse",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _btnStart.Enabled = true;
                _welcomePanel.Visible = true;
                _scanPanel.Visible = false;
                return;
            }

            _progressBar.Style = ProgressBarStyle.Continuous;
            _statusLabel.Text = string.Format("Найдено {0} прокси", feed.Proxies.Count);
            _progressBar.Maximum = feed.Proxies.Count;
            _progressBar.Value = 0;
            _progressLabel.Text = string.Format("Проверено: 0 / {0}", feed.Proxies.Count);

            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            _healthService.ProgressChanged += OnProgressChanged;
            _healthService.ProxyChecking += OnProxyChecking;
            _healthService.ProxyChecked += OnProxyChecked;

            try
            {
                await _healthService.ScanAsync(feed.Proxies, token).ConfigureAwait(true);
                _statusLabel.Text = token.IsCancellationRequested
                    ? "Проверка прервана"
                    : string.Format("Готово · доступно {0} из {1}", _sortedKeys.Count, feed.Proxies.Count);
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = "Проверка прервана";
            }
            finally
            {
                _healthService.ProgressChanged -= OnProgressChanged;
                _healthService.ProxyChecking -= OnProxyChecking;
                _healthService.ProxyChecked -= OnProxyChecked;
                SetSearchActive(false);
                _btnStart.Enabled = true;
            }
        }

        private void ResetScanUi()
        {
            if (_scanCts != null)
            {
                _scanCts.Cancel();
                _scanCts.Dispose();
            }
            _scanCts = null;
            _availableByKey.Clear();
            _sortedKeys.Clear();
            _cardsByKey.Clear();
            _cardsFlow.Controls.Clear();
            _progressBar.Value = 0;
            _progressLabel.Text = "Сканирование…";
            _progressLabel.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _statusLabel.Text = "";
        }

        private void SetActivityLine(string line)
        {
            if (_statusLabel == null || string.IsNullOrEmpty(line))
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetActivityLine(line)));
                return;
            }

            _statusLabel.Text = line;
        }

        private void OnProxyChecking(object sender, ProxyCheckingEventArgs e)
        {
            if (e.Entry == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnProxyChecking(sender, e)));
                return;
            }

            _statusLabel.Text = string.Format("Проверка: {0}", e.Entry.DisplayLabel);
        }

        private void OnProgressChanged(object sender, ScanProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnProgressChanged(sender, e)));
                return;
            }

            _progressBar.Value = Math.Min(e.Completed, _progressBar.Maximum);
            _progressLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _progressLabel.Text = string.Format("Проверено: {0} / {1}", e.Completed, e.Total);
        }

        private void OnProxyChecked(object sender, ProxyCheckEventArgs e)
        {
            if (!e.IsAvailable)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnProxyChecked(sender, e)));
                return;
            }

            e.Entry.IsAvailable = true;
            e.Entry.PingMs = e.PingMs;
            InsertOrUpdateAvailable(e.Entry);
        }

        private void InsertOrUpdateAvailable(ProxyEntry entry)
        {
            if (_availableByKey.ContainsKey(entry.Key))
            {
                _availableByKey[entry.Key] = entry;
                _sortedKeys.Remove(entry.Key);
            }
            else
            {
                _availableByKey[entry.Key] = entry;
            }

            var ping = entry.PingMs ?? int.MaxValue;
            var insertAt = _sortedKeys.FindIndex(k =>
            {
                var p = _availableByKey[k].PingMs ?? int.MaxValue;
                return ping < p;
            });
            if (insertAt < 0)
                _sortedKeys.Add(entry.Key);
            else
                _sortedKeys.Insert(insertAt, entry.Key);

            RebuildCards();
        }

        private void RebuildCards()
        {
            _cardsFlow.SuspendLayout();
            _cardsFlow.Controls.Clear();
            _cardsByKey.Clear();

            var cardWidth = Math.Max(200, _cardsHost.ClientSize.Width - 12);
            foreach (var key in _sortedKeys)
            {
                var entry = _availableByKey[key];
                var card = new ProxyCardControl { Width = cardWidth };
                card.Bind(entry);
                card.ConnectClick += Card_ConnectClick;
                card.RecheckClick += Card_RecheckClick;
                _cardsByKey[key] = card;
                _cardsFlow.Controls.Add(card);
            }

            _cardsFlow.ResumeLayout(true);
        }

        private void Card_ConnectClick(object sender, EventArgs e)
        {
            var card = sender as ProxyCardControl;
            if (card == null || card.Entry == null)
                return;
            OpenTelegram(card.Entry);
        }

        private async void Card_RecheckClick(object sender, EventArgs e)
        {
            var card = sender as ProxyCardControl;
            if (card == null || card.Entry == null)
                return;

            card.SetRechecking();
            try
            {
                var result = await _healthService
                    .CheckOneAsync(card.Entry, CancellationToken.None)
                    .ConfigureAwait(true);

                card.Entry.IsAvailable = result.IsAvailable;
                card.Entry.PingMs = result.PingMs;

                if (result.IsAvailable)
                {
                    if (_availableByKey.ContainsKey(card.Entry.Key))
                    {
                        _sortedKeys.Remove(card.Entry.Key);
                        InsertOrUpdateAvailable(card.Entry);
                    }
                    else
                    {
                        InsertOrUpdateAvailable(card.Entry);
                    }
                }
                else
                {
                    _availableByKey.Remove(card.Entry.Key);
                    _sortedKeys.Remove(card.Entry.Key);
                    RebuildCards();
                    MessageBox.Show(this, "Прокси недоступен.", "ProxyPulse",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                card.UpdatePing();
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenTelegram(ProxyEntry entry)
        {
            try
            {
                TelegramLauncher.OpenProxy(entry);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "ProxyPulse",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
