# Recovery and portable project files

Planora 0.2.1 adds local recovery and `.planora` project copies. Microsoft Project interchange remains `.mpp`.

## Save and open

Use **File → Save a Copy** and select **Planora (includes formatting and timelines)**, or enter a new `.planora` filename. The package includes the original project bytes, replayable edits, Undo/Redo, text/bar formatting, rich notes, timeline lanes and independent chart date/zoom settings. Open it with **File → Open**. Original files and existing destinations are protected: choose a new filename for each copy.

The package carries the original source path as provenance but does not need that file to reopen. It does not include API keys, connector secrets, or other projects' preferences. Table column configurations and application-wide appearance preferences remain local in this release.

## Recovery

The normal desktop app checks for changes every 15 seconds and writes an isolated checkpoint in the background. The status area reports checkpoint success or failure. Changes made immediately before an interruption can be newer than the last checkpoint. Continue saving project copies normally.

Use **File → Create Recovery Checkpoint** for an immediate snapshot. **File → Recovery** lists retained sessions; select one to inspect its provenance and restore it. **Use previous generation** can recover the preceding checkpoint if the latest is corrupt. Cancel leaves the current project intact. Recovering over an unsaved project uses Save / Don't Save / Cancel first.

Checkpoints are local, under the existing preferences directory's `recovery` subfolder (normally `%LOCALAPPDATA%/ProjectDesk/recovery`). The legacy settings directory is intentionally retained. Each running window/project uses a separate slot. Checkpoints are retained until deleted in the recovery dialog; **Don't Save** removes the current session's checkpoint. Recovery is not a cloud backup or collaborative sync service.

## Qualification

Model and GUI checks cover automatic/manual checkpointing, previous generations, malformed packages, copy protection, deferred automatic dates, history cursor/branch behavior, formatting, timelines, missing original files and close decisions. Large-project latency, disk-full/power-loss injection, and macOS behavior still need qualification. Packages use bounded input and integrity hashes; those detect corruption and are not a digital signature.
