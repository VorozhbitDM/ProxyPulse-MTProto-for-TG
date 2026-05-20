using System;
using System.Drawing;

namespace ProxyPulse.UI
{
    /// <summary>Ретро-типографика приложения (Tahoma / Trebuchet — эпоха WinXP–7).</summary>
    internal static class AppFonts
    {
        private static FontFamily _family;
        private static bool _resolved;

        public static Font Ui { get; private set; }
        public static Font UiSmall { get; private set; }
        public static Font UiBold { get; private set; }
        public static Font TitleBar { get; private set; }
        public static Font WelcomeTitle { get; private set; }
        public static Font WelcomeBody { get; private set; }
        public static Font WelcomeAction { get; private set; }
        public static Font Progress { get; private set; }
        public static Font ProgressBold { get; private set; }
        public static Font Button { get; private set; }
        public static Font ButtonCompact { get; private set; }
        public static Font ToolbarLink { get; private set; }
        public static Font CardTitle { get; private set; }
        public static Font CardMeta { get; private set; }
        public static Font CardHint { get; private set; }
        public static Font CardConnectHint { get; private set; }
        public static Font DialogHeading { get; private set; }

        static AppFonts()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (Ui != null)
                return;

            var family = ResolveFamily();
            Ui = new Font(family, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            UiSmall = new Font(family, 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            UiBold = new Font(family, 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            TitleBar = new Font(family, 11.5f, FontStyle.Bold, GraphicsUnit.Point);
            WelcomeTitle = new Font(family, 26f, FontStyle.Bold, GraphicsUnit.Point);
            WelcomeBody = new Font(family, 10.25f, FontStyle.Regular, GraphicsUnit.Point);
            WelcomeAction = new Font(family, 11.5f, FontStyle.Bold, GraphicsUnit.Point);
            Progress = new Font(family, 11f, FontStyle.Regular, GraphicsUnit.Point);
            ProgressBold = new Font(family, 11f, FontStyle.Bold, GraphicsUnit.Point);
            Button = new Font(family, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            ButtonCompact = new Font(family, 9.25f, FontStyle.Regular, GraphicsUnit.Point);
            ToolbarLink = new Font(family, 8.75f, FontStyle.Regular, GraphicsUnit.Point);
            CardTitle = new Font(family, 10f, FontStyle.Bold, GraphicsUnit.Point);
            CardMeta = new Font(family, 9f, FontStyle.Bold, GraphicsUnit.Point);
            CardHint = new Font(family, 8.25f, FontStyle.Regular, GraphicsUnit.Point);
            CardConnectHint = new Font(family, 10.5f, FontStyle.Bold, GraphicsUnit.Point);
            DialogHeading = new Font(family, 12f, FontStyle.Bold, GraphicsUnit.Point);
        }

        public static Font DialogTitle => TitleBar;
        public static Font DialogHint => UiSmall;

        private static FontFamily ResolveFamily()
        {
            if (_resolved)
                return _family;

            _resolved = true;
            foreach (var name in new[] { "Tahoma", "Trebuchet MS", "Segoe UI" })
            {
                try
                {
                    _family = new FontFamily(name);
                    return _family;
                }
                catch (ArgumentException)
                {
                }
            }

            _family = FontFamily.GenericSansSerif;
            return _family;
        }
    }
}
