using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ProxyPulse.Services
{
    /// <summary>Хранение cookies TGStat между запусками (DPAPI, только текущий пользователь Windows).</summary>
    internal static class TgStatSessionStore
    {
        private static string _cookieHeader;

        public static bool HasSession
        {
            get { return !string.IsNullOrWhiteSpace(CookieHeader); }
        }

        public static string CookieHeader
        {
            get
            {
                if (_cookieHeader != null)
                    return _cookieHeader;

                _cookieHeader = LoadFromDisk() ?? string.Empty;
                return _cookieHeader;
            }
            set
            {
                _cookieHeader = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(_cookieHeader))
                    DeleteFile();
                else
                    SaveToDisk(_cookieHeader);
            }
        }

        public static CookieContainer CreateCookieContainer()
        {
            return TgStatCookieParser.ToCookieContainer(CookieHeader);
        }

        public static void Clear()
        {
            CookieHeader = string.Empty;
        }

        public static void Reload()
        {
            _cookieHeader = null;
        }

        private static string GetFilePath()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProxyPulse");
            return Path.Combine(folder, "tgstat-session.dat");
        }

        private static string LoadFromDisk()
        {
            try
            {
                var path = GetFilePath();
                if (!File.Exists(path))
                    return null;

                var protectedBytes = File.ReadAllBytes(path);
                var plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveToDisk(string cookieHeader)
        {
            try
            {
                var path = GetFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var plain = Encoding.UTF8.GetBytes(cookieHeader);
                var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, protectedBytes);
            }
            catch
            {
            }
        }

        private static void DeleteFile()
        {
            try
            {
                var path = GetFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
