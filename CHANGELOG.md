# Changelog

## Unreleased — macOS validation

- Validation checks send ⌘ instead of Ctrl for wheel zoom and keyboard paste on macOS, matching the app's platform shortcuts. Timeline UI and multi-timeline checks pass on macOS (31 each).
- Fixed the Windows CI failure present since the public example corpus was added: `LICENSE-MPXJ.txt` was converted to CRLF on Windows checkouts, so its pinned SHA-256 never matched and the Windows build/model/UI steps were skipped. The file now keeps its exact bytes like the other hashed fixtures.

## 0.3.8 — 2026-10-19 · Modern identity and Apple/Office workspace

- More compact ribbon command groups, tinted active tabs and quiet centered overflow arrows.
- Neutral task-table headers retain the familiar spreadsheet and Gantt layout.

## 0.3.7 — modern timeline workspace

- Quieter timeline controls, Options menu and neutral navigation materials.
- Fit panel reclaims unused timeline height while preserving date limits and zoom.
- 49 workspace and 31 timeline checks plus packaged regression/native suites passed. Known MPP baseline unchanged.

## 0.3.6 — planning ribbon

- Redesign command groups, selected tabs and spacing; add explicit overflow arrows and consistent collapse/reopen labels.
- Add default Planning tab with working Gantt, timeline, task creation, scheduling, formatting and PDF actions.
- 47 workspace checks and packaged regression/native suites passed. Public MPP gate retains 10 known failures.

## 0.3.5 — inspector usability

- Pin computed dates and Apply/Cancel around independently scrolling fields.
- Show adjacent scalar-field validation errors without partially applying the draft.
- 35 workspace checks, Resource Sheet regression, template/saved-view and packaged core/recovery/close/native checks passed. Public MPP gate retains the same 10 known failures.

## 0.3.4 — Resource Sheet performance bugfix

- Refresh only the edited resource and affected assignment/task resource-name labels; remove repeated task-list allocation from general value refresh.
- Added failing-before/passing-after refresh regression, 1,002-task editing exercise, correction, Undo/Redo and separate MPP persistence checks. Packaged automated/native suites passed; public MPP baseline unchanged.
- Exact user freeze remains unconfirmed; this fixes a measured defect in the reported editing path. Chart-interaction feature release is deferred.

## 0.3.3 — Windows preview

- Apple-inspired Gantt-first adaptive navigation, row-density presets and F11 planning focus that preserves timeline visibility and panel preferences.
- Resizable grouped inspector with scheduling-mode selector and keyboard-accessible width presets.
- 32 workspace checks, 73 template/saved-view checks, core/scheduling/durable/recovery/close suites and exact packaged native stress passed.
- Public MPP regression gate: 78 passing stages, 10 unchanged known failures.
- No 0.3.2 package was published; the user requested 0.3.3. Broader range selection, Resources/Notes inspector sections and per-field validation remain pending; this release does not claim the entire earlier combined design scope.

## Unreleased documentation

- Prioritized six UI-focused release pairs with acceptance criteria; added an interactive original workspace concept. These future releases are planned, not delivered.

- Recorded owner authorization for a complete UI redesign, modular workspace acceptance criteria, broad template-family ambition and explicit MPP preservation requirements. Implementation remains pending.

- Added 18 provenance-checked public compatibility files (14 MPP + 4 producer XML), upstream license/manifest, integrity verification and official Microsoft template-gallery references. Private examples and unverified gallery binaries are excluded.

- Confirmed future Android and iOS apps after macOS completion and validation; added the mobile roadmap, platform requirements and implementation tasks. Mobile implementation has not started.

## 0.3.1 — Windows preview

- Added saved workspace views and ten original previewable schedule templates.
- 73 focused checks plus previous regression suites; native Windows stress passed.
- Added public MPP regression gate: 78 passing stages and 10 documented failures also present in 0.2.1.

## 0.3.0 — Windows preview

- Added compact workspace navigation and an editable task inspector with atomic Apply/Undo.
- 26 focused checks plus previous regression suites; native Windows stress passed.
- Added two-feature release packaging, per-version release.md and executable checksums.

## 0.2.1 — release candidate

- Added durable command history, local recovery checkpoints and previous-generation recovery.
- Added portable `.planora` copies with formatting, timelines and Undo/Redo.
- Preserved saved-history recognition after recovery.
- Fixed new-project dates created from local wall-clock input.
- Added recovery/save/close regression coverage and repository build/validation tooling.

Native Windows stress qualification of this candidate remains pending. macOS and live AI provider qualification remain open.

## 0.2.0 — Windows preview baseline

Spreadsheet/Gantt editing, dynamic columns, resources, baselines, multiple timelines, PDF, text/color formatting, theme selection and optional AI/MCP connections, with bounded compatibility.

### Gantt visual refinement (included in 0.3.8)
- Rounded, slightly taller task bars with clipped progress fills, inspired by Scabbard Dii's Gantt Chart Conception. Existing date endpoints and custom colors retained.

### Visual rebrand (included in 0.3.8)
- New indigo/aqua schedule-bar identity, shared header/window mark, regenerated Windows icons and refined wordmark.
- Added brand guidelines and visual preview. Name, file formats and recovery paths retained.

### Office ribbon refinement (included in 0.3.8)
- Bottom-aligned group captions, subtle separators and active-tab underline.
- Centered compact overflow arrows preserve command access.
- Public MPP regression gate rerun: 78 passing stages, 10 unchanged known failures. No compatibility fix claimed.

## 0.3.9 — 2026-10-19 · Entry preset and colored controls

- Added an explicit Microsoft Project Entry column preset in the column picker, matching the requested field order. Preview the selection and Apply columns to confirm.

### Dropdown appearance (included in 0.3.9)
- Shared rounded menu surfaces, compact menu rows, system typography and theme-aware hover/selection fills for context menus and dropdown items.

### Colored command icons (0.3.9)
- Theme-aware semantic colors for the existing 65 original vector symbols. Command labels and primary-button contrast retained.

## 0.3.10 — 2026-10-19 · One-click timelines
- Added Hide timelines / Show timelines to the Gantt and Table workspace toolbar. Visibility persists; lanes, contents and date ranges are preserved.
