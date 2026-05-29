using System;
using System.Collections.Generic;
using System.Globalization;
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

        private static readonly Regex MessageBlockRegex = new Regex(
            @"data-post=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MessageTimeRegex = new Regex(
            @"<time[^>]*datetime=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MessageDateTextRegex = new Regex(
            @"class=""[^""]*tgme_widget_message_date[^""]*""[^>]*>\s*(?:<time[^>]*>)?\s*([^<]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PlainPostRegex = new Regex(
            @"Server:\s*(?:<[^>]+>)?\s*`?([^`""<\s]+)`?\s*Port:\s*(?:<[^>]+>)?\s*`?(\d+)`?\s*Secret:\s*(?:<[^>]+>)?\s*`?([0-9a-fA-F+/=]+)`?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TgStatProxyRegex = new Regex(
            @"Server:\s*(?:<[^>]+>)*\s*(.+?)\s*(?:<[^>]+>)*\s*Port:\s*(?:<[^>]+>)*\s*(\d+)\s*(?:<[^>]+>)*\s*Secret:\s*(?:<[^>]+>)*\s*([0-9a-fA-F+/=]+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex TgStatDateRegex = new Regex(
            @"(\d{1,2}\s+[A-Za-z]{3},?\s+\d{1,2}:\d{2})",
            RegexOptions.Compiled);

        private static readonly Regex InlineFieldsRegex = new Regex(
            @"server=([^&\s""'<>]+)&port=(\d+)&secret=([0-9a-fA-F+/=]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Порядок ссылок = порядок в HTML (на 1-й стр. ленты — от новых постов к старым).</summary>
        /// <summary>Страница содержит ленту Telegram, а не пустую оболочку Wayback.</summary>
        public static bool LooksLikeTelegramFeed(string html)
        {
            if (string.IsNullOrEmpty(html))
                return false;

            return html.IndexOf("data-post=", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("tgme_widget_message", StringComparison.OrdinalIgnoreCase) >= 0
                || TgStatProxyRegex.IsMatch(html);
        }

        public static List<ProxyEntry> Parse(string text)
        {
            return ParseFeed(text);
        }

        /// <summary>Парсит прокси с датой публикации поста, если она есть в HTML.</summary>
        public static List<ProxyEntry> ParseFeed(string text)
        {
            var list = new List<ProxyEntry>();
            if (string.IsNullOrEmpty(text))
                return list;

            text = text.Replace("&amp;", "&");
            var pageDate = TryParseArchivePageDate(text);
            var isTgStat = htmlLooksLikeTgStat(text);

            if (isTgStat)
                ParseTgStatBlocks(text, list);

            ParseTelegramPosts(text, list, pageDate);
            ParsePlainLinks(text, list, pageDate);
            ParsePlainTextProxies(text, list, pageDate);

            if (!isTgStat)
                ParseTgStatBlocks(text, list);

            return MergeParsedEntries(list);
        }

        private static bool htmlLooksLikeTgStat(string html)
        {
            return html.IndexOf("tgstat.com", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("tgstat.ru", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<ProxyEntry> MergeParsedEntries(List<ProxyEntry> list)
        {
            var map = new Dictionary<string, ProxyEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in list)
            {
                ProxyEntry existing;
                if (!map.TryGetValue(entry.Key, out existing))
                {
                    map[entry.Key] = entry;
                    continue;
                }

                if (entry.PublishedAt.HasValue
                    && (!existing.PublishedAt.HasValue || entry.PublishedAt > existing.PublishedAt))
                {
                    existing.PublishedAt = entry.PublishedAt;
                }
            }

            return new List<ProxyEntry>(map.Values);
        }

        private static void ParseTelegramPosts(string html, List<ProxyEntry> list, DateTime? pageDate)
        {
            var matches = MessageBlockRegex.Matches(html);
            if (matches.Count == 0)
                return;

            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : html.Length;
                var chunk = html.Substring(start, end - start);
                var publishedAt = ExtractPostDate(chunk) ?? pageDate;
                ParsePlainLinks(chunk, list, publishedAt);
                ParsePlainTextProxies(chunk, list, publishedAt);
            }
        }

        private static DateTime? ExtractPostDate(string chunk)
        {
            var timeMatch = MessageTimeRegex.Match(chunk);
            if (timeMatch.Success)
            {
                var parsed = TryParseDateTime(timeMatch.Groups[1].Value);
                if (parsed.HasValue)
                    return parsed;
            }

            var textMatch = MessageDateTextRegex.Match(chunk);
            if (textMatch.Success)
            {
                var parsed = TryParseDateTime(textMatch.Groups[1].Value.Trim());
                if (parsed.HasValue)
                    return parsed;

                parsed = TryParseTgStatDate(textMatch.Groups[1].Value.Trim());
                if (parsed.HasValue)
                    return parsed;
            }

            return null;
        }

        private static void ParsePlainTextProxies(string text, List<ProxyEntry> list, DateTime? publishedAt)
        {
            foreach (Match match in PlainPostRegex.Matches(text))
            {
                ProxyEntry entry;
                if (!TryBuildEntry(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, out entry))
                    continue;

                entry.PublishedAt = publishedAt;
                list.Add(entry);
            }
        }

        private static DateTime? TryParseArchivePageDate(string html)
        {
            var snapshotId = GetArchiveSnapshotId(html);
            if (string.IsNullOrEmpty(snapshotId) || snapshotId.Length < 8)
                return null;

            DateTime parsed;
            if (DateTime.TryParseExact(
                    snapshotId.Substring(0, 8),
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static void ParsePlainLinks(string text, List<ProxyEntry> list, DateTime? publishedAt)
        {
            foreach (Match match in LinkRegex.Matches(text))
            {
                ProxyEntry entry;
                if (!TryParseQuery(match.Groups[1].Value, out entry))
                    continue;

                entry.PublishedAt = publishedAt;
                list.Add(entry);
            }

            if (list.Count > 0 || LinkRegex.IsMatch(text))
                return;

            foreach (Match match in InlineFieldsRegex.Matches(text))
            {
                ProxyEntry entry;
                if (!TryParseInlineFields(match, out entry))
                    continue;

                entry.PublishedAt = publishedAt;
                list.Add(entry);
            }
        }

        private static void ParseTgStatBlocks(string html, List<ProxyEntry> list)
        {
            var seenInPass = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in TgStatProxyRegex.Matches(html))
            {
                ProxyEntry entry;
                if (!TryBuildEntry(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, out entry))
                    continue;

                if (!seenInPass.Add(entry.Key))
                    continue;

                entry.PublishedAt = ExtractTgStatPostDate(html, match.Index);
                list.Add(entry);
            }
        }

        private static DateTime? ExtractTgStatPostDate(string html, int serverIndex)
        {
            var blockStart = Math.Max(0, serverIndex - 1500);
            var blockEnd = Math.Min(html.Length, serverIndex + 160);
            var block = html.Substring(blockStart, blockEnd - blockStart);

            var timeMatch = MessageTimeRegex.Match(block);
            if (timeMatch.Success)
            {
                var parsed = TryParseDateTime(timeMatch.Groups[1].Value);
                if (parsed.HasValue)
                    return parsed;
            }

            var dateMatches = TgStatDateRegex.Matches(block);
            for (var i = dateMatches.Count - 1; i >= 0; i--)
            {
                var parsed = TryParseTgStatDate(dateMatches[i].Groups[1].Value);
                if (parsed.HasValue)
                    return parsed;
            }

            return null;
        }

        private static bool TryParseInlineFields(Match match, out ProxyEntry entry)
        {
            return TryBuildEntry(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, out entry);
        }

        private static bool TryBuildEntry(string server, string portText, string secret, out ProxyEntry entry)
        {
            entry = null;
            int port;
            if (!int.TryParse(portText, out port))
                return false;

            server = StripTags(server).Trim().Trim('`').TrimEnd('.');
            secret = StripTags(secret).Trim().Trim('`');
            if (string.IsNullOrWhiteSpace(server) || port <= 0 || string.IsNullOrWhiteSpace(secret))
                return false;

            if (string.Equals(server, "Unknown", StringComparison.OrdinalIgnoreCase)
                || string.Equals(server, "For", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            entry = new ProxyEntry
            {
                Server = server,
                Port = port,
                Secret = secret,
                IsAvailable = false,
                PingMs = null
            };
            return true;
        }

        private static DateTime? TryParseDateTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            DateTime parsed;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
                return parsed;
            if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.AssumeUniversal, out parsed))
                return parsed;
            return null;
        }

        private static string StripTags(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(value, "<[^>]+>", string.Empty).Trim();
        }

        private static DateTime? TryParseTgStatDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = Regex.Replace(raw.Trim(), @"\s+", " ");
            var formats = new[]
            {
                "d MMM HH:mm",
                "dd MMM HH:mm",
                "d MMM, HH:mm",
                "dd MMM, HH:mm"
            };

            foreach (var format in formats)
            {
                DateTime parsed;
                if (DateTime.TryParseExact(
                        raw,
                        format,
                        CultureInfo.GetCultureInfo("en-US"),
                        DateTimeStyles.AssumeUniversal,
                        out parsed))
                {
                    if (parsed.Year <= 2001)
                    {
                        parsed = new DateTime(
                            DateTime.UtcNow.Year,
                            parsed.Month,
                            parsed.Day,
                            parsed.Hour,
                            parsed.Minute,
                            0,
                            DateTimeKind.Utc);
                    }

                    return parsed;
                }
            }

            raw = raw.Replace(",", string.Empty);
            raw = Regex.Replace(raw, @"\s+", " ");
            foreach (var format in formats)
            {
                DateTime parsed;
                if (DateTime.TryParseExact(
                        raw,
                        format,
                        CultureInfo.GetCultureInfo("en-US"),
                        DateTimeStyles.AssumeUniversal,
                        out parsed))
                {
                    if (parsed.Year <= 2001)
                    {
                        parsed = new DateTime(
                            DateTime.UtcNow.Year,
                            parsed.Month,
                            parsed.Day,
                            parsed.Hour,
                            parsed.Minute,
                            0,
                            DateTimeKind.Utc);
                    }

                    return parsed;
                }
            }

            return TryParseDateTime(raw);
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
                return TryBuildEntry(server, portText, secret, out entry);
            }
            catch
            {
                return false;
            }
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

            var url = "https://web.archive.org/web/" + snapshotId + "if_/https://t.me/s/ProxyMTProto";
            if (beforeId.HasValue)
                url += "?before=" + beforeId.Value;
            return url;
        }

        public static string BuildTelegramPageUrl(string baseUrl, long beforeId)
        {
            if (string.IsNullOrEmpty(baseUrl))
                return null;

            var separator = baseUrl.IndexOf("?", StringComparison.Ordinal) >= 0 ? "&" : "?";
            return baseUrl + separator + "before=" + beforeId;
        }
    }
}
