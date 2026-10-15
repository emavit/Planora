# How to use Planora

This guide describes the 0.2.1 Windows preview source. Some Microsoft Project functions remain unavailable or only partially supported. macOS is planned and unqualified.

## 1. Start the app

If you have a self-contained Windows build, keep its whole folder together and run `Planora.exe`. This GitHub source folder intentionally contains no compiled app. To build from source, install .NET 10 and follow the README. Run commands from the repository root.

The first screen uses a ribbon/menu, a task table beside the Gantt chart, and an optional timeline area above them. Use the ribbon tabs for Task, Resource, Project, View and formatting commands. Help → All Project Commands exposes the observed command catalogue and explains unsupported routes.

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
