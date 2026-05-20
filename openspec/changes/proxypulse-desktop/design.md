## Context

Greenfield Windows desktop app for users who need fresh MTProto proxies from [@ProxyMTProto](https://t.me/ProxyMTProto) but cannot reliably open `t.me`. Target stack: **.NET Framework 4.7.2**, single-file **exe**, minimal dependencies. Telegram Desktop handles actual proxy activation via `tg://proxy` deep links.

## Goals / Non-Goals

**Goals:**

- Fast path: open app → «Начать поиск» → see working proxies sorted by latency → click → connect in Telegram.
- Reliable ingest when `t.me` is blocked (fallback URLs + optional user transport proxy).
- Responsive UI: background ingest/scan, live list updates, cancel + per-row recheck.
- **ProxyPulse v1.0** with version in assembly and UI.

**Non-Goals:**

- Embedded full VPN/tunnel (WireGuard/OpenVPN) in v1 — too heavy and legally/operationally sensitive.
- MTProto protocol handshake validation (only TCP reachability to host:port).
- Auto-configuring Telegram without user confirmation.
- macOS/Linux in v1.

## Decisions

### 1. UI framework: WinForms on .NET Framework 4.7.2

**Choice:** WinForms + .NET 4.7.2  
**Rationale:** Matches installed Developer Pack; fast to build, small exe, easy custom-drawn list with hover.  
**Alternatives:** WPF (heavier), .NET 6+ (user asked for 4.7.2).

### 2. Project layout

```
src/ProxyPulse/
  ProxyPulse.csproj          # WinExe, net472
  Program.cs
  MainForm.cs                # welcome + scan views (panels)
  Models/ProxyEntry.cs
  Services/ProxyFeedService.cs
  Services/ProxyHealthService.cs
  Services/TelegramLinkBuilder.cs
  UI/ProxyListView.cs        # ListView or custom panel rows
  Properties/AssemblyInfo.cs # version 1.0.0.0
```

### 3. Ingest strategy (anti-block without built-in VPN)

**Choice:** Ordered multi-source HTTP fetch + optional SOCKS5/HTTP transport proxy in Settings.

| Priority | Source | URL pattern |
|----------|--------|-------------|
| 1 | Telegram public preview | `https://t.me/s/ProxyMTProto` |
| 2 | Telegram mirror host | `https://telegram.me/s/ProxyMTProto` (if reachable) |
| 3 | Channel-linked site | `https://proxymtpro.to` (parse same link patterns) |

**Parsing:** Regex for `tg://proxy?...` and `https://t.me/proxy?...`; decode `&amp;` in HTML. Take last N posts worth of links (e.g. scrape last 20 `data-post` blocks or full page if small).

**Transport proxy:** Settings → host, port, type (SOCKS5/HTTP). Use **SocksSharp** or **HttpToSocks5Proxy**-style handler on `HttpWebRequest` / custom `WebClient` for 4.7.2. Stored in `%AppData%\ProxyPulse\settings.json` (no secrets in repo).

**Rationale:** True “built-in VPN” is out of scope; a user-supplied local VPN/SOCKS (v2rayN, Clash, etc.) plus fallbacks covers the real-world case. `proxymtpro.to` is advertised on the channel and may work when `t.me` does not.

**Alternatives considered:** TDLib/Telethon (heavy, session); embedded VPN client (rejected).

### 4. Health check: TCP connect latency

**Choice:** `TcpClient.BeginConnect` / `ConnectAsync` with timeout (default 3000 ms). Ping = elapsed ms on success.

**Rationale:** MTProto secret validation requires crypto and is slow; TCP proves the endpoint is up, which matches user expectation of “ping”.

**Note:** Some proxies may accept TCP but fail MTProto — acceptable trade-off for v1.

**Concurrency:** `SemaphoreSlim(50)` + `CancellationToken` from global cancel.

### 5. UI flow

- **Welcome panel:** logo/title “ProxyPulse”, 2–3 lines description, version, «Начать поиск», link «Настройки».
- **Scan panel:** `ProgressBar` (Marquee off, continuous), label `Проверено: X / Y`, buttons «Прервать поиск» / «Новый поиск», `ListView` in Details mode or custom `FlowLayoutPanel` rows.
- **Row:** `server:port` (truncated secret), ping `123 ms` or `—`, button «Проверить».
- **Sort:** `BindingList` or sorted insert on `ObservableCollection` synced to UI via `BeginInvoke`.
- **Theme:** light gray background `#F5F5F5`, accent `#2AABEE` (Telegram-like), hover `#E3F2FD`, Segoe UI 9–10pt.

### 6. Telegram deep link

```text
tg://proxy?server={host}&port={port}&secret={secret}
```

`Process.Start` with try/catch; fallback message if Telegram not installed.

### 7. Versioning

- `AssemblyVersion` / `AssemblyFileVersion`: `1.0.0.0`
- Display: `1.0` in UI; bump minor for features (`1.1`), major for breaking changes.

### 8. Build & single exe

- SDK-style or classic csproj targeting **v4.7.2**.
- **Costura.Fody** (or ILMerge) to embed dependencies into one `ProxyPulse.exe`.
- Release build script: `build.ps1` → `dist/ProxyPulse.exe`.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| All ingest URLs blocked | User transport proxy; clear error UI |
| HTML structure change on t.me | Multiple regex patterns; fallback proxymtpro.to |
| TCP OK but proxy bad for MTProto | Document in About; future v1.1 optional MTProto probe |
| High proxy count slows scan | Cap ingest to latest ~100 links; configurable concurrency |
| Legal/ToS on scraping | Public channel preview only; no authenticated API abuse |

## Migration Plan

N/A (new app). Distribution: copy `ProxyPulse.exe` to user machine; requires .NET 4.7.2+ runtime (usually preinstalled on Win10+).

## Open Questions

- Confirm at implementation time which mirror (`telegram.me`, `tg.dev`, etc.) works from target network.
- Whether `proxymtpro.to` exposes the same HTML link format — validate during `/opsx:apply`.
- Optional: add «Копировать ссылку» on row context menu in v1.1.
