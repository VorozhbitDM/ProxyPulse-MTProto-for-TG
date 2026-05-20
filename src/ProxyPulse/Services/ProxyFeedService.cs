using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProxyPulse.Models;

namespace ProxyPulse.Services
{
    public sealed class ProxyFeedService
    {
        /// <summary>Сколько последних уникальных прокси собрать.</summary>
        public const int DefaultMaxRecentProxies = 100;

        /// <summary>Сколько снимков CDX перебрать (это «страницы» вместо ?before=).</summary>
        public const int DefaultMaxCdxSnapshotsToScan = 30;

        private const int MaxUrlCandidatesFirstPage = 3;
        private const int MsBetweenCdxSnapshots = 1200;

        private const string ArchiveEntryUrl =
            "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto";

        /// <summary>
        /// Сбор: 1-я стр. актуальной ленты + снимки CDX (проверено: ?before= не отдаёт ленту).
        /// </summary>
        public void FetchToList(
            List<ProxyEntry> output,
            Action<string> log,
            IProgress<CollectProgress> progress,
            CancellationToken cancellationToken,
            int maxRecentProxies = DefaultMaxRecentProxies,
            int maxCdxSnapshotsToScan = DefaultMaxCdxSnapshotsToScan)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var cap = Math.Max(1, maxRecentProxies);
            var cdxCap = Math.Max(1, maxCdxSnapshotsToScan);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            ReportCdx(progress, 0, cdxCap, 0, cap);
            Log(log, string.Format(
                "Сбор: лента (1 стр.) + до {0} снимков CDX → {1} прокси…",
                cdxCap,
                cap));

            var cdxListTask = Task.Run(
                () => ArchiveCdxService.GetRecentSnapshotIds(log, cancellationToken),
                cancellationToken);

            string feedSnapshotId;
            if (!TryLoadFeedFirstPage(seen, output, errors, log, cancellationToken, cap, out feedSnapshotId))
                Log(log, "Лента: первая страница недоступна");

            ReportCdx(progress, 0, cdxCap, seen.Count, cap);

            var cdxOk = 0;
            if (!cancellationToken.IsCancellationRequested && seen.Count < cap)
            {
                IList<string> snapshots;
                try
                {
                    snapshots = cdxListTask.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    Log(log, "CDX: " + ex.Message);
                    snapshots = new List<string>();
                }

                cdxOk = LoadCdxSnapshotsUntilFull(
                    seen,
                    output,
                    errors,
                    log,
                    progress,
                    cancellationToken,
                    cdxCap,
                    cap,
                    snapshots,
                    feedSnapshotId);
            }

            Log(log, string.Format(
                "Сбор завершён: {0}/{1} прокси · CDX снимков {2}",
                seen.Count,
                cap,
                cdxOk));
        }

        private bool TryLoadFeedFirstPage(
            HashSet<string> seen,
            List<ProxyEntry> output,
            List<string> errors,
            Action<string> log,
            CancellationToken cancellationToken,
            int proxyCap,
            out string feedSnapshotId)
        {
            feedSnapshotId = null;
            Log(log, "Лента: последний снимок (web/2)…");

            foreach (var candidate in ArchiveUrlHelper.GetFirstPageCandidates(ArchiveEntryUrl))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var html = HttpDownloadHelper.Download(
                        candidate,
                        "https://web.archive.org/",
                        cancellationToken);

                    if (!ProxyLinkParser.LooksLikeTelegramFeed(html))
                    {
                        Log(log, "Лента: ответ без постов Telegram, другой URL…");
                        continue;
                    }

                    feedSnapshotId = ProxyLinkParser.GetArchiveSnapshotId(html);
                    var added = EmitNewProxies(ProxyLinkParser.Parse(html), seen, output, cancellationToken, proxyCap);
                    if (!string.IsNullOrEmpty(feedSnapshotId))
                    {
                        Log(log, string.Format(
                            "Лента: снимок {0}, +{1} новых, всего {2}/{3}",
                            feedSnapshotId,
                            added,
                            seen.Count,
                            proxyCap));
                    }
                    else
                        Log(log, string.Format("Лента: +{0} новых, всего {1}/{2}", added, seen.Count, proxyCap));

                    return true;
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    Log(log, "Лента: " + ex.Message);
                }
            }

            lock (errors)
            {
                errors.Add("Лента: не удалось загрузить первую страницу");
            }

            return false;
        }

        private int LoadCdxSnapshotsUntilFull(
            HashSet<string> seen,
            List<ProxyEntry> output,
            List<string> errors,
            Action<string> log,
            IProgress<CollectProgress> progress,
            CancellationToken cancellationToken,
            int maxSnapshots,
            int proxyCap,
            IList<string> snapshots,
            string skipSnapshotId)
        {
            if (snapshots == null)
                snapshots = new List<string>();

            var batch = ArchiveCdxService.FilterNewestFirst(snapshots is List<string> list ? list : snapshots.ToList())
                .Where(id => string.IsNullOrEmpty(skipSnapshotId)
                    || !string.Equals(id, skipSnapshotId, StringComparison.Ordinal))
                .Take(maxSnapshots)
                .ToList();

            if (batch.Count == 0)
            {
                Log(log, "CDX: нет дополнительных свежих снимков");
                return 0;
            }

            Log(log, string.Format(
                "CDX: до {0} снимков, с {1}…",
                batch.Count,
                batch[0]));
            var loaded = 0;

            for (var i = 0; i < batch.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (seen.Count >= proxyCap)
                    break;

                var snapshotId = batch[i];
                if (i > 0)
                    Thread.Sleep(MsBetweenCdxSnapshots);

                try
                {
                    var url = ArchiveUrlHelper.FirstPageForSnapshot(snapshotId);
                    var html = HttpDownloadHelper.Download(
                        url,
                        "https://web.archive.org/",
                        cancellationToken,
                        HttpDownloadHelper.CdxSnapshotTimeoutMs,
                        2);

                    if (!ProxyLinkParser.LooksLikeTelegramFeed(html))
                    {
                        Log(log, string.Format("CDX {0}/{1} ({2}): пустая страница", i + 1, batch.Count, snapshotId));
                        continue;
                    }

                    var added = EmitNewProxies(ProxyLinkParser.Parse(html), seen, output, cancellationToken, proxyCap);
                    loaded++;
                    ReportCdx(progress, i + 1, batch.Count, seen.Count, proxyCap);

                    Log(log, string.Format(
                        "CDX {0}/{1} ({2}): +{3} новых, всего {4}/{5}",
                        i + 1,
                        batch.Count,
                        snapshotId,
                        added,
                        seen.Count,
                        proxyCap));

                    if (seen.Count >= proxyCap)
                    {
                        Log(log, string.Format("Достигнут лимит {0} прокси", proxyCap));
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    lock (errors)
                    {
                        errors.Add("CDX " + snapshotId + ": " + ex.Message);
                    }

                    Log(log, string.Format("CDX {0}/{1} ({2}): {3}", i + 1, batch.Count, snapshotId, ex.Message));
                }
            }

            return loaded;
        }

        private static int EmitNewProxies(
            List<ProxyEntry> parsed,
            HashSet<string> seen,
            List<ProxyEntry> output,
            CancellationToken cancellationToken,
            int proxyCap)
        {
            var added = 0;
            foreach (var p in parsed)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (seen.Count >= proxyCap)
                    break;

                if (!seen.Add(p.Key))
                    continue;

                added++;
                output.Add(p);
            }

            return added;
        }

        private static void ReportCdx(
            IProgress<CollectProgress> progress,
            int snapshotsDone,
            int snapshotsTarget,
            int proxiesFound,
            int proxyCap)
        {
            if (progress == null)
                return;

            progress.Report(new CollectProgress
            {
                IsCdxPhase = true,
                CdxSnapshotsDone = snapshotsDone,
                CdxSnapshotsTarget = Math.Max(1, snapshotsTarget),
                ProxiesFound = proxiesFound,
                ProxiesTarget = proxyCap
            });
        }

        private static void Log(Action<string> log, string message)
        {
            if (log != null)
                log(message);
        }
    }
}
