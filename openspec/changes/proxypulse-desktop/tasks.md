## 1. Solution setup

- [x] 1.1 Create `src/ProxyPulse/ProxyPulse.csproj` (WinExe, .NET Framework 4.7.2, assembly version 1.0.0.0)
- [x] 1.2 Add `Program.cs`, `Properties/AssemblyInfo.cs`, app icon placeholder
- [x] 1.3 Add NuGet: Costura.Fody for single-file exe; optional SOCKS library for transport proxy
- [x] 1.4 Add `build.ps1` producing `dist/ProxyPulse.exe` (Release)

## 2. Domain models and settings

- [x] 2.1 Implement `ProxyEntry` (server, port, secret, ping, isAvailable, display label)
- [x] 2.2 Implement `AppSettings` + load/save `%AppData%\ProxyPulse\settings.json` (transport proxy fields)
- [x] 2.3 Implement `TelegramLinkBuilder` → `tg://proxy?...` URL

## 3. Proxy feed ingest (`proxy-feed-ingest`)

- [x] 3.1 Implement `ProxyFeedService` with ordered sources: `t.me/s/ProxyMTProto`, `telegram.me/s/...`, `proxymtpro.to`
- [x] 3.2 Add HTML download via `HttpWebRequest` with timeout, User-Agent, optional transport proxy handler
- [x] 3.3 Add regex parser for `tg://proxy` and `t.me/proxy` links; dedupe by server+port+secret
- [x] 3.4 Cap ingest to latest ~100 unique proxies; surface clear error when all sources fail

## 4. Health scan engine (`proxy-health-scan`)

- [x] 4.1 Implement `ProxyHealthService` TCP connect check with timeout (3s default)
- [x] 4.2 Add bounded concurrency (`SemaphoreSlim`, default 50) and `CancellationToken` for global cancel
- [x] 4.3 Report progress events: `(completed, total)` and per-proxy result events
- [x] 4.4 Implement single-proxy recheck API used by row «Проверить» button
- [x] 4.5 Maintain sorted-by-ping list (insert on result update)

## 5. Desktop UI (`desktop-ui`)

- [x] 5.1 Build `MainForm` with welcome panel (description, v1.0, «Начать поиск», settings entry)
- [x] 5.2 Build scan panel: progress bar, `X / Y` label, «Прервать поиск», «Новый поиск»
- [x] 5.3 Implement proxy list (ListView Details or custom rows): address | ping | «Проверить»
- [x] 5.4 Wire hover highlight, row click → `Process.Start` tg:// link, recheck button handler
- [x] 5.5 Wire `BeginInvoke` for thread-safe UI updates during scan; keep list interactive while scanning
- [x] 5.6 Add Settings dialog (SOCKS5/HTTP transport proxy for ingest)
- [x] 5.7 Add About dialog with ProxyPulse name and version

## 6. Integration and polish

- [x] 6.1 Connect «Начать поиск»: ingest → start scan → live list updates
- [x] 6.2 Connect «Прервать поиск» to cancellation token
- [x] 6.3 Manual test: ingest on blocked network (with/without transport proxy), click opens Telegram
- [x] 6.4 Verify Release build outputs single `ProxyPulse.exe` in `dist/`

## 7. Documentation

- [x] 7.1 Add root `README.md` with build steps, .NET 4.7.2 requirement, transport proxy hint, channel credit
