using System;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProxyPulse.Services;
using ProxyPulse.UI;

namespace ProxyPulse
{
    public sealed class SettingsForm : Form
    {
        private readonly NumericUpDown _numMaxProxies;
        private readonly ThemeToggleControl _themeToggle;
        private readonly ComboBox _cmbFeedSource;
        private readonly Label _lblFeedHint;
        private readonly Label _lblMax;
        private readonly Label _lblRange;
        private readonly Label _title;
        private readonly Label _hint;
        private readonly Label _lblTgStatTitle;
        private readonly Label _lblTgStatHint;
        private readonly Label _lblTgStatStatus;
        private readonly TextBox _txtTgStatCookies;
        private readonly Button _btnTgStatOpen;
        private readonly Button _btnTgStatTest;
        private readonly Button _btnTgStatClear;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public SettingsForm(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            AppFonts.EnsureInitialized();
            AppTheme.ApplyFromSettings();

            Text = "Настройки";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 594);
            Font = AppFonts.Ui;
            AppBranding.ApplyWindowIcon(this);

            _title = new Label
            {
                Text = "Параметры",
                Font = AppFonts.DialogHeading,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            _hint = new Label
            {
                Text = "Лимит прокси, источник ленты и оформление.",
                Location = new Point(24, 48),
                Size = new Size(372, 22)
            };

            _themeToggle = new ThemeToggleControl
            {
                Location = new Point(24, 78)
            };
            _themeToggle.SetTheme(settings.UseDarkTheme, raiseEvent: false);
            _themeToggle.ThemeChanged += (_, __) => ApplyDialogTheme();

            _lblMax = new Label
            {
                Text = "Лимит прокси:",
                AutoSize = true,
                Location = new Point(24, 122)
            };

            _numMaxProxies = new NumericUpDown
            {
                Location = new Point(140, 119),
                Size = new Size(90, 24),
                Minimum = AppSettings.MinMaxProxiesToCollect,
                Maximum = AppSettings.MaxMaxProxiesToCollect,
                Value = AppSettings.Clamp(settings.MaxProxiesToCollect),
                ThousandsSeparator = true
            };

            _lblRange = new Label
            {
                Text = string.Format("от {0} до {1}", AppSettings.MinMaxProxiesToCollect, AppSettings.MaxMaxProxiesToCollect),
                AutoSize = true,
                Location = new Point(240, 124),
                Font = AppFonts.DialogHint
            };

            _cmbFeedSource = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(24, 162),
                Size = new Size(372, 24)
            };
            _cmbFeedSource.Items.AddRange(new object[]
            {
                "Прямой путь — t.me",
                "Обходной путь — TGStat + Archive"
            });
            _cmbFeedSource.SelectedIndex = FeedSourceToIndex(settings.FeedSource);

            _lblFeedHint = new Label
            {
                Text = GetFeedHint(settings.FeedSource),
                Location = new Point(24, 192),
                Size = new Size(372, 40),
                Font = AppFonts.DialogHint
            };
            _cmbFeedSource.SelectedIndexChanged += (_, __) =>
            {
                _lblFeedHint.Text = GetFeedHint(IndexToFeedSource(_cmbFeedSource.SelectedIndex));
                UpdateTgStatSectionVisibility();
            };

            _lblTgStatTitle = new Label
            {
                Text = "TGStat — доступ к полной ленте канала",
                Location = new Point(24, 238),
                Size = new Size(372, 36),
                Font = AppFonts.DialogHeading
            };

            _lblTgStatHint = new Label
            {
                Text = GetTgStatStepsHint(),
                Location = new Point(24, 276),
                Size = new Size(372, 108),
                Font = AppFonts.DialogHint
            };

            _txtTgStatCookies = new TextBox
            {
                Location = new Point(24, 390),
                Size = new Size(372, 56),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Text = TgStatSessionStore.CookieHeader
            };
            _txtTgStatCookies.TextChanged += (_, __) => UpdateTgStatStatusPreview();

            _lblTgStatStatus = new Label
            {
                AutoSize = true,
                Location = new Point(24, 452),
                Font = AppFonts.DialogHint
            };

            _btnTgStatOpen = new Button
            {
                Text = "Войти в Telegram",
                Location = new Point(24, 476),
                Size = new Size(132, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTgStatOpen.FlatAppearance.BorderSize = 0;
            _btnTgStatOpen.Click += OnTgStatTelegramLoginClick;

            _btnTgStatTest = new Button
            {
                Text = "Проверить",
                Location = new Point(164, 476),
                Size = new Size(96, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTgStatTest.FlatAppearance.BorderSize = 0;
            _btnTgStatTest.Click += OnTgStatTestClick;

            _btnTgStatClear = new Button
            {
                Text = "Очистить",
                Location = new Point(268, 476),
                Size = new Size(88, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTgStatClear.FlatAppearance.BorderSize = 0;
            _btnTgStatClear.Click += (_, __) =>
            {
                _txtTgStatCookies.Clear();
                UpdateTgStatStatusPreview();
            };

            _btnOk = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Location = new Point(216, 542),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnOk.FlatAppearance.BorderSize = 0;

            _btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(324, 542),
                Size = new Size(76, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Controls.Add(_title);
            Controls.Add(_hint);
            Controls.Add(_themeToggle);
            Controls.Add(_lblMax);
            Controls.Add(_numMaxProxies);
            Controls.Add(_lblRange);
            Controls.Add(_cmbFeedSource);
            Controls.Add(_lblFeedHint);
            Controls.Add(_lblTgStatTitle);
            Controls.Add(_lblTgStatHint);
            Controls.Add(_txtTgStatCookies);
            Controls.Add(_lblTgStatStatus);
            Controls.Add(_btnTgStatOpen);
            Controls.Add(_btnTgStatTest);
            Controls.Add(_btnTgStatClear);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            UpdateTgStatSectionVisibility();
            ApplyDialogTheme();
        }

        public int MaxProxiesToCollect
        {
            get { return (int)_numMaxProxies.Value; }
        }

        public bool UseDarkTheme
        {
            get { return _themeToggle.IsDarkTheme; }
        }

        public FeedSourceMode FeedSource
        {
            get { return IndexToFeedSource(_cmbFeedSource.SelectedIndex); }
        }

        public string TgStatCookieHeader
        {
            get { return _txtTgStatCookies.Text != null ? _txtTgStatCookies.Text.Trim() : string.Empty; }
        }

        private async void OnTgStatTelegramLoginClick(object sender, EventArgs e)
        {
            _btnTgStatOpen.Enabled = false;
            _btnTgStatTest.Enabled = false;
            UseWaitCursor = true;
            _lblTgStatStatus.Text = "Подключение к TGStat…";

            try
            {
                var start = await Task.Run(() => TgStatAuthService.StartTelegramLogin(CancellationToken.None));
                if (!start.Success)
                {
                    MessageBox.Show(this, start.Message, "TGStat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string openError;
                if (!TgStatAuthService.TryOpenTelegramBot(start.AuthKey, out openError))
                {
                    MessageBox.Show(
                        this,
                        openError + Environment.NewLine + Environment.NewLine + start.TelegramDeepLink,
                        "Telegram",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _lblTgStatStatus.Text = start.Message;
                var result = await Task.Run(() => TgStatAuthService.WaitForTelegramAuth(
                    start.Cookies,
                    start.AuthKey,
                    message =>
                    {
                        if (IsDisposed)
                            return;

                        try
                        {
                            BeginInvoke(new Action(() => _lblTgStatStatus.Text = message));
                        }
                        catch
                        {
                        }
                    },
                    CancellationToken.None));

                if (result.IsAuthenticated)
                {
                    var header = TgStatCookieParser.ToCookieHeader(start.Cookies);
                    _txtTgStatCookies.Text = header;
                    TgStatSessionStore.CookieHeader = header;
                    UpdateTgStatStatusPreview();
                }

                MessageBox.Show(
                    this,
                    result.Message,
                    result.CanPaginate ? "TGStat — OK" : "TGStat",
                    MessageBoxButtons.OK,
                    result.CanPaginate ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "TGStat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _btnTgStatOpen.Enabled = true;
                _btnTgStatTest.Enabled = true;
                ApplyDialogTheme();
            }
        }

        private void OnTgStatTestClick(object sender, EventArgs e)
        {
            var cookies = TgStatCookieParser.ToCookieContainer(TgStatCookieHeader);
            if (cookies == null || !HasAnyCookies(cookies))
            {
                MessageBox.Show(this, "Вставьте cookies tgstat.com из браузера.", "TGStat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _btnTgStatTest.Enabled = false;
            UseWaitCursor = true;
            try
            {
                var result = TgStatAuthService.TestSession(cookies, CancellationToken.None);
                _lblTgStatStatus.Text = result.Message;
                MessageBox.Show(
                    this,
                    result.Message,
                    result.CanPaginate ? "TGStat — OK" : "TGStat",
                    MessageBoxButtons.OK,
                    result.CanPaginate ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "TGStat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _btnTgStatTest.Enabled = true;
                ApplyDialogTheme();
            }
        }

        private static bool HasAnyCookies(CookieContainer container)
        {
            return container.GetCookies(new Uri("https://tgstat.com/")).Count > 0;
        }

        private void UpdateTgStatSectionVisibility()
        {
            var show = FeedSource == FeedSourceMode.Bypass;
            _lblTgStatTitle.Visible = show;
            _lblTgStatHint.Visible = show;
            _txtTgStatCookies.Visible = show;
            _lblTgStatStatus.Visible = show;
            _btnTgStatOpen.Visible = show;
            _btnTgStatTest.Visible = show;
            _btnTgStatClear.Visible = show;
        }

        private void UpdateTgStatStatusPreview()
        {
            if (!HasValidCookieHeader())
            {
                if (string.IsNullOrWhiteSpace(TgStatCookieHeader))
                    _lblTgStatStatus.Text = string.Empty;
                else
                    _lblTgStatStatus.Text = "Не удалось разобрать cookies — повторите шаги 1–3.";
            }
            else
            {
                _lblTgStatStatus.Text = string.Empty;
            }

            if (FeedSource == FeedSourceMode.Bypass)
                UpdateTgStatActionButtonsHighlight(AppTheme.Current);
        }

        private bool HasValidCookieHeader()
        {
            var text = TgStatCookieHeader;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var pair in TgStatCookieParser.ParsePairs(text))
            {
                if (!string.IsNullOrEmpty(pair.Key))
                    return true;
            }

            return false;
        }

        private void UpdateTgStatActionButtonsHighlight(ThemePalette t)
        {
            if (HasValidCookieHeader())
            {
                AppTheme.StyleAccentButton(_btnTgStatTest, t);
                AppTheme.StyleDialogSecondaryButton(_btnTgStatOpen, t);
            }
            else
            {
                AppTheme.StyleAccentButton(_btnTgStatOpen, t);
                AppTheme.StyleDialogSecondaryButton(_btnTgStatTest, t);
            }

            AppTheme.StyleDialogSecondaryButton(_btnTgStatClear, t);
        }

        private static string GetTgStatStepsHint()
        {
            return string.Join(
                Environment.NewLine,
                "Без входа TGStat отдаёт только первую страницу (~30 прокси).",
                "",
                "1. Нажмите «Войти в Telegram».",
                "2. В Telegram нажмите Start у @" + TgStatAuthService.BotUsernameForUi + ".",
                "3. Дождитесь сообщения «Вход подтверждён».",
                "4. Нажмите «Проверить» — должно собираться больше прокси с канала.",
                "5. Нажмите «Сохранить» внизу окна настроек.",
                "",
                "Поле cookies ниже — только если вход через Telegram не сработал.");
        }

        private static int FeedSourceToIndex(FeedSourceMode mode)
        {
            return mode == FeedSourceMode.Direct ? 0 : 1;
        }

        private static FeedSourceMode IndexToFeedSource(int index)
        {
            return index == 0 ? FeedSourceMode.Direct : FeedSourceMode.Bypass;
        }

        private static string GetFeedHint(FeedSourceMode mode)
        {
            if (mode == FeedSourceMode.Direct)
                return "Загрузка с t.me и telegram.me. Нужен прямой доступ к Telegram.";

            return "Сначала актуальная лента с TGStat, затем добор из web.archive.org до лимита.";
        }

        private void ApplyDialogTheme()
        {
            var dark = _themeToggle.IsDarkTheme;
            var t = dark ? AppTheme.Dark : AppTheme.Light;

            BackColor = t.BgSurface;
            _title.ForeColor = t.Accent;
            _hint.ForeColor = t.TextSecondary;
            _lblMax.ForeColor = t.TextPrimary;
            _lblRange.ForeColor = t.TextMuted;
            _lblFeedHint.ForeColor = t.TextMuted;
            _lblTgStatTitle.ForeColor = t.Accent;
            _lblTgStatHint.ForeColor = t.TextMuted;
            _lblTgStatStatus.ForeColor = t.TextMuted;
            _cmbFeedSource.BackColor = t.BtnSurface;
            _cmbFeedSource.ForeColor = t.TextPrimary;
            _numMaxProxies.BackColor = t.BtnSurface;
            _numMaxProxies.ForeColor = t.TextPrimary;
            _txtTgStatCookies.BackColor = t.BtnSurface;
            _txtTgStatCookies.ForeColor = t.TextPrimary;
            _themeToggle.ApplyTheme(t);
            AppTheme.StyleAccentButton(_btnOk, t);
            AppTheme.StyleDialogSecondaryButton(_btnCancel, t);
            WindowCaptionTheme.Apply(this, dark);
            UpdateTgStatStatusPreview();
        }
    }
}
