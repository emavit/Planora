# Functional and GUI improvement backlog

Scope: all major capabilities currently implemented or requested for Planora. This is a product/engineering improvement inventory, not a claim of complete Microsoft Project parity or a per-method code audit. [Source inventory](CODE-INVENTORY.md) lists the application modules. **Existing** means there is an implementation, not that every edge case is qualified. **Partial** means the workflow has explicit gaps; **Missing** means the proposed capability is not complete.

Priority: **P0** data integrity/release blockers, **P1** core daily usability, **P2** management/productivity extensions, **P3** later expansion. Implement in dependency order and record acceptance evidence.

## Data integrity, stability and delivery

| ID | Priority | Area / current status | Improvement and acceptance criteria | Main modules |
|---|---|---|---|---|
| REL-01 | P0 | Native Windows stress — pending on latest candidate | Exercise exact binary through insertion/paste/Undo/Redo/render/close; retain hashes and results | NativeStabilityCheck, release tooling |
| REC-01 | P0 | Recovery — existing | Disk-full, interrupted-write, corrupt/latest/previous generation tests; no loss of an earlier valid checkpoint | RecoveryStore, PlanoraPackage |
| REC-02 | P1 | Recovery storage — partial | Measure large-project latency and memory; avoid rewriting unchanged base data; no typing stalls | RecoveryStore, DurableCommands |
| DATA-01 | P0 | Save/reopen — existing | Broader round-trip regression matrix; atomic model/presentation save failure handling | ProjectSession, PlanoraPackage |
| DATA-02 | P0 | MPP fidelity — partial | Publish per-format/field/operation matrix; explicit unsupported cases; independent readback evidence | PublicMppValidation, vendor |
| DATA-03 | P1 | Durable history — existing | Test every operation, malformed arguments, identity collisions and imported duration precision | DurableCommands, DurableValidation |
| DATA-04 | P1 | Saved/dirty state — existing | Cover branches, redo, presentation-only edits, save while editing and project switches | ProjectSession, PlannerWindow.Closing |
| DATA-05 | P1 | Portable preferences — partial | Carry selected columns/width/order and saved views with a project without including other projects' settings | ViewPreferences, ProjectPresentation |
| DATA-06 | P1 | File management — copy-only | Design safe associated Save, Save As, external-change detection and restore points before supporting overwrite | ProjectSession, PlannerWindow |
| DATA-07 | P1 | Error handling — partial | Actionable errors and redacted diagnostics; asynchronous failures must not close the app or silently lose edits | UI actions, persistence/transport |
| PERF-01 | P1 | Large plans — unqualified | Define representative task/resource sizes, benchmark scrolling/filtering/scheduling/replay, set measured budgets | Table, Gantt, scheduling |
| REL-02 | P1 | Packaging — preview | Reproducible builds, dependency inventory, signing/update strategy, rollback and verified notices | scripts, CI |
| MAC-01 | P1 after Windows | Mac — unqualified | Actual M1 editing, fonts, dialogs, keychain, recovery, PDFs and independent MPP validation | Platform adapters, packaging |

## Task table and project scheduling

| ID | Priority | Area / current status | Improvement and acceptance criteria | Main modules |
|---|---|---|---|---|
| TASK-01 | P1 | Blank-row entry — existing | Excel-like focus/navigation through repeated insertions, empty projects and validation errors; one Undo per operation | TaskSheetModule, TaskPaste |
| TASK-02 | P1 | Selection/paste — partial | Multi-cell keyboard selection, resource/assignment block paste, predictable cut/delete/fill semantics | EditableSheetModule, TaskPaste |
| TASK-03 | P1 | Columns — existing selector | Better categorization, favorites, descriptions, editable/read-only indicators and saved column sets | ColumnPickerWindow, TaskColumns |
| TASK-04 | P1 | Custom fields — partial | Typed authoring, lookups, status definitions, formulas and validation shared by views | Field model, TaskFieldEditing |
| TASK-05 | P1 | Outline/WBS — existing | Deep hierarchies, filtered/sorted insert behavior, rollups and structural identity tests | ProjectSession, task table |
| TASK-06 | P2 | Find/filter/sort — existing | Compound saved filters, grouping, advanced search, visible active-filter state | TaskSheetModule |
| TASK-07 | P1 | Task inspector — existing | Consolidated properties with inline validation, clearer dependencies/calendars/constraints | EditorForms, TaskDetailsModule |
| SCH-01 | P1 | Automatic dates — bounded | Qualify constraints, holidays, elapsed/estimated time and manual/auto transitions against reference behavior | AutomaticTaskDates, vendor scheduling |
| SCH-02 | P1 | Dependencies — existing | Clearer lag/lead editing, cycle explanations, predecessor/successor navigation, all four link types | ProjectSession, Gantt |
| SCH-03 | P1 | Scheduling explanation — partial | Explain why a date moved: calendar, predecessor, lag, constraint and resource assumptions | Task drivers, scheduling result |
| SCH-04 | P1 | Critical path/slack — partial | Verify total/free slack and critical-path behavior across constraints and calendars | Scheduling, Gantt |
| SCH-05 | P2 | Calendar rules — partial | Derived/resource calendars, shifts, exceptions and recurrence qualification; understandable working-time editor | Calendar editor, vendor |
| SCH-06 | P2 | Task split/recurrence — missing | Explicit persisted representation, editing and MPP preservation rules before exposing controls | Model, scheduling, Gantt |
| SCH-07 | P2 | Scenarios — missing | Copy/compare a what-if plan and merge selected changes with a preview and Undo | Model commands, comparison UI |
| SCH-08 | P1 | Actuals/progress — partial | Broader percent/work/remaining-duration semantics and partial-progress export validation | ProjectSession, vendor |

## Resources, cost and baseline management

| ID | Priority | Area / current status | Improvement and acceptance criteria | Main modules |
|---|---|---|---|---|
| RES-01 | P1 | Resource editing/assignments — existing | Bulk assignment editing, duplicate-name clarity, units validation, calendar visibility | Resource sheet, assignment editors |
| RES-02 | P1 | Workload graph — limited | Timephased availability and assignment capacity; correct overload detection before claims | ResourceGraphModule, model |
| RES-03 | P2 | Leveling — missing/limited | Preview deterministic changes, protect constraints, explain conflicts, support Undo | Scheduling/leveling module |
| RES-04 | P2 | Time tracking — missing | Local time entries, actual-work rollup, corrections and audit history | New time-entry model |
| COST-01 | P1 | Cost calculation — bounded | Overtime, rate changes, cost resources and imported distributions with explicit qualification | Resource model, cost calculations |
| BASE-01 | P1 | Baselines 0–10 — bounded | Assignment/timephased baseline curves and native preservation tests | ProjectSession, vendor |
| BASE-02 | P2 | Variance reporting — partial | Consistent schedule/work/cost variance and earned-value definitions | Reports, baseline model |

## Gantt, timelines and graphics

| ID | Priority | Area / current status | Improvement and acceptance criteria | Main modules |
|---|---|---|---|---|
| GANTT-01 | P1 | Split table/chart — existing | Reliable row alignment under wrapped text, filters, hierarchy, DPI and resizing | TaskGanttModule, GanttCanvas |
| GANTT-02 | P1 | Chart navigation — existing | Smoother zoom/scroll, date limits, Go to Today/task and visible range feedback on long schedules | DateAxis, Gantt |
| GANTT-03 | P1 | Dependency graphics — existing | Avoid clipped/overlapping arrows, add link hit targets, accessible labels and selected-link emphasis | GanttCanvas |
| GANTT-04 | P2 | Gesture editing — partial | Drag/resize dates and link handles through validated commands, preview effects and Undo | GanttCanvas, ProjectSession |
| GANTT-05 | P2 | Bar styling — partial | Legend, per-task style rules, baseline comparison and consistent screen/PDF output | ChartColorsWindow, drawing |
| TIME-01 | P1 | Multiple timelines — existing | Better lane resizing, reorder, keyboard movement, scroll feedback and block hit targets | TimelineModule |
| TIME-02 | P1 | Milestone labels — existing | Collision avoidance, readable callouts at tight zoom, explicit text overrides | TimelineCanvas |
| TIME-03 | P1 | Timeline style/date controls — existing | Consistent compact inspector, reset defaults, preview and portable state tests | TimelineStyleWindow, DateRangeWindow |
| TIME-04 | P2 | Timeline exports — partial | Lane selection, fit and clipping behavior across very long plans; clear page boundaries | PDF and timeline modules |
| PDF-01 | P1 | Three PDF modes — existing | Match more Project print conventions: legends, repeated headings, page order and margins | PdfExport, PdfExportWindow |
| PDF-02 | P1 | Print fidelity — bounded | Unicode/font fallback, high-DPI layout, many rows/columns, transparent styles, output size budgets | Drawing/PDF pipeline |
| PDF-03 | P2 | Print workflow — partial | Saved presets, richer preview/page selection and native print integration | PDF windows, platform services |

## Text and appearance

| ID | Priority | Area / current status | Improvement and acceptance criteria | Main modules |
|---|---|---|---|---|
| TXT-01 | P1 | Font/color/style — existing | Consistent scope (cell/row/style), mixed selection values, contrast and keyboard access | TextFormatting, FormattingWindows |
| TXT-02 | P2 | Rich notes — existing subset | Reliable runs after edits, paragraph tools, paste sanitization and accessible preview | RichNotesWindow, RichNote |
| TXT-03 | P1 | Formatting persistence — partial | Package/companion matching, stale-companion handling and model/format Undo interleaving | FormattingStore |
| THEME-01 | P1 | Light/dark/system — existing | Audit every dialog, status/error/selection and export preview for contrast | DesignTokens, Appearance partial |
| THEME-02 | P1 | Glass/solid chrome — existing | Keep translucency restrained; honor reduced transparency/motion and ensure text legibility | GlassNavigationSurface |
| ICON-01 | P1 | Icons — existing | Consistent icon size/stroke, meaningful tooltips and labels; audit missing commands without reusing proprietary assets | UiIcons, ribbon/menu |

## GUI and interaction improvements

| ID | Priority | Improvement | Acceptance criteria |
|---|---|---|---|
| GUI-01 | P1 | Ribbon density and hierarchy | Common actions visible on smaller windows; groups never overlap or disappear without an overflow route |
| GUI-02 | P1 | Menus and command palette | Matching labels/shortcuts/enabled state across menu, ribbon and palette; unsupported commands explain why |
| GUI-03 | P1 | Editing feedback | Inline validation identifies the cell and retains typed text; errors never silently commit |
| GUI-04 | P1 | Focus and keyboard | Predictable Tab/Enter/Esc/F2, focus restoration after dialogs, and platform-appropriate shortcuts |
| GUI-05 | P1 | Resizable panes | Table/Gantt split, timeline height and inspector sizing persist with sensible bounds |
| GUI-06 | P1 | Accessible semantics | Screen-reader names, focus indicators, keyboard reachability and non-color-only status cues |
| GUI-07 | P1 | High DPI and scaling | Test common Windows scales and Mac Retina; readable text and aligned rows/arrows at each scale |
| GUI-08 | P1 | Status and recovery feedback | Clear saved/unsaved/recoverable states; visible background failures without distracting per-keystroke messages |
| GUI-09 | P1 | Empty/loading/error states | Helpful next action for no project/tasks/resources; cancellable long operations and recoverable errors |
| GUI-10 | P1 | Dialog consistency | Consistent buttons, padding, validation, default action, Escape/X behavior and minimum sizing |
| GUI-11 | P2 | Inspector/context menus | Fast contextual actions without forcing multi-step modal editing for ordinary tasks |
| GUI-12 | P2 | Onboarding | A small generated example and discoverable keyboard tips; no confidential sample files |
| GUI-13 | P2 | Theme polish | Office-for-Mac/Apple-inspired restraint while preserving the familiar Project layout; no unlicensed asset copies |
| GUI-14 | P2 | Responsive layouts | Explicit overflow and compact ribbon behavior for smaller displays; no hidden essential controls |
| GUI-15 | P2 | Multi-window consistency | Independent project/recovery state, visible project identity and no cross-window settings loss |
| GUI-16 | P2 | Usability qualification | Walk through create → link → assign → baseline → export → reopen with representative users |

## Management views, automation and integrations

| ID | Priority | Area / current status | Improvement and acceptance criteria | Main modules |
|---|---|---|---|---|
| VIEW-01 | P1 | Progress Board — limited | Custom statuses, inline cards, drag/drop with shared typed state and Undo | ProgressBoardModule |
| VIEW-02 | P2 | Calendar — browsing | Rescheduling with confirmation/preview, clear all-day/milestone rules and filters | CalendarViewModule |
| VIEW-03 | P2 | Task Usage / Network — existing | Scalable rendering, editable relevant fields and synchronized selection | TaskUsageModule, NetworkDiagramModule |
| DASH-01 | P2 | Reports — fixed | Configurable dashboards, consistent metric definitions, filters and export | ProjectReportsModule |
| RISK-01 | P2 | Risks/issues/decisions — missing | Linked local records, status/owner/due date, review history and reports | New management model |
| TEMP-01 | P2 | Templates — partial/missing | Safe generated project templates, reusable structures and documented MPP/MPT behavior | File/model layer |
| PORT-01 | P2 | Portfolios — missing | Multi-project rollups without conflating local task IDs; versioned references | New portfolio module |
| PORT-02 | P2 | Cross-project links — missing | Explicit cycle/conflict handling and offline unavailable-project behavior | Portfolio/scheduling |
| AI-01 | P1 | Graphical setup — partial | Clear provider/mode cards, connection status and supported sign-in paths | AiConnectionEditor |
| AI-02 | P1 | Chart image attachments — missing | Select/review bounded Gantt/timeline images, exact destination disclosure, supported provider payloads and cancellation | AI clients, PDF snapshots |
| AI-03 | P1 | AI request reliability — bounded | Timeout/cancel/size/error tests and accurate usage/model status without credential leaks | AiConnections, AiCodexClient |
| AI-04 | P2 | AI project edits — task import only | Typed broader proposals, explicit diff review, stale-revision protection and one Undo | AI task draft, commands |
| MCP-01 | P1 | Generic MCP client — existing subset | Broader authentication/server profiles, reconnect and capability-specific errors | McpConnections, settings/editor |
| MCP-02 | P2 | Project MCP host — missing | Least-privilege read/edit tools with reviewed mutation and local permissions | New host module |
| AUTO-01 | P2 | Local rules — missing | Deterministic triggers/actions, recursion guards, preview/history and Undo | New automation engine |
| SYNC-01 | P2 | Personal sync — missing | Conflict detection/merge, offline versioning and no silent last-writer data loss | Persistence and sync interfaces |
| COLLAB-01 | P3 | Live collaboration — separate scope | Identity/permissions, concurrency and hosted infrastructure require an explicit product decision | Future service |

## Architecture and maintenance

| ID | Priority | Improvement | Acceptance criteria |
|---|---|---|---|
| ARCH-01 | P1 | Core/platform boundaries | Approved migration map; characterize current behavior before extracting interfaces/adapters |
| ARCH-02 | P1 | Async cancellation and ownership | Every long action has cancellation/lifetime rules; only owned subprocesses are stopped |
| ARCH-03 | P1 | Testing structure | Separate model correctness, headless UI, native stress and performance qualification; no false-green aggregation |
| ARCH-04 | P1 | Security/privacy review | Malformed inputs bounded, external content treated as data, credentials redacted and publication artifacts scanned |
| ARCH-05 | P2 | Dependency provenance | Record upstream revisions/local patches and resolved license inventory before releases |
| ARCH-06 | P2 | Documentation | Keep user instructions, limitations, task acceptance criteria and validation evidence current together |

## Recommended execution order

Start with REL-01, REC-01, DATA-01/02/03 and GUI-03/04. Then qualify large-project behavior and finish AI-01/02. Expand custom fields/views and scheduling only after shared model semantics are explicit. Resource/portfolio/rule/sync work follows the durable foundation; macOS qualification follows the Windows release gate.

Do not mark a row done merely because a button exists. Record behavior, persistence, Undo, error/cancel handling, affected platform and relevant tests. The complete source method inventory and real-project compatibility matrix remain separate engineering audits.
