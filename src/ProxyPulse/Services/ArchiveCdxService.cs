using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProxyPulse.Services
{
    /// <summary>Снимки канала через CDX API archive.org.</summary>
    internal static class ArchiveCdxService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
        private static readonly object CacheLock = new object();
        private static List<string> _cachedIds;
        private static DateTime _cachedAtUtc;

        /// <summary>Брать только снимки не старше этого срока (строка timestamp CDX).</summary>
        public const int FreshSnapshotMaxAgeDays = 120;

        private const string SortNewestFirst = "&sort=reverse";

        private static readonly string[] CdxUrlCandidates =
        {
            "https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=text&fl=timestamp&collapse=timestamp&limit=40" + SortNewestFirst,
            "https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=json&fl=timestamp&filter=statuscode:200&collapse=digest&limit=40" + SortNewestFirst,
            "https://web.archive.org/cdx/search/cdx?url=https://t.me/s/ProxyMTProto&output=json&fl=timestamp&filter=statuscode:200&collapse=digest&limit=40" + SortNewestFirst
        };

        private static readonly Regex TimestampJsonRegex = new Regex(
            @"\[""(\d{14})""\]",
            RegexOptions.Compiled);

        public static IList<string> GetRecentSnapshotIds(
            Action<string> log = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (CacheLock)
            {
                if (_cachedIds != null && _cachedIds.Count > 0 &&
                    DateTime.UtcNow - _cachedAtUtc < CacheTtl)
                {
                    if (log != null)
                        log(string.Format("CDX: список из кэша ({0} снимков)", _cachedIds.Count));
                    return new List<string>(_cachedIds);
                }
            }

            if (log != null)
                log("CDX: запрос списка снимков (сервер archive.org, обычно 5–20 с)…");

            var ids = new List<string>();
            string lastError = null;

            foreach (var url in CdxUrlCandidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var isText = url.IndexOf("output=text", StringComparison.OrdinalIgnoreCase) >= 0;

                try
                {
                    var body = HttpDownloadHelper.Download(
                        url,
                        "https://web.archive.org/",
                        cancellationToken,
                        HttpDownloadHelper.CdxListTimeoutMs,
                        1);

                    ParseTimestamps(body, isText, ids);
                    if (ids.Count > 0)
                    {
                        lock (CacheLock)
                        {
                            _cachedIds = new List<string>(ids);
                            _cachedAtUtc = DateTime.UtcNow;
                        }

                        ids = FilterNewestFirst(ids);

                        if (log != null)
                        {
                            var newest = ids.Count > 0 ? ids[0] : "—";
                            log(string.Format(
                                "CDX: {0} снимков (сначала самые новые, от {1})",
                                ids.Count,
                                FormatSnapshotDate(newest)));
                        }

                        return ids;
                    }

                    lastError = "пустой ответ";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    if (log != null)
                        log("CDX: " + ex.Message);
                }
            }

            if (log != null && ids.Count == 0)
                log(string.Format("CDX: список не получен ({0})", lastError ?? "нет данных"));

            return FilterNewestFirst(ids);
        }

        /// <summary>Новые → старые; отсекаем снимки старше <see cref="FreshSnapshotMaxAgeDays"/>.</summary>
        public static List<string> FilterNewestFirst(IList<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<string>();

            var cutoff = GetFreshnessCutoffTimestamp();
            var sorted = ids
                .Where(id => !string.IsNullOrEmpty(id) && id.Length == 14)
                .Where(id => string.Compare(id, cutoff, StringComparison.Ordinal) >= 0)
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(id => id, StringComparer.Ordinal)
                .ToList();

            return sorted;
        }

        public static string GetFreshnessCutoffTimestamp()
        {
            return DateTime.UtcNow
                .AddDays(-FreshSnapshotMaxAgeDays)
                .ToString("yyyyMMddHHmmss");
        }

        private static string FormatSnapshotDate(string timestampId)
        {
            if (string.IsNullOrEmpty(timestampId) || timestampId.Length != 14)
                return timestampId ?? "—";

            DateTime dt;
            if (DateTime.TryParseExact(
                    timestampId,
                    "yyyyMMddHHmmss",
                    null,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out dt))
                return dt.ToString("dd.MM.yyyy HH:mm") + " UTC";

            return timestampId;
        }

        private static void ParseTimestamps(string body, bool isText, List<string> ids)
        {
            if (string.IsNullOrEmpty(body))
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
                seen.Add(id);

            if (isText)
            {
                foreach (var line in body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = line.Trim().Trim('"', '[', ']', ',');
                    if (t.Length != 14 || !char.IsDigit(t[0]))
                        continue;
                    if (string.Equals(t, "timestamp", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (seen.Add(t))
                        ids.Add(t);
                }

                return;
            }

            foreach (Match m in TimestampJsonRegex.Matches(body))
            {
                var ts = m.Groups[1].Value;
                if (seen.Add(ts))
                    ids.Add(ts);
            }

            if (ids.Count > 0)
                return;

            foreach (Match m in Regex.Matches(body, @"(\d{14})"))
            {
                var ts = m.Groups[1].Value;
                if (seen.Add(ts))
                    ids.Add(ts);
            }
        }
    }
}
