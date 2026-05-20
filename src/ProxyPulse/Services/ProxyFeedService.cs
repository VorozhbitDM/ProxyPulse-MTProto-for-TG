using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ProxyPulse.Models;

namespace ProxyPulse.Services
{
    public sealed class ProxyFeedFetchResult
    {
        public ProxyFeedFetchResult()
        {
            Proxies = new List<ProxyEntry>();
            Errors = new List<string>();
        }

        public List<ProxyEntry> Proxies { get; set; }
        public int PagesLoaded { get; set; }
        public List<string> Errors { get; set; }
    }

    public sealed class ProxyFeedService
    {
        private const int MaxProxies = 500;
        private const int MaxPagesPerSource = 250;
        private const int MaxEmptyPagesInRow = 4;

        private static readonly string[] ArchiveBases =
        {
            "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto",
            "https://web.archive.org/web/https://t.me/s/ProxyMTProto"
        };

        public ProxyFeedFetchResult Fetch(Action<string> log = null)
        {
            var result = new ProxyFeedFetchResult();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Log(log, "Архив, снимок 1…");
            LoadArchivePages(ArchiveBases[0], seen, result, log, 1);

            Log(log, "Архив, снимок 2…");
            LoadArchivePages(ArchiveBases[1], seen, result, log, 2);

            if (result.Proxies.Count > MaxProxies)
                result.Proxies = result.Proxies.Take(MaxProxies).ToList();

            Log(log, string.Format(
                "Загрузка завершена: {0} прокси, {1} стр.",
                result.Proxies.Count,
                result.PagesLoaded));
            return result;
        }

        private static void LoadArchivePages(
            string archiveBase,
            HashSet<string> seen,
            ProxyFeedFetchResult result,
            Action<string> log,
            int sourceIndex)
        {
            var url = archiveBase;
            long? lastBeforeId = null;
            var emptyPagesInRow = 0;

            for (var page = 0; page < MaxPagesPerSource; page++)
            {
                try
                {
                    if (page > 0)
                        Thread.Sleep(350);

                    Log(log, string.Format(
                        "Архив {0}, стр. {1}: запрос…",
                        sourceIndex,
                        page + 1));

                    var html = HttpDownloadHelper.Download(url);
                    var parsed = ProxyLinkParser.Parse(html);

                    var added = 0;
                    foreach (var p in parsed)
                    {
                        if (seen.Add(p.Key))
                        {
                            result.Proxies.Add(p);
                            added++;
                        }
                    }

                    result.PagesLoaded++;

                    if (added == 0)
                        emptyPagesInRow++;
                    else
                        emptyPagesInRow = 0;

                    Log(log, string.Format(
                        "Архив {0}, стр. {1}: ссылок {2}, новых +{3}, всего {4}",
                        sourceIndex,
                        page + 1,
                        parsed.Count,
                        added,
                        result.Proxies.Count));

                    var beforeId = ProxyLinkParser.GetPaginationBeforeId(html);
                    if (!beforeId.HasValue)
                        break;

                    if (lastBeforeId.HasValue && beforeId.Value >= lastBeforeId.Value)
                        break;

                    lastBeforeId = beforeId;
                    url = archiveBase + "?before=" + beforeId.Value;

                    if (emptyPagesInRow >= MaxEmptyPagesInRow)
                        break;

                    if (result.Proxies.Count >= MaxProxies)
                        break;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(string.Format(
                        "Архив {0}, стр. {1}: {2}",
                        sourceIndex,
                        page + 1,
                        ex.Message));
                    Log(log, string.Format(
                        "Архив {0}, стр. {1}: ошибка — {2}",
                        sourceIndex,
                        page + 1,
                        ex.Message));
                    break;
                }
            }
        }

        private static void Log(Action<string> log, string message)
        {
            if (log != null)
                log(message);
        }
    }
}
