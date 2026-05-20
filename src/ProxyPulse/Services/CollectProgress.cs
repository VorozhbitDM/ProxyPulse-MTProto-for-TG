namespace ProxyPulse.Services
{
    /// <summary>Прогресс сбора с archive.org.</summary>
    public sealed class CollectProgress
    {
        public bool IsCdxPhase { get; set; }
        public int CdxSnapshotsDone { get; set; }
        public int CdxSnapshotsTarget { get; set; }
        public int PagesDone { get; set; }
        public int PagesTarget { get; set; }
        public int ProxiesFound { get; set; }
        public int ProxiesTarget { get; set; }
    }
}
