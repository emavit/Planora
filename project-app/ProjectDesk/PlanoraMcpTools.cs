using System.Text.Json;
using ModelContextProtocol.Protocol;
namespace ProjectDesk;

// The tools Planora offers to Claude Code. Reading and navigation need no approval; every other tool proposes a change
// that the person using Planora reviews in the app before it is applied as one Undo step.
internal static class PlanoraMcpTools {
 public const string Instructions="Planora is the desktop project planner (Microsoft Project .mpp compatible) open on this computer. Read with get_project, list_tasks, get_task, list_resources and list_assignments. Every change tool takes the revision returned by your latest read; when the project changed meanwhile the change is refused, so read again. Each change is shown to the user in Planora for approval and becomes one Undo step there. Dates use yyyy-MM-dd or yyyy-MM-dd HH:mm; durations look like 2d, 4h, 1w or 3ed; predecessors use task IDs (not UIDs) such as 3, 5FS+2d or 8SS-4h. Planora never overwrites a project file: save_project_copy writes a new file.";
 public static readonly string[] Views=["Gantt Chart","Task Sheet","Resource Sheet","Resource Usage","Task Information","File Information","Task Usage","Network Diagram","Resource Graph","Reports","Task Drivers","Calendar","Progress Board"];
 public static readonly string[] TaskFields=["Name","Duration","Start","Finish","TaskMode","Milestone","PercentComplete","Predecessors","ResourceNames","Notes","ConstraintType","ConstraintDate","Deadline","Priority","FixedCost","Wbs","Contact","Active","Type","EffortDriven"];
 public static readonly string[] ResourceFields=["Name","Type","Capacity","Rate","OvertimeRate","CostPerUse","Group","Initials"];
 public static Implementation Server=>new(){Name=AppBrand.Name,Version=typeof(PlanoraMcpTools).Assembly.GetName().Version?.ToString(3)??"1.0"};
 public static CallToolResult Result(object data,bool error)=>new(){Content=[new TextContentBlock{Text=JsonSerializer.Serialize(data)}],StructuredContent=JsonSerializer.SerializeToElement(data),IsError=error};

 private static readonly object Revision=new{type="string",description="The revision returned by your latest read of this project."};
 private static readonly object Uids=new{type="array",items=new{type="integer"},minItems=1,maxItems=500,description="Task UIDs (stable identifiers from list_tasks), not displayed IDs."};
 private static Tool Make(string name,string description,object properties,string[] required,bool read=false,bool destructive=false)=>new(){Name=name,Description=description,InputSchema=JsonSerializer.SerializeToElement(new{type="object",properties,required,additionalProperties=false}),Annotations=new(){ReadOnlyHint=read,DestructiveHint=read?null:destructive,OpenWorldHint=false}};
 public static IList<Tool> All=>[
  Make("get_project","Summary of the project open in Planora: name, file, dates, counts, unsaved edits, Undo/Redo availability, current view, selected tasks and the revision for change tools.",new{},[],read:true),
  Make("list_tasks","Tasks in project order with UID, ID, outline level, dates, duration, progress, predecessors and resources. Page with offset/limit; query filters by name, WBS or notes.",new{offset=new{type="integer",minimum=0},limit=new{type="integer",minimum=1,maximum=500},query=new{type="string"}},[],read:true),
  Make("get_task","All editable fields of one task, its predecessors, successors and assignments.",new{uid=new{type="integer"}},["uid"],read:true),
  Make("list_resources","Resources with UID, ID, type, capacity, rates, group and initials.",new{},[],read:true),
  Make("list_assignments","Resource assignments with units and work; optionally for one task.",new{taskUid=new{type="integer"}},[],read:true),
  Make("show_view","Switch Planora to a view. Changes only what is displayed.",new{view=new{type="string",@enum=Views}},["view"],read:true),
  Make("select_task","Select and reveal a task in Planora. Changes only what is displayed.",new{uid=new{type="integer"}},["uid"],read:true),
  Make("add_tasks","Propose new tasks, appended or inserted before a task. Predecessors may refer to the new tasks by the IDs they will have.",new{revision=Revision,beforeUid=new{type="integer",description="Insert before this task UID; omit to append."},tasks=new{type="array",minItems=1,maxItems=500,items=new{type="object",properties=new{name=new{type="string"},duration=new{type="string"},start=new{type="string"},finish=new{type="string"},manual=new{type="boolean",description="Manually scheduled; default automatic."},milestone=new{type="boolean"},predecessors=new{type="string"},resources=new{type="string",description="Existing resource names, optionally with units: Engineer[50%]; Designer"},percentComplete=new{type="integer",minimum=0,maximum=100},notes=new{type="string"}},required=new[]{"name"},additionalProperties=false}}},["revision","tasks"]),
  Make("update_tasks","Propose field changes on existing tasks. TaskMode accepts Manual or Auto; Milestone, Active and EffortDriven accept Yes or No; an empty value clears optional fields.",new{revision=Revision,changes=new{type="array",minItems=1,maxItems=2000,items=new{type="object",properties=new{uid=new{type="integer"},field=new{type="string",@enum=TaskFields},value=new{type="string"}},required=new[]{"uid","field","value"},additionalProperties=false}}},["revision","changes"]),
  Make("delete_tasks","Propose deleting tasks (summary tasks delete their subtasks).",new{revision=Revision,uids=Uids},["revision","uids"],destructive:true),
  Make("outline_tasks","Propose indenting (make subtask of the task above) or outdenting tasks.",new{revision=Revision,uids=Uids,direction=new{type="string",@enum=new[]{"indent","outdent"}}},["revision","uids","direction"]),
  Make("add_resources","Propose new resources.",new{revision=Revision,resources=new{type="array",minItems=1,maxItems=200,items=new{type="object",properties=new{name=new{type="string"},type=new{type="string",@enum=new[]{"Work","Material","Cost"}},capacityPercent=new{type="number",minimum=0},standardRate=new{type="number",minimum=0}},required=new[]{"name"},additionalProperties=false}}},["revision","resources"]),
  Make("update_resources","Propose resource field changes. Capacity is a percentage; rates are per hour.",new{revision=Revision,changes=new{type="array",minItems=1,maxItems=1000,items=new{type="object",properties=new{uid=new{type="integer"},field=new{type="string",@enum=ResourceFields},value=new{type="string"}},required=new[]{"uid","field","value"},additionalProperties=false}}},["revision","changes"]),
  Make("delete_resources","Propose deleting resources and their assignments.",new{revision=Revision,uids=new{type="array",items=new{type="integer"},minItems=1,maxItems=200}},["revision","uids"],destructive:true),
  Make("assign_resources","Propose assigning resources to tasks (units in percent, default 100) or removing assignments.",new{revision=Revision,assignments=new{type="array",minItems=1,maxItems=1000,items=new{type="object",properties=new{taskUid=new{type="integer"},resourceUid=new{type="integer"},unitsPercent=new{type="number",minimum=0},remove=new{type="boolean"}},required=new[]{"taskUid","resourceUid"},additionalProperties=false}}},["revision","assignments"]),
  Make("update_project","Propose a new project name, start date or status date.",new{revision=Revision,name=new{type="string"},start=new{type="string"},statusDate=new{type="string",description="Empty clears the status date."}},["revision"]),
  Make("set_baseline","Propose saving the current schedule as a baseline (0-10).",new{revision=Revision,number=new{type="integer",minimum=0,maximum=10},overwrite=new{type="boolean"}},["revision","number"]),
  Make("calculate_project","Propose recalculating schedule, work and cost totals.",new{revision=Revision},["revision"]),
  Make("undo","Propose undoing the latest change in Planora.",new{revision=Revision},["revision"]),
  Make("redo","Propose redoing the latest undone change in Planora.",new{revision=Revision},["revision"]),
  Make("new_project","Propose closing the current project and starting a new one. Unsaved changes are offered for saving first.",new{name=new{type="string"},start=new{type="string"}},["name"]),
  Make("open_project","Propose opening an .mpp or .planora file (absolute path). Unsaved changes are offered for saving first. The file itself is not modified.",new{path=new{type="string"}},["path"]),
  Make("save_project_copy","Propose saving the project as a new .mpp or .planora file (absolute path). Existing files, including the opened original, are never overwritten.",new{path=new{type="string"}},["path"]),
 ];
}
