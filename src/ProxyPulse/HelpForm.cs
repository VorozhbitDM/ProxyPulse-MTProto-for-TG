using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ProxyPulse
{
    public sealed class HelpForm : Form
    {
        private const string Tagline = "Ищем MTProto-прокси и проверяем доступность";

        public HelpForm()
        {
            Text = "Справка";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 228);
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;

            var accent = Color.FromArgb(42, 171, 238);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionText = version != null
                ? string.Format("{0}.{1}", version.Major, version.Minor)
                : "2.3";

            var title = new Label
            {
                Text = "ProxyPulse",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            var ver = new Label
            {
                Text = string.Format("Версия {0}", versionText),
                AutoSize = true,
                Location = new Point(24, 48),
                ForeColor = Color.Gray
            };

            var desc = new Label
            {
                Text = Tagline + ".",
                Location = new Point(24, 76),
                Size = new Size(392, 22),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            var btnGit = new Button
            {
                Text = "Проект на GitHub",
                Location = new Point(24, 112),
                Size = new Size(188, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnGit.FlatAppearance.BorderSize = 0;
            btnGit.Click += (_, __) => OpenUrl(AppLinks.GitHubProject);

            var btnDonate = new Button
            {
                Text = "Задонатить",
                Location = new Point(224, 112),
                Size = new Size(188, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(50, 50, 50),
                Cursor = Cursors.Hand
            };
            btnDonate.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 224);
            btnDonate.Click += (_, __) => OpenUrl(AppLinks.Donate);

            var close = new Button
            {
                Text = "Закрыть",
                DialogResult = DialogResult.OK,
                Location = new Point(312, 176),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 244, 248)
            };
            close.FlatAppearance.BorderColor = Color.FromArgb(220, 224, 230);

            Controls.Add(title);
            Controls.Add(ver);
            Controls.Add(desc);
            Controls.Add(btnGit);
            Controls.Add(btnDonate);
            Controls.Add(close);
            AcceptButton = close;
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
