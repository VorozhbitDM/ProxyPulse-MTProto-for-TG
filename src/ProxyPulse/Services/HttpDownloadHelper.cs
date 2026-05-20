using System.IO;
using System.Net;
namespace ProxyPulse.Services
{
    internal static class HttpDownloadHelper
    {
        private const int TimeoutMs = 15000;
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        static HttpDownloadHelper()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        public static string Download(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var systemProxy = WebRequest.GetSystemWebProxy();
            if (systemProxy != null && url.StartsWith("https://t.me"))
            {
                request.Proxy = systemProxy;
                request.Credentials = CredentialCache.DefaultCredentials;
            }
            else
            {
                request.Proxy = null;
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
