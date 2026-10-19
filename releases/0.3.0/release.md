# Planora 0.3.0 — workspace and inspector

Changes from 0.2.1. Windows preview; 2026-10-16.

## Two new features

1. Compact workspace: direct Table/Gantt/Board/Calendar/Resources/Reports navigation, additional views menu and persistent ribbon expansion. Menus and command search remain available.
2. Editable task inspector: name, duration, scheduling mode, predecessors and completion; calculated start/finish display; Apply/Cancel; atomic validation and Undo. Pending edits remain bound to their task when selection changes. Commands and close validate pending inspector edits.

## How to use

Run Planora.exe. Choose a view in the navigation bar. Choose Inspector, select a task, edit its fields and Apply. Use Show ribbon for the existing expanded commands. Hide inspector to recover chart space. Theme selection remains under Appearance.

## Compatibility and limitations

No MPP serialization or scheduling algorithms were replaced. Synthetic MPP round-trip and existing core regression checks are required by packaging. This does not establish universal Microsoft Project fidelity. macOS and mobile remain unqualified. The broader redesign, template catalog and roadmap are still in progress.

## Release policy

Each version contains exactly two new user-facing features. Fixes and validation accompany the pair. A failed check leaves a staging build, not a completed release. Local release folders contain the executable, dependencies, notices, validation results, hashes and this release.md. GitHub publication is a separate action.
