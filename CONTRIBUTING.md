# Contributing

Use .NET 10 and build from the root. Keep changes small and preserve existing project data. Run `./scripts/validate.ps1`; Windows UI changes also require `-IncludeUi` and a relevant visual/manual check. CI is configured for Windows/macOS/Linux; a green model build does not qualify a desktop platform release.

Start with an issue describing the user-visible problem and acceptance criteria. Link the task or describe the gap in your PR. Add regression coverage for meaningful behavioral changes. Include the exact commands/results and remaining limitations; do not fabricate test evidence.

Do not commit user projects, screenshots containing private work, credentials, binaries, generated logs or runtime caches. Use synthetic test data. Keep vendored code and license notices intact; document deliberate local changes and provenance. Do not replace the local file library with a new dependency without discussing compatibility.

Update docs and CHANGELOG for user-facing changes. Use consistent C# formatting in touched sections; broad formatting-only changes should be separate. There is no separate Python lint/typecheck pipeline: C# compiler diagnostics and functional checks are the current gates.

PRs require maintainer review before merge. Contribution does not grant authorization to publish packages, send external messages or modify users' files.
