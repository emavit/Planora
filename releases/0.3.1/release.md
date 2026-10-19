# Planora 0.3.1 — saved views and starter templates

Changes from 0.3.0. Windows preview; 2026-10-16. Includes the compact workspace and task inspector from 0.3.0.

## Two new features

1. Saved workspace views: save and restore columns/order/widths, task search/filter, active view, timeline/inspector visibility, ribbon mode and Gantt date range/zoom. Stored on this computer; up to 50 named views. Duplicate names are rejected and views can be deleted.
2. Searchable template library with task preview, project name and start date. Ten original starters cover software, marketing, design, sales, operations, service readiness, HR onboarding, document review, IT rollout and period close. Each creates five linked tasks including a milestone and calculated dates. Creation is one Undo/Redo step; current unsaved work uses the existing save prompt.

## How to use

- Choose Saved views… in the workspace bar (or View → Saved Views…). Name the current configuration and Save current; select a saved entry and Apply to restore it.
- Choose File → New from Template…, search or select a starter, review the schedule and choose Create project.
- Keep the whole release folder together and run Planora.exe.

## Validation and limits

The 14-file public MPP corpus has 78 passing stages and 10 existing failures, reproduced with the prior 0.2.1 binary. The regression gate rejects new failures; it does not treat known failures as successful compatibility. Assignment comparison and editing/baseline export on certain imported files remain limited. See validation/public-mpp/results.json and docs/compatibility-known-failures.json in the source repository.

The packaging process runs the previous regression checks, the workspace/inspector checks, and 73 template/saved-view checks. Template validation includes dates, dependencies, milestones, atomic Undo/Redo and MPP readback for all ten starters. Local settings are not a portable project template format. These starters do not implement Jira workflows, sprints, forms or service-desk behavior. Full MPP fidelity, macOS and mobile remain unqualified.
