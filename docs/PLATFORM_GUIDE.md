# Platform guide

Windows 11 is the first target; Apple Silicon macOS follows actual device validation. Linux CI runs model checks and does not imply Linux desktop support.

Avalonia supplies the shared UI. Credential vaults, browser opening, subprocess launch/cancellation, font availability, file dialogs, keyboard shortcuts, signing and packaging need explicit platform qualification. Keep secrets outside project/recovery packages. Do not fall back to plaintext secrets silently.

macOS acceptance must cover M1 execution, editing/navigation, date behavior, recovery, PDF fonts, keychain/connector behavior and independent offline MPP operations. Merely publishing `osx-arm64` does not meet these criteria. Use runtime platform checks in adapter construction, not spread through scheduling logic.
