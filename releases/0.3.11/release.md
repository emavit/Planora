# Planora 0.3.11 — macOS preview

macOS preview · 25 October 2026 · Previous local release: 0.3.10

## Changes from 0.3.10

**Planora on the Mac.** Planora.app runs on Apple Silicon Macs with macOS 12 or later. Menus appear in the macOS menu bar, the interface uses the system font, shortcuts use ⌘, documents open from Finder (double-click or Open With), and connector tokens and AI API keys are kept in the Keychain. ChatGPT subscription mode finds the Codex CLI automatically.

**Microsoft Project files.** All ten documented failures of the public MPP corpus are fixed in the shared engine, so the fixes apply on Windows too: baseline fixed cost is written for MPP9, MPP12 and MPP14 files, manually scheduled tasks show the dates Project displays, and producer data that Project itself wrote is kept instead of blocking unrelated edits.

## Included from recent work

Readable Gantt and timeline date labels on wide ranges, a readable ID column header, a repaired Windows CI, and more automated checks in CI (AI, MCP, PDF, theme, formatting, smoke and the public MPP gate).

## Run on macOS

Extract the complete Planora-0.3.11-osx-arm64.zip, then open Planora.app inside the extracted 0.3.11-osx-arm64 folder. The app includes its .NET runtime. It is not Developer ID signed or notarized: if macOS blocks it, open System Settings → Privacy & Security and choose Open Anyway for Planora.

## Qualification

macOS preview, not a final release. The public MPP corpus passes all 88 stages; full Microsoft Project compatibility is not claimed. macOS evidence and the remaining manual checks are in docs/MAC-QUALIFICATION.md. No Windows package was built for this version; Windows users can keep the 0.3.10 Windows preview. Validation logs and file hashes are included. Only public and synthetic examples are included.
