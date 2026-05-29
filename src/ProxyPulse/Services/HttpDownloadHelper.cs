using System;
using System.IO;
using System.Net;
using System.Threading;

namespace ProxyPulse.Services
{
    internal static class HttpDownloadHelper
    {
        public const int DefaultTimeoutMs = 20000;
        public const int PaginationTimeoutMs = 22000;
        public const int CdxListTimeoutMs = 35000;
        public const int CdxSnapshotTimeoutMs = 20000;

        private const int MaxAttempts = 3;
        public const int PaginationMaxAttempts = 3;
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        private static readonly CookieContainer ArchiveCookies = new CookieContainer();

        static HttpDownloadHelper()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        public static string Download(
            string url,
            string referer = null,
            CancellationToken cancellationToken = default)
        {
            return Download(url, referer, cancellationToken, DefaultTimeoutMs, MaxAttempts);
        }

        public static string Download(
            string url,
            string referer,
            CancellationToken cancellationToken,
            int timeoutMs,
            int maxAttempts)
        {
            return Download(url, referer, cancellationToken, timeoutMs, maxAttempts, null);
        }

        public static string Download(
            string url,
            string referer,
            CancellationToken cancellationToken,
            int timeoutMs,
            int maxAttempts,
            CookieContainer cookies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isArchive = url.IndexOf("web.archive.org", StringComparison.OrdinalIgnoreCase) >= 0;
            Exception lastError = null;
            maxAttempts = Math.Max(1, maxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return DownloadOnce(url, referer, isArchive, cancellationToken, timeoutMs, cookies, false, null);
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    lastError = ex;
                    if (attempt < maxAttempts)
                        Thread.Sleep(600 * attempt);
                }
            }

            throw lastError ?? new WebException("Download failed");
        }

        public static string PostForm(
            string url,
            string referer,
            CookieContainer cookies,
            string formBody,
            CancellationToken cancellationToken,
            int timeoutMs = PaginationTimeoutMs,
            int maxAttempts = PaginationMaxAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Exception lastError = null;
            maxAttempts = Math.Max(1, maxAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return DownloadOnce(url, referer, false, cancellationToken, timeoutMs, cookies, true, formBody);
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    lastError = ex;
                    if (attempt < maxAttempts)
                        Thread.Sleep(600 * attempt);
                }
            }

            throw lastError ?? new WebException("POST failed");
        }

        private static string DownloadOnce(
            string url,
            string referer,
            bool isArchive,
            CancellationToken cancellationToken,
            int timeoutMs,
            CookieContainer cookies,
            bool isPost,
            string formBody)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = isPost ? "POST" : "GET";
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;
            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var isPagination = url.IndexOf("before=", StringComparison.OrdinalIgnoreCase) >= 0;
            request.KeepAlive = !isPagination && !isPost;
            request.Accept = isPost
                ? "*/*"
                : "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            if (!string.IsNullOrEmpty(referer))
            {
                request.Referer = referer;
                if (isPost)
                {
                    var origin = GetOrigin(referer);
                    if (!string.IsNullOrEmpty(origin))
                        request.Headers["Origin"] = origin;
                }
            }

            if (isPost)
            {
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers["X-Requested-With"] = "XMLHttpRequest";
            }

            if (isArchive)
            {
                request.Proxy = null;
                request.CookieContainer = cookies ?? ArchiveCookies;
            }
            else if (url.StartsWith("https://t.me", StringComparison.OrdinalIgnoreCase))
            {
                var systemProxy = WebRequest.GetSystemWebProxy();
                request.Proxy = systemProxy;
                request.Credentials = CredentialCache.DefaultCredentials;
                if (cookies != null)
                    request.CookieContainer = cookies;
            }
            else
            {
                request.Proxy = null;
                if (cookies != null)
                    request.CookieContainer = cookies;
            }

            if (isPost && formBody != null)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(formBody);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(bytes, 0, bytes.Length);
            }

            using (cancellationToken.Register(() =>
            {
                try
                {
                    request.Abort();
                }
                catch
                {
                }
            }))
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null))
            {
                return reader.ReadToEnd();
            }
        }

        private static string GetOrigin(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return null;

            return uri.Scheme + "://" + uri.Authority;
        }
    }
}
