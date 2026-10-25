# Planora release history

The usual cadence is two user-facing features per release. The user requested a combined release numbered 0.3.3; it delivers four workspace improvements. No 0.3.2 binary was published. Broader confident-editing criteria remain pending and are disclosed in the release notes. Keep versioned release folders; never overwrite an older release.

| Version | Previous | Feature 1 | Feature 2 | Changes |
|---|---|---|---|---|
| 0.3.10 | 0.3.9 | One-click timeline visibility | User-requested focused release | [release.md](0.3.10/release.md) |
| 0.3.9 | 0.3.8 | Project Entry column preset | Colored icons and dropdown styling | [release.md](0.3.9/release.md) |
| 0.3.8 | 0.3.7 | Modern Planora identity | Apple/Office ribbon and rounded Gantt | [release.md](0.3.8/release.md) |
| 0.3.7 | 0.3.6 | Modern timeline toolbar | Fit panel for more Gantt space | [release.md](0.3.7/release.md) |
| 0.3.6 | 0.3.5 | Grouped ribbon redesign | Planning chart/timeline controls | [release.md](0.3.6/release.md) |
| 0.3.5 | 0.3.4 | Pinned inspector dates/actions | Adjacent typed-field validation | [release.md](0.3.5/release.md) |
| 0.3.4 | 0.3.3 | Resource Sheet refresh bugfix | Indexed task value refresh | [release.md](0.3.4/release.md) |
| 0.3.3 | 0.3.1 | Apple-inspired workspace, density and focus | Resizable grouped inspector | [release.md](0.3.3/release.md) |
| 0.3.1 | 0.3.0 | Saved workspace views | Original starter-template library | [release.md](0.3.1/release.md) |
| 0.3.0 | 0.2.1 | Compact workspace navigation | Editable task inspector | [release.md](0.3.0/release.md) |

Source release notes live here. Built Windows releases live under project-app/releases/VERSION-win-x64 and each includes release.md, Planora.exe, dependencies, licenses, validation output and SHA256SUMS.json.

Build with ./scripts/release.ps1 -Version VERSION -IncludeNative on Windows. The script validates the version and two declared features (four for 0.3.3), or an explicit bugfix definition with fixes and no new features, builds in staging, tests the packaged binaries and creates the release directory only after successful checks. Omitting IncludeNative produces an explicitly unqualified native preview. No automatic GitHub publication is performed.

On a Mac, ./scripts/release.ps1 -Version VERSION -Runtime osx-arm64 -IncludeNative runs the same checks, packages project-app/releases/VERSION-osx-arm64 with an ad-hoc signed Planora.app, and writes Planora-VERSION-osx-arm64.zip (made with ditto so the bundle keeps its symlinks and executable bits) and its .sha256 file next to it. The app is not Developer ID signed or notarized.

The broader roadmap remains open. A template category is not equivalent to complete workflow support.

Delivered scope and remaining work: [UI-first sequence](../docs/UI-RELEASE-PLAN.md). Future versions are not included in the delivered table above.
