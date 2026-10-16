# Release preparation

This repository contains the Planora source and the separately licensed public example collection. It includes only reviewed, separately licensed public fixtures under examples/public-mpp and excludes private user MPP files, credentials, local settings/recovery, generated screenshots/logs, published binaries and package caches. The source is published at https://github.com/emavit/Planora.

The source snapshot corresponds to Planora 0.2.1. Prior local checks passed: durable 22, recovery GUI 20, scheduling 22, close 19, PDF 28, AI fixtures 53, MCP fixtures 55, core authoring/save/reopen and spreadsheet smoke. These counts describe the earlier working-tree candidate, not a GitHub CI run. Fresh bundle verification is recorded in PACKAGE-CHECKS.json.

Remaining qualification: native Windows stress on this candidate, large-project behavior, interruption/disk-full cases, broader MPP coverage, macOS and real provider sign-in/inference. GitHub CI results must be checked for the specific commit; the earlier no-CI statement described initial bundle preparation, not current publication.

Before making the repository public: choose the owner/repository name; enable private vulnerability reporting; review vendor provenance and license notices; inspect the upload contents. No repository-specific badges, URLs, contact emails or maintainer identities have been invented.

Upload the contents of this folder at the repository root. GitHub web upload supports fewer files per batch than this source tree; using Git or GitHub Desktop is preferable. Keep generated `bin`, `obj` and `artifacts` out of commits.
