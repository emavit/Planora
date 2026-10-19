# How to use Planora

This guide describes the 0.3.1 Windows preview source. Some Microsoft Project functions remain unavailable or only partially supported. macOS is planned and unqualified. Android and iOS apps are planned after the macOS app is completed and validated; mobile versions are not available yet. [Mobile roadmap](MOBILE-PLAN.md).

## 1. Start the app

If you have a self-contained Windows build, keep its whole folder together and run `Planora.exe`. This GitHub source folder intentionally contains no compiled app. To build from source, install .NET 10 and follow the README. Run commands from the repository root.

The first screen uses compact view navigation, menus, a task table beside the Gantt chart, and optional timelines above them. Choose Show ribbon for expanded Task, Resource, Project, View and formatting commands. Help → All Project Commands exposes the observed command catalogue and explains unsupported routes.

## 2. Create or open a project

Choose **File → New**. Enter the project name and start date in Project Information. For an existing project, choose **File → Open** and select `.mpp` or `.planora`.

`.mpp` is Microsoft Project interchange; compatibility depends on the fields and operations used. `.planora` retains the original file bytes together with Planora edits/history and presentation. A portable file can reopen without its original source path being available. Use copies of important projects while evaluating the preview.

## 3. Enter tasks like spreadsheet rows

1. Click the blank row at the bottom of the task table and type a task name.
2. Commit with **Enter** or **Tab**. Use **Esc** to cancel the current edit.
3. Double-click a cell or press **F2** to edit an existing task.
4. Enter a duration such as `2d`, `4h` or `1w`.
5. Set **Task Mode** to automatic when dependencies should calculate dates.

Use Task → Insert Task to insert a row; Insert Milestone creates a milestone. Indent/Outdent organizes summary hierarchies. A copied tab/newline block can be pasted into editable task cells; the block validates atomically and one Undo reverses it. Resource/assignment sheets currently accept one pasted cell at a time.

Calculated fields and identity columns are read-only. If a cell refuses input, inspect the message rather than assuming it is an editable custom field. The full custom-field/formula authoring system is not implemented.

## 4. Link tasks and calculate dates

Show the **Predecessors** column. Enter the preceding task's displayed row ID, optionally with link type and lag:

| Input | Meaning |
|---|---|
| `1FS` | This task starts after task 1 finishes |
| `1FS+2d` | Finish-to-start with two working days of lag |
| `1SS` | Start-to-start |
| `1FF` | Finish-to-finish |
| `1SF` | Start-to-finish |
| `1FS; 3SS+4h` | Two predecessors |

Automatic task dates update when an edit commits. Working calendars affect dates. Manual tasks retain manually controlled dates. Circular dependencies are rejected. If dates differ from expectations, check task mode, duration, predecessor IDs, constraints and working time. Advanced actual-work/assignment distributions remain bounded; do not assume every Microsoft Project scheduling case is equivalent.

Project → Change Working Time edits the working week and exceptions. Project → Calculate Project distinguishes task scheduling from resource work/cost calculation; review the options before recalculating an imported file.

## 5. Choose columns and navigate

Use **View → Insert Columns** to search, insert, reorder or hide available fields. Header menus and table presets provide additional controls. The source tests observe 147 selectable fields on their sample; this is not a promise that every field is editable in every document.

Use Find, Replace, Go To and task filters to navigate. Collapse/expand outlines for long plans. Display sorting changes the view rather than rewriting the task hierarchy. Undo/Redo is available from Edit and the toolbar.

## 6. Resources, work, costs and progress

Use Resource → Add Resource, then Assign Resources or the **Resource Names** task column. For example, `Engineer[50%]` assigns an existing resource at 50%; names must identify one existing resource.

Resource Sheet supports names, rates and selected properties. Resource Usage exposes assignment units/work. Scheduling and cost calculations have qualified limits; overtime, contours and arbitrary imported native recalculation are not fully supported.

Use completion controls for progress and Project → Set Baseline for supported baseline snapshots. Baseline 0 through 10 have implementation support with bounded export qualification. Preserve a pre-change copy when testing updates on important projects.

## 7. Timelines and Gantt

Use **View → Show / Hide Timeline**. In the timeline area:

- Add or rename lanes, and select which tasks appear.
- Drag a block between lanes; Alt+Up/Down can move the selected block between adjacent lanes.
- Right-click a block to format it; lane Format controls appearance.
- Use Dates, zoom buttons and Fit for each timeline independently.

Moving a block between timeline lanes changes its presentation, not the scheduled task dates. Gantt dates/zoom are separate, through View → Zoom and Gantt Dates. Timeline milestones have callouts. Each view still has usability improvements listed in the improvement backlog.

## 8. Appearance and text

Choose **Appearance → Theme → Automatic, Light or Dark**, or the theme picker. Automatic follows the computer. View → Reduce Transparency gives solid navigation.

Text Format / Format → Text and Color controls supported fonts, styles, wrapping and colors with cell/row scope. Formatted Notes supports rich notes. Gantt Colors and timeline Format control chart appearance. Formatting in an MPP copy uses a companion `.projectdesk.json` file where applicable; keep the companion with the copy. Prefer `.planora` for carrying formatting and timelines together. Application-wide preferences and table column configurations remain local.

## 9. Export PDF

Choose **File → Export PDF** and select task table + Gantt, Gantt only, or timelines. Review paper size/orientation, rows, page width/date limits and the preview before exporting. Large plans can span multiple pages. Exporting a chart does not qualify all print-layout behavior of Microsoft Project.

## 10. Save, close and recover

**File → Save a Copy** lets you choose `.mpp` or `.planora`. Choose a new filename: existing destinations are protected. MPP compatibility warnings describe possible preservation limits; review them before accepting.

Closing an unsaved project offers **Save / Don't Save / Cancel**. Cancel returns to editing. For portable projects, timeline-only changes also count as unsaved presentation changes.

Recovery checks every 15 seconds. The status area reports success/failure. File → Create Recovery Checkpoint creates one immediately; File → Recovery reviews retained sessions and can restore the previous generation. A checkpoint is not a substitute for a saved project copy, and the last few seconds can be missing after an interruption. [Detailed recovery guide](RECOVERY.md).

## 11. Optional AI and MCP connections

AI → ChatGPT / Claude opens the assistant panel. Connection Settings supports the currently implemented API, official Codex/ChatGPT subscription route, or browser handoff where offered. API keys use credential storage; API usage may be billed separately from subscriptions.

Select task rows before opening AI if you want to include them. Review and Send shows the outgoing content. Task drafts require review before import; an answer alone does not change the project. Chart-image attachments and the full graphical onboarding flow remain unfinished.

Connectors provides general MCP setup. Configure a trusted server's supported URL or local executable and inspect its tools. Review proposed task imports. Do not assume all server authentication modes or arbitrary tool schemas are supported. Never add secrets to GitHub or a project file.

## 12. Troubleshooting

| Symptom | Check |
|---|---|
| Cannot edit a cell | Is it calculated/read-only? Double-click or F2; inspect the validation message |
| Auto dates missing/unexpected | Automatic mode, valid duration/predecessors, calendar and constraints |
| Save refuses a file | Choose a new filename; review compatibility or preservation diagnostics |
| Formatting missing in an MPP copy | Keep its companion file; use `.planora` for portable presentation |
| Recovery fails | Save a new project copy; review the status message and previous generation |
| AI request fails | Connection mode, executable/key/model, provider availability and usage limits |
| App instability | Use synthetic reproduction data; retain redacted diagnostics and report version/OS |

See [all improvement areas](IMPROVEMENTS.md) and [open implementation tasks](TASKS.md). Never attach confidential project files or API keys to a public issue.

## Public practice files

Open the MPP files under [examples/public-mpp](../examples/public-mpp/) for public compatibility practice. Read [provenance, license and Microsoft gallery coverage](../examples/README.md) first. The XML files are producer references, not additional UI-open templates. Do not overwrite checked-in files; save a new copy. Private examples are excluded.

## New workspace tools (0.3.0–0.3.1)

- **Inspector** opens the editable task panel. Select a task, edit name, duration, scheduling mode, predecessors or completion, then Apply. The changes are one Undo step; invalid input stays in the panel. Cancel edits discards the draft. Changing row selection keeps an unapplied draft attached to its original task. Commands validate pending edits before continuing.
- **Saved views…** stores columns, filters, active view, pane visibility and Gantt dates/zoom on this computer. Save current with a unique name; select a saved view and Apply. Delete removes only the saved layout.
- **File → New from Template…** offers ten original schedule starters with preview, project name and start date. Create project runs the existing save prompt if needed. These are linked task schedules; they do not add sprints, custom workflows or service-desk behavior.
- Each built release folder includes **release.md**, validation evidence and file hashes. Keep the full folder together when copying the app. [Version differences](../releases/release.md).

## Workspace controls in 0.3.3

Use Row density to choose Compact or Comfortable. Text wrapping and larger custom fonts still take precedence. Press F11 or Focus to hide optional ribbon/sidebar/inspector while retaining timeline visibility; Exit focus restores panel preferences. Resize the inspector with its left divider or the Inspector width presets. Width persists on this computer. Schedule mode uses an automatic/manual selector; Apply commits the task fields together and Undo restores the previous values. Invalid drafts remain attached to their original task.

## Inspector layout in 0.3.5

The date summary and Apply/Cancel controls stay visible while the grouped fields scroll. Invalid duration, task name or completion values show an error below the field; changing the rejected value clears that message. Apply validates the batch before modifying the project. Dependency/cross-field errors are shown in the pinned status area. Cancel discards the draft and follows the current task selection.

## Planning ribbon in 0.3.6

Click Show ribbon if it is hidden. Planning gathers Create, Gantt, Dates & selection, Timeline, Schedule and Presentation commands. Gantt zoom/fit and Gantt dates affect the chart; Timeline dates affect the active timeline. Use Previous/Next command arrows to reach overflowing groups and All ribbon tabs for tabs outside the visible strip. Hide controls leaves the tabs visible; selecting a tab opens its commands again. Existing Task, Resource, Project, View and formatting tabs remain available.

## Timeline workspace in 0.3.7

Use Options beside the timeline selector to rename or remove the active lane. The last lane cannot be removed; hide the timeline module instead. Fit panel adjusts the panel height to its lanes without changing dates or zoom. It is useful after deleting a lane or making the timeline panel large. Each lane retains direct Tasks, Dates, Format and zoom actions.


## Version 0.3.8 appearance

The indigo/aqua icon identifies the new Planora build. The familiar ribbon commands now show group names below their controls; the active tab has an underline. Show ribbon restores the command area if your saved workspace is compact. Gantt bars have rounded ends; progress and custom colors still use your existing formatting. This release does not change .mpp or .planora file identities.

## Microsoft Project Entry column preset

In Gantt, choose **Choose columns** (or a column header menu → Insert Column). Select **Preset: Microsoft Project — Entry**, then **Apply columns**. The preset restores Indicators, Task Mode, Task Name, Duration, Start, Finish, Predecessors and Resource Names in that order, with standard field widths. You can adjust the selection before applying; Cancel leaves the current view unchanged. Column order and widths are saved in workspace preferences. The preset follows the current light/dark theme.

## Colored command icons — 0.3.9

Planning commands use teal, resource commands purple, milestones and notes amber, PDF/delete/unlink red, and general tools blue. Dark mode uses lighter variants. Color supplements the existing labels and shapes; command behavior is unchanged. Dropdown and context-menu rows use the same rounded style and selection surfaces as the workspace.

## Quickly hide timelines

Click **Hide timelines** in the workspace toolbar above the Gantt or task table. The same button becomes **Show timelines**. This hides all lanes together, remembers your choice, and keeps timeline tasks, formatting and date ranges intact.
