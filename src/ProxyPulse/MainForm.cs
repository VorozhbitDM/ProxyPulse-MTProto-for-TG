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
        private static readonly string AppVersion = GetAppVersionLabel();
        private const string WelcomeTagline = "Ищем MTProto-прокси и проверяем доступность";
        private readonly ProxyHealthService _healthService = new ProxyHealthService();
        private readonly AppSettings _settings = AppSettings.Current;

        private Panel _welcomePanel;
        private Panel _scanPanel;
        private ProgressBar _progressBar;
        private Label _progressLabel;
        private Label _statusLabel;
        private Label _foundCountLabel;
        private Button _btnStart;
        private Button _btnCancel;
        private Button _btnNewSearch;
        private Panel _topToolbar;
        private Panel _footerPanel;
        private ToolTip _appToolTip;
        private Label _toolbarTitle;
        private Button _btnClose;
        private Button _btnMinimize;
        private Button _btnToolbarSettings;
        private Button _btnToolbarHelp;
        private Panel _welcomeCard;
        private Label _welcomeTitleLabel;
        private Label _welcomeDescLabel;
        private Label _welcomeVerLabel;
        private ThemedScrollPanel _cardsHost;
        private FlowLayoutPanel _cardsFlow;
        private readonly Dictionary<string, ProxyCardControl> _cardsByKey = new Dictionary<string, ProxyCardControl>();

        private CancellationTokenSource _scanCts;
        private readonly Dictionary<string, ProxyEntry> _availableByKey = new Dictionary<string, ProxyEntry>();
        private readonly List<string> _sortedKeys = new List<string>();
        private int _discoveredCount;
        private int _checkedCount;
        private int _proxiesTarget;
        private int _collectProxiesFound;
        private volatile bool _fetchComplete;
        private volatile bool _searchCancelled;
        private int _finalDiscovered;
        public MainForm()
        {
            InitializeComponent();
        }

        private static string GetAppVersionLabel()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? string.Format("{0}.{1}", v.Major, v.Minor) : "2.8";
        }

        private void InitializeComponent()
        {
            AppFonts.EnsureInitialized();
            AppTheme.ApplyFromSettings();
            Text = string.Format("ProxyPulse {0}", AppVersion);
            MinimumSize = new Size(480, 560);
            Size = new Size(520, 680);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            Padding = new Padding(1);
            Font = AppFonts.Ui;

            BuildTopToolbar();
            BuildWelcomePanel();
            BuildScanPanel();
            BuildFooter();
            _scanPanel.Visible = false;

            Controls.Add(_welcomePanel);
            Controls.Add(_scanPanel);
            Controls.Add(_footerPanel);
            Controls.Add(_topToolbar);

            ApplyTheme();

            AppBranding.ApplyWindowIcon(this);
            FormClosed += (_, __) =>
            {
                if (_appToolTip != null)
                    _appToolTip.Dispose();
            };
        }

        private void BuildWelcomePanel()
        {
            _welcomePanel = new Panel { Dock = DockStyle.Fill };

            const int cardWidth = 448;
            const int padH = 36;
            const int contentW = cardWidth - padH * 2;

            _welcomeCard = new Panel
            {
                Width = cardWidth,
                Anchor = AnchorStyles.None
            };
            _welcomeCard.Paint += (_, e) =>
            {
                var theme = AppTheme.Current;
                var g = e.Graphics;
                var r = _welcomeCard.ClientRectangle;
                r.Width--;
                r.Height--;
                using (var pen = new Pen(theme.Border))
                    g.DrawRectangle(pen, r);
                using (var accent = new SolidBrush(theme.Accent))
                    g.FillRectangle(accent, 0, 0, _welcomeCard.Width, 4);
            };

            _welcomeTitleLabel = new Label
            {
                Text = "ProxyPulse",
                Font = AppFonts.WelcomeTitle,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(contentW, 52)
            };

            _welcomeDescLabel = new Label
            {
                Text = WelcomeTagline,
                AutoSize = false,
                Size = new Size(contentW, 26),
                Font = AppFonts.WelcomeBody,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnStart = new Button
            {
                Text = "Начать поиск",
                Size = new Size(contentW, 46),
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.WelcomeAction,
                Cursor = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += async (_, __) => await StartSearchAsync();

            _welcomeVerLabel = new Label
            {
                Text = string.Format("v{0}", AppVersion),
                AutoSize = false,
                Size = new Size(contentW, 18),
                Font = AppFonts.UiSmall,
                TextAlign = ContentAlignment.MiddleCenter
            };

            const int topPad = 32;
            var y = topPad;
            _welcomeTitleLabel.Location = new Point(padH, y);
            _welcomeCard.Controls.Add(_welcomeTitleLabel);
            y += _welcomeTitleLabel.Height + 12;

            _welcomeDescLabel.Location = new Point(padH, y);
            _welcomeCard.Controls.Add(_welcomeDescLabel);
            y += _welcomeDescLabel.Height + 22;

            _btnStart.Location = new Point(padH, y);
            _welcomeCard.Controls.Add(_btnStart);
            y += _btnStart.Height + 14;

            _welcomeVerLabel.Location = new Point(padH, y);
            _welcomeCard.Controls.Add(_welcomeVerLabel);
            y += _welcomeVerLabel.Height + 28;

            _welcomeCard.Height = y;

            void CenterCard()
            {
                _welcomeCard.Left = Math.Max(16, (_welcomePanel.Width - _welcomeCard.Width) / 2);
                _welcomeCard.Top = Math.Max(24, (_welcomePanel.Height - _welcomeCard.Height) / 2);
            }

            _welcomePanel.Resize += (_, __) => CenterCard();
            _welcomePanel.Controls.Add(_welcomeCard);
            CenterCard();
        }

        private void ShowSettings()
        {
            using (var dlg = new SettingsForm(_settings))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                _settings.MaxProxiesToCollect = dlg.MaxProxiesToCollect;
                _settings.UseDarkTheme = dlg.UseDarkTheme;
                _settings.Save();
                ApplyTheme();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            var t = AppTheme.Current;
            var w = ClientSize.Width;
            if (w <= 0)
                return;

            using (var brush = new SolidBrush(t.WindowBorder))
                e.Graphics.FillRectangle(brush, 0, 0, w, 1);
        }

        private void ApplyTheme()
        {
            AppTheme.ApplyFromSettings();
            var t = AppTheme.Current;

            BackColor = t.WindowBorder;
            Invalidate(true);
            _welcomePanel.BackColor = t.BgApp;
            _scanPanel.BackColor = t.BgApp;
            _cardsHost.ApplyTheme(AppSettings.Current.UseDarkTheme);
            _cardsFlow.BackColor = t.BgApp;
            _footerPanel.BackColor = t.BgApp;
            _statusLabel.ForeColor = t.TextMuted;
            _topToolbar.BackColor = t.BgSurface;
            _topToolbar.Invalidate();
            _toolbarTitle.ForeColor = t.Accent;

            if (_welcomeCard != null)
            {
                _welcomeCard.BackColor = t.BgCard;
                _welcomeCard.Invalidate();
            }

            _welcomeTitleLabel.ForeColor = t.Accent;
            _welcomeDescLabel.ForeColor = t.WelcomeDesc;
            _welcomeVerLabel.ForeColor = t.WelcomeVersion;
            _btnStart.UseVisualStyleBackColor = false;
            _btnStart.FlatStyle = FlatStyle.Flat;
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.BackColor = t.Accent;
            _btnStart.ForeColor = t.AccentButtonFore;

            _progressLabel.ForeColor = t.TextSecondary;
            _foundCountLabel.ForeColor = t.TextSecondary;

            StyleToolbarLinkButton(_btnToolbarSettings);
            StyleToolbarLinkButton(_btnToolbarHelp);
            StyleSecondaryButton(_btnCancel);
            StyleSecondaryButton(_btnNewSearch);
            StyleChromeButton(_btnMinimize);
            StyleCloseButton(_btnClose);

            foreach (var card in _cardsByKey.Values)
                card.ApplyTheme();

            WindowCaptionTheme.ApplyMainWindow(this, AppSettings.Current.UseDarkTheme);
        }

        private static void StyleSecondaryButton(Button b)
        {
            if (b == null)
                return;

            AppTheme.StyleSecondaryButton(b);
            if (b.Enabled)
                return;

            AppTheme.StyleDisabledButton(b);
        }

        private static void StyleToolbarLinkButton(Button b)
        {
            if (b == null)
                return;

            AppTheme.StyleToolbarLinkButton(b);
            var t = AppTheme.Current;
            if (!b.Enabled)
            {
                AppTheme.StyleDisabledButton(b);
                return;
            }

            b.ForeColor = t.TextMuted;
        }

        private static void StyleChromeButton(Button b)
        {
            if (b == null)
                return;

            var t = AppTheme.Current;
            b.UseVisualStyleBackColor = false;
            b.BackColor = t.BtnSurface;
            b.ForeColor = t.ChromeButtonFore;
            b.FlatAppearance.BorderColor = t.BtnBorder;
            b.Tag = "chrome";
        }

        private static void StyleCloseButton(Button b)
        {
            if (b == null)
                return;

            var t = AppTheme.Current;
            b.UseVisualStyleBackColor = false;
            b.BackColor = t.CloseButtonBg;
            b.ForeColor = t.CloseButtonFore;
            b.FlatAppearance.BorderColor = t.CloseButtonFore;
            b.Tag = "close";
        }

        private void BuildTopToolbar()
        {
            _topToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(14, 10, 12, 12)
            };
            _topToolbar.Paint += (_, e) =>
            {
                var bottom = _topToolbar.ClientRectangle.Bottom - 1;
                using (var pen = new Pen(AppTheme.Current.Border))
                    e.Graphics.DrawLine(pen, 0, bottom, _topToolbar.ClientRectangle.Right, bottom);
            };
            WindowChrome.WireDrag(_topToolbar, this);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, 1)
            };

            _btnToolbarSettings = CreateToolbarLinkButton("Настройки", (_, __) => ShowSettings());
            _btnToolbarHelp = CreateToolbarLinkButton("Справка", (_, __) => new HelpForm().ShowDialog(this));
            _btnMinimize = CreateChromeButton("—", (_, __) => WindowState = FormWindowState.Minimized);
            _btnClose = CreateCloseButton((_, __) => Close());
            actions.Controls.Add(_btnToolbarSettings);
            actions.Controls.Add(_btnToolbarHelp);
            actions.Controls.Add(_btnMinimize);
            actions.Controls.Add(_btnClose);

            _toolbarTitle = new Label
            {
                Text = string.Format("ProxyPulse {0}", AppVersion),
                Dock = DockStyle.Fill,
                Font = AppFonts.TitleBar,
                TextAlign = ContentAlignment.MiddleLeft
            };
            WindowChrome.WireDrag(_toolbarTitle, this);

            _appToolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 200 };
            _appToolTip.SetToolTip(_btnToolbarSettings, "Лимит прокси и параметры поиска");
            _appToolTip.SetToolTip(_btnToolbarHelp, "Как пользоваться ProxyPulse");
            _appToolTip.SetToolTip(_btnMinimize, "Свернуть");
            _appToolTip.SetToolTip(_btnClose, "Закрыть");

            _topToolbar.Controls.Add(actions);
            _topToolbar.Controls.Add(_toolbarTitle);
        }

        private static Button CreateToolbarLinkButton(string text, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 26,
                MinimumSize = new Size(0, 26),
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0, 0, 2, 0),
                Font = AppFonts.ToolbarLink,
                Cursor = Cursors.Hand,
                TabStop = true,
                Tag = "toolbar-link"
            };
            StyleToolbarLinkButton(b);
            WireToolbarLinkHover(b);
            b.Click += onClick;
            return b;
        }

        private static Button CreateChromeButton(string text, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(34, 30),
                MinimumSize = new Size(34, 30),
                FlatStyle = FlatStyle.Flat,
                Font = AppFonts.ButtonCompact,
                Margin = new Padding(4, 0, 0, 0),
                Cursor = Cursors.Hand,
                TabStop = false,
                Tag = "chrome"
            };
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 1;
            WireChromeButtonHover(b);
            b.Click += onClick;
            return b;
        }

        private static Button CreateCloseButton(EventHandler onClick)
        {
            var b = new Button
            {
                Text = "✕",
                Size = new Size(34, 30),
                MinimumSize = new Size(34, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    AppFonts.ButtonCompact.FontFamily,
                    AppFonts.ButtonCompact.Size,
                    FontStyle.Bold,
                    GraphicsUnit.Point),
                Margin = new Padding(4, 0, 0, 0),
                Cursor = Cursors.Hand,
                TabStop = false,
                Tag = "close"
            };
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 1;
            WireCloseButtonHover(b);
            b.Click += onClick;
            return b;
        }

        private static void WireToolbarLinkHover(Button b)
        {
            b.MouseEnter += (_, __) =>
            {
                if (!b.Enabled)
                    return;

                var t = AppTheme.Current;
                b.ForeColor = t.TextSecondary;
            };
            b.MouseLeave += (_, __) => StyleToolbarLinkButton(b);
        }

        private static void WireChromeButtonHover(Button b)
        {
            b.MouseEnter += (_, __) =>
            {
                var t = AppTheme.Current;
                b.BackColor = t.ChromeButtonHoverBg;
                b.ForeColor = t.ChromeButtonHoverFore;
            };
            b.MouseLeave += (_, __) => StyleChromeButton(b);
        }

        private static void WireCloseButtonHover(Button b)
        {
            b.MouseEnter += (_, __) =>
            {
                var t = AppTheme.Current;
                b.BackColor = t.CloseButtonHoverBg;
                b.ForeColor = t.CloseButtonHoverFore;
                b.FlatAppearance.BorderColor = t.CloseButtonHoverBg;
            };
            b.MouseLeave += (_, __) => StyleCloseButton(b);
        }

        private void BuildFooter()
        {
            _footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(16, 4, 16, 6)
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = ""
            };

            _footerPanel.Controls.Add(_statusLabel);
        }

        private void BuildScanPanel()
        {
            _scanPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 10)
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
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Font = AppFonts.Progress,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _foundCountLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Font = AppFonts.ProgressBold,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "",
                Visible = false,
                Margin = new Padding(12, 0, 0, 0)
            };

            var progressTextRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Margin = new Padding(0, 10, 0, 4),
                BackColor = Color.Transparent
            };
            progressTextRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            progressTextRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            progressTextRow.Controls.Add(_progressLabel, 0, 0);
            progressTextRow.Controls.Add(_foundCountLabel, 1, 0);

            var btnRow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 8)
            };

            _btnCancel = CreateGhostButton("Прервать");
            _btnCancel.Click += (_, __) => RequestCancelSearch();

            _btnNewSearch = CreateGhostButton("Новый поиск");
            _btnNewSearch.Enabled = false;
            _btnNewSearch.Visible = false;
            _btnNewSearch.Click += async (_, __) => await StartSearchAsync(restart: true);

            btnRow.Controls.Add(_btnCancel);
            btnRow.Controls.Add(_btnNewSearch);

            headerStack.Controls.Add(_progressBar, 0, 0);
            headerStack.Controls.Add(progressTextRow, 0, 1);
            headerStack.Controls.Add(btnRow, 0, 2);

            header.Controls.Add(headerStack);

            _cardsHost = new ThemedScrollPanel
            {
                Padding = new Padding(0, 8, 0, 0)
            };

            _cardsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Width = 460,
            };
            EnableDoubleBuffer(_cardsFlow);

            _cardsHost.SetContent(_cardsFlow);
            _cardsHost.Resize += (_, __) => LayoutProxyCardsWidth();
            _cardsHost.Viewport.Resize += (_, __) => LayoutProxyCardsWidth();

            _scanPanel.Controls.Add(_cardsHost);
            _scanPanel.Resize += (_, __) => LayoutProxyCardsWidth();
            _scanPanel.Controls.Add(header);
        }

        private static Button CreateGhostButton(string text)
        {
            return CreateSecondaryButton(text, null, compact: false);
        }

        private static Button CreateSecondaryButton(string text, EventHandler onClick, bool compact)
        {
            var height = compact ? 30 : 34;
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                Height = height,
                MinimumSize = new Size(0, height),
                Padding = new Padding(compact ? 12 : 14, 0, compact ? 12 : 14, 0),
                Font = compact ? AppFonts.ButtonCompact : AppFonts.Button,
                Margin = new Padding(0, 0, compact ? 6 : 8, compact ? 1 : 2),
                Cursor = Cursors.Hand,
                TabStop = true,
                Tag = "secondary"
            };
            StyleSecondaryButton(b);
            WireSecondaryButtonHover(b);

            if (onClick != null)
                b.Click += onClick;

            return b;
        }

        private static void WireSecondaryButtonHover(Button b)
        {
            b.MouseEnter += (_, __) =>
            {
                if (!b.Enabled)
                    return;

                b.BackColor = AppTheme.Current.BtnHover;
            };
            b.MouseLeave += (_, __) => StyleSecondaryButton(b);
            b.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left && b.Enabled)
                    b.BackColor = AppTheme.Current.BtnPressed;
            };
            b.MouseUp += (_, __) =>
            {
                if (!b.Enabled)
                    return;

                var t = AppTheme.Current;
                var hover = b.ClientRectangle.Contains(b.PointToClient(Cursor.Position));
                b.BackColor = hover ? t.BtnHover : t.BtnSurface;
            };
            b.EnabledChanged += (_, __) => StyleSecondaryButton(b);
            b.VisibleChanged += (_, __) =>
            {
                if (b.Visible)
                    StyleSecondaryButton(b);
            };
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
            StyleSecondaryButton(_btnCancel);
            StyleSecondaryButton(_btnNewSearch);
        }

        private async Task StartSearchAsync(bool restart = false)
        {
            if (!restart)
                _welcomePanel.Visible = false;

            _scanPanel.Visible = true;
            ResetScanUi();
            _btnStart.Enabled = false;
            SetSearchActive(true);
            _statusLabel.Text = "Подключение…";
            _proxiesTarget = _settings.MaxProxiesToCollect;
            _collectProxiesFound = 0;
            _fetchComplete = false;
            _searchCancelled = false;
            _finalDiscovered = 0;
            _discoveredCount = 0;
            _checkedCount = 0;

            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            _progressBar.Value = 0;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressLabel.Font = AppFonts.Progress;
            UpdateOverallProgress();

            IProgress<string> fetchLog = new Progress<string>(SetActivityLine);
            var collectProgress = new Progress<CollectProgress>(OnCollectProgress);

            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;
            var collected = new List<ProxyEntry>();

            _healthService.ProxyChecking += OnProxyChecking;

            try
            {
                await Task.Run(
                    () => new ProxyFeedService().FetchToList(
                        collected,
                        fetchLog.Report,
                        collectProgress,
                        token,
                        _proxiesTarget,
                        ProxyFeedService.DefaultMaxCdxSnapshotsToScan),
                    token).ConfigureAwait(true);

                _fetchComplete = true;
                _finalDiscovered = collected.Count;
                _discoveredCount = collected.Count;
                _collectProxiesFound = collected.Count;
                UpdateFoundCountLabel();
                UpdateOverallProgress();

                if (collected.Count > 0)
                    await CheckProxiesAsync(collected, token).ConfigureAwait(true);

                if (_discoveredCount == 0)
                {
                    MessageBox.Show(
                        this,
                        "Не удалось загрузить прокси. Проверьте интернет и доступ к web.archive.org.",
                        "ProxyPulse",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _btnStart.Enabled = true;
                    _welcomePanel.Visible = true;
                    _scanPanel.Visible = false;
                    return;
                }

                if (token.IsCancellationRequested)
                    ShowSearchPaused();
                else
                {
                    _progressBar.Value = 100;
                    _statusLabel.Text = string.Format("Готово · {0} прокси", _sortedKeys.Count);
                    _progressLabel.Text = FormatProgressText();
                }
            }
            catch (OperationCanceledException)
            {
                ShowSearchPaused();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnStart.Enabled = true;
                _welcomePanel.Visible = true;
                _scanPanel.Visible = false;
                return;
            }
            finally
            {
                _healthService.ProxyChecking -= OnProxyChecking;
                SetSearchActive(false);
                _btnStart.Enabled = true;
            }
        }

        private void OnCollectProgress(CollectProgress p)
        {
            _proxiesTarget = Math.Max(1, p.ProxiesTarget);
            _collectProxiesFound = p.ProxiesFound;
            UpdateOverallProgress();
        }

        private async Task CheckProxiesAsync(List<ProxyEntry> proxies, CancellationToken token)
        {
            for (var i = 0; i < proxies.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                var entry = proxies[i];
                var result = await _healthService.CheckOneAsync(entry, token).ConfigureAwait(true);
                _checkedCount = i + 1;
                UpdateOverallProgress();

                if (result.IsAvailable)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => ApplyAvailableResult(result)));
                    else
                        ApplyAvailableResult(result);
                }
            }
        }

        private void ApplyAvailableResult(ProxyCheckEventArgs result)
        {
            result.Entry.IsAvailable = true;
            result.Entry.PingMs = result.PingMs;
            InsertOrUpdateAvailable(result.Entry);
            UpdateFoundCountLabel();
        }

        private void UpdateFoundCountLabel()
        {
            if (_foundCountLabel == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateFoundCountLabel));
                return;
            }

            var show = _scanPanel.Visible && (
                (_fetchComplete && !_searchCancelled)
                || (_searchCancelled && _sortedKeys.Count > 0));
            _foundCountLabel.Visible = show;
            if (show)
                _foundCountLabel.Text = string.Format("Найдено {0}", _sortedKeys.Count);
        }

        private void RequestCancelSearch()
        {
            if (_scanCts == null || _searchCancelled)
                return;

            _searchCancelled = true;
            _scanCts.Cancel();
            ShowSearchPaused();
        }

        private void ShowSearchPaused()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowSearchPaused));
                return;
            }

            _btnCancel.Enabled = false;
            StyleSecondaryButton(_btnCancel);
            _progressBar.Value = 0;
            _progressLabel.Text = "Поиск приостановлен";
            _statusLabel.Text = "Поиск приостановлен";
            UpdateFoundCountLabel();
        }

        private string FormatProgressText()
        {
            var checkedN = _checkedCount;
            var total = Math.Max(1, _finalDiscovered);

            if (_searchCancelled)
                return "Поиск приостановлен";

            if (!_fetchComplete)
            {
                if (_collectProxiesFound == 0)
                    return "Подключение…";

                return string.Format(
                    "Сбор… {0} из {1}",
                    _collectProxiesFound,
                    _proxiesTarget);
            }

            if (checkedN >= total)
                return string.Format("Проверка завершена {0}/{1}", checkedN, total);

            return string.Format("Проверка… {0}/{1}", checkedN, total);
        }

        /// <summary>0–50% сбор страниц/снимков, 50–100% проверка найденных прокси.</summary>
        private void UpdateOverallProgress()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateOverallProgress));
                return;
            }

            if (_searchCancelled)
            {
                ShowSearchPaused();
                return;
            }

            var checkedN = _checkedCount;
            int percent;

            if (!_fetchComplete)
            {
                var cap = Math.Max(1, _proxiesTarget);
                percent = (int)Math.Min(50, 50.0 * _collectProxiesFound / cap);
            }
            else
            {
                var total = Math.Max(1, _finalDiscovered);
                percent = 50 + (int)Math.Min(50, 50.0 * checkedN / total);
            }

            _progressBar.Value = Math.Max(0, Math.Min(100, percent));
            _progressLabel.Text = FormatProgressText();
            if (_fetchComplete)
                UpdateFoundCountLabel();
        }

        private void ResetScanUi()
        {
            if (_scanCts != null)
            {
                _scanCts.Cancel();
                _scanCts.Dispose();
            }
            _scanCts = null;
            _fetchComplete = false;
            _searchCancelled = false;
            _finalDiscovered = 0;
            _discoveredCount = 0;
            _checkedCount = 0;
            _proxiesTarget = _settings.MaxProxiesToCollect;
            _collectProxiesFound = 0;
            _availableByKey.Clear();
            _sortedKeys.Clear();
            _cardsByKey.Clear();
            _cardsFlow.Controls.Clear();
            _progressBar.Value = 0;
            _progressLabel.Text = "Сканирование…";
            _progressLabel.Font = AppFonts.Progress;
            _statusLabel.Text = "";
            if (_foundCountLabel != null)
            {
                _foundCountLabel.Text = "";
                _foundCountLabel.Visible = false;
            }
        }

        private void SetActivityLine(string line)
        {
            if (_statusLabel == null || string.IsNullOrEmpty(line) || _searchCancelled)
                return;

            if (_fetchComplete)
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
            if (e.Entry == null || _searchCancelled || !_fetchComplete)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnProxyChecking(sender, e)));
                return;
            }

            _statusLabel.Text = string.Format("Проверка: {0}", e.Entry.DisplayLabel);
            UpdateFoundCountLabel();
        }

        private void InsertOrUpdateAvailable(ProxyEntry entry)
        {
            var isNew = !_availableByKey.ContainsKey(entry.Key);
            _availableByKey[entry.Key] = entry;

            if (!isNew)
            {
                _sortedKeys.Remove(entry.Key);
                if (_cardsByKey.ContainsKey(entry.Key))
                    _cardsByKey[entry.Key].Bind(entry);
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

            if (isNew)
                AddCardAt(entry, insertAt < 0 ? _sortedKeys.Count - 1 : insertAt);
            else
                ReorderCard(entry.Key);
        }

        private int GetProxyCardWidth()
        {
            if (_cardsHost == null)
                return 460;
            return Math.Max(200, _cardsHost.Viewport.ClientSize.Width - 12);
        }

        private void LayoutProxyCardsWidth()
        {
            if (_cardsFlow == null || _cardsHost == null)
                return;

            _cardsFlow.Width = Math.Max(200, _cardsHost.Viewport.ClientSize.Width - 8);
            _cardsHost.RefreshScrollMetrics();
            var cardWidth = GetProxyCardWidth();
            foreach (Control c in _cardsFlow.Controls)
                c.Width = cardWidth;
        }

        private void AddCardAt(ProxyEntry entry, int index)
        {
            var card = new ProxyCardControl { Width = GetProxyCardWidth() };
            card.Bind(entry);
            card.ConnectClick += Card_ConnectClick;
            card.RecheckClick += Card_RecheckClick;
            _cardsByKey[entry.Key] = card;
            _cardsFlow.Controls.Add(card);
            _cardsFlow.Controls.SetChildIndex(card, Math.Min(index, _cardsFlow.Controls.Count - 1));
            _cardsHost.RefreshScrollMetrics();
        }

        private void ReorderCard(string key)
        {
            if (!_cardsByKey.ContainsKey(key))
            {
                RebuildCards();
                return;
            }

            var card = _cardsByKey[key];
            var index = _sortedKeys.IndexOf(key);
            if (index >= 0)
                _cardsFlow.Controls.SetChildIndex(card, index);
        }

        private void RebuildCards()
        {
            _cardsFlow.SuspendLayout();
            _cardsFlow.Controls.Clear();
            _cardsByKey.Clear();

            LayoutProxyCardsWidth();
            var cardWidth = GetProxyCardWidth();
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
            _cardsHost.RefreshScrollMetrics();
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
                    UpdateFoundCountLabel();
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

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            BorderlessResize.AfterWndProc(this, ref m);
        }
    }
}
