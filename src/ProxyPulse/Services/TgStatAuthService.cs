using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProxyPulse.Services
{
    internal sealed class TgStatAuthResult
    {
        public bool IsAuthenticated { get; set; }
        public bool CanPaginate { get; set; }
        public string Message { get; set; }
    }

    internal sealed class TgStatLoginStartResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AuthKey { get; set; }
        public CookieContainer Cookies { get; set; }
        public string TelegramDeepLink { get; set; }
    }

    internal sealed class TgStatPollResult
    {
        public bool IsAuthenticated { get; set; }
        public string Message { get; set; }
    }

    internal static class TgStatAuthService
    {
        private const string LoginUrl = "https://tgstat.com/login";
        private const string AuthUrl = "https://tgstat.com/auth";
        private const string TestChannelUrl = "https://tgstat.com/channel/@ProxyMTProto";
        private const string BotUsername = "tg_analytics_bot";
        public const string BotUsernameForUi = BotUsername;
        private const int PollIntervalMs = 2000;
        private const int AuthTimeoutMs = 180000;

        private static readonly Regex AuthButtonRegex = new Regex(
            @"data-telegram-auth-button=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static TgStatLoginStartResult StartTelegramLogin(CancellationToken cancellationToken)
        {
            try
            {
                var cookies = new CookieContainer();
                var html = HttpDownloadHelper.Download(
                    LoginUrl,
                    null,
                    cancellationToken,
                    20000,
                    2,
                    cookies);

                string authKey;
                if (!TryParseAuthKey(html, out authKey))
                {
                    return new TgStatLoginStartResult
                    {
                        Success = false,
                        Message = "Не удалось получить ключ входа TGStat."
                    };
                }

                return new TgStatLoginStartResult
                {
                    Success = true,
                    AuthKey = authKey,
                    Cookies = cookies,
                    TelegramDeepLink = BuildTelegramDeepLink(authKey),
                    Message = "Откройте Telegram и нажмите Start у @" + BotUsername + "."
                };
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;

                return new TgStatLoginStartResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public static TgStatPollResult PollTelegramAuth(
            CookieContainer cookies,
            string authKey,
            CancellationToken cancellationToken)
        {
            if (cookies == null || string.IsNullOrEmpty(authKey))
            {
                return new TgStatPollResult
                {
                    IsAuthenticated = false,
                    Message = "Сессия входа не инициализирована."
                };
            }

            try
            {
                var body = "auth_key=" + Uri.EscapeDataString(authKey);
                var response = HttpDownloadHelper.PostForm(
                    AuthUrl,
                    LoginUrl,
                    cookies,
                    body,
                    cancellationToken);

                if (IsAuthOk(response))
                {
                    return new TgStatPollResult
                    {
                        IsAuthenticated = true,
                        Message = "TGStat принял вход."
                    };
                }

                return new TgStatPollResult
                {
                    IsAuthenticated = false,
                    Message = "Ожидание Start у @" + BotUsername + "…"
                };
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;

                return new TgStatPollResult
                {
                    IsAuthenticated = false,
                    Message = ex.Message
                };
            }
        }

        public static TgStatAuthResult WaitForTelegramAuth(
            CookieContainer cookies,
            string authKey,
            Action<string> progress,
            CancellationToken cancellationToken)
        {
            var deadline = Environment.TickCount + AuthTimeoutMs;
            while (Environment.TickCount < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var poll = PollTelegramAuth(cookies, authKey, cancellationToken);
                if (progress != null)
                    progress(poll.Message);

                if (poll.IsAuthenticated)
                    return TestSession(cookies, cancellationToken);

                Thread.Sleep(PollIntervalMs);
            }

            return Fail("Время ожидания истекло. Нажмите Start у @" + BotUsername + " и повторите вход.");
        }

        public static bool TryOpenTelegramBot(string authKey, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(authKey))
            {
                error = "Ключ входа пустой.";
                return false;
            }

            var links = new[]
            {
                BuildTelegramDeepLink(authKey),
                "https://t.me/" + BotUsername + "?start=" + Uri.EscapeDataString(authKey)
            };

            Exception lastError = null;
            foreach (var link in links)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = link,
                        UseShellExecute = true
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            error = lastError != null ? lastError.Message : "Не удалось открыть Telegram.";
            return false;
        }

        public static TgStatAuthResult TestSession(CookieContainer cookies, CancellationToken cancellationToken)
        {
            if (cookies == null)
                return Fail("Cookies не заданы");

            try
            {
                var html = HttpDownloadHelper.Download(
                    TestChannelUrl,
                    null,
                    cancellationToken,
                    20000,
                    2,
                    cookies);

                if (html.IndexOf("Sign In", StringComparison.OrdinalIgnoreCase) >= 0
                    && html.IndexOf("popup_ajax", StringComparison.OrdinalIgnoreCase) >= 0
                    && html.IndexOf("data-src=\"/login\"", StringComparison.OrdinalIgnoreCase) >= 0
                    && html.IndexOf("data-post=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return Fail("Сессия не принята TGStat. Повторите вход через Telegram.");
                }

                string csrf;
                string page;
                int offset;
                if (!TgStatFeedHelper.TryParsePaginationForm(html, out csrf, out page, out offset))
                {
                    return new TgStatAuthResult
                    {
                        IsAuthenticated = true,
                        CanPaginate = false,
                        Message = "Страница канала загружена, но форма пагинации не найдена."
                    };
                }

                var postsUrl = TgStatFeedHelper.GetPostsEndpoint(TestChannelUrl, html);
                var response = HttpDownloadHelper.PostForm(
                    postsUrl,
                    TestChannelUrl,
                    cookies,
                    TgStatFeedHelper.BuildPostsBody(csrf, page, offset),
                    cancellationToken);

                if (TgStatFeedHelper.IsRestrictedResponse(response))
                {
                    return new TgStatAuthResult
                    {
                        IsAuthenticated = false,
                        CanPaginate = false,
                        Message = "Сессия сохранена, но TGStat всё ещё требует вход для «Show more»."
                    };
                }

                var chunkHtml = TgStatFeedHelper.ExtractPostsHtml(response);
                var hasPosts = !string.IsNullOrEmpty(chunkHtml)
                    && ProxyLinkParser.LooksLikeTelegramFeed(chunkHtml);

                return new TgStatAuthResult
                {
                    IsAuthenticated = true,
                    CanPaginate = hasPosts,
                    Message = hasPosts
                        ? "Вход подтверждён: следующие страницы TGStat доступны."
                        : "Ответ получен, но вторая страница пустая."
                };
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;

                return Fail(ex.Message);
            }
        }

        public static string BuildTelegramDeepLink(string authKey)
        {
            return "tg://resolve?domain=" + BotUsername + "&start=" + Uri.EscapeDataString(authKey);
        }

        private static bool TryParseAuthKey(string html, out string authKey)
        {
            authKey = null;
            if (string.IsNullOrEmpty(html))
                return false;

            var match = AuthButtonRegex.Match(html);
            if (!match.Success)
                return false;

            authKey = match.Groups[1].Value.Trim();
            return authKey.Length > 0;
        }

        private static bool IsAuthOk(string response)
        {
            return !string.IsNullOrEmpty(response)
                && response.IndexOf("\"status\":\"ok\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static TgStatAuthResult Fail(string message)
        {
            return new TgStatAuthResult
            {
                IsAuthenticated = false,
                CanPaginate = false,
                Message = message
            };
        }
    }
}
