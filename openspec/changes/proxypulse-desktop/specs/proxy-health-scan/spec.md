## ADDED Requirements

### Requirement: Concurrent proxy health check

The system SHALL check each proxy for reachability by attempting a TCP connection to the advertised host and port within a configurable timeout.

#### Scenario: Proxy is reachable

- **WHEN** a TCP connect to the proxy host:port succeeds within the timeout
- **THEN** the system SHALL mark the proxy as available and record round-trip time in milliseconds as ping

#### Scenario: Proxy is unreachable

- **WHEN** TCP connect fails or times out
- **THEN** the system SHALL mark the proxy as unavailable and SHALL NOT show it in the working list (or show as offline per UI spec)

### Requirement: Global scan progress and cancel

The system SHALL run health checks with bounded concurrency and support cancellation.

#### Scenario: Scan in progress

- **WHEN** a global search is running
- **THEN** the system SHALL update progress as completed/total checks
- **THEN** newly available proxies SHALL appear in the UI list without waiting for the full scan to finish

#### Scenario: User cancels scan

- **WHEN** the user clicks «Прервать поиск»
- **THEN** the system SHALL stop scheduling new checks and cancel in-flight work as soon as practical
- **THEN** results collected before cancel SHALL remain visible

### Requirement: Re-check single proxy

The system SHALL allow re-checking one proxy without restarting the global scan.

#### Scenario: Per-row recheck during global scan

- **WHEN** the user clicks «Проверить» on a row while a global scan is active
- **THEN** the system SHALL run an independent health check for that proxy
- **THEN** the row ping and availability status SHALL update when the check completes

### Requirement: Sort working proxies by ping

The system SHALL keep the list of available proxies sorted by ping ascending (fastest first).

#### Scenario: New faster proxy found

- **WHEN** a proxy becomes available with lower ping than existing entries
- **THEN** the system SHALL re-insert it at the correct sorted position in the UI list

#### Scenario: Recheck changes ping

- **WHEN** a per-row recheck returns a new ping value
- **THEN** the system SHALL re-sort that entry among available proxies

### Requirement: Bounded concurrency for performance

The system SHALL limit parallel checks (default 50, configurable in design) to keep the UI responsive on typical hardware.

#### Scenario: Large proxy list

- **WHEN** ingest returns more than 200 proxies
- **THEN** the system SHALL queue checks and process them with bounded concurrency without freezing the UI thread
