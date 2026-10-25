# Platform guide

Windows 11 is the first target; Apple Silicon macOS follows actual device validation. Android and iOS are planned after the macOS app is finished and validated. Linux CI runs model checks and does not imply Linux desktop support.

Avalonia supplies the shared UI. Credential vaults, browser opening, subprocess launch/cancellation, font availability, file dialogs, keyboard shortcuts, signing and packaging need explicit platform qualification. Keep secrets outside project/recovery packages. Do not fall back to plaintext secrets silently.

macOS acceptance must cover M1 execution, editing/navigation, date behavior, recovery, PDF fonts, keychain/connector behavior and independent offline MPP operations. Merely publishing `osx-arm64` does not meet these criteria. Use runtime platform checks in adapter construction, not spread through scheduling logic. Current macOS evidence and open items: [macOS qualification](MAC-QUALIFICATION.md).

Claude Code control: the running Planora answers MCP on a Unix domain socket (`PlanoraMcpHost.SocketPath`). macOS uses `$TMPDIR/planora-<user>/mcp.sock`; Linux uses `$XDG_RUNTIME_DIR` (or the temporary folder) with the same layout; the folder is 0700 and the socket 0600. Windows uses `%LOCALAPPDATA%\ProjectDesk\planora-mcp.sock` (AF_UNIX, Windows 10 1803 or later). `PLANORA_MCP_SOCKET` overrides the path for tests. `Planora --mcp-connect` (what Claude Code runs) starts a macOS bundle through LaunchServices (`open -a`), Windows through the shell and other builds detached from the relay's stdio.

Credential vaults: `McpCredentialStore` (the composition point for `IMcpSecrets`, shared by MCP connectors and AI API keys) uses Windows Credential Manager on Windows and the user's default Keychain on macOS (`MacKeychain`, generic-password items with service "Planora" through the Security framework; values never pass through a command line). Other platforms keep secrets for the running process only. The MCP check verifies roundtrip, persistence beyond one store instance, overwrite and removal against the real platform vault. An unsigned macOS app is identified by its binary, so after an update macOS may ask once to allow Keychain access; signing gives it a stable identity.

## Android and iOS — future phase

Begin after the macOS completion gate, not in parallel with unfinished desktop qualification. Reuse qualified scheduling/project contracts through testable interfaces where feasible; evaluate the mobile UI framework separately. Phone/tablet layouts, touch interaction, app lifecycle, sandboxed file access, secure credential storage, offline persistence, optional sync and distribution each need mobile-specific design and tests. Do not assume desktop MPP, PDF, CLI or connector support transfers automatically. See [mobile plan](MOBILE-PLAN.md).
