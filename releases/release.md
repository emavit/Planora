# Planora release history

The usual cadence is two user-facing features per release. The user requested a combined release numbered 0.3.3; it delivers four workspace improvements. No 0.3.2 binary was published. Broader confident-editing criteria remain pending and are disclosed in the release notes. Keep versioned release folders; never overwrite an older release.

| Version | Previous | Feature 1 | Feature 2 | Changes |
|---|---|---|---|---|
| 0.3.12 | 0.3.11 | Claude Code control with approval in Planora | Windows and macOS released together by the Release workflow | [release.md](0.3.12/release.md) |
| 0.3.11 | 0.3.10 | Planora.app for macOS (menu bar, Finder, Keychain, system font) | All documented public MPP failures fixed | [release.md](0.3.11/release.md) |
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

Source release notes live here. Every release ships Windows and macOS together.

## How to release

Releases are published only by the [Release workflow](../.github/workflows/release.yml), which builds and checks both platforms on GitHub's Windows and macOS machines. It does not matter who prepares the release or on which computer.

1. On `main`, set the version in `project-app/ProjectDesk/ProjectDesk.csproj` and add `releases/VERSION/release.json` and `releases/VERSION/release.md` (first line: `# Planora VERSION — title`). Add `"prerelease": false` to release.json only for a final, non-preview release.
2. Optional rehearsal: Actions → Release → Run workflow with the version and publish unticked. Both packages are built and checked and kept as workflow artifacts; nothing is published.
3. Push the tag: `git tag vVERSION && git push origin vVERSION` (or run the workflow with publish ticked).

The workflow runs `release.ps1 -IncludeNative` on each platform and publishes one GitHub release with `Planora-VERSION-win-x64.zip` and `Planora-VERSION-osx-arm64.zip` (each with a `.sha256`), using release.md as the notes. If either platform fails, or the commit is not on `main`, or the release already exists, nothing is published. Do not attach locally built packages to a GitHub release or publish one platform alone.

Each package is the release folder VERSION-RUNTIME as one ZIP and includes release.md, the app (Planora.exe and its dependencies, or Planora.app), licenses, validation output and SHA256SUMS.json.

`./scripts/release.ps1 -Version VERSION -IncludeNative` (Windows) builds the same package locally, for example to investigate a failed workflow run. The script validates the version and two declared features (four for 0.3.3), or an explicit bugfix definition with fixes and no new features, builds in staging, tests the packaged binaries and creates the release directory only after successful checks. Omitting IncludeNative produces an explicitly unqualified native preview. The script itself never publishes.

On a Mac, `./scripts/release.ps1 -Version VERSION -Runtime osx-arm64 -IncludeNative` runs the same checks and packages project-app/releases/VERSION-osx-arm64 with an ad-hoc signed Planora.app; its ZIP is made with ditto so the bundle keeps its symlinks and executable bits. Both runtimes write Planora-VERSION-RUNTIME.zip and its .sha256 file next to the release folder. The app is not Developer ID signed or notarized.

The broader roadmap remains open. A template category is not equivalent to complete workflow support.

Delivered scope and remaining work: [UI-first sequence](../docs/UI-RELEASE-PLAN.md). Future versions are not included in the delivered table above.
