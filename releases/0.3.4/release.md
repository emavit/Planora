# Planora 0.3.4 — Resource Sheet refresh fix

Previous version: 0.3.3. This focused bugfix takes priority over the chart-interaction work previously proposed for 0.3.4.

## Fixed

Resource Sheet edits previously refreshed every resource, every assignment and every field of every task. Task lookups also rebuilt the complete task list for each row. This work ran on the UI thread after committing a cell and could delay interaction in long projects.

Resource edits now refresh the edited resource row. Name changes additionally update the affected assignment labels and task Resource Names cells. Unrelated task rows are untouched. General task value refresh builds its UID lookup once.

## Validation scope

Regression first failed on the previous implementation because every name edit refreshed unrelated tasks. The fix passes repeated Resource Sheet name editing on a synthetic 1,002-task project, assigned-name propagation, invalid-name correction, Undo/Redo and view switching. A separate initialized authoring fixture verifies resource-name MPP save/reopen. The bulk performance fixture is not an MPP compatibility fixture.

The user's exact freeze was not reproduced in the small fixture; this fixes an identified performance defect in the reported editing path, not proof that every possible freeze is resolved. The user is being asked whether their freeze occurs during typing, on Enter/Tab, or on dialog confirmation.

No new product features are claimed. Existing MPP limitations remain: 10 known failures in the public regression corpus. macOS remains unqualified.

Run Planora.exe from this entire release folder. Keep its dependencies beside it. Original project files and older release folders are not modified.
