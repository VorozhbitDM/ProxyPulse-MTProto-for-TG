using System;
using System.Text.RegularExpressions;

namespace ProxyPulse.Services
{
    internal static class TgStatFeedHelper
    {
        private static readonly Regex CsrfRegex = new Regex(
            @"name=""_tgstat_csrk""\s+value=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MetaCsrfRegex = new Regex(
            @"name=""csrf-token""\s+content=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PageRegex = new Regex(
            @"class=""lm-page""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex OffsetRegex = new Regex(
            @"class=""lm-offset""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex FormActionRegex = new Regex(
            @"<form[^>]*class=""[^""]*lm-form[^""]*""[^>]*action=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsTgStatUrl(string url)
        {
            return url != null
                && (url.IndexOf("tgstat.com", StringComparison.OrdinalIgnoreCase) >= 0
                    || url.IndexOf("tgstat.ru", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool TryParsePaginationForm(string html, out string csrf, out string page, out int offset)
        {
            csrf = null;
            page = null;
            offset = 0;

            if (string.IsNullOrEmpty(html))
                return false;

            var csrfMatch = CsrfRegex.Match(html);
            if (!csrfMatch.Success)
                csrfMatch = MetaCsrfRegex.Match(html);

            var pageMatch = PageRegex.Match(html);
            if (!pageMatch.Success)
                pageMatch = Regex.Match(html, @"name=""page""\s+value=""([^""]+)""", RegexOptions.IgnoreCase);

            var offsetMatch = OffsetRegex.Match(html);
            if (!offsetMatch.Success)
                offsetMatch = Regex.Match(html, @"name=""offset""\s+value=""([^""]+)""", RegexOptions.IgnoreCase);

            if (!csrfMatch.Success || !pageMatch.Success || !offsetMatch.Success)
                return false;

            csrf = csrfMatch.Groups[1].Value;
            page = pageMatch.Groups[1].Value;
            int parsedOffset;
            if (!int.TryParse(offsetMatch.Groups[1].Value, out parsedOffset))
                return false;

            offset = parsedOffset;
            return !string.IsNullOrEmpty(csrf) && !string.IsNullOrEmpty(page);
        }

        private static readonly Regex JsonPageRegex = new Regex(
            @"""page""\s*:\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex JsonOffsetRegex = new Regex(
            @"""offset""\s*:\s*(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsEndOfFeedResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return true;

            if (response.IndexOf("\"status\":\"ok\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var html = ExtractPostsHtml(response);
                return !ContainsProxyPosts(html) && !ContainsProxyPosts(response);
            }

            return false;
        }

        /// <summary>Берёт последний page/offset из JSON или HTML-фрагмента ответа.</summary>
        public static bool TryAdvancePagination(
            string rawResponse,
            string chunkHtml,
            string currentPage,
            int currentOffset,
            out string nextPage,
            out int nextOffset)
        {
            nextPage = currentPage;
            nextOffset = currentOffset + 20;

            string parsedPage;
            int parsedOffset;
            if (TryParsePageOffset(rawResponse, out parsedPage, out parsedOffset)
                && parsedOffset > currentOffset)
            {
                nextPage = parsedPage;
                nextOffset = parsedOffset;
                return true;
            }

            if (!string.IsNullOrEmpty(chunkHtml)
                && TryParsePageOffset(chunkHtml, out parsedPage, out parsedOffset)
                && parsedOffset > currentOffset)
            {
                nextPage = parsedPage;
                nextOffset = parsedOffset;
                return true;
            }

            return nextOffset > currentOffset;
        }

        public static bool TryRefreshCsrf(string rawResponse, string chunkHtml, out string csrf)
        {
            csrf = null;
            if (TryParsePaginationForm(rawResponse, out csrf, out _, out _))
                return true;
            if (!string.IsNullOrEmpty(chunkHtml) && TryParsePaginationForm(chunkHtml, out csrf, out _, out _))
                return true;

            csrf = null;
            return false;
        }

        private static bool TryParsePageOffset(string text, out string page, out int offset)
        {
            page = null;
            offset = -1;
            if (string.IsNullOrEmpty(text))
                return false;

            Match pageMatch = null;
            foreach (Match match in PageRegex.Matches(text))
                pageMatch = match;
            if (pageMatch == null)
            {
                foreach (Match match in Regex.Matches(text, @"name=""page""\s+value=""([^""]+)""", RegexOptions.IgnoreCase))
                    pageMatch = match;
            }

            if (pageMatch == null)
            {
                foreach (Match match in JsonPageRegex.Matches(text))
                    pageMatch = match;
            }

            Match offsetMatch = null;
            foreach (Match match in OffsetRegex.Matches(text))
                offsetMatch = match;
            if (offsetMatch == null)
            {
                foreach (Match match in Regex.Matches(text, @"name=""offset""\s+value=""([^""]+)""", RegexOptions.IgnoreCase))
                    offsetMatch = match;
            }

            if (offsetMatch == null)
            {
                foreach (Match match in JsonOffsetRegex.Matches(text))
                    offsetMatch = match;
            }

            if (pageMatch == null || offsetMatch == null)
                return false;

            page = pageMatch.Groups[1].Value;
            int parsedOffset;
            if (!int.TryParse(offsetMatch.Groups[1].Value, out parsedOffset))
                return false;

            offset = parsedOffset;
            return !string.IsNullOrEmpty(page);
        }

        public static string GetPostsEndpoint(string channelUrl, string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                var actionMatch = FormActionRegex.Match(html);
                if (actionMatch.Success)
                    return ResolvePostsUrl(channelUrl, actionMatch.Groups[1].Value);
            }

            return channelUrl.TrimEnd('/') + "/posts-last";
        }

        public static bool IsRestrictedResponse(string response)
        {
            return !string.IsNullOrEmpty(response)
                && response.IndexOf("\"status\":\"restricted\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool ContainsProxyPosts(string html)
        {
            if (string.IsNullOrEmpty(html))
                return false;

            return ProxyLinkParser.LooksLikeTelegramFeed(html)
                || html.IndexOf("Server:", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("/proxy?", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("tg://proxy", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string ExtractPostsHtml(string response)
        {
            if (string.IsNullOrEmpty(response))
                return response;

            if (response.TrimStart().StartsWith("{", StringComparison.Ordinal))
                return UnescapeJsonHtmlField(response);

            return response;
        }

        public static string BuildPostsBody(string csrf, string page, int offset)
        {
            return "_tgstat_csrk="
                + Uri.EscapeDataString(csrf)
                + "&page="
                + Uri.EscapeDataString(page)
                + "&offset="
                + offset
                + "&hideDeleted=1";
        }

        private static string ResolvePostsUrl(string channelUrl, string action)
        {
            action = action.Trim();
            if (action.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return action;

            Uri baseUri;
            if (!Uri.TryCreate(channelUrl, UriKind.Absolute, out baseUri))
                return action;

            Uri resolved;
            if (Uri.TryCreate(baseUri, action, out resolved))
                return resolved.ToString();

            return channelUrl.TrimEnd('/') + "/posts-last";
        }

        private static string UnescapeJsonHtmlField(string json)
        {
            var key = "\"html\":\"";
            var start = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return json;

            start += key.Length;
            var sb = new System.Text.StringBuilder();
            for (var i = start; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    var next = json[i + 1];
                    switch (next)
                    {
                        case '"':
                            sb.Append('"');
                            i++;
                            continue;
                        case '\\':
                            sb.Append('\\');
                            i++;
                            continue;
                        case 'n':
                            sb.Append('\n');
                            i++;
                            continue;
                        case 'r':
                            sb.Append('\r');
                            i++;
                            continue;
                        case 't':
                            sb.Append('\t');
                            i++;
                            continue;
                        case '/':
                            sb.Append('/');
                            i++;
                            continue;
                        case 'u':
                            if (i + 5 < json.Length)
                            {
                                var hex = json.Substring(i + 2, 4);
                                int code;
                                if (int.TryParse(
                                        hex,
                                        System.Globalization.NumberStyles.HexNumber,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out code))
                                {
                                    sb.Append((char)code);
                                    i += 5;
                                    continue;
                                }
                            }

                            break;
                    }
                }

                if (c == '"')
                    break;

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
