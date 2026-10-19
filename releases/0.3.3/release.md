# Planora 0.3.3 — focused planning workspace

Previous packaged version: 0.3.1. The user requested the next combined release be numbered 0.3.3; no 0.3.2 binary was published.

## Changes

1. Apple-inspired neutral light/dark surfaces, quiet actions and Gantt-first segmented navigation. Navigation wraps at smaller widths instead of requiring horizontal toolbar scrolling.
2. Row density menu: Compact (28) and Comfortable (38). Gantt row positions follow the table; text wrapping and larger custom fonts retain their formatting rules.
3. Planning focus via the Focus button or F11. It hides the optional ribbon, sidebar and inspector while preserving timeline visibility, chart date limits and stored panel preferences. Exit restores the configured layout. Invalid inspector drafts prevent entering focus.
4. Resizable task inspector with Schedule, Dependencies and Progress groups, an automatic/manual scheduling selector, persisted width and keyboard-accessible width presets. Existing atomic Apply, retained invalid drafts and Undo behavior remain.

## How to use

Open the Gantt view. Choose Row density for spacing. Press F11 to concentrate on the chart and timeline. Open Inspector and drag its left divider, or use Inspector width presets. Edit a task and Apply; automatically scheduled tasks show computed dates after commit. Enter new tasks directly in the blank task-name row.

## Scope and known limitations

This is a Windows preview release, not full Microsoft Project parity or a completed implementation of every item in the earlier combined design plan. Rectangular cell-range editing feedback, inspector Resources/Notes sections, per-field validation messages and Gantt drag previews remain planned. The implemented inspector retains form-level validation. Existing timeline/Gantt date controls and ordinary spreadsheet editing are preserved.

The public MPP corpus has 10 documented pre-existing failures across 88 stages (78 passing in the prior baseline). The release gate checks for new regressions; retained failures do not establish full compatibility. See docs/compatibility-known-failures.json in the source repository. macOS and mobile are not qualified by this Windows release.

Validation results for this exact package are appended by the release script and retained in validation/.
