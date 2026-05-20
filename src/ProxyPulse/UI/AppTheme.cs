using System.Drawing;
using System.Windows.Forms;

namespace ProxyPulse.UI
{
    internal sealed class ThemePalette
    {
        public Color Accent { get; set; }
        public Color WindowBorder { get; set; }
        public Color BgApp { get; set; }
        public Color BgSurface { get; set; }
        public Color BgCard { get; set; }
        public Color Border { get; set; }
        public Color BtnSurface { get; set; }
        public Color BtnBorder { get; set; }
        public Color BtnHover { get; set; }
        public Color BtnPressed { get; set; }
        public Color BtnText { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color TextMuted { get; set; }
        public Color WelcomeDesc { get; set; }
        public Color WelcomeVersion { get; set; }
        public Color CardAddress { get; set; }
        public Color CardRecheckBg { get; set; }
        public Color CardRecheckBorder { get; set; }
        public Color CardRecheckText { get; set; }
        public Color CloseButtonFore { get; set; }
        public Color CloseButtonBg { get; set; }
        public Color CloseButtonHoverBg { get; set; }
        public Color CloseButtonHoverFore { get; set; }
        public Color ChromeButtonFore { get; set; }
        public Color ChromeButtonHoverBg { get; set; }
        public Color ChromeButtonHoverFore { get; set; }
        public Color AccentButtonFore { get; set; }
        public Color SecondaryButtonBg { get; set; }
        public Color SecondaryButtonBorder { get; set; }
        public Color SecondaryButtonText { get; set; }
    }

    internal static class AppTheme
    {
        private static readonly ThemePalette LightPalette = new ThemePalette
        {
            Accent = Color.FromArgb(42, 171, 238),
            WindowBorder = Color.FromArgb(148, 156, 170),
            BgApp = Color.FromArgb(240, 243, 247),
            BgSurface = Color.White,
            BgCard = Color.White,
            Border = Color.FromArgb(220, 226, 234),
            BtnSurface = Color.White,
            BtnBorder = Color.FromArgb(220, 224, 230),
            BtnHover = Color.FromArgb(228, 234, 242),
            BtnPressed = Color.FromArgb(210, 218, 228),
            BtnText = Color.FromArgb(55, 60, 68),
            TextPrimary = Color.FromArgb(50, 50, 50),
            TextSecondary = Color.FromArgb(80, 85, 92),
            TextMuted = Color.FromArgb(110, 110, 110),
            WelcomeDesc = Color.FromArgb(95, 100, 108),
            WelcomeVersion = Color.FromArgb(170, 175, 182),
            CardAddress = Color.FromArgb(33, 33, 33),
            CardRecheckBg = Color.FromArgb(240, 244, 248),
            CardRecheckBorder = Color.FromArgb(210, 216, 224),
            CardRecheckText = Color.FromArgb(70, 70, 70),
            CloseButtonFore = Color.FromArgb(196, 43, 43),
            CloseButtonBg = Color.FromArgb(255, 245, 245),
            CloseButtonHoverBg = Color.FromArgb(232, 17, 35),
            CloseButtonHoverFore = Color.White,
            ChromeButtonFore = Color.FromArgb(90, 95, 102),
            ChromeButtonHoverBg = Color.FromArgb(228, 234, 242),
            ChromeButtonHoverFore = Color.FromArgb(55, 60, 68),
            AccentButtonFore = Color.White,
            SecondaryButtonBg = Color.FromArgb(240, 244, 248),
            SecondaryButtonBorder = Color.FromArgb(210, 216, 224),
            SecondaryButtonText = Color.FromArgb(50, 50, 50)
        };

        private static readonly ThemePalette DarkPalette = new ThemePalette
        {
            Accent = Color.FromArgb(56, 189, 248),
            WindowBorder = Color.FromArgb(28, 32, 38),
            BgApp = Color.FromArgb(30, 34, 40),
            BgSurface = Color.FromArgb(38, 43, 51),
            BgCard = Color.FromArgb(45, 50, 58),
            Border = Color.FromArgb(58, 65, 76),
            BtnSurface = Color.FromArgb(52, 58, 68),
            BtnBorder = Color.FromArgb(72, 80, 92),
            BtnHover = Color.FromArgb(65, 72, 84),
            BtnPressed = Color.FromArgb(78, 86, 98),
            BtnText = Color.FromArgb(220, 224, 230),
            TextPrimary = Color.FromArgb(230, 232, 235),
            TextSecondary = Color.FromArgb(170, 176, 188),
            TextMuted = Color.FromArgb(130, 138, 150),
            WelcomeDesc = Color.FromArgb(170, 176, 188),
            WelcomeVersion = Color.FromArgb(110, 118, 130),
            CardAddress = Color.FromArgb(230, 232, 235),
            CardRecheckBg = Color.FromArgb(52, 58, 68),
            CardRecheckBorder = Color.FromArgb(72, 80, 92),
            CardRecheckText = Color.FromArgb(200, 204, 212),
            CloseButtonFore = Color.FromArgb(255, 120, 120),
            CloseButtonBg = Color.FromArgb(72, 38, 42),
            CloseButtonHoverBg = Color.FromArgb(232, 17, 35),
            CloseButtonHoverFore = Color.White,
            ChromeButtonFore = Color.FromArgb(190, 196, 206),
            ChromeButtonHoverBg = Color.FromArgb(65, 72, 84),
            ChromeButtonHoverFore = Color.FromArgb(230, 232, 235),
            AccentButtonFore = Color.White,
            SecondaryButtonBg = Color.FromArgb(52, 58, 68),
            SecondaryButtonBorder = Color.FromArgb(72, 80, 92),
            SecondaryButtonText = Color.FromArgb(220, 224, 230)
        };

        public static ThemePalette Current { get; private set; } = LightPalette;
        public static ThemePalette Light => LightPalette;
        public static ThemePalette Dark => DarkPalette;

        public static void ApplyFromSettings()
        {
            Current = AppSettings.Current.UseDarkTheme ? DarkPalette : LightPalette;
        }

        public static void StyleSecondaryButton(Button b)
        {
            if (b == null)
                return;

            var t = Current;
            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = t.BtnSurface;
            b.ForeColor = t.BtnText;
            b.FlatAppearance.BorderColor = t.BtnBorder;
            b.FlatAppearance.MouseOverBackColor = t.BtnHover;
            b.FlatAppearance.MouseDownBackColor = t.BtnPressed;
        }

        /// <summary>Ненавязчивые кнопки в шапке (Настройки, Справка).</summary>
        public static void StyleToolbarLinkButton(Button b)
        {
            if (b == null)
                return;

            var t = Current;
            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = t.BgSurface;
            b.ForeColor = t.TextMuted;
            b.FlatAppearance.MouseOverBackColor = t.BtnHover;
            b.FlatAppearance.MouseDownBackColor = t.BtnPressed;
        }

        public static void StyleDisabledButton(Button b)
        {
            if (b == null)
                return;

            var t = Current;
            b.UseVisualStyleBackColor = false;
            b.BackColor = t.BgApp;
            b.ForeColor = t.TextMuted;
        }

        public static void StyleAccentButton(Button b)
        {
            StyleAccentButton(b, Current);
        }

        public static void StyleAccentButton(Button b, ThemePalette t)
        {
            if (b == null)
                return;

            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = t.Accent;
            b.ForeColor = t.AccentButtonFore;
        }

        public static void StyleDialogSecondaryButton(Button b)
        {
            StyleDialogSecondaryButton(b, Current);
        }

        public static void StyleDialogSecondaryButton(Button b, ThemePalette t)
        {
            if (b == null)
                return;

            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = t.SecondaryButtonBg;
            b.ForeColor = t.SecondaryButtonText;
            b.FlatAppearance.BorderColor = t.SecondaryButtonBorder;
            b.FlatAppearance.MouseOverBackColor = t.BtnHover;
            b.FlatAppearance.MouseDownBackColor = t.BtnPressed;
        }
    }
}
