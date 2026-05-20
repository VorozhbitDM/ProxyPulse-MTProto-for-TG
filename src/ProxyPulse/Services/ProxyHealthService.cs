using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int DefaultTimeoutMs = 2500;
        private const int DefaultConcurrency = 50;

        private readonly int _timeoutMs;
        private readonly int _concurrency;

        public ProxyHealthService(int timeoutMs = DefaultTimeoutMs, int concurrency = DefaultConcurrency)
        {
            _timeoutMs = timeoutMs;
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
                var sw = Stopwatch.StartNew();
                var available = TryConnect(proxy.Server, proxy.Port, _timeoutMs, cancellationToken);
                sw.Stop();

                return new ProxyCheckEventArgs
                {
                    Entry = proxy,
                    IsAvailable = available,
                    PingMs = available ? (int?)sw.ElapsedMilliseconds : null
                };
            }, cancellationToken);
        }

        private static bool TryConnect(string host, int port, int timeoutMs, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            var addresses = ResolveAddresses(host, RemainingMs(deadline), cancellationToken);
            if (addresses == null || addresses.Length == 0)
                return false;

            foreach (var address in addresses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var left = RemainingMs(deadline);
                if (left <= 0)
                    return false;

                if (TryConnectAddress(address, port, left, cancellationToken))
                    return true;
            }

            return false;
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

        private static int RemainingMs(DateTime deadlineUtc)
        {
            var left = (int)(deadlineUtc - DateTime.UtcNow).TotalMilliseconds;
            return left < 0 ? 0 : left;
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
