using System;
using System.Diagnostics;
using ProxyPulse.Models;

namespace ProxyPulse.Services
{
    internal static class TelegramLauncher
    {
        public static void OpenProxy(ProxyEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            if (string.IsNullOrWhiteSpace(entry.Server) || entry.Port <= 0)
                throw new InvalidOperationException("Некорректные данные прокси.");

            if (string.IsNullOrWhiteSpace(entry.Secret))
                throw new InvalidOperationException("У прокси нет secret — подключение невозможно.");

            Exception lastError = null;
            foreach (var url in TelegramLinkBuilder.BuildAll(entry))
            {
                try
                {
                    if (TryStart(url))
                        return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            if (lastError != null)
                throw new InvalidOperationException(
                    "Не удалось открыть Telegram. Установите Telegram Desktop или откройте ссылку вручную.\r\n" +
                    lastError.Message,
                    lastError);
        }

        private static bool TryStart(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
    }
}
