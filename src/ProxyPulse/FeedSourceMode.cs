namespace ProxyPulse
{
    /// <summary>Откуда загружать ленту @ProxyMTProto.</summary>
    public enum FeedSourceMode
    {
        /// <summary>Прямой доступ: t.me / telegram.me.</summary>
        Direct = 0,

        /// <summary>Обходной: TGStat (актуальная лента) + web.archive.org.</summary>
        Bypass = 1
    }
}
