# Working desktop app delivery draft

Date: 2026-10-16. Status: implementation plan, not a completion claim.

## Decision

Complete Planora's existing C#/.NET/Avalonia desktop app. Gantt and multiple timelines are the primary workspace. Apply the Apple-inspired design to working desktop controls. The HTML concept remains a design reference, not the deliverable. Retain the original platform order: Windows, qualified macOS, then Android and iOS.

Lovable is optional for bounded visual experiments or a future web companion. Its available connector creates full-stack TypeScript apps with Tailwind and shadcn/ui, rather than directly modifying this Avalonia application. A generated web frontend would require deliberate integration with local scheduling, persistence and MPP conversion. It does not establish offline MPP compatibility. Do not introduce a second scheduling implementation or cloud dependency to finish the Windows app.

Official reference: https://docs.lovable.dev/tips-tricks/chatgpt-app and https://lovable.dev/build/mobile-app-builder . Connector capabilities inspected on the date above. No Lovable project was created, private files uploaded, or cloud app deployed for this assessment.

## Current evidence

- Latest packaged executable: 0.3.1; combined 0.3.2 remains unfinished.
- First Apple-inspired desktop pass builds successfully; 26 workspace/inspector checks passed. These are focused checks, not whole-product qualification.
- Last packaged public MPP baseline: 78 passing stages and 10 known failures; the source baseline since 2026-10-25 is 88 passing stages and 0 known failures. A matching baseline prevents regressions but does not establish full compatibility.
- Native Windows checks previously passed for 0.3.1; repeat against the next exact packaged binary.

## Execution order

### 1. Finish combined 0.3.2: workspace and editing

Deliver all four agreed features in one release: adaptive navigation; compact/comfortable density and focus; spreadsheet selection/editing feedback; resizable grouped inspector.

- Keep Gantt and timeline visible and usable while optional navigation and inspector collapse. Preserve user timeline visibility and independent date ranges.
- New task entry must work directly in the blank row. Enter/Tab/Shift+Tab/Esc/F2, paste, insertion/deletion and Undo must have consistent outcomes.
- Duration, Auto mode and a predecessor such as 1FS must yield computed start/finish dates immediately after commit, consistently in table, inspector, Gantt and timeline.
- Use constrained selectors, grouped properties, field-level errors and persistent bounded inspector width. Preserve invalid drafts without partial model changes.
- Show selected rows, active cell and ranges distinctly; keep table rows, bars and dependency arrows aligned at each density.
- Validate 1280x800 and 1440x900, minimum width, both themes and representative Windows scaling. Preserve keyboard focus through resize and panel changes.

Ship only when all four features meet docs/UI-RELEASE-PLAN.md acceptance. Include release.md and exact binary validation evidence. No separate 0.3.3 package.

### 2. Close core workflow defects before expanding views

Audit and repair existing controls against this scenario:

Create a project -> type tasks -> add FS/SS/FF/SF dependencies and lag -> inspect calculated dates -> assign resources and costs -> record a baseline -> update progress -> compare Gantt -> arrange multiple timelines -> save -> close -> reopen -> export and reopen MPP -> export PDF.

For each operation, record supported behavior, expected result, regression coverage and any unresolved limitation. Test weekends, custom calendars, milestones, summaries, manual tasks, invalid links and cycles. Every enabled command must produce its documented result or explain why it cannot run. Unsupported advanced features must not silently appear successful.

This hardening is release-blocking for affected supported workflows; it is not a claim that every Microsoft Project feature already exists.

### 3. Deliver chart interactions in 0.3.4

Two features: Gantt move/resize/link previews and timeline layout controls. Preview affected schedule changes, allow Escape cancellation and reverse the complete operation with one Undo. Timeline presentation changes must not silently alter schedule dates. Provide keyboard alternatives for dragging, independent zoom/date limits and persistent styling.

### 4. Qualify storage, MPP and print

- Preserve originals; save atomically; test failed writes, interrupted saves, recovery fallback and Save / Don't Save / Cancel.
- Fix and independently verify known MPP failures by field/operation, including assignment data, baseline fixed costs, calendar references and baseline date ranges. Add regression fixtures for each fix; never erase failures by regenerating the baseline.
- Compare exported values with an independent reader or Microsoft Project on generated/public copies; internal roundtrip alone is insufficient.
- Keep explicit unsupported-field diagnostics and a published compatibility matrix. Unknown imported data must not be silently dropped when editing supported fields.
- Verify table+Gantt, Gantt-only and timeline PDFs, with date bounds, page breaks, repeated headings, readable text and the same layout in preview/export.

MPP compatibility is a release gate for the declared supported scope. Universal compatibility remains unproven unless demonstrated; essential unresolved scenarios prevent declaring the app fully ready for those files.

### 5. Qualify a usable Windows release

Use the packaged executable on a clean Windows 11 environment without developer tooling. Test offline new/open/edit/save/reopen/export, native dialogs, keyboard workflows and a sustained editing session. Use synthetic projects of 100, 1,000 and 10,000 tasks as proposed measurement tiers; record load, edit, scroll, save and memory results, then publish the supported size envelope. These tiers are test targets, not current performance claims.

Required outputs: self-contained executable and dependencies, release.md, checksums, licenses, quick-start guide, compatibility limits and test evidence. Keep prior releases intact. Public GitHub publication is separate from local packaging.

### 6. Expand breadth after the dependable planning workflow

Retain the broader roadmap: typed Board/Calendar edits, template gallery/local project hub and PDF/formatting improvements. Verify AI/MCP connection failure, cancellation, credential storage and explicit review of proposed changes; provider-specific live tests remain separately qualified. Actual Mac M1 testing follows Windows, with mobile architecture and app development after macOS qualification.

## Definition of a working app

A user can complete the agreed end-to-end planning workflow offline with the packaged Windows executable, without losing data, using only functioning controls. Save/reopen and supported MPP/PDF exports preserve the expected result. Known limitations are visible and documented. A screenshot, passing build, web prototype or finite MPP sample is insufficient by itself.

## Immediate next implementation

Finish density/focus behavior and the inspector/selection work in combined 0.3.2, then run the full release gates. Keep design work attached to functioning controls; do not add new mock-only screens to the delivery path.

## Delivery update: 0.3.3

The requested release number is now 0.3.3. Its packaged Windows build passed 32 workspace checks, 73 template/saved-view checks, core/scheduling/durable/recovery/close checks, native stress and the unchanged MPP regression gate. The narrower implemented scope and remaining combined-plan criteria are recorded in releases/0.3.3/release.md. Full-product qualification, clean-machine testing and the known MPP failures remain open.
