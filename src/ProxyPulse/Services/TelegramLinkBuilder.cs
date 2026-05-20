using System;
using System.Collections.Generic;
using ProxyPulse.Models;

namespace ProxyPulse.Services
{
    public static class TelegramLinkBuilder
    {
        public static string BuildTg(ProxyEntry entry)
        {
            return BuildUri("tg://proxy?", entry);
        }

        public static string BuildHttps(ProxyEntry entry)
        {
            return BuildUri("https://t.me/proxy?", entry);
        }

        public static IReadOnlyList<string> BuildAll(ProxyEntry entry)
        {
            return new[] { BuildTg(entry), BuildHttps(entry) };
        }

        private static string BuildUri(string prefix, ProxyEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            return string.Format(
                "{0}server={1}&port={2}&secret={3}",
                prefix,
                Uri.EscapeDataString(entry.Server ?? ""),
                entry.Port,
                Uri.EscapeDataString(entry.Secret ?? ""));
        }
    }
}
