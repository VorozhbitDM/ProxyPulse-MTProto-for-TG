using System;
using System.Drawing;
using System.Windows.Forms;
using ProxyPulse.UI;

namespace ProxyPulse
{
    public sealed class SettingsForm : Form
    {
        private readonly NumericUpDown _numMaxProxies;
        private readonly ThemeToggleControl _themeToggle;
        private readonly Label _lblMax;
        private readonly Label _lblRange;
        private readonly Label _title;
        private readonly Label _hint;
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
            ClientSize = new Size(400, 256);
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
                Text = "Лимит прокси и оформление окна.",
                Location = new Point(24, 48),
                Size = new Size(352, 22)
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

            _btnOk = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 204),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnOk.FlatAppearance.BorderSize = 0;

            _btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(308, 204),
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
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

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

        private void ApplyDialogTheme()
        {
            var dark = _themeToggle.IsDarkTheme;
            var t = dark ? AppTheme.Dark : AppTheme.Light;

            BackColor = t.BgSurface;
            _title.ForeColor = t.Accent;
            _hint.ForeColor = t.TextSecondary;
            _lblMax.ForeColor = t.TextPrimary;
            _lblRange.ForeColor = t.TextMuted;
            _numMaxProxies.BackColor = t.BtnSurface;
            _numMaxProxies.ForeColor = t.TextPrimary;
            _themeToggle.ApplyTheme(t);
            AppTheme.StyleAccentButton(_btnOk, t);
            AppTheme.StyleDialogSecondaryButton(_btnCancel, t);
            WindowCaptionTheme.Apply(this, dark);
        }
    }
}
