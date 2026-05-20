using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ProxyPulse.UI;

namespace ProxyPulse
{
    public sealed class HelpForm : Form
    {
        private const string Tagline = "Ищем MTProto-прокси и проверяем доступность";

        private readonly Label _title;
        private readonly Label _ver;
        private readonly Label _desc;
        private readonly Button _btnGit;
        private readonly Button _btnYooMoney;
        private readonly Button _close;

        public HelpForm()
        {
            AppFonts.EnsureInitialized();
            AppTheme.ApplyFromSettings();

            Text = "Справка";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 228);
            Font = AppFonts.Ui;
            AppBranding.ApplyWindowIcon(this);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionText = version != null
                ? string.Format("{0}.{1}", version.Major, version.Minor)
                : "2.8";

            _title = new Label
            {
                Text = "ProxyPulse",
                Font = AppFonts.DialogHeading,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            _ver = new Label
            {
                Text = string.Format("Версия {0}", versionText),
                AutoSize = true,
                Location = new Point(24, 48)
            };

            _desc = new Label
            {
                Text = Tagline + ".",
                Location = new Point(24, 76),
                Size = new Size(392, 22)
            };

            _btnGit = new Button
            {
                Text = "Проект на GitHub",
                Location = new Point(24, 112),
                Size = new Size(188, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnGit.FlatAppearance.BorderSize = 0;
            _btnGit.Click += (_, __) => OpenUrl(AppLinks.GitHubProject);

            _btnYooMoney = new Button
            {
                Text = "ЮMoney",
                Location = new Point(224, 112),
                Size = new Size(188, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnYooMoney.Click += (_, __) => OpenUrl(AppLinks.YooMoney);

            _close = new Button
            {
                Text = "Закрыть",
                DialogResult = DialogResult.OK,
                Location = new Point(312, 176),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat
            };

            Controls.Add(_title);
            Controls.Add(_ver);
            Controls.Add(_desc);
            Controls.Add(_btnGit);
            Controls.Add(_btnYooMoney);
            Controls.Add(_close);
            AcceptButton = _close;

            ApplyTheme();
            WindowCaptionTheme.Apply(this, AppSettings.Current.UseDarkTheme);
        }

        private void ApplyTheme()
        {
            var t = AppTheme.Current;
            BackColor = t.BgSurface;
            _title.ForeColor = t.Accent;
            _ver.ForeColor = t.TextMuted;
            _desc.ForeColor = t.TextSecondary;
            AppTheme.StyleAccentButton(_btnGit);
            AppTheme.StyleDialogSecondaryButton(_btnYooMoney);
            AppTheme.StyleDialogSecondaryButton(_close);
            WindowCaptionTheme.Apply(this, AppSettings.Current.UseDarkTheme);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть ссылку.\r\n" + ex.Message, "ProxyPulse",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
