# Planora 0.3.12 — Claude Code, Windows and macOS

Windows and macOS preview · 26 October 2026 · Previous releases: 0.3.11 (macOS), 0.3.10 (Windows)

## Changes from 0.3.11

**Work with Claude from Claude Code.** Use your Claude subscription through Claude Code to work on the project open in Planora. Choose Connectors → Claude Code… and Connect Claude Code (or paste the shown command into Terminal), then ask Claude Code, for example, to list the tasks or add a testing phase after design. Claude Code can read tasks, resources and assignments, switch views and select tasks. Every change it proposes appears in Planora with the old and new values and is applied only when you choose Apply Change, as one Undo step. Planora never overwrites an existing file, and your Claude sign-in stays in Claude Code. The Claude connection settings now offer Connect Claude Code instead of ending at "embedded login requires Anthropic approval".

**One release for Windows and macOS.** Both downloads are now built and checked together on GitHub's Windows and macOS machines, including the native check on each. A release is published only when both pass.

## For Windows users: included from 0.3.11

This is the first Windows package since 0.3.10. All ten documented failures of the public MPP corpus are fixed (baseline fixed cost is written for MPP9, MPP12 and MPP14 files, manually scheduled tasks show the dates Project displays, and producer data that Project wrote is kept instead of blocking unrelated edits). Gantt and timeline date labels stay readable on wide ranges, and the ID column header is readable.

## Also fixed

The AI and connector settings name the credential store of your platform (the macOS Keychain on a Mac) instead of always saying Windows Credential Manager.

## Run on Windows

Extract the complete Planora-0.3.12-win-x64.zip, then run Planora.exe inside the extracted 0.3.12-win-x64 folder. Keep the files together. The package includes its .NET runtime.

## Run on macOS

Extract the complete Planora-0.3.12-osx-arm64.zip and move Planora.app from the extracted 0.3.12-osx-arm64 folder to Applications (needed before connecting Claude Code). The app includes its .NET runtime and runs on Apple Silicon Macs with macOS 12 or later. It is not Developer ID signed or notarized: if macOS blocks it, open System Settings → Privacy & Security and choose Open Anyway for Planora.

## Qualification

Preview, not a final release. The public MPP corpus passes all 88 stages; full Microsoft Project compatibility is not claimed. macOS evidence and the remaining manual checks are in docs/MAC-QUALIFICATION.md. Approving a Claude Code change in the live app and a full Claude Code session are manual checks; the automated Claude Code checks use a scripted client. Validation logs and file hashes are included. Only public and synthetic examples are included.
