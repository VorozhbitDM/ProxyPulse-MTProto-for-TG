using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProxyPulse.Services
{
    internal static class TgStatCookieParser
    {
        private static readonly Uri TgStatCom = new Uri("https://tgstat.com/");
        private static readonly Uri TgStatRu = new Uri("https://tgstat.ru/");

        public static string ToCookieHeader(CookieContainer container)
        {
            if (container == null)
                return string.Empty;

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            AppendCookies(sb, names, container.GetCookies(TgStatCom));
            AppendCookies(sb, names, container.GetCookies(TgStatRu));
            return sb.ToString();
        }

        private static void AppendCookies(StringBuilder sb, HashSet<string> names, CookieCollection cookies)
        {
            if (cookies == null)
                return;

            foreach (Cookie cookie in cookies)
            {
                if (cookie == null || string.IsNullOrEmpty(cookie.Name) || names.Contains(cookie.Name))
                    continue;

                names.Add(cookie.Name);
                if (sb.Length > 0)
                    sb.Append("; ");

                sb.Append(cookie.Name).Append('=').Append(cookie.Value ?? string.Empty);
            }
        }

        public static CookieContainer ToCookieContainer(string raw)
        {
            var container = new CookieContainer();
            if (string.IsNullOrWhiteSpace(raw))
                return container;

            foreach (var pair in ParsePairs(raw))
            {
                if (string.IsNullOrEmpty(pair.Key))
                    continue;

                try
                {
                    container.Add(TgStatCom, CreateCookie(pair.Key, pair.Value, "tgstat.com"));
                    container.Add(TgStatRu, CreateCookie(pair.Key, pair.Value, "tgstat.ru"));
                }
                catch
                {
                }
            }

            return container;
        }

        private static Cookie CreateCookie(string name, string value, string domain)
        {
            return new Cookie(name, value, "/", domain)
            {
                Secure = true
            };
        }

        public static IEnumerable<KeyValuePair<string, string>> ParsePairs(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                yield break;

            raw = raw.Trim();
            if (raw.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(7).Trim();

            foreach (var part in raw.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var segment = part.Trim();
                if (segment.Length == 0 || segment.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var eq = segment.IndexOf('=');
                if (eq <= 0)
                    continue;

                var name = segment.Substring(0, eq).Trim();
                var value = segment.Substring(eq + 1).Trim();
                if (name.Length == 0)
                    continue;

                if (value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal) && value.Length >= 2)
                    value = value.Substring(1, value.Length - 2);

                yield return new KeyValuePair<string, string>(name, value);
            }
        }
    }
}
