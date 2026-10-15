# Platform guide

Windows 11 is the first target; Apple Silicon macOS follows actual device validation. Android and iOS are planned after the macOS app is finished and validated. Linux CI runs model checks and does not imply Linux desktop support.

Avalonia supplies the shared UI. Credential vaults, browser opening, subprocess launch/cancellation, font availability, file dialogs, keyboard shortcuts, signing and packaging need explicit platform qualification. Keep secrets outside project/recovery packages. Do not fall back to plaintext secrets silently.

macOS acceptance must cover M1 execution, editing/navigation, date behavior, recovery, PDF fonts, keychain/connector behavior and independent offline MPP operations. Merely publishing `osx-arm64` does not meet these criteria. Use runtime platform checks in adapter construction, not spread through scheduling logic.

## Android and iOS — future phase

Begin after the macOS completion gate, not in parallel with unfinished desktop qualification. Reuse qualified scheduling/project contracts through testable interfaces where feasible; evaluate the mobile UI framework separately. Phone/tablet layouts, touch interaction, app lifecycle, sandboxed file access, secure credential storage, offline persistence, optional sync and distribution each need mobile-specific design and tests. Do not assume desktop MPP, PDF, CLI or connector support transfers automatically. See [mobile plan](MOBILE-PLAN.md).
