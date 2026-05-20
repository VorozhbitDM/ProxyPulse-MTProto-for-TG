## ADDED Requirements

### Requirement: Welcome screen with start action

The application SHALL show a welcome view on first launch with program description and a «Начать поиск» button.

#### Scenario: Application starts

- **WHEN** the user launches ProxyPulse
- **THEN** the main window SHALL display app name, short description, version (e.g. 1.0), and «Начать поиск»

#### Scenario: User starts search

- **WHEN** the user clicks «Начать поиск»
- **THEN** the UI SHALL switch to the scan view with progress bar and proxy list area

### Requirement: Scan view controls

The scan view SHALL provide global progress, cancel, and live results.

#### Scenario: Scan view layout

- **WHEN** a search is active or completed
- **THEN** the UI SHALL show a progress bar, text progress (e.g. `42/120`), «Прервать поиск» while running, and a scrollable list of available proxies

### Requirement: Proxy row interaction

Each available proxy row SHALL show address, ping, and a per-row «Проверить» control.

#### Scenario: Row hover highlight

- **WHEN** the pointer hovers over a proxy row
- **THEN** the row background SHALL change to a distinct highlight color

#### Scenario: Row click opens Telegram

- **WHEN** the user clicks a proxy row (not the recheck button)
- **THEN** the system SHALL launch `tg://proxy?server=...&port=...&secret=...` via the OS shell so Telegram offers to enable the proxy

#### Scenario: Recheck button

- **WHEN** the user clicks «Проверить» on a row
- **THEN** only that proxy SHALL be re-tested and the ping column SHALL update

### Requirement: Interaction during global scan

The user SHALL be able to scroll the list, click rows, and use per-row recheck while a global scan runs.

#### Scenario: Click proxy during scan

- **WHEN** the user clicks an available proxy during an ongoing global scan
- **THEN** Telegram SHALL open and the global scan SHALL continue

### Requirement: Application branding and version

The application SHALL be named **ProxyPulse** and display semantic version in About and window title.

#### Scenario: Version display

- **WHEN** the About dialog or title bar is shown
- **THEN** it SHALL include version `1.0` for the initial release and follow `major.minor` for subsequent releases

### Requirement: Minimal fast UI

The UI SHALL use a light, minimal visual style with clear typography and SHALL keep the UI thread non-blocking during network work.

#### Scenario: UI remains responsive

- **WHEN** hundreds of proxies are scanned
- **THEN** window repaint and user input SHALL remain responsive (work on background threads)

### Requirement: Optional settings for transport proxy

The application SHALL provide access to transport proxy settings (SOCKS5/HTTP) from the main window menu or gear icon.

#### Scenario: Open settings

- **WHEN** the user opens settings
- **THEN** they SHALL configure host, port, type, and optional credentials for ingest bypass
