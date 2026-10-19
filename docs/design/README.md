# Next workspace concept

Open planora-next.html in a browser; it is self-contained and uses synthetic data. The PNG is the earlier 1440×900 concept reference, before the Apple-inspired refinement. The HTML is the current design.

Interactive: light/dark, compact/comfortable rows, Gantt/Board, search, task selection, timeline/inspector visibility and focus mode. Editing, project navigation and scheduling are illustrative, not desktop feature implementations. The rail is deliberately non-interactive.

Reviewed 2026-10-16 using the in-app browser: theme/density/view toggles, category-free task search, selection updating the inspector and focus-mode restoration. The narrow browser viewport uses the responsive layout. REA supplied the desktop reference screenshot; the capture artifacts were retained but its owned-browser cleanup could not be confirmed after a timeout. This is not a complete browser/device/accessibility qualification.

Implementation acceptance criteria are in ../UI-RELEASE-PLAN.md. No new desktop release was built for this planning update.

Apple-inspired refinement (2026-10-16): neutral sidebar and toolbar materials, compact segmented views, restrained blue accent, grouped property surfaces and chart-preserving focus mode. Desktop shared tokens and navigation received the same treatment; the rest of the combined 0.3.2 scope remains in progress. Build passed with zero warnings/errors and all 26 workspace/inspector checks passed, including direct insertion, Undo, retained invalid drafts and MPP readback. This is not a new packaged release.
