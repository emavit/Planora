# Third-party notices

The root MIT license covers Planora's own code, not a relicensing of third-party components.

- Vendored OfficeIMO.Project and OfficeIMO.Core version metadata: 3.4.5, from the local development fork of https://github.com/EvotecIT/OfficeIMO. The exact upstream commit was not retained; do not represent this subset as an unmodified upstream release.
- [OfficeIMO MIT license](project-app/vendor/OfficeIMO/LICENSE).
- OfficeIMO.Core declares `MIT AND Apache-2.0 AND BSD-3-Clause`; retain its [third-party notices](project-app/vendor/OfficeIMO/OfficeIMO.Core/THIRD-PARTY-NOTICES.md), [license directory](project-app/vendor/OfficeIMO/OfficeIMO.Core/Licenses/) and embedded-resource notices.
- NuGet dependencies are restored from package metadata: Avalonia 11.3.13, Avalonia.Themes.Fluent 11.3.13, Avalonia.Controls.DataGrid 11.3.13, Avalonia.Headless 11.3.13, Tmds.DBus.Protocol 0.21.3 and ModelContextProtocol.Core 2.2.0. Their licenses and transitive/native runtime notices remain applicable. Source packaging does not embed their compiled binaries.

Before distributing compiled builds, inventory the resolved packages and include applicable runtime notices. This repository does not include proprietary Microsoft/Apple binaries, commercial project documents or reference screenshots. UI command labels describe interoperability/workflows; they do not establish affiliation or full compatibility.

[Resolved dependency/version/license metadata inventory](docs/DEPENDENCIES.md).
