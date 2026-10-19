# Planora 0.3.7 — calmer timeline workspace

Previous version: 0.3.6.

## Two improvements

1. Modernized timeline toolbar: clear Timelines heading, quiet New timeline and Options controls, less button chrome within each lane, descriptive zoom tooltips and full-date tooltips. Rename/remove moved into Options; removing the last lane is disabled. Ribbon materials now use more neutral light/dark tones.
2. Fit panel resizes timeline space to its lane count (200–600 logical pixels), returning unused space to Gantt after removing lanes. This changes the panel size only, preserves independent date ranges/zoom and saves the chosen height. Additional lanes scroll within the bounded panel.

## Use

Click Fit panel beside the timeline selector to reclaim chart space. Options contains Rename active timeline and Remove active timeline. Tasks, Dates, Format and zoom remain directly accessible on each lane. To hide timelines entirely, use the existing View timeline control.

## Validation and scope

Workspace checks include panel-height persistence and unchanged Gantt dates. The timeline suite uses a generated project to exercise dragging, cancellation, keyboard movement, independent date limits/zoom and layout persistence. Exact package results are appended below.

The public MPP baseline still has 10 known failures. This is a Windows preview, not Mac qualification or full Microsoft Project compatibility. PDF preview and formatting-studio features previously proposed for this version remain in the backlog; existing PDF export is preserved.
