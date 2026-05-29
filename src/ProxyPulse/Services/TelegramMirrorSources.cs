using System.Collections.Generic;

namespace ProxyPulse.Services
{
    internal static class TelegramMirrorSources
    {
        public static readonly string[] DirectFeedUrls =
        {
            "https://t.me/s/ProxyMTProto",
            "https://telegram.me/s/ProxyMTProto"
        };

        public static readonly string[] MirrorFeedUrls =
        {
            "https://tgstat.com/channel/@ProxyMTProto",
            "https://tgstat.ru/channel/@ProxyMTProto"
        };

        public static IEnumerable<string> GetUrls(FeedSourceMode mode)
        {
            switch (mode)
            {
                case FeedSourceMode.Direct:
                    foreach (var url in DirectFeedUrls)
                        yield return url;
                    yield break;
                case FeedSourceMode.Bypass:
                    foreach (var url in MirrorFeedUrls)
                        yield return url;
                    yield break;
                default:
                    yield break;
            }
        }
    }
}
