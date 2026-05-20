using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProxyPulse
{
    public sealed class SettingsForm : Form
    {
        private readonly NumericUpDown _numMaxProxies;

        public SettingsForm(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Text = "Настройки";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(400, 200);
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;
            AppBranding.ApplyWindowIcon(this);

            var accent = Color.FromArgb(42, 171, 238);

            var title = new Label
            {
                Text = "Параметры поиска",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            var hint = new Label
            {
                Text = "Макс. число прокси для проверки.",
                Location = new Point(24, 48),
                Size = new Size(352, 22),
                ForeColor = Color.FromArgb(80, 85, 92)
            };

            var lblMax = new Label
            {
                Text = "Лимит прокси:",
                AutoSize = true,
                Location = new Point(24, 100),
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            _numMaxProxies = new NumericUpDown
            {
                Location = new Point(140, 97),
                Size = new Size(90, 24),
                Minimum = AppSettings.MinMaxProxiesToCollect,
                Maximum = AppSettings.MaxMaxProxiesToCollect,
                Value = AppSettings.Clamp(settings.MaxProxiesToCollect),
                ThousandsSeparator = true
            };

            var lblRange = new Label
            {
                Text = string.Format("от {0} до {1}", AppSettings.MinMaxProxiesToCollect, AppSettings.MaxMaxProxiesToCollect),
                AutoSize = true,
                Location = new Point(240, 102),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f)
            };

            var btnOk = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 148),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(308, 148),
                Size = new Size(76, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(50, 50, 50),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 224);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(title);
            Controls.Add(hint);
            Controls.Add(lblMax);
            Controls.Add(_numMaxProxies);
            Controls.Add(lblRange);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        public int MaxProxiesToCollect
        {
            get { return (int)_numMaxProxies.Value; }
        }
    }
}
