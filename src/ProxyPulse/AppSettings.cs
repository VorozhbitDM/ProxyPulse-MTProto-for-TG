using System;
using System.IO;

namespace ProxyPulse
{
    /// <summary>Настройки пользователя (файл в %AppData%\ProxyPulse).</summary>
    public sealed class AppSettings
    {
        public const int DefaultMaxProxiesToCollect = 100;
        public const int MinMaxProxiesToCollect = 10;
        public const int MaxMaxProxiesToCollect = 500;

        public int MaxProxiesToCollect { get; set; }

        private static AppSettings _current;

        public AppSettings()
        {
            MaxProxiesToCollect = DefaultMaxProxiesToCollect;
        }

        public static AppSettings Current
        {
            get { return _current ?? (_current = Load()); }
        }

        public static void Reload()
        {
            _current = Load();
        }

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            try
            {
                var path = GetSettingsFilePath();
                if (!File.Exists(path))
                    return settings;

                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    var key = line.Substring(0, eq).Trim();
                    var value = line.Substring(eq + 1).Trim();
                    if (string.Equals(key, "MaxProxiesToCollect", StringComparison.OrdinalIgnoreCase))
                    {
                        int n;
                        if (int.TryParse(value, out n))
                            settings.MaxProxiesToCollect = Clamp(n);
                    }
                }
            }
            catch
            {
            }

            return settings;
        }

        public void Save()
        {
            MaxProxiesToCollect = Clamp(MaxProxiesToCollect);
            var dir = Path.GetDirectoryName(GetSettingsFilePath());
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(
                GetSettingsFilePath(),
                "MaxProxiesToCollect=" + MaxProxiesToCollect + Environment.NewLine);

            _current = this;
        }

        public static int Clamp(int value)
        {
            if (value < MinMaxProxiesToCollect)
                return MinMaxProxiesToCollect;
            if (value > MaxMaxProxiesToCollect)
                return MaxMaxProxiesToCollect;
            return value;
        }

        private static string GetSettingsFilePath()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProxyPulse");
            return Path.Combine(folder, "settings.txt");
        }
    }
}
