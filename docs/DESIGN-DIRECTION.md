# Planora redesign direction

Status: staged implementation. Releases 0.3.0–0.3.1 implement compact navigation, editable inspector, saved workspace views and ten original schedule starters. The complete redesign and broad workflow coverage remain open.

## Owner decision — 2026-10-16

The owner authorizes a complete visual redesign. Matching Microsoft Project's ribbon or Office for Mac appearance is no longer a constraint. Preserve `.mpp` compatibility and pursue the broadest practical feature coverage. Existing original-file protections, offline operation and Windows → validated macOS → Android/iOS sequencing still apply. This decision authorizes UI design changes, not an unreviewed source-directory migration or publication.

## Workspace

- Compact top bar: project name, save/dirty state, Undo/Redo, command search and contextual actions. Keep discoverable File/Edit/View menus and keyboard equivalents.
- Collapsible navigation: projects, templates and management views. The active project remains the primary context.
- View tabs: Table, Gantt, Board, Calendar and qualified management views. Share selection, filters and field definitions across views.
- Central spreadsheet/Gantt split: resizable panes, aligned rows, sticky headers, searchable column insertion and a persistent blank entry row. Enter/Tab commits; Esc cancels; F2 edits. Task creation requires no modal wizard.
- Optional stacked timelines above the workspace: drag blocks between lanes, edit labels/colors, independent date limits and zoom; hide without losing configuration.
- Collapsible right inspector: task details, dependencies, resources, progress, notes and formatting. Show a property in context without forcing a dialog for routine edits.
- Bottom status area: scheduling mode, validation issues, zoom and background activity. Explain failed edits without discarding input.

Use original icons, restrained color, clear typography and a dense default suitable for long plans. Offer comfortable density and light/dark/system themes. Glass effects belong on navigation surfaces only, with an opaque high-contrast/reduced-transparency fallback. Keep table cells, dates and dependency lines readable. Do not use transparency behind dense project data.

## Inspiration and boundaries

The public [Microsoft planning page](https://www.microsoft.com/it-it/microsoft-365/project/simple-project-management?market=it) suggests connected Grid/Board/Timeline modes and visible assignment/date information. The [Jira template gallery](https://www.atlassian.com/software/jira/templates) suggests category navigation, concise preview cards and clear entry points. Translate those ideas into a desktop workspace; marketing hero layouts do not belong above daily planning data.

The [Jira–monday comparison](https://www.atlassian.com/software/jira/comparison/jira-vs-monday) is vendor marketing, not neutral proof of competitors' capabilities. Use its workflow topics as research questions. Do not reproduce vendor assets, branding or unsupported comparison claims.

REA public-page observations: Microsoft `ev_47b20a91e2ad8c1cc09bf9f2848822957e60e4cb4a97043a2df095de2f034206`; Jira templates `ev_b4e6d19db9974328dad66a525bba827304e3ef9c15c2077b8b4862704b7e2555`. These establish public page content/design only, not authenticated application behavior. Comparison capture `ev_ea23c74c1e9231097cfb6e0ea2da20499e9430ef7c35baa63717470253926d4e` retained page artifacts but its heading wait was incomplete because the selector matched two headings.

## Feature breadth

Aim to cover the publicly listed Jira template families: software development, marketing, design, sales, operations, service management, human resources, legal, IT operations and finance. A category label alone is not support. Inventory linked templates, identify required capabilities, and qualify each preset before marking it available. Full catalog enumeration and implementation remain open.

Build reusable capabilities before multiplying presets: typed custom fields/statuses, configurable workflows, saved views, reusable templates, risks/issues, dashboards and portfolios. Add backlog/sprints/releases and local automation when their underlying models are ready. Hosted collaboration, public forms and external services require separate scope decisions. Preserve scheduling depth rather than reducing the product to a task board.

## Compatibility contract

Keep model commands, scheduling and interchange separate from layout/theme components. A UI change must preserve stable task/resource identities and command history. Save Planora-only views, colors, template metadata and workflow fields in the `.planora` presentation/extension model unless a verified MPP mapping exists. Report unsupported export fields before export; never silently claim lossless conversion.

Qualify import → edit → export → independent readback against public/synthetic fixtures. Compare hierarchy, relationships and lag, calendars, dates, resources, assignments, work/costs, baselines and actuals within documented supported subsets. Retain failures by format/operation. Protect source files and avoid overwriting originals. Universal MPP fidelity remains unproven.

## Delivery and acceptance

1. Record existing keyboard/editing, selection, scheduling and save behavior; complete current Windows stability gate.
2. Introduce shared appearance tokens and modular workspace shell without changing model/persistence semantics. Validate existing editing and view switching.
3. Migrate Table/Gantt and inspector first, then timelines, remaining views and template library. Keep each increment usable.
4. Expand domain capabilities and original template presets behind explicit compatibility requirements; retain accurate supported/partial/missing status.
5. Qualify keyboard-only use, focus visibility, screen-reader labels, contrast, scaling, theme switching and representative large plans. Exercise dirty close Save/Don't Save/Cancel and recovery after interrupted work.
6. Run MPP regression checks after affected model/export changes; real Windows qualification precedes Mac qualification and later mobile development.

No release date, complete template coverage or full Microsoft Project parity is implied by this direction.


## Approved hybrid direction — 2026-10-17

Combine the Apple-inspired appearance (preview 1) with the Microsoft Project working layout (preview 2). Retain ribbon categories and commands, editable task table beside the Gantt, timelines above, and the optional inspector. Use neutral surfaces, restrained blue selection, compact command groups and quiet overflow controls. Keep chart data surfaces opaque and legible in both themes. This is an Avalonia implementation, not native AppKit.

The first refinement reduces ribbon group height and spacing, uses a tinted active tab, replaces full-height overflow blocks with centered arrows, and softens table headers. Existing scheduling and MPP behavior are unchanged.


## Gantt reference — 2026-10-17

Reference: [Gantt Chart Conception by Scabbard Dii](https://dribbble.com/shots/7115251-Gantt-Chart-Conception). Visually reviewed the published artwork. Adopt its rounded task bars and restrained chart framing within the approved Apple/Project hybrid. Implemented slightly taller rounded bars with progress clipped to their silhouette; exact date endpoints, custom colors, critical outlines, dependency routing and baseline positions remain intact. In-bar task labels, group color presets and richer date-header treatment are further design candidates, not implemented features. No artwork or assets are bundled.

## Office influence — 2026-10-19

User confirmed both Office styling (primary) and Microsoft Project compatibility. Ribbon group captions now sit below commands, with subtle vertical separators and an underlined selected tab. Retain Planora identity and the Apple/Project hybrid workspace. No Microsoft logos or proprietary assets are added.
