using System.Globalization;
using System.Text;
using System.Text.Json;
using OfficeIMO.Project;
namespace ProjectDesk;

// Planora tools for Claude Code (see PlanoraMcpHost). Reads and navigation answer directly. A change is checked against
// the revision the client last read, shown to the user for approval, and applied as one Undo step.
public sealed partial class PlannerWindow {
 private readonly McpProjectRevision _mcpRevision=new();
 private PlanoraMcpHost? _mcpHost;private string? _mcpSocketPath;
 // Validation replaces the approval dialog: it receives the title, the reviewed description and the accept label.
 internal Func<string,string,string,Task<bool>>? McpApprovalOverride{get;set;}
 internal string ClaudeCodeStatus{get;private set;}="Claude Code control is off.";
 internal bool ClaudeCodeListening=>_mcpHost!=null;
 internal string McpRevision=>_mcpRevision.Current(_session);
 internal bool McpApprovalPending{get;private set;}

 internal void StartClaudeCodeControl(string? socketPath=null){
  if(_mcpHost!=null)return;
  if(!_preferences.ClaudeCodeControl){_mcpSocketPath=socketPath??_mcpSocketPath;ClaudeCodeStatus="Claude Code control is off.";return;}
  _mcpSocketPath=socketPath??_mcpSocketPath??PlanoraMcpHost.SocketPath();
  try{_mcpHost=PlanoraMcpHost.Start(_mcpSocketPath,HandleMcpTool);ClaudeCodeStatus=_mcpHost==null?"Another Planora window is already available to Claude Code.":"Ready for Claude Code. Every change it proposes asks for your approval here.";}
  catch(Exception e)when(e is IOException or UnauthorizedAccessException or System.Net.Sockets.SocketException){ClaudeCodeStatus="Claude Code control is unavailable: "+e.Message;}
 }
 internal async Task StopClaudeCodeControl(){var host=_mcpHost;_mcpHost=null;if(host!=null)await host.DisposeAsync();ClaudeCodeStatus="Claude Code control is off.";}
 // Used while the application exits, when no further UI turn runs; the socket file is removed.
 internal void ReleaseClaudeCodeControl(){var host=_mcpHost;_mcpHost=null;host?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));}
 internal bool ClaudeCodeControlEnabled=>_preferences.ClaudeCodeControl;
 internal async Task SetClaudeCodeControl(bool enabled){_preferences.ClaudeCodeControl=enabled;Persist();if(enabled)StartClaudeCodeControl();else await StopClaudeCodeControl();}
 private async Task ShowClaudeCode()=>await new ClaudeCodeWindow(this).ShowDialog(this);

 internal async Task<object> HandleMcpTool(string client,string tool,JsonElement a){
  if(a.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Arguments must be an object.");
  switch(tool){
   case "get_project":return ProjectSummary();
   case "list_tasks":{
    var query=Text(a,"query");int offset=Integer(a,"offset")??0,limit=Math.Clamp(Integer(a,"limit")??200,1,500);
    var tasks=_session.Tasks.Where(t=>string.IsNullOrWhiteSpace(query)||new[]{t.Name,t.Wbs,t.Notes}.Any(v=>v?.Contains(query,StringComparison.OrdinalIgnoreCase)==true)).ToList();
    var predecessors=Predecessors();
    return new{revision=McpRevision,total=tasks.Count,offset,tasks=tasks.Skip(Math.Max(0,offset)).Take(limit).Select(t=>TaskRow(t,predecessors)).ToList()};
   }
   case "get_task":{
    var t=FindTask(Required(Integer(a,"uid"),"uid"));var d=_session.Document!;
    var fields=PlanoraMcpTools.TaskFields.ToDictionary(f=>f,f=>_session.EditText(t.Uid,TaskKey(f)));
    return new{revision=McpRevision,uid=t.Uid,id=t.DisplayId,outlineLevel=Depth(t),parentUid=ParentUid(t),summary=t.IsSummary,fields,workHours=t.Work?.Minutes/60,cost=t.Cost,critical=t.IsCritical==true,
     successors=d.Dependencies.Where(l=>l.Predecessor?.Uid==t.Uid).Select(l=>new{uid=l.Successor.Uid,id=l.Successor.DisplayId,name=l.Successor.Name,type=l.Type.ToString(),lag=EditorInput.DurationText(l.Lag)}).ToList(),
     assignments=Assignments(t.Uid)};
   }
   case "list_resources":return new{revision=McpRevision,resources=_session.Resources.Select(r=>new{uid=r.Uid,id=r.DisplayId,fields=PlanoraMcpTools.ResourceFields.ToDictionary(f=>f,f=>_session.ResourceText(r.Uid,f))}).ToList()};
   case "list_assignments":return new{revision=McpRevision,assignments=Assignments(Integer(a,"taskUid"))};
   case "show_view":{
    int index=Array.IndexOf(PlanoraMcpTools.Views,Text(a,"view"));if(index<0)throw new ArgumentException("Choose one of: "+string.Join(", ",PlanoraMcpTools.Views)+".");
    RequireIdle();SelectPage(index switch{0=>1,1=>0,_=>index});Activate();return new{view=PlanoraMcpTools.Views[index]};
   }
   case "select_task":{
    var t=FindTask(Required(Integer(a,"uid"),"uid"));RequireIdle();if(_currentPage>1)SelectPage(1);SelectUid(t.Uid);
    bool shown=Sheet.SelectedUid==t.Uid;if(shown)Sheet.Grid.ScrollIntoView(Sheet.Grid.SelectedItem,Sheet.Grid.Columns.FirstOrDefault());Activate();
    return new{selected=shown,note=shown?null:"The task is hidden by a filter or a collapsed summary task in Planora."};
   }
   case "add_tasks":{
    var rows=Items(a,"tasks",500);int? before=Integer(a,"beforeUid");if(before is int b)FindTask(b);
    string[] columns=["Name","Duration","TaskMode","IsMilestone","Start","Finish","Predecessors","ResourceNames","PercentComplete","Notes"];
    var cells=rows.Select(r=>{bool milestone=Flag(r,"milestone")==true;var name=Text(r,"name");if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Every task needs a name.");
     return new[]{name.Trim(),Text(r,"duration")??(milestone?"0d":"1d"),Flag(r,"manual")==true?"Manually Scheduled":"Auto Scheduled",milestone?"Yes":"No",Text(r,"start")??"",Text(r,"finish")??"",Text(r,"predecessors")??"",Text(r,"resources")??"",(Integer(r,"percentComplete")??0).ToString(CultureInfo.InvariantCulture),Text(r,"notes")??""};}).ToList();
    string Quote(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
    var lines=cells.Select(c=>"• "+c[0]+(c[3]=="Yes"?" · milestone":" · "+c[1])+(c[2].StartsWith("Manual")?" · manual":"")+(c[4]+c[5]!=""?" · "+c[4]+" → "+c[5]:"")+(c[6]!=""?" · after "+c[6]:"")+(c[7]!=""?" · "+c[7]:"")+(c[8]!="0"?" · "+c[8]+"% complete":""));
    string where=before is int at?"before task "+FindTask(at).DisplayId+" “"+FindTask(at).Name+"”":"at the end of the project";
    return await Propose(client,a,$"Add {cells.Count} task{(cells.Count==1?"":"s")} {where}:\n"+string.Join("\n",lines),()=>{var uids=_session.PasteTasks(null,columns,"Name",string.Join("\n",cells.Select(c=>string.Join("\t",c.Select(Quote)))),before);return new{taskUids=uids};});
   }
   case "update_tasks":{
    var changes=Items(a,"changes",2000).Select(c=>{var t=FindTask(Required(Integer(c,"uid"),"uid"));string field=Text(c,"field")??"";if(!PlanoraMcpTools.TaskFields.Contains(field))throw new ArgumentException("Unknown task field "+field+".");string key=TaskKey(field),value=Text(c,"value")??"";
     if(key is not ("Predecessors" or "ResourceNames")){var parsed=TaskFieldEditing.Parse(TaskFieldEditing.PropertyKey(key),value);if(parsed is Enum e&&!Enum.IsDefined(e.GetType(),e))throw new ArgumentException("Choose a valid "+field+".");}
     return (Task:t,Field:field,Change:new TextCellChange(t.Uid,key,value));}).ToList();
    var lines=changes.Select(c=>$"• {c.Task.DisplayId} “{c.Task.Name}” · {c.Field}: {Shown(_session.EditText(c.Task.Uid,c.Change.Key))} → {Shown(c.Change.Text)}");
    return await Propose(client,a,$"Change {changes.Count} task value{(changes.Count==1?"":"s")}:\n"+string.Join("\n",lines),()=>{_session.EditCells(changes.Select(c=>c.Change),"Edit tasks");return new{};});
   }
   case "delete_tasks":{
    var tasks=Uids(a,"uids").Select(FindTask).ToList();int nested=tasks.Where(t=>t.IsSummary).Sum(t=>Descendants(t).Count(d=>!tasks.Contains(d)));
    return await Propose(client,a,$"Delete {tasks.Count} task{(tasks.Count==1?"":"s")}{(nested>0?$" and {nested} subtask{(nested==1?"":"s")}":"")}:\n"+string.Join("\n",tasks.Select(t=>$"• {t.DisplayId} “{t.Name}”")),()=>{_session.DeleteTasks(tasks.Select(t=>t.Uid));return new{};},"Delete");
   }
   case "outline_tasks":{
    var tasks=Uids(a,"uids").Select(FindTask).OrderBy(t=>t.DisplayId).ToList();bool indent=Text(a,"direction") switch{"indent"=>true,"outdent"=>false,_=>throw new ArgumentException("direction must be indent or outdent.")};
    // Outdenting from the bottom up keeps the selected tasks in their order below the former parent.
    return await Propose(client,a,(indent?"Indent":"Outdent")+$" {tasks.Count} task{(tasks.Count==1?"":"s")}:\n"+string.Join("\n",tasks.Select(t=>$"• {t.DisplayId} “{t.Name}”")),()=>{_session.Group(indent?"Indent tasks":"Outdent tasks",s=>{foreach(var t in indent?tasks:Enumerable.Reverse(tasks)){if(indent)s.Indent(t.Uid);else s.Outdent(t.Uid);}});return new{};});
   }
   case "add_resources":{
    var resources=Items(a,"resources",200).Select(r=>{var name=Text(r,"name");if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Every resource needs a name.");var type=Text(r,"type") is string kind?Enum.Parse<ProjectResourceType>(kind,true):ProjectResourceType.Work;if(!Enum.IsDefined(type))throw new ArgumentException("Resource type must be Work, Material or Cost.");return (Name:name.Trim(),Type:type,Capacity:Number(r,"capacityPercent")??100,Rate:Number(r,"standardRate")??0);}).ToList();
    return await Propose(client,a,$"Add {resources.Count} resource{(resources.Count==1?"":"s")}:\n"+string.Join("\n",resources.Select(r=>$"• {r.Name} · {r.Type} · {r.Capacity.ToString(CultureInfo.InvariantCulture)}% · rate {r.Rate.ToString(CultureInfo.InvariantCulture)}")),()=>{var uids=new List<int>();_session.Group("Add resources",s=>{foreach(var r in resources)uids.Add(s.AddResource(r.Name,r.Type,r.Capacity,r.Rate));});return new{resourceUids=uids};});
   }
   case "update_resources":{
    var changes=Items(a,"changes",1000).Select(c=>{var r=FindResource(Required(Integer(c,"uid"),"uid"));string field=Text(c,"field")??"";if(!PlanoraMcpTools.ResourceFields.Contains(field))throw new ArgumentException("Unknown resource field "+field+".");return (Resource:r,Field:field,Value:Text(c,"value")??"");}).ToList();
    return await Propose(client,a,$"Change {changes.Count} resource value{(changes.Count==1?"":"s")}:\n"+string.Join("\n",changes.Select(c=>$"• {c.Resource.Name} · {c.Field}: {Shown(_session.ResourceText(c.Resource.Uid,c.Field))} → {Shown(c.Value)}")),()=>{_session.Group("Edit resources",s=>{foreach(var c in changes)s.EditResourceText(c.Resource.Uid,c.Field,c.Value);});return new{};});
   }
   case "delete_resources":{
    var resources=Items(a,"uids",200,false).Select(v=>v.TryGetInt32(out int uid)?FindResource(uid):throw new ArgumentException("uids must be whole numbers.")).Distinct().ToList();
    return await Propose(client,a,$"Delete {resources.Count} resource{(resources.Count==1?"":"s")} and their assignments:\n"+string.Join("\n",resources.Select(r=>"• "+r.Name)),()=>{_session.Group("Delete resources",s=>{foreach(var r in resources)s.DeleteResource(r.Uid);});return new{};},"Delete");
   }
   case "assign_resources":{
    var items=Items(a,"assignments",1000).Select(i=>(Task:FindTask(Required(Integer(i,"taskUid"),"taskUid")),Resource:FindResource(Required(Integer(i,"resourceUid"),"resourceUid")),Units:Number(i,"unitsPercent")??100,Remove:Flag(i,"remove")==true)).ToList();
    return await Propose(client,a,$"Change {items.Count} assignment{(items.Count==1?"":"s")}:\n"+string.Join("\n",items.Select(i=>i.Remove?$"• Remove {i.Resource.Name} from {i.Task.DisplayId} “{i.Task.Name}”":$"• Assign {i.Resource.Name} at {i.Units.ToString(CultureInfo.InvariantCulture)}% to {i.Task.DisplayId} “{i.Task.Name}”")),()=>{_session.Group("Assign resources",s=>{foreach(var i in items){if(i.Remove)s.Unassign(i.Task.Uid,i.Resource.Uid);else s.Assign(i.Task.Uid,i.Resource.Uid,i.Units);}});return new{};});
   }
   case "update_project":{
    var d=RequireProject();string? name=Text(a,"name");DateTime? start=Text(a,"start") is string s?EditorInput.Date(s):null;bool status=a.TryGetProperty("statusDate",out _);DateTime? statusDate=status?EditorInput.Date(Text(a,"statusDate")):d.Settings.StatusDate;
    if(name!=null&&string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a project name.");if(name==null&&start==null&&!status)throw new ArgumentException("Provide name, start or statusDate.");
    var lines=new List<string>();if(name!=null)lines.Add($"• Name: {d.Name} → {name}");if(start!=null)lines.Add($"• Start: {EditorInput.DateText(d.Settings.StartDate)} → {EditorInput.DateText(start)}");if(status)lines.Add($"• Status date: {Shown(EditorInput.DateText(d.Settings.StatusDate))} → {Shown(EditorInput.DateText(statusDate))}");
    return await Propose(client,a,"Change project information:\n"+string.Join("\n",lines),()=>{_session.Group("Project information",g=>{if(name!=null)g.RenameProject(name.Trim());if(start!=null||status)g.SetProjectDates(start??d.Settings.StartDate??DateTime.Today,statusDate,null);});return new{};});
   }
   case "set_baseline":{
    var d=RequireProject();int number=Required(Integer(a,"number"),"number");bool overwrite=Flag(a,"overwrite")==true;if(number is <0 or >10)throw new ArgumentException("Baseline number must be 0 to 10.");
    bool exists=d.AllTasks.Any(t=>t.Baselines.Any(b=>(b.Number??0)==number));if(exists&&!overwrite)throw new InvalidOperationException($"Baseline {number} already exists. Ask the user before overwriting it, then pass overwrite=true.");
    return await Propose(client,a,$"{(exists?"Overwrite":"Save")} baseline {number} with the current schedule.",()=>{_session.CaptureBaseline(number,overwrite);return new{};});
   }
   case "calculate_project":RequireProject();return await Propose(client,a,"Calculate the project: reschedule automatic tasks and update work and cost totals.",()=>{_session.Recalculate();return new{};});
   case "undo":{
    bool formatting=_formats.CanUndo&&_formats.ModelPositionAtEdit==_session.HistoryPosition;if(!formatting&&!_session.CanUndo)throw new InvalidOperationException("There is nothing to undo.");
    return await Propose(client,a,"Undo: "+(formatting?"formatting change":_session.UndoLabel),()=>{UndoEdit();return new{};},"Undo");
   }
   case "redo":{
    bool formatting=_formats.CanRedo&&_formats.RedoModelPosition==_session.HistoryPosition;if(!formatting&&!_session.CanRedo)throw new InvalidOperationException("There is nothing to redo.");
    return await Propose(client,a,"Redo: "+(formatting?"formatting change":_session.RedoLabel),()=>{RedoEdit();return new{};},"Redo");
   }
   case "new_project":{
    var name=Text(a,"name");if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a project name.");var start=EditorInput.Date(Text(a,"start"))??DateTime.Today.AddHours(8);
    return await Propose(client,null,$"Start a new project “{name.Trim()}” beginning {EditorInput.DateText(start)}."+(HasUnsavedChanges?"\nThe current project has unsaved changes; Planora asks what to do with them first.":""),async()=>{if(!await ResolveUnsavedChanges())throw new InvalidOperationException("The user kept the current project. Nothing was changed.");_session.NewProject(name.Trim(),start);SelectPage(1);return new{};},"New Project");
   }
   case "open_project":{
    string path=AbsolutePath(a);if(!File.Exists(path))throw new ArgumentException("File not found: "+path);if(!Path.GetExtension(path).ToLowerInvariant().Equals(".mpp")&&!Path.GetExtension(path).Equals(".planora",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Open an .mpp or .planora file.");
    return await Propose(client,null,$"Open {path}"+(HasUnsavedChanges?"\nThe current project has unsaved changes; Planora asks what to do with them first.":"")+"\nThe file itself is not modified.",async()=>{if(!await ResolveUnsavedChanges())throw new InvalidOperationException("The user kept the current project. Nothing was opened.");await OpenPath(path);return new{};},"Open");
   }
   case "save_project_copy":{
    RequireProject();string path=AbsolutePath(a);string extension=Path.GetExtension(path).ToLowerInvariant();
    if(extension is not (".mpp" or ".planora"))throw new ArgumentException("Save as .mpp or .planora.");
    if(File.Exists(path)||Directory.Exists(path))throw new IOException("Planora never overwrites files. Choose a new file name: "+path);
    if(string.Equals(path,_session.SourcePath,StringComparison.OrdinalIgnoreCase))throw new IOException("Planora never overwrites the opened original.");
    if(!Directory.Exists(Path.GetDirectoryName(path)))throw new IOException("The folder does not exist: "+Path.GetDirectoryName(path));
    RequireIdle();
    _busy=true;
    try{
     bool saved=await SaveCopyTo(path,async notices=>{string text=$"Save a copy of the project as\n{path}"+(notices.Count>0?"\n\nThis edited copy needs a compatibility review. Existing views, stored work/cost distributions or other information may not fully follow the edits. Your original file will remain unchanged.\n"+string.Join("\n",notices):"")+"\n\nNo existing file is replaced.";return await Approve(client,"save a copy of "+ProjectName,text,"Save Copy");});
     if(!saved)throw Declined();return new{saved=path};
    }finally{_busy=false;}
   }
   default:throw new ArgumentException("Unknown Planora tool "+tool+".");
  }
 }

 // Checks the revision, asks the user, then applies. Stale proposals are refused before and after the review.
 private Task<object> Propose(string client,JsonElement? request,string summary,Func<object> apply,string accept="Apply Change")=>Propose(client,request,summary,()=>Task.FromResult(apply()),accept);
 private async Task<object> Propose(string client,JsonElement? request,string summary,Func<Task<object>> apply,string accept="Apply Change"){
  string? revision=request is JsonElement r?Required(Text(r,"revision"),"revision"):null;
  if(revision!=null)RequireProject();RequireIdle();
  if(revision!=null&&revision!=McpRevision)throw Stale();
  _busy=true;
  try{
   string text=summary+(revision!=null?"\n\nThis becomes one Undo step in Planora. Your project file changes only when you save a copy.":"");
   if(!await Approve(client,"change "+ProjectName,text,accept))throw Declined();
   if(revision!=null&&revision!=McpRevision)throw Stale();
   var result=await apply();Refresh();_status.Text=client+": "+summary.Split('\n')[0].TrimEnd(':');
   return new Dictionary<string,object?>{["applied"]=true,["revision"]=McpRevision,["result"]=result};
  }finally{_busy=false;}
 }
 private async Task<bool> Approve(string client,string action,string text,string accept){
  McpApprovalPending=true;
  try{
   if(McpApprovalOverride!=null)return await McpApprovalOverride(client+" wants to "+action,text,accept);
   if(WindowState==Avalonia.Controls.WindowState.Minimized)WindowState=Avalonia.Controls.WindowState.Normal;Activate();
   return await McpReviewWindow.Confirm(this,client+" wants to "+action,text,accept);
  }finally{McpApprovalPending=false;}
 }
 private void RequireIdle(){if(_busy)throw Busy();if(!_inspector.TryCommit()||!Sheet.Grid.CommitEdit()||!_resources.CommitEdit()||!_assignments.CommitEdit())throw new InvalidOperationException("A cell is being edited in Planora. Ask the user to finish or cancel it, then try again.");}
 private static InvalidOperationException Busy()=>new("Planora is busy with another action or waiting for the user's decision. Try again when it finishes.");
 private static InvalidOperationException Stale()=>new("The project changed since your last read (an edit, Undo or another project). Read it again and propose the change against the new revision.");
 private static InvalidOperationException Declined()=>new("The user declined this change in Planora. Nothing was changed.");
 private string ProjectName=>"“"+(_session.Document?.Name??"Untitled Project")+"”";

 private object ProjectSummary(){
  var d=_session.Document;
  return new{revision=McpRevision,open=d!=null,name=d?.Name,file=_session.SourcePath,start=EditorInput.DateText(d?.Settings.StartDate),finish=EditorInput.DateText(_session.Tasks.Max(t=>t.Finish)),statusDate=EditorInput.DateText(d?.Settings.StatusDate),
   calendar=d?.Calendar?.Name,tasks=_session.Tasks.Count,resources=_session.Resources.Count,unsavedChanges=HasUnsavedChanges,canUndo=_session.CanUndo,undo=_session.UndoLabel,canRedo=_session.CanRedo,redo=_session.RedoLabel,
   view=_viewTitle.Text,selectedTaskUids=SelectedTaskUids(),busy=_busy};
 }
 private ProjectDocument RequireProject()=>_session.Document??throw new InvalidOperationException("No project is open in Planora. Use new_project or open_project.");
 private ProjectTask FindTask(int uid)=>_session.Tasks.FirstOrDefault(t=>t.Uid==uid)??throw new ArgumentException($"Task UID {uid} was not found. Task UIDs come from list_tasks.");
 private ProjectResource FindResource(int uid)=>_session.Resources.FirstOrDefault(r=>r.Uid==uid)??throw new ArgumentException($"Resource UID {uid} was not found. Resource UIDs come from list_resources.");
 private TaskColumn? Predecessors()=>_session.Document is {} d?TaskColumns.Catalogue(d).FirstOrDefault(c=>c.Key=="Predecessors"):null;
 private object TaskRow(ProjectTask t,TaskColumn? predecessors)=>new{uid=t.Uid,id=t.DisplayId,name=t.Name,outlineLevel=Depth(t),parentUid=ParentUid(t),summary=t.IsSummary,milestone=t.IsMilestone==true,mode=t.IsManual==true?"Manual":"Auto",
  duration=EditorInput.DurationText(t.Duration),start=EditorInput.DateText(t.Start),finish=EditorInput.DateText(t.Finish),percentComplete=t.PercentComplete??0,predecessors=predecessors?.Read(t,_session.Document!)??"",resources=_session.EditText(t.Uid,"ResourceNames"),critical=t.IsCritical==true,wbs=t.Wbs};
 private List<object> Assignments(int? taskUid)=>(_session.Document?.Assignments.Where(x=>x.Task?.Uid>0&&x.Resource?.Uid>0&&(taskUid==null||x.Task.Uid==taskUid)).Select(x=>(object)new{uid=x.Uid,taskUid=x.Task!.Uid,task=x.Task.Name,resourceUid=x.Resource!.Uid,resource=x.Resource.Name,unitsPercent=(x.Units?.Value??1)*100,workHours=(x.Work?.Minutes??0)/60}).ToList())??[];
 private static int Depth(ProjectTask t){int depth=1;for(var p=t.Parent;p!=null&&p.Uid!=0;p=p.Parent)depth++;return depth;}
 private static int? ParentUid(ProjectTask t)=>t.Parent is {Uid:>0} p?p.Uid:null;
 private static IEnumerable<ProjectTask> Descendants(ProjectTask t){foreach(var child in t.Children){yield return child;foreach(var inner in Descendants(child))yield return inner;}}
 private static string TaskKey(string field)=>field switch{"Milestone"=>"IsMilestone","Active"=>"IsActive",_=>field};
 private static string Shown(string? value)=>string.IsNullOrEmpty(value)?"(empty)":value.Length>200?value[..200]+"…":value;
 private static string AbsolutePath(JsonElement a){var path=Text(a,"path");if(string.IsNullOrWhiteSpace(path)||!Path.IsPathFullyQualified(path))throw new ArgumentException("Give an absolute file path.");return Path.GetFullPath(path);}

 private static string? Text(JsonElement a,string name)=>a.TryGetProperty(name,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ValueKind==JsonValueKind.String?v.GetString():throw new ArgumentException(name+" must be text."):null;
 private static int? Integer(JsonElement a,string name)=>a.TryGetProperty(name,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ValueKind==JsonValueKind.Number&&v.TryGetInt32(out int i)?i:throw new ArgumentException(name+" must be a whole number."):null;
 private static decimal? Number(JsonElement a,string name)=>a.TryGetProperty(name,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ValueKind==JsonValueKind.Number&&v.TryGetDecimal(out decimal n)&&n>=0?n:throw new ArgumentException(name+" must be a nonnegative number."):null;
 private static bool? Flag(JsonElement a,string name)=>a.TryGetProperty(name,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ValueKind is JsonValueKind.True or JsonValueKind.False?v.GetBoolean():throw new ArgumentException(name+" must be true or false."):null;
 private static T Required<T>(T? value,string name)where T:struct=>value??throw new ArgumentException(name+" is required.");
 private static string Required(string? value,string name)=>value??throw new ArgumentException(name+" is required.");
 private static List<JsonElement> Items(JsonElement a,string name,int max,bool records=true){if(!a.TryGetProperty(name,out var v)||v.ValueKind!=JsonValueKind.Array)throw new ArgumentException(name+" must be a list.");int count=v.GetArrayLength();if(count<1||count>max)throw new ArgumentException($"{name} must contain 1 to {max} items.");var items=v.EnumerateArray().ToList();if(records&&items.Any(i=>i.ValueKind!=JsonValueKind.Object))throw new ArgumentException("Each item in "+name+" must be an object.");return items;}
 private static List<int> Uids(JsonElement a,string name)=>Items(a,name,500,false).Select(v=>v.ValueKind==JsonValueKind.Number&&v.TryGetInt32(out int uid)?uid:throw new ArgumentException(name+" must be whole numbers.")).Distinct().ToList();
}
