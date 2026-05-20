using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProxyPulse.Models;

namespace ProxyPulse.Services
{
    public sealed class ScanProgressEventArgs : EventArgs
    {
        public int Completed { get; set; }
        public int Total { get; set; }
    }

    public sealed class ProxyCheckingEventArgs : EventArgs
    {
        public ProxyEntry Entry { get; set; }
    }

    public sealed class ProxyCheckEventArgs : EventArgs
    {
        public ProxyEntry Entry { get; set; }
        public bool IsAvailable { get; set; }
        public int? PingMs { get; set; }
    }

    public sealed class ProxyHealthService
    {
        private const int DefaultDnsTimeoutMs = 2000;
        private const int DefaultConnectTimeoutMs = 2000;
        private const int DefaultConcurrency = 50;

        private readonly int _dnsTimeoutMs;
        private readonly int _connectTimeoutMs;
        private readonly int _concurrency;

        public ProxyHealthService(
            int connectTimeoutMs = DefaultConnectTimeoutMs,
            int concurrency = DefaultConcurrency,
            int dnsTimeoutMs = DefaultDnsTimeoutMs)
        {
            _connectTimeoutMs = connectTimeoutMs;
            _dnsTimeoutMs = dnsTimeoutMs;
            _concurrency = concurrency;
        }

        public event EventHandler<ScanProgressEventArgs> ProgressChanged;
        public event EventHandler<ProxyCheckingEventArgs> ProxyChecking;
        public event EventHandler<ProxyCheckEventArgs> ProxyChecked;

        public async Task ScanAsync(IList<ProxyEntry> proxies, CancellationToken cancellationToken)
        {
            if (proxies == null || proxies.Count == 0)
                return;

            var total = proxies.Count;
            var completed = 0;
            using (var gate = new SemaphoreSlim(_concurrency, _concurrency))
            {
                var tasks = new List<Task>();
                foreach (var proxy in proxies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            RaiseChecking(proxy);
                            var result = await CheckOneAsync(proxy, cancellationToken).ConfigureAwait(false);
                            RaiseChecked(result);
                        }
                        finally
                        {
                            var done = Interlocked.Increment(ref completed);
                            RaiseProgress(done, total);
                            gate.Release();
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        public Task<ProxyCheckEventArgs> CheckOneAsync(ProxyEntry proxy, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                int? pingMs;
                var available = TryMeasureTcpPing(
                    proxy.Server,
                    proxy.Port,
                    out pingMs,
                    cancellationToken);

                return new ProxyCheckEventArgs
                {
                    Entry = proxy,
                    IsAvailable = available,
                    PingMs = pingMs
                };
            }, cancellationToken);
        }

        /// <summary>
        /// Пинг = время успешного TCP connect (без DNS). При повторной проверке DNS уже в кэше —
        /// раньше первый замер завышался из-за DNS внутри Stopwatch.
        /// </summary>
        private bool TryMeasureTcpPing(
            string host,
            int port,
            out int? pingMs,
            CancellationToken cancellationToken)
        {
            pingMs = null;

            var addresses = ResolveAddresses(host, _dnsTimeoutMs, cancellationToken);
            if (addresses == null || addresses.Length == 0)
                return false;

            foreach (var address in OrderAddressesForConnect(addresses))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sw = Stopwatch.StartNew();
                if (TryConnectAddress(address, port, _connectTimeoutMs, cancellationToken))
                {
                    sw.Stop();
                    pingMs = (int)Math.Max(1, sw.ElapsedMilliseconds);
                    return true;
                }
            }

            return false;
        }

        private static IPAddress[] OrderAddressesForConnect(IPAddress[] addresses)
        {
            return addresses
                .OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ThenBy(a => a.AddressFamily == AddressFamily.InterNetworkV6 ? 0 : 1)
                .ToArray();
        }

        private static IPAddress[] ResolveAddresses(string host, int timeoutMs, CancellationToken cancellationToken)
        {
            IPAddress parsed;
            if (IPAddress.TryParse(host, out parsed))
                return new[] { parsed };

            if (timeoutMs <= 0)
                return null;

            IPAddress[] result = null;
            using (var done = new ManualResetEventSlim(false))
            {
                Dns.BeginGetHostAddresses(host, ar =>
                {
                    try
                    {
                        result = Dns.EndGetHostAddresses(ar);
                    }
                    catch
                    {
                        result = null;
                    }
                    finally
                    {
                        done.Set();
                    }
                }, null);

                cancellationToken.ThrowIfCancellationRequested();
                if (!done.Wait(timeoutMs, cancellationToken))
                    return null;
            }

            return result;
        }

        private static bool TryConnectAddress(
            IPAddress address,
            int port,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            if (timeoutMs <= 0)
                return false;

            TcpClient client = null;
            try
            {
                client = new TcpClient(address.AddressFamily);
                var connect = client.BeginConnect(address, port, null, null);

                using (var wait = connect.AsyncWaitHandle)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!wait.WaitOne(timeoutMs))
                        return false;
                }

                client.EndConnect(connect);
                return client.Connected;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (client != null)
                    client.Close();
            }
        }

        private void RaiseProgress(int completed, int total)
        {
            var handler = ProgressChanged;
            if (handler != null)
                handler(this, new ScanProgressEventArgs { Completed = completed, Total = total });
        }

        private void RaiseChecking(ProxyEntry entry)
        {
            var handler = ProxyChecking;
            if (handler != null)
                handler(this, new ProxyCheckingEventArgs { Entry = entry });
        }

        private void RaiseChecked(ProxyCheckEventArgs args)
        {
            var handler = ProxyChecked;
            if (handler != null)
                handler(this, args);
        }
    }
}
