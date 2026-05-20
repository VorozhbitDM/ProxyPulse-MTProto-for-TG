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

        /// <summary>Ссылка на более старые посты (rel=prev, в т.ч. if_/id_ в пути archive.org).</summary>
        private static readonly Regex ArchiveNextPageRegex = new Regex(
            @"<link\s+rel=""prev""\s+href=""([^""]*?ProxyMTProto\?before=\d+[^""]*)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ArchiveMorePageRegex = new Regex(
            @"href=""(/web/\d+(?:if_|id_)?/?https://t\.me/s/ProxyMTProto\?before=\d+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ArchiveSnapshotRegex = new Regex(
            @"/web/(\d{14})(?:if_|id_)?/?https://t\.me/s/ProxyMTProto",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Порядок ссылок = порядок в HTML (на 1-й стр. ленты — от новых постов к старым).</summary>
        /// <summary>Страница содержит ленту Telegram, а не пустую оболочку Wayback.</summary>
        public static bool LooksLikeTelegramFeed(string html)
        {
            if (string.IsNullOrEmpty(html))
                return false;

            return html.IndexOf("data-post=", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("tgme_widget_message", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static List<ProxyEntry> Parse(string text)
        {
            var list = new List<ProxyEntry>();
            if (string.IsNullOrEmpty(text))
                return list;

            text = text.Replace("&amp;", "&");

            foreach (Match match in LinkRegex.Matches(text))
            {
                ProxyEntry entry;
                if (!TryParseQuery(match.Groups[1].Value, out entry))
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

        /// <summary>Относительный href следующей (более старой) страницы ленты в archive.org.</summary>
        public static string GetArchiveNextPageHref(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            var m = ArchiveNextPageRegex.Match(html);
            if (m.Success)
                return NormalizeArchiveFeedHref(m.Groups[1].Value);

            m = ArchiveMorePageRegex.Match(html);
            return m.Success ? NormalizeArchiveFeedHref(m.Groups[1].Value) : null;
        }

        private static string NormalizeArchiveFeedHref(string href)
        {
            if (string.IsNullOrWhiteSpace(href))
                return null;

            href = href.Trim();
            if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (href.IndexOf("web.archive.org", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var pathStart = href.IndexOf("/web/", StringComparison.OrdinalIgnoreCase);
                    if (pathStart >= 0)
                        href = href.Substring(pathStart);
                }
            }

            return href.StartsWith("/") ? href : "/" + href;
        }

        /// <summary>before= для следующей страницы (из rel=prev или минимального data-post).</summary>
        public static long? GetNextPageBeforeId(string html)
        {
            var href = GetArchiveNextPageHref(html);
            if (!string.IsNullOrEmpty(href))
            {
                var fromUrl = ArchiveUrlHelper.ParseBeforeId("https://web.archive.org" + href);
                if (fromUrl.HasValue)
                    return fromUrl.Value;
            }

            return GetPaginationBeforeId(html);
        }

        /// <summary>ID снимка archive.org (20260519104022) для сборки URL, если нет rel=prev.</summary>
        public static string GetArchiveSnapshotId(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            var m = ArchiveSnapshotRegex.Match(html);
            return m.Success ? m.Groups[1].Value : null;
        }

        public static string BuildArchivePageUrl(string snapshotId, long? beforeId)
        {
            if (string.IsNullOrEmpty(snapshotId))
                return null;

            var url = "https://web.archive.org/web/" + snapshotId + "/https://t.me/s/ProxyMTProto";
            if (beforeId.HasValue)
                url += "?before=" + beforeId.Value;
            return url;
        }
    }
}
