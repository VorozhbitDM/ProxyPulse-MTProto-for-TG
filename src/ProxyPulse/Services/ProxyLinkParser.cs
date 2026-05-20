using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProxyPulse.Models;

namespace ProxyPulse.Services
{
    internal static class ProxyLinkParser
    {
        /// <summary>
        /// MTProto-ссылки в ленте Telegram и в снимках web.archive.org:
        /// tg://proxy/?server=…, https://t.me/proxy?…, /proxy?… (в т.ч. внутри archive URL).
        /// </summary>
        private static readonly Regex LinkRegex = new Regex(
            @"(?:tg://proxy/?\?|https?://(?:t\.me|telegram\.me)/proxy\?|/proxy\?)([^""'\s<>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PostIdRegex = new Regex(
            @"data-post=""[^""]+/(\d+)""",
            RegexOptions.Compiled);

        public static List<ProxyEntry> Parse(string text)
        {
            var list = new List<ProxyEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text))
                return list;

            text = text.Replace("&amp;", "&");

            foreach (Match match in LinkRegex.Matches(text))
            {
                ProxyEntry entry;
                if (!TryParseQuery(match.Groups[1].Value, out entry))
                    continue;

                if (!seen.Add(entry.Key))
                    continue;

                list.Add(entry);
            }

            return list;
        }

        private static bool TryParseQuery(string rawQuery, out ProxyEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(rawQuery))
                return false;

            var query = Uri.UnescapeDataString(rawQuery.Trim().TrimEnd('.'));
            var fields = ParseQueryFields(query);

            string server;
            string portText;
            string secret;
            if (!fields.TryGetValue("server", out server)
                || !fields.TryGetValue("port", out portText)
                || !fields.TryGetValue("secret", out secret))
            {
                return false;
            }

            int port;
            if (!int.TryParse(portText, out port))
                return false;

            try
            {
                entry = new ProxyEntry
                {
                    Server = server.Trim().TrimEnd('.'),
                    Port = port,
                    Secret = secret.Trim(),
                    IsAvailable = false,
                    PingMs = null
                };
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.Server) || entry.Port <= 0 || string.IsNullOrWhiteSpace(entry.Secret))
                return false;

            return true;
        }

        private static Dictionary<string, string> ParseQueryFields(string query)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in query.Split('&'))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                var eq = part.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = Uri.UnescapeDataString(part.Substring(0, eq)).Trim();
                var value = Uri.UnescapeDataString(part.Substring(eq + 1)).Trim();
                if (key.Length > 0)
                    fields[key] = value;
            }

            return fields;
        }

        /// <summary>Минимальный id поста на странице — курсор ?before= для более старых сообщений.</summary>
        public static long? GetPaginationBeforeId(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            long? minId = null;

            foreach (Match m in PostIdRegex.Matches(html))
            {
                long id;
                if (!long.TryParse(m.Groups[1].Value, out id))
                    continue;

                if (!minId.HasValue || id < minId.Value)
                    minId = id;
            }

            return minId;
        }
    }
}
