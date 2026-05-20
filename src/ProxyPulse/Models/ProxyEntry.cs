using System;

namespace ProxyPulse.Models
{
    public sealed class ProxyEntry
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Secret { get; set; }
        public int? PingMs { get; set; }
        public bool IsAvailable { get; set; }

        public string Key
        {
            get { return string.Format("{0}|{1}|{2}", Server, Port, Secret); }
        }

        public string DisplayLabel
        {
            get { return string.Format("{0}:{1}", Server, Port); }
        }

        public string PingDisplay
        {
            get
            {
                return IsAvailable && PingMs.HasValue
                    ? string.Format("{0} ms", PingMs.Value)
                    : "—";
            }
        }

        public ProxyEntry Clone()
        {
            return new ProxyEntry
            {
                Server = Server,
                Port = Port,
                Secret = Secret,
                PingMs = PingMs,
                IsAvailable = IsAvailable
            };
        }
    }
}
