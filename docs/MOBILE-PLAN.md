# Android and iOS development plan

Status: confirmed future product targets; implementation not started. Decision recorded 15 October 2026.

## Delivery sequence

**Windows → completed and validated macOS app → Android and iOS apps.**

Mobile development starts after the macOS app is finished and validated against its agreed release criteria. Existing desktop work and macOS qualification remain the priority. Android and iOS order, exact release dates, framework and minimum OS versions have not been selected.

## Entry gate

- Windows release criteria and actual Mac M1 qualification are met.
- Agreed macOS planning, editing, recovery, file compatibility, PDF and platform-integration criteria are complete, with remaining limitations documented.
- Shared-core and file-format contracts are stable enough to evaluate reuse without weakening desktop behavior.

## First mobile milestone: decisions before implementation

Evaluate shared C# core reuse and mobile UI/framework options; record the choice in an ADR. Define a feature matrix for Android phones/tablets and iPhone/iPad: project browsing/editing, Gantt/timeline interaction, resources/progress, notes, import/export, PDF and optional connectors. Decide which desktop functions are feasible in the first mobile release and which require a different workflow. Do not assume desktop binaries, CLI-based AI sign-in or desktop file paths work on mobile.

## Mobile design and integration requirements

- Touch-first layouts with readable tables, responsive Gantt/timeline navigation and accessible controls.
- Lifecycle-safe local persistence: suspension, termination, recovery, migrations and low-storage handling.
- Sandboxed document access and sharing; compatibility tests before claiming independent mobile MPP import/export.
- Secure platform credential storage and authentication; review provider/MCP support within mobile sandbox limits.
- Explicit offline behavior, and conflict-safe optional sync if included. Do not introduce a mandatory hosted backend by assumption.
- Platform appearance, accessibility, screen sizes, localization readiness and battery/memory/performance qualification.

## Validation and distribution

Test Android and iOS on real devices as well as emulators/simulators. Qualify editing, scheduling consistency, touch gestures, offline data/recovery, interruption, security, import/export and sharing per platform. Decide minimum OS/device support and internal/public distribution requirements when preparing the mobile phase. Store accounts, signing and any associated costs require a separate distribution decision; no account purchases or store publication are authorized by this roadmap note.

Track MOB-01 through MOB-03 in [TASKS](TASKS.md), and update the [roadmap](ROADMAP.md), [platform guide](PLATFORM_GUIDE.md), [architecture](ARCHITECTURE.md) and [improvement backlog](IMPROVEMENTS.md) as decisions and evidence become available.
