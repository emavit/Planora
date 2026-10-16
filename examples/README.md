# Public Microsoft Project examples

## Included files

[public-mpp/](public-mpp/) contains **all 18 files from Planora's previously used public compatibility corpus**: 14 Microsoft Project MPP files and 4 corresponding producer XML references. They are unmodified MPXJ public test fixtures, pinned by [manifest](public-mpp/manifest.json), with [upstream notices](public-mpp/NOTICE.md) and [license](public-mpp/LICENSE-MPXJ.txt).

| Example group | Files | Useful for |
|---|---|---|
| General / legacy | sample1.mpp, mpp9task.mpp, mpp12task.mpp, mpp14task.mpp | Different native generations |
| Calendars | mpp14calendar.mpp | Working-time/calendar import |
| Baselines | mpp14baselines.mpp, task-baselines-project2019-mpp14.mpp | Baseline records and dates |
| Resources / assignments | mpp14resource.mpp, mpp14assignmentfields.mpp, assignment-assignments-project2019-mpp14.mpp | Resource/assignment records |
| Links / durations | mpp14relations.mpp, task-links-project2019-mpp14.mpp, task-durations-project2019-mpp14.mpp | Dependencies, lag and duration variants |
| Subprojects | mpp12subproject.mpp | External/subproject reference preservation |
| Producer references | Four *-mspdi.xml files | Bounded independent-format comparison |

No private project, generated edit/output copy, spreadsheet, user setting or recovery package is included. Inclusion does not establish support for every field in every fixture.

## Open and verify

Open an MPP through Planora **File → Open**, then save edits to a **new** filename. Keep checked-in fixtures unchanged. XML files are comparison references; the current open-file UI filters for MPP/Planora.

From repository root:

    ./scripts/verify-public-examples.ps1

Optional broader compatibility investigation after building:

    dotnet project-app/ProjectDesk/bin/Release/net10.0/Planora.dll --public-mpp-test examples/public-mpp artifacts/public-mpp-new

Use a fresh output directory. That optional investigation deliberately records unresolved cases and may return nonzero; the corpus is not a universal MPP-fidelity certificate. Do not execute embedded content or resolve external project links.

## Microsoft File → New gallery

The [official public planning-template page](https://www.microsoft.com/en-us/microsoft-365/planner/project-planning-templates) describes simple planning, agile planning, product development/launch and goals/objectives. The [official Project template guide](https://support.microsoft.com/en-us/project/save-a-project-file-as-a-template) describes reusable Project templates. [microsoft-gallery.json](microsoft-gallery.json) records these references and coverage.

**The complete File → New gallery has not been downloaded or bundled.** No full public downloadable set with redistribution permission was verified. Public accessibility alone is not recorded as a redistribution license. Microsoft gallery links are references; the MPP files here are MPXJ fixtures, not copies of Microsoft's entire gallery. Account-only/installed/private template caches are not included.
