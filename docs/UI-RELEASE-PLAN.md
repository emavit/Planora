# UI-first release plan

Updated 2026-10-16. Versions 0.3.0, 0.3.1 and 0.3.3 are packaged. The user changed the combined release number to 0.3.3; no 0.3.2 binary was published. Version 0.3.3 delivers Apple-inspired navigation, row density, focus and a resizable grouped inspector. It does not complete every original acceptance criterion below. Rectangular selection feedback, Resources/Notes inspector sections, per-field errors and broader scaling/accessibility qualification remain open. The earlier combined scope is retained below as outstanding acceptance, not as a claim of completion. Later version numbers remain proposals.

## Design review of 0.3.1

Reviewed the packaged light/dark workspace captures and template preview. This is a visual review, not a completed accessibility or usability audit.

| Observed issue | Impact | Decision |
|---|---|---|
| Menu, project bar, view bar, timeline toolbar and lane toolbar stack above the table | Too many controls compete with planning content | Keep project identity in one bar, views in one row; move secondary timeline actions into an explicit menu |
| All controls have similar visual weight | Main actions are hard to pick out | One contextual primary action; quiet secondary buttons, consistent line icons and separators |
| Inspector is a fixed-width form with free-text scheduling mode | Long scanning path and avoidable invalid input | Resizable grouped inspector with a mode selector, duration/unit editor and adjacent computed dates |
| Table shows few columns when the inspector is open | Planning information requires more horizontal scrolling | Save pane sizes, add density modes and a focus mode; retain linked table/Gantt row alignment |
| Row selection and active cell use competing blue treatments | Hard to distinguish what typing will change | Subtle row tint plus an unambiguous cell focus outline, in both themes |
| Timeline blocks truncate names and dates | Important milestones are difficult to scan | Lane-specific label rules, tooltips and explicit selected-block styling |
| Template navigation is a text list | Categories and contents are harder to compare | Searchable category cards with concise previews and capability labels |

The existing direct blank-row entry, view switching, command search and optional panels are good foundations. Preserve these while improving the shell.

## Visual direction

Apple-inspired desktop design is the user’s explicit visual direction. Use neutral sidebar materials, compact segmented navigation, restrained depth, grouped inspectors and quiet controls. Keep native platform fonts and window chrome; do not simulate macOS window buttons on Windows. Gantt and timelines remain the dominant content.

A calm desktop planning workspace: neutral document surfaces, a restrained blue/teal accent, readable 13–14 px body text, clear project identity and less border clutter. Original line icons should share stroke weight and optical size. Use color for selection and meaning; pair status colors with labels or shapes.

Use a compact left navigation rail that expands when wanted. Keep menus available and commands discoverable. Table/Gantt remain the default working surface. Board and Calendar share the same task model. Glass is optional on navigation only, with an opaque fallback; it must never reduce chart or text legibility.

The accompanying [interactive concept](design/planora-next.html) demonstrates layout, themes, density, panel visibility, task selection and Gantt/Board switching using synthetic data. It is a design prototype, not the desktop executable or a feature-completion claim.

## Release sequence

| Release | Workspace / interaction scope | Editing / presentation scope | Visible outcome |
|---|---|---|---|
| **0.3.3 — planning workspace (partially delivered; editing remainder open)** | Adaptive navigation and command hierarchy; compact/comfortable density and focus mode | Spreadsheet cell/range selection and editing feedback; resizable grouped task inspector | Gantt and timelines stay central, with more planning space, clear editing targets and predictable keyboard movement |
| **Next feature release — chart interaction (version pending)** | Gantt move/resize/link gestures with previews | Timeline lane and block layout controls | Schedule changes can be previewed and undone; presentation timelines are easier to arrange |
| **Future feature release — everyday views (version pending)** | Editable Board with typed custom statuses | Editable Calendar with schedule-change preview | A task remains consistent when changed from different views |
| **Future feature release — project entry points (version pending)** | Visual template gallery with capability previews | Local project hub with recent files and recovery entry | Faster startup and template choice, with clear local-file/recovery state |
| **Future feature release — presentation tools (version pending)** | Print/PDF preview with reusable page presets | Contextual formatting studio for tasks, bars and timelines | Predictable exported pages and consistent formatting without scattered dialogs |

### Combined scope acceptance — workspace and charts

- At 1280×800 and 1440×900 logical pixels, view navigation, add task, search and essential save commands remain reachable without horizontal toolbar scrolling. At the current minimum window width, secondary commands use explicit overflow.
- Compact/comfortable row heights change the table and Gantt together; dependency endpoints and selection remain aligned.
- Focus mode keeps Gantt and timelines central and available, hides optional navigation/ribbon/inspector, restores the exact previous layout on exit and has a discoverable keyboard command. Timeline visibility remains user-controlled; focus mode must not hide it.
- Light/dark/system and reduced-transparency behavior apply to every new control. Add disabled, hover, selected and keyboard-focus states.
- Existing menus, insertion, undo, recovery and close flow still work. Layout changes do not mark the scheduling model modified.

### Combined scope acceptance — confident editing (formerly 0.3.3)

- Clearly distinguish active cell, selected rows and rectangular range. Enter/Tab/Shift+Tab/Esc/F2 and multiline paste follow documented rules, including blank-row insertion.
- Invalid input stays visible with an error attached to the affected field. No silent partial batch commit.
- Inspector sections: Schedule, Dependencies, Resources, Progress, Notes. Use selectors where values are constrained; preserve full text when validation fails.
- Inspector width persists within bounds; closing it restores workspace space. Pending drafts remain bound to their original task and are checked on save, navigation and close.
- Add meaningful keyboard and focus-restoration tests, not just screenshot comparisons.

### Chart interaction acceptance (version pending)

- Moving/resizing a schedule bar previews dates and affected successors before commit; Esc cancels and one Undo reverses the complete operation.
- Respect manual/automatic mode, calendars, constraints and dependency cycles. Unsupported scheduling operations explain the limitation.
- Timeline lane reorder/resize and block label placement have keyboard alternatives. Presentation movement never silently edits task dates.
- Zoom and explicit date limits remain independent for Gantt and timelines.

### Everyday views dependencies and acceptance (version pending)

- First define typed statuses and their persistence/export policy. A custom workflow status must not silently become percent complete.
- Board changes and calendar scheduling use shared model commands with validation, Undo and synchronized selection.
- Protect imported constraints and calculated dates; preview calendar changes that affect linked tasks.
- Show Planora-only field/export limitations before MPP export. Domain/schema work is part of the feature and must be tested before UI polish is marked complete.

### Project entry points acceptance (version pending)

- Template cards show category, tasks, dependencies and supported capabilities before creation. Distinguish basic schedule starters from richer workflows.
- Search and keyboard navigation work with no results, long names and category filters.
- Local recent-file entries handle moved/missing files. Recovery is clearly distinguished from the last saved project.
- No private project names, paths or previews are bundled as examples.

### Presentation tools acceptance (version pending)

- PDF preview uses the same layout calculation as export; paper, orientation, date limits, repeated headers and page count update consistently.
- Saved print presets are validated; table + Gantt, Gantt-only and timeline modes remain available.
- Formatting controls clearly show whether an edit applies to a cell, row, task bar, timeline item or style. Mixed selections are represented accurately.
- Formatting remains undoable and survives the appropriate supported storage format; unsupported MPP mappings are disclosed.

## Definition of ready to release

1. All features in the release meet their acceptance criteria (four for 0.3.3, two for subsequent releases); no placeholder buttons counted as completion.
2. Review real screenshots in both themes, with empty, populated, long-name and error states. Use synthetic/public projects.
3. Exercise keyboard-only workflows and representative Windows scaling; record contrast measurements before claiming an accessibility pass.
4. Run relevant functional suites, the exact packaged native Windows stress test and the public MPP regression gate. The current baseline is 78 passing stages and 10 documented existing failures; it is not full compatibility.
5. Preserve each version folder. Include executable, dependencies, notices, hashes, validation evidence and release.md explaining changes from the preceding version.
6. Update delivered/planned status accurately. GitHub publication is a separate action.

Windows first. Mac qualification follows the Windows gate; Android/iOS follow completed and validated macOS. This UI priority changes the next implementation order, not the platform sequence.

0.3.4 is reserved for the Resource Sheet performance bugfix; it does not deliver the chart gestures formerly proposed under that number.

0.3.5 delivers pinned inspector dates/actions and adjacent errors for typed scalar fields. Dependency errors remain at form level. Board/Calendar work formerly proposed for this number is still pending.

0.3.6 delivers the grouped ribbon redesign and Planning controls. The gallery/project hub formerly proposed for this version remain pending. User-requested ribbon improvements take priority over that proposed sequence.

0.3.7 delivers the modern timeline toolbar and Fit panel. The PDF preview/formatting studio previously proposed under this number remain pending.
