// Mimics ProxyPulse HttpDownloadHelper for pagination verification.
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

class VerifyPagination
{
    const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    static readonly CookieContainer Cookies = new CookieContainer();

    static void Main()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        var entry = "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto";
        Console.WriteLine("=== C# HttpWebRequest (like app) ===");

        string html1;
        try
        {
            html1 = Download(entry, null, false);
            Console.WriteLine("P1 OK len=" + html1.Length + " proxies=" + CountProxies(html1));
        }
        catch (Exception ex)
        {
            Console.WriteLine("P1 FAIL: " + ex.Message);
            return;
        }

        var snap = Match(html1, @"/web/(\d{14})");
        var rel = Match(html1, @"<link\s+rel=""prev""\s+href=""([^""]+)""");
        var before = Match(html1, @"before=(\d+)");
        Console.WriteLine("snap=" + snap + " rel=" + rel + " before=" + before);

        System.Threading.Thread.Sleep(2000);

        var star = "https://web.archive.org/web/" + snap + "*/https://t.me/s/ProxyMTProto?before=" + before;
        var urls = new[]
        {
            star,
            "https://web.archive.org" + rel,
            "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto?before=" + before,
            "https://web.archive.org/web/" + snap + "if_/https://t.me/s/ProxyMTProto?before=" + before
        };

        foreach (var url in urls)
        {
            System.Threading.Thread.Sleep(1500);
            try
            {
                var html = Download(url, entry, true);
                Console.WriteLine("OK " + Short(url) + " len=" + html.Length + " proxies=" + CountProxies(html) +
                    " minPost=" + MinPost(html));
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL " + Short(url));
                Console.WriteLine("     " + ex.Message);
            }
        }
    }

    static string Download(string url, string referer, bool pagination)
    {
        var req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "GET";
        req.Timeout = 45000;
        req.ReadWriteTimeout = 45000;
        req.UserAgent = UserAgent;
        req.AllowAutoRedirect = true;
        req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        req.KeepAlive = !pagination;
        req.CookieContainer = Cookies;
        req.Proxy = null;
        if (!string.IsNullOrEmpty(referer))
            req.Referer = referer;

        using (var res = (HttpWebResponse)req.GetResponse())
        using (var stream = res.GetResponseStream())
        using (var reader = new StreamReader(stream ?? Stream.Null))
            return reader.ReadToEnd();
    }

    static int CountProxies(string html) =>
        Regex.Matches(html ?? "", @"(?:tg://proxy\?|/proxy\?|t\.me/proxy\?)").Count;

    static string MinPost(string html)
    {
        long? min = null;
        foreach (Match m in Regex.Matches(html ?? "", @"data-post=""[^""]+/(\d+)"""))
        {
            var id = long.Parse(m.Groups[1].Value);
            if (!min.HasValue || id < min) min = id;
        }
        return min?.ToString() ?? "?";
    }

    static string Match(string html, string pattern)
    {
        var m = Regex.Match(html ?? "", pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "";
    }

    static string Short(string url) =>
        url.Length > 72 ? url.Substring(0, 72) + "..." : url;
}
