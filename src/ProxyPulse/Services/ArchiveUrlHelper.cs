using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProxyPulse.Services
{
    internal static class ArchiveUrlHelper
    {
        private static readonly Regex BeforeInUrlRegex = new Regex(
            @"before=(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static long? ParseBeforeId(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var m = BeforeInUrlRegex.Match(url);
            if (!m.Success)
                return null;

            long id;
            return long.TryParse(m.Groups[1].Value, out id) ? id : (long?)null;
        }

        public static IEnumerable<string> GetFirstPageCandidates(string entryUrl)
        {
            if (!string.IsNullOrEmpty(entryUrl))
                yield return entryUrl;

            yield return "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto";
        }

        public static string FirstPageForSnapshot(string snapshotId)
        {
            return "https://web.archive.org/web/" + snapshotId + "if_/https://t.me/s/ProxyMTProto";
        }
    }
}
