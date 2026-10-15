using System.Text.Json;
using OfficeIMO.Project;
namespace ProjectDesk;

// Persist values and an explicit operation allowlist, never executable delegates or type names.
public sealed record DurableCommand(string Operation, JsonElement[] Arguments) {
 public static readonly JsonSerializerOptions Json=CreateJson();
 private static JsonSerializerOptions CreateJson(){var options=new JsonSerializerOptions{IncludeFields=true,MaxDepth=64};options.Converters.Add(new DurationJsonConverter());options.Converters.Add(new WorkingTimeJsonConverter());return options;}
 public static DurableCommand For(string operation,params object?[] arguments)=>new(operation,arguments.Select(a=>JsonSerializer.SerializeToElement(a,a?.GetType()??typeof(object),Json)).ToArray());
 public static DurableCommand Batch(IEnumerable<DurableCommand?> commands){var values=commands.ToArray();if(values.Any(c=>c==null))throw new InvalidOperationException("This operation has no durable representation.");return For("Batch",(object)values);}
 public static DurableCommand Dates(IEnumerable<int> roots,IReadOnlySet<(int Uid,string Key)>? dates)=>For("AutomaticDates",roots.ToArray(),dates?.Select(d=>new DateCell(d.Uid,d.Key)).ToArray());
 public T Read<T>(int index)=>Arguments[index].Deserialize<T>(Json)!;
 public void Apply(ProjectSession s,int depth=0){
  if(depth>16||Arguments==null||Arguments.Length>10)throw new InvalidDataException("Invalid command nesting or arguments.");
  switch(Operation){
   case "Batch":s.ApplyDurableBatch(Read<DurableCommand[]>(0),depth+1);break;
   case "Rename":s.Rename(Read<int>(0),Read<string>(1));break;
   case "RenameProject":s.RenameProject(Read<string>(0));break;
   case "EditFieldValue":s.RestoreField(Read<int>(0),Read<string>(1),Arguments[2]);break;
   case "SetPredecessors":s.EditText(Read<int>(0),"Predecessors",Read<string>(1));break;
   case "SetResourceNames":s.EditText(Read<int>(0),"ResourceNames",Read<string>(1));break;
   case "AddTask":s.AddTask(Read<string>(0),Read<bool>(1),Read<int?>(2),Read<int?>(3),Read<bool>(4),Read<Guid?>(5));break;
   case "DeleteTasks":s.DeleteTasks(Read<int[]>(0));break;
   case "Indent":s.Indent(Read<int>(0));break;
   case "Outdent":s.Outdent(Read<int>(0));break;
   case "RenumberWbs":s.RenumberWbs();break;
   case "EditTask":s.EditTask(Read<int>(0),Read<TaskEdit>(1));break;
   case "SetProgress":s.SetProgress(Read<int[]>(0),Read<int>(1));break;
   case "LinkTasks":s.LinkTasks(Read<int>(0),Read<int>(1),Read<ProjectDependencyType>(2),Read<ProjectDuration>(3));break;
   case "UnlinkTasks":s.UnlinkTasks(Read<int[]>(0));break;
   case "AddResource":s.AddResource(Read<string>(0),Read<ProjectResourceType>(1),Read<decimal>(2),Read<decimal>(3),Read<Guid?>(4));break;
   case "EditResource":s.EditResource(Read<int>(0),Read<ResourceEdit>(1));break;
   case "EditResourceText":s.EditResourceText(Read<int>(0),Read<string>(1),Read<string>(2));break;
   case "EditAssignmentText":s.EditAssignmentText(Read<int>(0),Read<string>(1),Read<string>(2));break;
   case "DeleteResource":s.DeleteResource(Read<int>(0));break;
   case "Assign":s.Assign(Read<int>(0),Read<int>(1),Read<decimal>(2),Read<decimal?>(3));break;
   case "Unassign":s.Unassign(Read<int>(0),Read<int>(1));break;
   case "Recalculate":s.Recalculate(Read<bool>(0),Read<bool>(1));break;
   case "CaptureBaseline":s.CaptureBaseline(Read<int>(0),Read<bool>(1));break;
   case "ClearBaseline":s.ClearBaseline(Read<int>(0));break;
   case "SetWorkingWeek":s.SetWorkingWeek(Read<int>(0),Read<Dictionary<DayOfWeek,ProjectWorkingTime[]>>(1));break;
   case "SetCalendarException":s.SetCalendarException(Read<CalendarExceptionEdit>(0));break;
   case "SetProjectDates":s.SetProjectDates(Read<DateTime>(0),Read<DateTime?>(1),Read<int?>(2));break;
   case "AutomaticDates":s.RestoreAutomaticDates(Read<int[]>(0),Read<DateCell[]?>(1));break;
   case "RemoveIncoming":s.RemoveIncomingLinks(Read<int[]>(0));break;
   default:throw new InvalidDataException("Unsupported project command: "+Operation);
  }
 }
 internal void Validate(ref int budget,int depth=0){
  if(--budget<0||depth>16||string.IsNullOrWhiteSpace(Operation)||Arguments==null||Arguments.Length>10)throw new InvalidDataException("Invalid command history or size limit exceeded.");
  if(Operation=="Batch"){if(Arguments.Length!=1||Arguments[0].ValueKind!=JsonValueKind.Array)throw new InvalidDataException("Invalid batch.");foreach(var child in Read<DurableCommand[]>(0)){if(child==null)throw new InvalidDataException("Missing command.");child.Validate(ref budget,depth+1);}}
 }
}
public sealed record DateCell(int Uid,string Key);
public sealed record DurableHistoryEntry(string Name,DurableCommand Command,Guid Id=default);
public sealed record DurableProjectState(int SchemaVersion,Guid ProjectId,byte[] BaseBytes,string? SourcePath,DurableHistoryEntry[] History,int Position,int SavedPosition,string? LastSavedPath,string? SavedFingerprint,ProjectPresentation? Presentation=null);

public sealed partial class ProjectSession {
 public Guid ProjectIdentity {get;private set;}=Guid.NewGuid();
 public string BaseFingerprint=>_baseBytes==null?"empty":Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(_baseBytes));
 public event Action? StateChanged;
 private string? _savedFingerprint;
 private string HistoryFingerprint()=>string.Join("|",_commands.Take(_position).Select(c=>c.Id));
 public DurableProjectState CaptureState(){if(_baseBytes==null||Document==null)throw new InvalidOperationException("No project open.");if(_commands.Any(c=>c.Command==null))throw new InvalidOperationException("An unsupported edit cannot be saved to durable history.");return new(1,ProjectIdentity,_baseBytes.ToArray(),SourcePath,_commands.Select(c=>new DurableHistoryEntry(c.Name,c.Command!,c.Id)).ToArray(),_position,_savedPosition,LastSavedPath,_savedFingerprint);}
 public void RestoreState(DurableProjectState state){
  if(state.SchemaVersion!=1||state.ProjectId==Guid.Empty||state.BaseBytes==null||state.BaseBytes.Length is <1 or >67108864||state.History==null||state.History.Length>10000||state.Position<0||state.Position>state.History.Length||state.SavedPosition<0||state.SavedPosition>10000)throw new InvalidDataException("Invalid or unsupported project state.");
  state.Presentation?.Checked();
  int budget=100000;foreach(var entry in state.History){if(entry?.Command==null)throw new InvalidDataException("Missing history entry.");entry.Command.Validate(ref budget);}
  // Fully reconstruct in isolation, including redo, before replacing the live session.
  using var staging=new ProjectSession();staging.ReplaceBase(state.BaseBytes,ProjectDocument.Load(new MemoryStream(state.BaseBytes)),state.SourcePath);
  foreach(var entry in state.History){int first=staging._commands.Count;entry.Command.Apply(staging);var generated=staging._commands.Skip(first).ToArray();staging._commands.RemoveRange(first,generated.Length);staging._commands.Add(new(entry.Name,d=>{foreach(var part in generated)part.Apply(d);},entry.Command,entry.Id==Guid.Empty?Guid.NewGuid():entry.Id));staging._position=staging._commands.Count;}
  if(staging._position!=state.Position){var current=staging.Replay(state.Position);staging.Document!.Dispose();staging.Document=current;staging._position=state.Position;}
  Document?.Dispose();Document=staging.Document;staging.Document=null;_baseBytes=state.BaseBytes.ToArray();SourcePath=state.SourcePath;LastSavedPath=state.LastSavedPath;_commands.Clear();_commands.AddRange(staging._commands);_position=state.Position;_savedPosition=state.SavedPosition;ProjectIdentity=state.ProjectId;_savedFingerprint=state.SavedFingerprint=="saved"?HistoryFingerprint():state.SavedFingerprint;LastCalculation=null;StateChanged?.Invoke();
 }
 public void RenameProject(string name){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a project name.");Execute("Project name",d=>d.Name=name,DurableCommand.For("RenameProject",name));}
 internal void AcceptSavedState(DurableProjectState state){if(ProjectIdentity!=state.ProjectId)return;LastSavedPath=state.LastSavedPath;_savedPosition=state.Position;_savedFingerprint=state.SavedFingerprint;StateChanged?.Invoke();}
 internal void ApplyDurableBatch(DurableCommand[] commands,int depth){bool previous=_deferAutoDates;_deferAutoDates=true;try{foreach(var command in commands)command.Apply(this,depth);}finally{_deferAutoDates=previous;}}
 internal void RestoreField(int uid,string key,JsonElement value){var property=TaskFieldEditing.Property(key)??throw new InvalidDataException("Unknown editable field.");EditField(uid,new(key,value.ValueKind==JsonValueKind.Null?null:value.Deserialize(property.PropertyType,DurableCommand.Json)));}
 internal void RestoreAutomaticDates(int[] roots,DateCell[]? cells){var dates=cells?.Select(c=>(c.Uid,c.Key)).ToHashSet();Execute("Update automatic dates",d=>ComputeAutoDates(d,roots,dates),DurableCommand.Dates(roots,dates));}
 internal void RemoveIncomingLinks(int[] roots){var ids=roots.ToHashSet();Execute("Replace pasted predecessors",d=>{foreach(var link in d.Dependencies.Where(l=>ids.Contains(l.Successor.Uid)).ToArray())d.Dependencies.Remove(link);},DurableCommand.For("RemoveIncoming",(object)roots));}
 private sealed record SessionCommand(string Name,Action<ProjectDocument> Apply,DurableCommand? Command,Guid Id);
}
