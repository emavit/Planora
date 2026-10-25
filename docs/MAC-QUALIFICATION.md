# macOS qualification

Status: **in progress — not qualified.** This report records macOS evidence against the acceptance list in [PLATFORM_GUIDE](PLATFORM_GUIDE.md) (task MAC-01). Each row says what was verified, how, and what remains. Update it with every macOS change; never mark a row passed from Windows or fixture evidence alone.

## Environment

| Item | Value |
|---|---|
| Device | Apple M4 Pro, 24 GB (Apple Silicon; the original M1 target has not been tested separately) |
| macOS | 26.6.2 (25G83) |
| .NET | SDK 10.0.402, runtime 10.0.13, osx-arm64 |
| PowerShell | 7.6.8 (repository scripts run unchanged with `pwsh`) |
| Baseline | 0.3.10 source (`eb42efe`) plus the macOS validation branch |

## Summary

| Area | Status | Evidence | Remaining work |
|---|---|---|---|
| Build | Pass | `dotnet build` Release: 0 warnings, 0 errors. `build.ps1 -Runtime osx-arm64` produces a self-contained arm64 build (122 MB). | No `.app` bundle, icon set, file associations or signing yet. |
| Automated checks | Pass | All 21 CLI checks pass on macOS: durable 22, core, auto-model 22, recovery UI 20, close 19, workspace 61, templates 73, resource sheet, timelines 42, icons 4,878, PDF 28, multi-timeline 31, theme 37, formatting 59, auto-date 19, visual review 42, smoke, AI 56, MCP 55. | — |
| Native window stress | Pass, one open observation | `--stability-check` in a real window: 17 of 18 runs passed (20 insert/name/paste/format/undo/redo/scroll cycles, ~150 Gantt renders each, input file byte-identical). One early run failed because the inserted row's name editor was not visible 130 ms after insertion. | The check now reports delayed editor openings; investigate if a delay or failure recurs. |
| Editing and navigation | Pass (automated) | Blank-row entry, Enter/Tab/Esc/F2, ⌘V paste, Undo/Redo and focus are covered by workspace, auto-date, smoke and native checks. Wheel zoom and paste use ⌘ on macOS. | Manual check with a real keyboard and trackpad. |
| Scheduling and dates | Pass (automated) | FS/SS/FF/SF, lag/lead, weekends, holidays, manual/automatic, summary dates: core, auto-model and auto-date checks. | Large plans: see Performance. |
| Recovery and close | Pass (headless) | Recovery UI 20 and close flow 19 checks on APFS: checkpoint, chooser, previous generation, Save/Don't Save/Cancel, failed save. | A real crash during editing (force-quit, relaunch, restore) is not automated; manual check pending. |
| PDF and fonts | Pass | PDF 28 checks. Generated PDFs embed Helvetica Neue / Helvetica Neue Bold; table, Gantt, links, legend and footer render correctly. | Unicode/CJK fallback and very large plans not yet checked. |
| Appearance | Partial | Window follows the macOS dark/light setting; theme 37 and visual review 42 checks in both themes; Retina rendering is sharp. Date header labels no longer overlap (fixed for all platforms; covered by the timeline UI check). | Menus now appear in the macOS menu bar (verified from an `.app` launch: the in-window menu is hidden once macOS exports the native menu); menu shortcut labels are not shown yet. Mac look-and-feel phase in progress. |
| Credentials (Keychain) | **Fail** | On non-Windows systems connector secrets are held in memory only (`McpConnectorSettings.cs`), so saved keys and tokens are lost on restart. | Keychain adapter required by the acceptance list. |
| Connectors (AI/MCP) | Partial | AI fixture 56 and MCP fixture 55 checks pass with both `dotnet Planora.dll` and the self-contained build. The Codex CLI is found on macOS, including npm installs, where the bundled native binary is used (it runs under the app's minimal environment; the Node.js launcher does not). | Apps started from Finder get a minimal `PATH`, which can hide `npx`-based MCP servers; to verify with an `.app` bundle. No live provider was called. |
| MPP compatibility | Parity with Windows | Public corpus on macOS: **78 passed stages / 10 known failures**, identical to the Windows baseline, including native save after rename/undo/redo, new task/link/resource and baseline capture. Pasted 500–5,000-task plans save as `.mpp` and reopen with all tasks. | The 10 known failures are shared engine defects on every platform. Independent readback of Mac-saved files in Microsoft Project (Windows) pending. |
| Performance | Measured, no budgets | See below. | Duration changes and Undo approach 1 s from about 2,000 tasks. |

## Performance (headless, M4 Pro)

`pwsh ./scripts/validate.ps1 -IncludePerformance` — plans pasted through the normal multi-column paste path (chains of 50 linked tasks, every third task assigned). Edit timings run from pressing Enter (or Undo) to the updated screen.

| Operation | 500 tasks | 2,000 tasks | 5,000 tasks |
|---|---|---|---|
| Paste whole plan | 0.28 s | 1.3 s | 2.0 s |
| Open window | 0.61 s | 0.54 s | 0.60 s |
| Rename a task | 47 ms | 184 ms | 273 ms |
| Duration change rescheduling 50 linked tasks | 135 ms | 905 ms | 944 ms |
| Undo | 252 ms | 816 ms | 1,277 ms |
| Save `.mpp` | 275 ms | 561 ms | 1,357 ms |
| Reopen `.mpp` | 36 ms | 94 ms | 224 ms |

Timings vary by a few hundred milliseconds between runs. Budgets need an agreed representative workload (PERF-01).

## Engine observations for the owner

- The native `.mpp` writer stops at 1,000 diagnostics and turns the overflow into an error. Tasks created directly through the document API (not through the app's add-task or paste paths) carry about six informational "default normalized" warnings each, so such plans above roughly 165 tasks cannot be saved. App user paths are not affected (400 added or pasted tasks save with 2 diagnostics). Counting only warnings that can change data, or not escalating the truncation marker, would remove this latent limit; it is an engine decision.
- Before this branch, GitHub CI failed on Windows at its first step since the public example corpus was added, so the Windows build, model and UI steps had not run in CI. Fixed on this branch.

## Manual checks still needed on a Mac

1. Force-quit Planora during editing, relaunch and restore from File → Recovery.
2. Type, select ranges, copy/paste between Planora and Numbers/Excel with a physical keyboard and trackpad; check ⌘ shortcuts and IME input.
3. Open a Mac-saved `.mpp` in Microsoft Project on Windows and compare tasks, links, resources and baselines (requires the Windows machine).
4. Run an M1 device once if available.

## How to reproduce

```sh
pwsh ./scripts/verify-public-examples.ps1
pwsh ./scripts/validate.ps1 -IncludeUi -IncludeExtended -IncludePerformance
pwsh ./scripts/verify-mpp-regressions.ps1 -Assembly project-app/ProjectDesk/bin/Release/net10.0/Planora.dll -Output artifacts/validation/public-mpp
pwsh ./scripts/build.ps1 -Runtime osx-arm64   # self-contained build; run its Planora --ai-test/--mcp-test <dir>
dotnet project-app/ProjectDesk/bin/Release/net10.0/Planora.dll --stability-check <copy of core-test/core-functions.mpp>
```
