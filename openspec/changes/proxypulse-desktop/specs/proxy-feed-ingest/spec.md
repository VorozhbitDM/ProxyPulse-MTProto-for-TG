## ADDED Requirements

### Requirement: Load proxies from @ProxyMTProto channel

The system SHALL fetch and parse MTProto proxy links from the public Telegram channel @ProxyMTProto (`https://t.me/ProxyMTProto`).

#### Scenario: Primary web preview succeeds

- **WHEN** the user starts a search and the primary ingest endpoint responds with HTML containing proxy links
- **THEN** the system SHALL extract all valid `tg://proxy?server=...&port=...&secret=...` (and equivalent `https://t.me/proxy?...`) links from the latest channel posts
- **THEN** the system SHALL deduplicate proxies by server, port, and secret

#### Scenario: Deduplicate identical proxies

- **WHEN** the same proxy appears in multiple posts or sources
- **THEN** the system SHALL include it only once in the scan queue

### Requirement: Bypass blocked t.me via fallback sources

The system SHALL attempt multiple ingest sources in order until at least one returns proxy data.

#### Scenario: Primary t.me blocked, fallback succeeds

- **WHEN** requests to `t.me` / `t.me/s/ProxyMTProto` fail due to timeout, DNS, or HTTP error
- **THEN** the system SHALL try configured fallback sources (including `https://proxymtpro.to` and mirror hosts documented in design)
- **THEN** the system SHALL proceed with scanning using proxies from the first successful source

#### Scenario: All sources fail

- **WHEN** every ingest source fails
- **THEN** the system SHALL show a clear error message suggesting transport proxy configuration or network check
- **THEN** the system SHALL NOT start the health scan with an empty list without user acknowledgment

### Requirement: Optional transport proxy for ingest

The system SHALL allow the user to configure an optional SOCKS5 or HTTP proxy used only for feed HTTP requests.

#### Scenario: User configures transport proxy

- **WHEN** the user saves a valid SOCKS5 or HTTP proxy in settings
- **THEN** all ingest HTTP requests SHALL route through that proxy until cleared

#### Scenario: Invalid transport proxy

- **WHEN** the configured transport proxy is unreachable
- **THEN** the system SHALL report that the transport proxy failed and SHALL still attempt direct/fallback sources if applicable

### Requirement: Parse MTProto link format

The system SHALL normalize parsed proxies into an internal model with server host, port, secret, and display label.

#### Scenario: Valid tg link parsed

- **WHEN** a post contains `tg://proxy?server=example.com&port=443&secret=ee...`
- **THEN** the system SHALL store server=`example.com`, port=`443`, secret=`ee...`, and a human-readable row label

#### Scenario: Malformed link ignored

- **WHEN** a string resembles a proxy link but lacks required fields
- **THEN** the system SHALL skip it without aborting ingest
