# Planora 0.3.5 — clearer task inspector

Previous version: 0.3.4. Two user-facing improvements:

1. Start/finish dates remain at the top of the inspector. Apply/Cancel and the edit status remain at the bottom while task fields scroll independently. You can commit or discard changes without scrolling to the end of the form.
2. Invalid typed values (such as duration and completion percentage) show an adjacent error message. Valid fields are not partially committed. Errors stay visible for the rejected value and clear when it changes. Shared scheduling/dependency validation still reports its error in the pinned status area.

Gantt, timelines, density, focus mode and Resource Sheet performance fixes are retained. No scheduling or MPP model changes are introduced by these UI improvements.

## Validation

Workspace tests cover atomic edits, automatic dates, Undo, invalid-draft retention, adjacent duration errors, pinned actions at an 800-pixel window height and stationary actions while fields scroll. Exact-package results are appended below and stored in validation/.

## Remaining work

This release does not deliver editable Board/Calendar workflows formerly proposed under this version number. Those remain in the roadmap alongside rectangular selection feedback, inspector Resources/Notes sections and Gantt gesture previews. The public MPP corpus still has 10 known failures. The Resource Sheet performance defect is fixed, but the user's exact earlier freeze has not been independently reproduced. macOS is not qualified by this Windows package.
