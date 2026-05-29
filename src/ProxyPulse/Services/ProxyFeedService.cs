using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ProxyPulse;
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
        private const int MaxFeedPagesToScan = 40;
        private const int MsBetweenFeedPages = 900;
        private const int MsBetweenCdxSnapshots = 1200;

        private const string ArchiveEntryUrl =
            "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto";

        /// <summary>
        /// Сбор из выбранного источника ленты (t.me / archive / зеркала).
        /// </summary>
        public void FetchToList(
            List<ProxyEntry> output,
            Action<string> log,
            IProgress<CollectProgress> progress,
            CancellationToken cancellationToken,
            int maxRecentProxies = DefaultMaxRecentProxies,
            int maxCdxSnapshotsToScan = DefaultMaxCdxSnapshotsToScan,
            FeedSourceMode feedSource = FeedSourceMode.Bypass)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var cap = Math.Max(1, maxRecentProxies);
            var cdxCap = Math.Max(1, maxCdxSnapshotsToScan);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            ReportCdx(progress, 0, cdxCap, 0, cap);
            Log(log, string.Format(
                "Загрузка ленты → {0} прокси…",
                cap));

            if (feedSource == FeedSourceMode.Direct)
            {
                Log(log, "Источник: t.me");
                TryLoadExternalFeed(
                    seen,
                    output,
                    errors,
                    log,
                    cancellationToken,
                    cap,
                    feedSource);

                Log(log, string.Format(
                    "Сбор завершён (t.me): {0}/{1} прокси",
                    seen.Count,
                    cap));
                return;
            }

            Log(log, "Источник: TGStat + archive.org");
            TryLoadExternalFeed(
                seen,
                output,
                errors,
                log,
                cancellationToken,
                cap,
                feedSource);

            if (seen.Count >= cap)
            {
                Log(log, string.Format(
                    "Сбор завершён: {0}/{1} прокси",
                    seen.Count,
                    cap));
                return;
            }

            Log(log, string.Format(
                "TGStat: {0}/{1} — добираем из archive.org…",
                seen.Count,
                cap));

            LoadArchiveFeed(
                seen,
                output,
                errors,
                log,
                progress,
                cancellationToken,
                cdxCap,
                cap,
                maxCdxSnapshotsToScan);
        }

        private void LoadArchiveFeed(
            HashSet<string> seen,
            List<ProxyEntry> output,
            List<string> errors,
            Action<string> log,
            IProgress<CollectProgress> progress,
            CancellationToken cancellationToken,
            int cdxCap,
            int cap,
            int maxCdxSnapshotsToScan)
        {
            Log(log, string.Format(
                "Архив: лента (1 стр.) + до {0} снимков CDX…",
                cdxCap));

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

        private void TryLoadExternalFeed(
            HashSet<string> seen,
            List<ProxyEntry> output,
            List<string> errors,
            Action<string> log,
            CancellationToken cancellationToken,
            int proxyCap,
            FeedSourceMode feedSource)
        {
            TgStatSessionStore.Reload();
            var loadedAny = false;
            foreach (var url in TelegramMirrorSources.GetUrls(feedSource))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (seen.Count >= proxyCap)
                    break;

                try
                {
                    if (feedSource == FeedSourceMode.Bypass && TgStatFeedHelper.IsTgStatUrl(url))
                    {
                        var added = LoadTgStatFeed(
                            url,
                            seen,
                            output,
                            log,
                            cancellationToken,
                            proxyCap);
                        if (added > 0)
                            loadedAny = true;
                    }
                    else
                    {
                        var added = LoadTelegramWebFeed(url, seen, output, log, cancellationToken, proxyCap);
                        if (added > 0)
                            loadedAny = true;
                    }
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    Log(log, "Лента: " + ex.Message);
                }
            }

            if (!loadedAny && feedSource == FeedSourceMode.Bypass)
            {
                lock (errors)
                {
                    errors.Add("TGStat: не удалось загрузить ленту");
                }
            }
            else if (!loadedAny && feedSource == FeedSourceMode.Direct)
            {
                lock (errors)
                {
                    errors.Add("Лента: не удалось загрузить t.me");
                }
            }
        }

        private int LoadTelegramWebFeed(
            string url,
            HashSet<string> seen,
            List<ProxyEntry> output,
            Action<string> log,
            CancellationToken cancellationToken,
            int proxyCap)
        {
            Log(log, string.Format("Лента: {0}…", url));
            var html = HttpDownloadHelper.Download(url, null, cancellationToken, 20000, 2);
            if (!ProxyLinkParser.LooksLikeTelegramFeed(html))
            {
                Log(log, "Лента: ответ без постов Telegram, другой URL…");
                return 0;
            }

            var totalAdded = EmitNewProxies(ProxyLinkParser.ParseFeed(html), seen, output, cancellationToken, proxyCap);
            Log(log, string.Format("Лента: {0}, +{1} новых, всего {2}/{3}", url, totalAdded, seen.Count, proxyCap));
            if (seen.Count >= proxyCap)
                return totalAdded;

            var beforeId = ProxyLinkParser.GetNextPageBeforeId(html);
            var pageNum = 1;
            while (beforeId.HasValue && seen.Count < proxyCap && pageNum < MaxFeedPagesToScan)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(MsBetweenFeedPages);
                pageNum++;

                var pageUrl = ProxyLinkParser.BuildTelegramPageUrl(url, beforeId.Value);
                Log(log, string.Format("Лента: страница {0}, before={1}…", pageNum, beforeId.Value));
                html = HttpDownloadHelper.Download(
                    pageUrl,
                    url,
                    cancellationToken,
                    HttpDownloadHelper.PaginationTimeoutMs,
                    HttpDownloadHelper.PaginationMaxAttempts);

                if (!ProxyLinkParser.LooksLikeTelegramFeed(html))
                {
                    Log(log, string.Format("Лента: страница {0} пустая, остановка", pageNum));
                    break;
                }

                var added = EmitNewProxies(ProxyLinkParser.ParseFeed(html), seen, output, cancellationToken, proxyCap);
                totalAdded += added;
                Log(log, string.Format("Лента: стр. {0}, +{1} новых, всего {2}/{3}", pageNum, added, seen.Count, proxyCap));
                if (added == 0)
                    break;

                var nextBefore = ProxyLinkParser.GetNextPageBeforeId(html);
                if (!nextBefore.HasValue || nextBefore.Value >= beforeId.Value)
                    break;

                beforeId = nextBefore;
            }

            return totalAdded;
        }

        private int LoadTgStatFeed(
            string channelUrl,
            HashSet<string> seen,
            List<ProxyEntry> output,
            Action<string> log,
            CancellationToken cancellationToken,
            int proxyCap)
        {
            Log(log, string.Format("Лента: {0}…", channelUrl));
            var cookies = TgStatSessionStore.HasSession
                ? TgStatSessionStore.CreateCookieContainer()
                : new CookieContainer();

            if (TgStatSessionStore.HasSession)
                Log(log, "TGStat: используется сохранённая сессия из настроек");
            var html = HttpDownloadHelper.Download(
                channelUrl,
                null,
                cancellationToken,
                20000,
                2,
                cookies);

            if (!ProxyLinkParser.LooksLikeTelegramFeed(html))
            {
                Log(log, "Лента: ответ без постов, другой URL…");
                return 0;
            }

            var totalAdded = EmitNewProxies(ProxyLinkParser.ParseFeed(html), seen, output, cancellationToken, proxyCap);
            Log(log, string.Format("Лента: {0}, +{1} новых, всего {2}/{3}", channelUrl, totalAdded, seen.Count, proxyCap));
            if (seen.Count >= proxyCap)
                return totalAdded;

            string csrf;
            string page;
            int offset;
            if (!TgStatFeedHelper.TryParsePaginationForm(html, out csrf, out page, out offset))
            {
                Log(log, "TGStat: форма «Show more» не найдена");
                return totalAdded;
            }

            var postsUrl = TgStatFeedHelper.GetPostsEndpoint(channelUrl, html);
            var initialCsrf = csrf;
            var pageNum = 0;
            var restrictedLogged = false;
            var emptyPagesInRow = 0;
            var duplicatePagesInRow = 0;
            while (seen.Count < proxyCap && pageNum < MaxFeedPagesToScan)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(MsBetweenFeedPages);
                pageNum++;

                var response = HttpDownloadHelper.PostForm(
                    postsUrl,
                    channelUrl,
                    cookies,
                    TgStatFeedHelper.BuildPostsBody(csrf, page, offset),
                    cancellationToken);

                if (TgStatFeedHelper.IsRestrictedResponse(response))
                {
                    if (!restrictedLogged)
                    {
                        if (TgStatSessionStore.HasSession)
                            Log(log, "TGStat: сессия не принята — повторите вход в настройках");
                        else
                            Log(log, "TGStat: следующие страницы только после входа (Настройки → TGStat)");

                        restrictedLogged = true;
                    }

                    break;
                }

                if (TgStatFeedHelper.IsEndOfFeedResponse(response))
                {
                    Log(log, string.Format("TGStat: лента закончилась на стр. {0}", pageNum));
                    break;
                }

                var chunkHtml = TgStatFeedHelper.ExtractPostsHtml(response);
                if (!TgStatFeedHelper.ContainsProxyPosts(chunkHtml))
                {
                    if (TgStatFeedHelper.ContainsProxyPosts(response))
                    {
                        chunkHtml = response;
                        emptyPagesInRow = 0;
                    }
                    else
                    {
                        emptyPagesInRow++;
                        Log(log, string.Format(
                            "TGStat: стр. {0} пустая ({1}/3), offset={2}",
                            pageNum,
                            emptyPagesInRow,
                            offset));
                        if (emptyPagesInRow >= 3)
                            break;
                    }
                }
                else
                {
                    emptyPagesInRow = 0;
                }

                if (TgStatFeedHelper.ContainsProxyPosts(chunkHtml))
                {
                    var added = EmitNewProxies(ProxyLinkParser.ParseFeed(chunkHtml), seen, output, cancellationToken, proxyCap);
                    totalAdded += added;
                    if (added > 0)
                    {
                        duplicatePagesInRow = 0;
                        Log(log, string.Format(
                            "TGStat: стр. {0}, +{1} новых, всего {2}/{3}",
                            pageNum,
                            added,
                            seen.Count,
                            proxyCap));
                    }
                    else
                    {
                        duplicatePagesInRow++;
                        Log(log, string.Format(
                            "TGStat: стр. {0}, только дубликаты ({1}/3), всего {2}/{3}",
                            pageNum,
                            duplicatePagesInRow,
                            seen.Count,
                            proxyCap));
                        if (duplicatePagesInRow >= 3)
                        {
                            Log(log, "TGStat: 3 страницы подряд с дубликатами — переход к archive.org");
                            break;
                        }
                    }
                }

                string refreshedCsrf;
                if (TgStatFeedHelper.TryRefreshCsrf(response, chunkHtml, out refreshedCsrf)
                    && !string.IsNullOrEmpty(refreshedCsrf))
                {
                    csrf = refreshedCsrf;
                }
                else
                {
                    csrf = initialCsrf;
                }

                string nextPage;
                int nextOffset;
                var previousOffset = offset;
                if (!TgStatFeedHelper.TryAdvancePagination(response, chunkHtml, page, offset, out nextPage, out nextOffset))
                {
                    Log(log, string.Format("TGStat: offset не меняется ({0}), остановка", offset));
                    break;
                }

                page = nextPage;
                offset = nextOffset;
                if (offset <= previousOffset)
                {
                    Log(log, string.Format("TGStat: offset застрял на {0}, остановка", offset));
                    break;
                }
            }

            return totalAdded;
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
                    var added = EmitNewProxies(ProxyLinkParser.ParseFeed(html), seen, output, cancellationToken, proxyCap);
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

                    LoadArchiveFeedPages(
                        html,
                        feedSnapshotId,
                        seen,
                        output,
                        log,
                        cancellationToken,
                        proxyCap,
                        candidate);

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

        private void LoadArchiveFeedPages(
            string html,
            string snapshotId,
            HashSet<string> seen,
            List<ProxyEntry> output,
            Action<string> log,
            CancellationToken cancellationToken,
            int proxyCap,
            string referer)
        {
            if (seen.Count >= proxyCap)
                return;

            var beforeId = ProxyLinkParser.GetNextPageBeforeId(html);
            if (!beforeId.HasValue)
                return;

            var snap = string.IsNullOrEmpty(snapshotId) ? "2" : snapshotId;
            var pageNum = 1;
            while (beforeId.HasValue && seen.Count < proxyCap && pageNum < MaxFeedPagesToScan)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(MsBetweenFeedPages);
                pageNum++;

                var pageUrl = ProxyLinkParser.BuildArchivePageUrl(snap, beforeId);
                Log(log, string.Format("Лента archive: стр. {0}, before={1}…", pageNum, beforeId.Value));
                try
                {
                    html = HttpDownloadHelper.Download(
                        pageUrl,
                        referer ?? "https://web.archive.org/",
                        cancellationToken,
                        HttpDownloadHelper.PaginationTimeoutMs,
                        HttpDownloadHelper.PaginationMaxAttempts);

                    if (!ProxyLinkParser.LooksLikeTelegramFeed(html))
                    {
                        Log(log, string.Format("Лента archive: стр. {0} пустая, остановка", pageNum));
                        break;
                    }

                    var added = EmitNewProxies(ProxyLinkParser.ParseFeed(html), seen, output, cancellationToken, proxyCap);
                    Log(log, string.Format(
                        "Лента archive: стр. {0}, +{1} новых, всего {2}/{3}",
                        pageNum,
                        added,
                        seen.Count,
                        proxyCap));

                    if (added == 0)
                        break;

                    var nextBefore = ProxyLinkParser.GetNextPageBeforeId(html);
                    if (!nextBefore.HasValue || nextBefore.Value >= beforeId.Value)
                        break;

                    beforeId = nextBefore;
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    Log(log, string.Format("Лента archive: стр. {0}: {1}", pageNum, ex.Message));
                    break;
                }
            }
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

                    var added = EmitNewProxies(ProxyLinkParser.ParseFeed(html), seen, output, cancellationToken, proxyCap);
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
                if (seen.Count >= proxyCap && !seen.Contains(p.Key))
                    break;

                if (seen.Contains(p.Key))
                {
                    MergePublishedDate(output, p);
                    continue;
                }

                seen.Add(p.Key);
                added++;
                output.Add(p);
            }

            return added;
        }

        private static void MergePublishedDate(List<ProxyEntry> output, ProxyEntry incoming)
        {
            if (!incoming.PublishedAt.HasValue)
                return;

            for (var i = 0; i < output.Count; i++)
            {
                if (!string.Equals(output[i].Key, incoming.Key, StringComparison.OrdinalIgnoreCase))
                    continue;

                var existing = output[i];
                if (!existing.PublishedAt.HasValue || incoming.PublishedAt > existing.PublishedAt)
                    existing.PublishedAt = incoming.PublishedAt;
                return;
            }
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
